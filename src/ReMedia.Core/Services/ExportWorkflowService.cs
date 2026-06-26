namespace ReMedia.Core.Services;

using ReMedia.Core.Diagnostics;
using ReMedia.Core.Interfaces;
using ReMedia.Core.Models;

/// <summary>
/// Orchestrates a full export workflow — building options from user selections,
/// exporting tracks, and optionally exporting chapters.
/// Shared between CLI and desktop hosts.
/// </summary>
public sealed class ExportWorkflowService
{
    private readonly ITrackExportService _trackExportService;
    private readonly IChapterExportService _chapterExportService;
    private readonly IMuxService? _muxService;
    private readonly IToolLogger? _logger;

    public ExportWorkflowService(
        ITrackExportService trackExportService,
        IChapterExportService chapterExportService,
        IToolLogger? logger = null,
        IMuxService? muxService = null)
    {
        _trackExportService = trackExportService;
        _chapterExportService = chapterExportService;
        _muxService = muxService;
        _logger = logger;
    }

    public async Task<ExportWorkflowResult> ExecuteAsync(
        ExportWorkflowRequest request,
        CancellationToken cancellationToken = default,
        IProgress<WorkflowProgress>? progress = null)
    {
        Directory.CreateDirectory(request.OutputFolder);

        bool applyTiming = request.SourceFps.HasValue && request.TargetFps.HasValue
                           && request.SourceFps != request.TargetFps;

        // For multi-part sources, create a concat demuxer file
        string effectiveInputPath = request.InputPath;
        string? concatFilePath = null;
        if (request.IsMultiPart)
        {
            concatFilePath = Path.Combine(request.OutputFolder, "concat.txt");
            await ConcatFileWriter.WriteToFileAsync(request.AllInputPaths, concatFilePath, cancellationToken);
            effectiveInputPath = concatFilePath;
            _logger?.LogMessage($"Multi-part source: {request.AllInputPaths.Count} files combined via concat demuxer.");
        }

        int totalSteps = CountSteps(request, applyTiming);
        int currentStep = 0;

        void ReportProgress(string stepName)
        {
            currentStep++;
            progress?.Report(new WorkflowProgress(stepName, currentStep, totalSteps));
        }

        IReadOnlyList<ToolOperationResult> trackResults = [];
        ToolOperationResult? chapterResult = null;

        if (request.Tracks.Count > 0)
        {
            List<TrackExportOptions> options = request.Tracks
                .Where(t => t.AssetType is not MediaAssetType.Video)
                .Select(t =>
                {
                    bool timingForTrack = applyTiming && !t.CopyStream && t.AssetType == MediaAssetType.Audio;
                    bool hasGain = t.AppliedGainDb.HasValue && t.AppliedGainDb.Value != 0m && t.AssetType == MediaAssetType.Audio;
                    bool hasSyncOffset = t.AudioSyncOffsetMs.HasValue && t.AudioSyncOffsetMs.Value != 0m && t.AssetType == MediaAssetType.Audio;

                    bool needsReEncode = timingForTrack || hasGain || hasSyncOffset;
                    bool copyStream = t.CopyStream && !needsReEncode;

                    return new TrackExportOptions(
                        StreamIndex: t.StreamIndex,
                        AssetType: t.AssetType,
                        OutputContainer: t.OutputContainer,
                        OutputCodec: copyStream ? "copy" : t.OutputCodec,
                        OutputPath: Path.Combine(request.OutputFolder, $"stream-{t.StreamIndex}{t.OutputContainer}"),
                        CopyStream: copyStream,
                        ApplyTimingConversion: timingForTrack,
                        SourceFps: timingForTrack ? request.SourceFps : null,
                        TargetFps: timingForTrack ? request.TargetFps : null,
                        AppliedGainDb: hasGain ? t.AppliedGainDb : null,
                        AudioSyncOffsetMs: hasSyncOffset ? t.AudioSyncOffsetMs : null);
                })
                .ToList();

            int skipped = request.Tracks.Count - options.Count;
            if (skipped > 0)
            {
                _logger?.LogMessage($"Skipped {skipped} video track(s) (not exported in Phase 0).");
            }

            if (applyTiming)
            {
                int retimed = options.Count(o => o.ApplyTimingConversion);
                if (retimed > 0)
                {
                    _logger?.LogMessage($"Applying timing conversion ({request.SourceFps} -> {request.TargetFps} fps) to {retimed} audio track(s).");
                }
            }

            int syncAdjusted = options.Count(o => o.AudioSyncOffsetMs.HasValue);
            if (syncAdjusted > 0)
            {
                _logger?.LogMessage($"Applying audio sync offset to {syncAdjusted} track(s).");
            }

            if (options.Count > 0)
            {
                _logger?.LogMessage($"Exporting {options.Count} track(s)...");

                List<ToolOperationResult> results = [];
                foreach (TrackExportOptions opt in options)
                {
                    ReportProgress($"Exporting stream {opt.StreamIndex}");
                    IReadOnlyList<ToolOperationResult> singleResult =
                        await _trackExportService.ExportAsync(effectiveInputPath, [opt], cancellationToken, request.IsMultiPart);
                    results.AddRange(singleResult);
                }

                trackResults = results;
            }

            if (applyTiming && trackResults.Count > 0)
            {
                ReportProgress("Retiming subtitles");
                trackResults = await RetimeExportedSubtitlesAsync(
                    trackResults, options, request.SourceFps!.Value, request.TargetFps!.Value, cancellationToken);
            }
        }

        if (request.ExportChapters && request.Chapters.Count > 0)
        {
            ReportProgress("Exporting chapters");
            string chapterOutputPath = Path.Combine(request.OutputFolder, "chapters.txt");

            if (applyTiming)
            {
                decimal stretchFactor = request.SourceFps!.Value / request.TargetFps!.Value;
                IReadOnlyList<MediaChapterInfo> retimed = ChapterRetimingService.Retime(request.Chapters, stretchFactor);
                _logger?.LogMessage($"Exporting {retimed.Count} retimed chapter(s)...");

                try
                {
                    await FfmetadataWriter.WriteChaptersToFileAsync(retimed, chapterOutputPath, cancellationToken);
                    chapterResult = new ToolOperationResult(
                        "chapters (retimed)",
                        chapterOutputPath,
                        $"[written directly] {retimed.Count} chapters, stretch={stretchFactor}",
                        Succeeded: true,
                        ExitCode: 0,
                        Duration: TimeSpan.Zero,
                        ErrorDetail: null);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    chapterResult = new ToolOperationResult(
                        "chapters (retimed)",
                        chapterOutputPath,
                        $"[write failed]",
                        Succeeded: false,
                        ExitCode: -1,
                        Duration: TimeSpan.Zero,
                        ex.Message);
                }
            }
            else
            {
                _logger?.LogMessage("Exporting chapters...");
                chapterResult = await _chapterExportService.ExportAsync(effectiveInputPath, chapterOutputPath, cancellationToken);
            }
        }

        bool hasFailures = trackResults.Any(r => !r.Succeeded) || (chapterResult is not null && !chapterResult.Succeeded);

        ToolOperationResult? muxResult = null;
        if (request.MuxToMkv && _muxService is not null && !hasFailures)
        {
            ReportProgress("Muxing to MKV");
            muxResult = await MuxToMkvAsync(request, trackResults, chapterResult, cancellationToken);
            if (muxResult is not null && !muxResult.Succeeded)
            {
                hasFailures = true;
            }
        }

        return new ExportWorkflowResult(trackResults, chapterResult, muxResult, hasFailures);
    }

    private static int CountSteps(ExportWorkflowRequest request, bool applyTiming)
    {
        List<ExportTrackSelection> filtered = request.Tracks
            .Where(t => t.AssetType is not MediaAssetType.Video)
            .ToList();

        int steps = filtered.Count;

        bool hasRetimeableSubtitles = applyTiming && filtered.Any(t => t.AssetType == MediaAssetType.Subtitle);
        if (hasRetimeableSubtitles) steps++;

        if (request.ExportChapters && request.Chapters.Count > 0) steps++;
        if (request.MuxToMkv) steps++;

        return steps;
    }

    private async Task<ToolOperationResult?> MuxToMkvAsync(
        ExportWorkflowRequest request,
        IReadOnlyList<ToolOperationResult> trackResults,
        ToolOperationResult? chapterResult,
        CancellationToken cancellationToken)
    {
        // Build the filtered selection list (same filter as ExecuteAsync) to keep indexes aligned with trackResults
        List<ExportTrackSelection> filteredSelections = request.Tracks
            .Where(t => t.AssetType is not MediaAssetType.Video)
            .ToList();

        List<MuxInputAsset> assets = [];
        int inputIdx = request.DestinationMasterPath is not null ? 1 : 0;

        if (chapterResult is not null && chapterResult.Succeeded)
        {
            inputIdx++;
        }

        for (int i = 0; i < trackResults.Count; i++)
        {
            ToolOperationResult tr = trackResults[i];
            if (!tr.Succeeded || !File.Exists(tr.OutputPath))
            {
                continue;
            }

            ExportTrackSelection? sel = i < filteredSelections.Count ? filteredSelections[i] : null;
            MediaAssetType assetType = sel?.AssetType ?? MediaAssetType.Audio;

            assets.Add(new MuxInputAsset(
                tr.OutputPath,
                InputIndex: inputIdx,
                StreamIndex: 0,
                assetType,
                Language: null,
                Title: null,
                IsDefault: false,
                IsForced: false));

            inputIdx++;
        }

        if (assets.Count == 0 && chapterResult is null && request.DestinationMasterPath is null)
        {
            _logger?.LogMessage("Skipping mux: no assets to combine.");
            return null;
        }

        string outputFileName = Path.GetFileNameWithoutExtension(request.InputPath) + "_muxed.mkv";
        string outputPath = Path.Combine(request.OutputFolder, outputFileName);

        string? chaptersPath = chapterResult is not null && chapterResult.Succeeded
            ? chapterResult.OutputPath
            : null;

        MuxRequest muxRequest = new(
            outputPath,
            request.DestinationMasterPath,
            assets,
            chaptersPath);

        int inputCount = assets.Count
            + (request.DestinationMasterPath is not null ? 1 : 0)
            + (chaptersPath is not null ? 1 : 0);

        _logger?.LogMessage($"Muxing {inputCount} input(s) into {outputPath}...");
        return await _muxService!.MuxAsync(muxRequest, cancellationToken);
    }

    private async Task<IReadOnlyList<ToolOperationResult>> RetimeExportedSubtitlesAsync(
        IReadOnlyList<ToolOperationResult> trackResults,
        List<TrackExportOptions> options,
        decimal sourceFps,
        decimal targetFps,
        CancellationToken cancellationToken)
    {
        decimal stretchFactor = sourceFps / targetFps;
        List<ToolOperationResult> updated = [.. trackResults];

        for (int i = 0; i < options.Count; i++)
        {
            TrackExportOptions opt = options[i];
            if (opt.AssetType != MediaAssetType.Subtitle)
            {
                continue;
            }

            if (i >= updated.Count || !updated[i].Succeeded)
            {
                continue;
            }

            string ext = Path.GetExtension(opt.OutputPath).ToLowerInvariant();
            if (ext is not ".srt" and not ".vtt")
            {
                continue;
            }

            if (!File.Exists(opt.OutputPath))
            {
                continue;
            }

            try
            {
                IReadOnlyList<SubtitleCue> cues = ext == ".vtt"
                    ? VttParser.ParseFile(opt.OutputPath)
                    : SrtParser.ParseFile(opt.OutputPath);

                IReadOnlyList<SubtitleCue> retimed = SubtitleRetimingService.Retime(cues, stretchFactor);

                if (ext == ".vtt")
                {
                    await VttWriter.WriteToFileAsync(retimed, opt.OutputPath, cancellationToken);
                }
                else
                {
                    await SrtWriter.WriteToFileAsync(retimed, opt.OutputPath, cancellationToken);
                }

                _logger?.LogMessage($"Retimed {retimed.Count} subtitle cue(s) in stream {opt.StreamIndex}.");

                updated[i] = updated[i] with
                {
                    OperationName = $"{updated[i].OperationName} (retimed)"
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogMessage($"Warning: failed to retime subtitles for stream {opt.StreamIndex}: {ex.Message}");
            }
        }

        return updated;
    }
}
