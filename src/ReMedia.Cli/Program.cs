using ReMedia.Cli.Support;
using ReMedia.Core.Diagnostics;
using ReMedia.Core.Interfaces;
using ReMedia.Core.Models;
using ReMedia.Core.Services;
using ReMedia.Tooling.Configuration;
using ReMedia.Tooling.Diagnostics;
using ReMedia.Tooling.Ffmpeg;
using ReMedia.Tooling.Ffprobe;

if (args.Length == 0)
{
    CliPrinter.PrintUsage();
    return 1;
}

ExternalToolPaths toolPaths = ToolPathResolver.ResolveFromEnvironment();
IProcessRunner processRunner = new ProcessRunner();
IMediaProbeService probeService = new FfprobeMediaProbeService(toolPaths, processRunner);
ITrackExportService exportService = new FfmpegTrackExportService(toolPaths, processRunner);
IChapterExportService chapterExportService = new FfmpegChapterExportService(toolPaths, processRunner);
ITimingAnalysisService timingAnalysisService = new TimingAnalysisService();
ILoudnessService loudnessService = new FfmpegLoudnessService(toolPaths, processRunner);
IMuxService muxService = new FfmpegMuxService(toolPaths, processRunner);
IToolLogger logger = new ConsoleToolLogger();
ExportWorkflowService exportWorkflow = new(exportService, chapterExportService, logger, muxService);

try
{
    string command = args[0].ToLowerInvariant();

    switch (command)
    {
        case "help":
        case "--help":
        case "-h":
            CliPrinter.PrintUsage();
            return 0;

        case "list-codecs":
            CliPrinter.PrintCodecCatalog();
            return 0;

        case "probe":
            return await HandleProbeAsync(args, probeService);

        case "detect":
            return HandleDetect(args);

        case "analyze":
            return await HandleAnalyzeAsync(args, probeService, timingAnalysisService);

        case "export":
            return await HandleExportAsync(args, probeService, timingAnalysisService, exportWorkflow);

        case "loudness":
            return await HandleLoudnessAsync(args, probeService, loudnessService);

        case "cleanup":
            return HandleSubtitleCleanup(args);

        default:
            Console.Error.WriteLine($"Unknown command: {command}");
            CliPrinter.PrintUsage();
            return 1;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 1;
}

static async Task<int> HandleProbeAsync(string[] args, IMediaProbeService probeService)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Missing input file.");
        return 1;
    }

    MediaProbeResult result = await probeService.ProbeAsync(args[1]);
    CliPrinter.PrintProbe(result);
    return 0;
}

// Reads an optional decimal flag, but errors instead of silently defaulting when the
// flag is present with a missing or unparseable value (per "never hide warnings").
static bool TryReadOptionalDecimal(string[] args, string name, out decimal? value)
{
    value = CliArguments.ReadDecimal(args, name);
    if (value is null && CliArguments.HasFlag(args, name))
    {
        Console.Error.WriteLine($"Invalid or missing value for {name}.");
        return false;
    }

    return true;
}

static async Task<int> HandleAnalyzeAsync(string[] args, IMediaProbeService probeService, ITimingAnalysisService timingAnalysisService)
{
    if (!TryReadOptionalDecimal(args, "--source-fps", out decimal? sourceFps) ||
        !TryReadOptionalDecimal(args, "--target-fps", out decimal? targetFpsOverride))
    {
        return 1;
    }

    decimal targetFps = targetFpsOverride ?? 24000m / 1001m;
    TimeSpan? duration = CliArguments.ReadTimeSpan(args, "--duration");

    if (args.Length >= 2 && !args[1].StartsWith("--"))
    {
        string inputPath = args[1];
        Console.WriteLine($"Probing {inputPath}...");
        MediaProbeResult probeResult = await probeService.ProbeAsync(inputPath);
        CliPrinter.PrintProbe(probeResult);
        Console.WriteLine();

        if (!sourceFps.HasValue)
        {
            MediaStreamInfo? videoStream = probeResult.GetPrimaryVideoStream();
            if (videoStream is not null && videoStream.AverageFramesPerSecond > 0)
            {
                sourceFps = videoStream.AverageFramesPerSecond;
            }
        }

        duration ??= probeResult.Duration;
    }

    sourceFps ??= 25m;
    duration ??= TimeSpan.Zero;

    TimingAnalysisResult result = timingAnalysisService.Analyze(new TimingAnalysisRequest(duration.Value, sourceFps.Value, targetFps));
    CliPrinter.PrintTiming(result);
    return 0;
}

static async Task<int> HandleExportAsync(
    string[] args,
    IMediaProbeService probeService,
    ITimingAnalysisService timingAnalysisService,
    ExportWorkflowService exportWorkflow)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Missing input file.");
        return 1;
    }

    string inputPath = args[1];
    string outputFolder = CliArguments.ReadString(args, "--output-folder") ?? Environment.CurrentDirectory;
    bool exportAll = args.Any(a => string.Equals(a, "--all", StringComparison.OrdinalIgnoreCase));
    bool exportChapters = args.Any(a => string.Equals(a, "--chapters", StringComparison.OrdinalIgnoreCase));
    bool muxToMkv = args.Any(a => string.Equals(a, "--mux", StringComparison.OrdinalIgnoreCase));
    string? destinationMaster = CliArguments.ReadString(args, "--destination");
    List<string> additionalParts = CliArguments.ReadManyStrings(args, "--part");
    if (!CliArguments.TryReadManyIntegers(args, "--stream", out List<int> streamIndexes))
    {
        Console.Error.WriteLine("Invalid or missing value for --stream (expected an integer index).");
        return 1;
    }

    string? codecOverride = CliArguments.ReadString(args, "--codec");
    if (!TryReadOptionalDecimal(args, "--source-fps", out decimal? sourceFpsOverride) ||
        !TryReadOptionalDecimal(args, "--target-fps", out decimal? targetFps))
    {
        return 1;
    }

    if (!exportAll && streamIndexes.Count == 0 && !exportChapters)
    {
        Console.Error.WriteLine("Provide at least one --stream <index>, --all, or --chapters.");
        return 1;
    }

    Console.WriteLine($"Probing {inputPath}...");
    MediaProbeResult probeResult;

    if (additionalParts.Count > 0)
    {
        List<string> allPaths = [inputPath, .. additionalParts];
        Console.WriteLine($"Multi-part source: {allPaths.Count} file(s)");
        MultiPartProbeResult multiResult = await MultiPartProbeHelper.ProbeAllAsync(probeService, allPaths);
        probeResult = multiResult.Combined;

        for (int i = 0; i < multiResult.PartDurations.Count; i++)
        {
            string name = Path.GetFileName(allPaths[i]);
            Console.WriteLine($"  Part {i + 1}: {name} ({multiResult.PartDurations[i]})");
        }

        Console.WriteLine($"  Combined: {probeResult.Duration}, {probeResult.Chapters.Count} chapter(s)");
    }
    else
    {
        probeResult = await probeService.ProbeAsync(inputPath);
    }

    CliPrinter.PrintProbe(probeResult);
    Console.WriteLine();

    IEnumerable<MediaStreamInfo> streamsToExport;
    if (exportAll)
    {
        streamsToExport = probeResult.Streams.Where(s => s.AssetType is MediaAssetType.Audio or MediaAssetType.Subtitle);
    }
    else
    {
        List<MediaStreamInfo> requested = probeResult.Streams.Where(s => streamIndexes.Contains(s.Index)).ToList();

        List<int> unknownIndexes = streamIndexes.Where(i => requested.All(s => s.Index != i)).ToList();
        foreach (int idx in unknownIndexes)
        {
            Console.Error.WriteLine($"Warning: stream index {idx} not found in file.");
        }

        List<MediaStreamInfo> videoStreams = requested.Where(s => s.AssetType == MediaAssetType.Video).ToList();
        foreach (MediaStreamInfo vs in videoStreams)
        {
            Console.Error.WriteLine($"Warning: stream {vs.Index} is a video track and will be skipped (video export is not supported in Phase 0).");
        }

        streamsToExport = requested;
    }

    decimal? sourceFps = sourceFpsOverride;
    if (!sourceFps.HasValue)
    {
        MediaStreamInfo? videoStream = probeResult.GetPrimaryVideoStream();
        if (videoStream is not null && videoStream.AverageFramesPerSecond > 0)
        {
            sourceFps = videoStream.AverageFramesPerSecond;
        }
    }

    bool hasTimingConversion = targetFps.HasValue && sourceFps.HasValue && sourceFps != targetFps;
    if (hasTimingConversion)
    {
        TimingAnalysisResult timing = timingAnalysisService.Analyze(
            new TimingAnalysisRequest(probeResult.Duration ?? TimeSpan.Zero, sourceFps!.Value, targetFps!.Value));
        CliPrinter.PrintTiming(timing);
        Console.WriteLine();
    }

    List<ExportTrackSelection> selections = streamsToExport
        .Select(s =>
        {
            string outputCodec = codecOverride ?? "copy";
            bool copyStream = string.Equals(outputCodec, "copy", StringComparison.OrdinalIgnoreCase);

            if (hasTimingConversion && s.AssetType == MediaAssetType.Audio && copyStream)
            {
                Console.Error.WriteLine($"Warning: stream {s.Index} uses codec copy but timing conversion requires re-encoding. Falling back to flac.");
                outputCodec = "flac";
                copyStream = false;
            }

            string container = copyStream
                ? ContainerDefaults.GetDefaultContainer(s.AssetType, s.CodecName)
                : ContainerDefaults.GetDefaultContainer(s.AssetType, outputCodec);

            return new ExportTrackSelection(
                s.Index,
                s.AssetType,
                s.CodecName,
                outputCodec,
                container,
                copyStream);
        })
        .ToList();

    ExportWorkflowRequest request = new(
        inputPath,
        outputFolder,
        selections,
        exportChapters || exportAll,
        probeResult.Chapters,
        hasTimingConversion ? sourceFps : null,
        hasTimingConversion ? targetFps : null,
        MuxToMkv: muxToMkv,
        DestinationMasterPath: destinationMaster,
        AdditionalInputPaths: additionalParts.Count > 0 ? additionalParts : null);

    ExportWorkflowResult result = await exportWorkflow.ExecuteAsync(request);
    CliPrinter.PrintWorkflowResult(result);

    Console.WriteLine();
    Console.WriteLine(result.HasFailures ? "Export completed with errors." : "Export completed successfully.");
    return result.HasFailures ? 1 : 0;
}

static int HandleDetect(string[] args)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Missing input file.");
        return 1;
    }

    string inputPath = args[1];
    ContainerFormatInfo format = ContainerFormatDetector.Detect(inputPath);

    Console.WriteLine($"File:      {inputPath}");
    Console.WriteLine($"Format:    {format.ShortName}");
    Console.WriteLine($"Container: {format.Description}");
    return 0;
}

static async Task<int> HandleLoudnessAsync(
    string[] args,
    IMediaProbeService probeService,
    ILoudnessService loudnessService)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Missing input file.");
        return 1;
    }

    string inputPath = args[1];
    List<int> requestedStreams = CliArguments.ReadManyIntegers(args, "--stream");
    if (!TryReadOptionalDecimal(args, "--gain", out decimal? gainDb) ||
        !TryReadOptionalDecimal(args, "--target-lufs", out decimal? targetLufs) ||
        !TryReadOptionalDecimal(args, "--ceiling", out decimal? ceilingDbtp))
    {
        return 1;
    }

    decimal ceiling = ceilingDbtp ?? -1.0m;

    Console.WriteLine($"Probing {inputPath}...");
    MediaProbeResult probeResult = await probeService.ProbeAsync(inputPath);

    List<MediaStreamInfo> audioStreams = probeResult.Streams
        .Where(s => s.AssetType == MediaAssetType.Audio)
        .ToList();

    if (audioStreams.Count == 0)
    {
        Console.Error.WriteLine("No audio streams found.");
        return 1;
    }

    IEnumerable<MediaStreamInfo> targets = requestedStreams.Count > 0
        ? audioStreams.Where(s => requestedStreams.Contains(s.Index))
        : audioStreams;

    foreach (MediaStreamInfo stream in targets)
    {
        Console.WriteLine($"Analyzing loudness for stream {stream.Index} ({stream.CodecName}, {stream.Language ?? "und"})...");
        LoudnessAnalysisResult loudness = await loudnessService.AnalyzeAsync(inputPath, stream.Index);
        CliPrinter.PrintLoudness(stream.Index, loudness);

        if (gainDb.HasValue)
        {
            ClippingPredictionResult clipping = loudnessService.PredictClipping(loudness, gainDb.Value);
            CliPrinter.PrintClippingPrediction(clipping);
        }

        if (targetLufs.HasValue)
        {
            LoudnessMatchResult match = loudnessService.MatchToTarget(loudness, targetLufs.Value, ceiling);
            CliPrinter.PrintLoudnessMatch(match);
        }

        Console.WriteLine();
    }

    return 0;
}

static int HandleSubtitleCleanup(string[] args)
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Missing subtitle file.");
        return 1;
    }

    string inputPath = args[1];
    string? outputPath = CliArguments.ReadString(args, "--output");
    bool stripSdh = args.Any(a => string.Equals(a, "--strip-sdh", StringComparison.OrdinalIgnoreCase));
    bool stripHtml = args.Any(a => string.Equals(a, "--strip-html", StringComparison.OrdinalIgnoreCase));
    bool fixOverlaps = args.Any(a => string.Equals(a, "--fix-overlaps", StringComparison.OrdinalIgnoreCase));
    bool all = args.Any(a => string.Equals(a, "--all", StringComparison.OrdinalIgnoreCase));

    if (!all && !stripSdh && !stripHtml && !fixOverlaps)
    {
        all = true;
    }

    string ext = Path.GetExtension(inputPath).ToLowerInvariant();
    IReadOnlyList<string> parseWarnings;
    IReadOnlyList<SubtitleCue> cues = ext switch
    {
        ".vtt" => VttParser.ParseFile(inputPath, out parseWarnings),
        ".srt" => SrtParser.ParseFile(inputPath, out parseWarnings),
        _ => throw new NotSupportedException($"Unsupported subtitle format: {ext}")
    };

    foreach (string warning in parseWarnings)
    {
        Console.Error.WriteLine($"Warning: {warning}");
    }

    Console.WriteLine($"Loaded {cues.Count} cue(s) from {inputPath}");

    if (all)
    {
        cues = SubtitleCleanupService.CleanAll(cues);
    }
    else
    {
        if (stripSdh) cues = SubtitleCleanupService.StripSdh(cues);
        if (stripHtml) cues = SubtitleCleanupService.StripHtmlTags(cues);
        if (fixOverlaps) cues = SubtitleCleanupService.FixOverlaps(cues);
        cues = SubtitleCleanupService.RemoveEmpty(cues);
    }

    Console.WriteLine($"After cleanup: {cues.Count} cue(s)");

    outputPath ??= Path.Combine(
        Path.GetDirectoryName(inputPath) ?? ".",
        Path.GetFileNameWithoutExtension(inputPath) + "_clean" + ext);

    string output = ext == ".vtt" ? VttWriter.Write(cues) : SrtWriter.Write(cues);
    File.WriteAllText(outputPath, output, System.Text.Encoding.UTF8);

    Console.WriteLine($"Written to {outputPath}");
    return 0;
}
