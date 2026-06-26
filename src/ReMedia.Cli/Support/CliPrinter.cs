namespace ReMedia.Cli.Support;

using System.Globalization;
using ReMedia.Core.Models;
using ReMedia.Core.Services;

internal static class CliPrinter
{
    public static void PrintUsage()
    {
        Console.WriteLine("ReMedia Sync CLI");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  help                   Show this help");
        Console.WriteLine("  list-codecs            Show available codecs and FPS presets");
        Console.WriteLine("  probe <input>");
        Console.WriteLine("  detect <input>         Identify container format natively");
        Console.WriteLine("  analyze <input> --target-fps 23.976023976");
        Console.WriteLine("  analyze --duration 01:32:14 --source-fps 25 --target-fps 23.976023976");
        Console.WriteLine("  export <input> --all --output-folder <path>");
        Console.WriteLine("  export <input> --all --chapters --output-folder <path>");
        Console.WriteLine("  export <input> --stream 1 --stream 2 --output-folder <path>");
        Console.WriteLine("  export <input> --all --target-fps 23.976023976 --codec flac --output-folder <path>");
        Console.WriteLine("  export <input> --all --mux --output-folder <path>");
        Console.WriteLine("  export <input> --all --mux --destination dest.mkv --output-folder <path>");
        Console.WriteLine("  export <input> --part part2.mkv --all --output-folder <path>");
        Console.WriteLine("  loudness <input>");
        Console.WriteLine("  loudness <input> --stream 1 --gain 3.5");
        Console.WriteLine("  cleanup subs.srt");
        Console.WriteLine("  cleanup subs.vtt --strip-sdh --output clean.vtt");
        Console.WriteLine();
        Console.WriteLine("Export options:");
        Console.WriteLine("  --stream <index>       Export specific stream (repeatable)");
        Console.WriteLine("  --all                  Export all audio and subtitle streams");
        Console.WriteLine("  --chapters             Export chapter metadata");
        Console.WriteLine("  --codec <name>         Output codec (default: copy)");
        Console.WriteLine("  --source-fps <fps>     Override source FPS (auto-detected from video)");
        Console.WriteLine("  --target-fps <fps>     Target FPS for timing conversion");
        Console.WriteLine("  --output-folder <path> Output directory");
        Console.WriteLine("  --mux                  Combine exported tracks into a single .mkv");
        Console.WriteLine("  --destination <file>   Mux into an existing file (keeps its video + streams)");
        Console.WriteLine("  --part <file>          Additional source part (repeatable, for split files)");
        Console.WriteLine();
        Console.WriteLine("Loudness options:");
        Console.WriteLine("  --stream <index>       Analyze specific stream (default: all audio)");
        Console.WriteLine("  --gain <dB>            Predict clipping at this gain level");
        Console.WriteLine();
        Console.WriteLine("Cleanup options:");
        Console.WriteLine("  --output <file>        Output path (default: <input>_clean.<ext>)");
        Console.WriteLine("  --strip-sdh            Remove [music], (door slams), etc.");
        Console.WriteLine("  --strip-html           Remove <i>, <b>, <font> tags");
        Console.WriteLine("  --fix-overlaps         Trim overlapping cue end times");
        Console.WriteLine("  --all                  Apply all cleanup operations (default)");
    }

    public static void PrintCodecCatalog()
    {
        Console.WriteLine("Audio codecs:");
        foreach (CodecOption codec in CodecCatalog.Audio)
        {
            string flags = codec.Lossless ? "lossless" : "lossy";
            Console.WriteLine($"  {codec.Id,-16} {codec.DisplayName,-20} {codec.SuggestedContainer,-8} {flags}");
        }

        Console.WriteLine();
        Console.WriteLine("Subtitle codecs:");
        foreach (CodecOption codec in CodecCatalog.Subtitle)
        {
            string retime = codec.SupportsRetiming ? "retimeable" : string.Empty;
            Console.WriteLine($"  {codec.Id,-16} {codec.DisplayName,-20} {codec.SuggestedContainer,-8} {retime}");
        }

        Console.WriteLine();
        Console.WriteLine("Common FPS presets:");
        foreach (FpsPreset preset in FpsPresets.All)
        {
            Console.WriteLine($"  {preset.ValueText,-24} {preset.DisplayName}");
        }
    }

    public static void PrintProbe(MediaProbeResult result)
    {
        Console.WriteLine($"Input:      {result.InputPath}");
        Console.WriteLine($"Format:     {result.FormatName} ({result.FormatLongName})");
        Console.WriteLine($"Duration:   {result.Duration}");
        Console.WriteLine($"Size:       {FormatSize(result.SizeBytes)}");
        Console.WriteLine();

        Console.WriteLine("Streams:");
        foreach (MediaStreamInfo stream in result.Streams)
        {
            string flags = FormatFlags(stream.Default, stream.Forced);
            string detail = stream.AssetType switch
            {
                MediaAssetType.Video => FormatVideoDetail(stream),
                MediaAssetType.Audio => FormatAudioDetail(stream),
                _ => string.Empty
            };

            Console.WriteLine(
                $"  [{stream.Index}] {stream.AssetType,-10} " +
                $"codec={stream.CodecName,-16} " +
                $"lang={stream.Language ?? "und",-5} " +
                $"title={stream.Title ?? "-",-20} " +
                $"{detail}{flags}");
        }

        Console.WriteLine();
        if (result.Chapters.Count > 0)
        {
            Console.WriteLine("Chapters:");
            foreach (MediaChapterInfo chapter in result.Chapters)
            {
                Console.WriteLine($"  [{chapter.Id}] {chapter.Start:hh\\:mm\\:ss\\.fff} -> {chapter.End:hh\\:mm\\:ss\\.fff} {chapter.Title}");
            }
        }
        else
        {
            Console.WriteLine("Chapters: (none)");
        }
    }

    public static void PrintTiming(TimingAnalysisResult result)
    {
        Console.WriteLine($"Original duration:    {result.OriginalDuration}");
        Console.WriteLine($"Destination duration: {result.DestinationDuration}");
        Console.WriteLine($"Source FPS:           {Format(result.SourceFps)}");
        Console.WriteLine($"Target FPS:           {Format(result.TargetFps)}");
        Console.WriteLine($"Stretch factor:       {Format(result.StretchFactor)}");
        Console.WriteLine($"Audio tempo factor:   {Format(result.AudioTempoFactor)}");
    }

    public static void PrintExportResults(IReadOnlyList<ToolOperationResult> results)
    {
        foreach (ToolOperationResult result in results)
        {
            PrintOperationResult(result);
        }
    }

    public static void PrintChapterExportResult(ToolOperationResult result)
    {
        PrintOperationResult(result);
    }

    public static void PrintWorkflowResult(ExportWorkflowResult workflowResult)
    {
        PrintExportResults(workflowResult.TrackResults);

        if (workflowResult.ChapterResult is not null)
        {
            PrintChapterExportResult(workflowResult.ChapterResult);
        }

        if (workflowResult.MuxResult is not null)
        {
            PrintOperationResult(workflowResult.MuxResult);
        }
    }

    public static void PrintLoudness(int streamIndex, LoudnessAnalysisResult result)
    {
        Console.WriteLine($"  [stream {streamIndex}] Loudness analysis:");
        Console.WriteLine($"    Integrated:   {FormatNullable(result.IntegratedLufs, "0.#")} LUFS");
        Console.WriteLine($"    Range (LRA):  {FormatNullable(result.LoudnessRange, "0.#")} LU");
        Console.WriteLine($"    True peak:    {FormatNullable(result.TruePeakDbtp, "0.#")} dBTP");
        Console.WriteLine($"    Sample peak:  {FormatNullable(result.SamplePeakDbfs, "0.#")} dBFS");
    }

    public static void PrintClippingPrediction(ClippingPredictionResult result)
    {
        string label = result.Danger ? "DANGER" : result.Warning ? "WARNING" : "OK";
        Console.WriteLine($"    Clipping ({result.AppliedGainDb:+0.#;-0.#;0} dB): [{label}] {result.Message}");
    }

    private static void PrintOperationResult(ToolOperationResult result)
    {
        string status = result.Succeeded ? "OK" : $"FAILED (exit {result.ExitCode})";
        string duration = result.Duration > TimeSpan.Zero ? $" ({result.Duration.TotalSeconds:0.##}s)" : string.Empty;

        Console.WriteLine($"[{result.OperationName}] {status}{duration} -> {result.OutputPath}");
        Console.WriteLine($"  command: {result.GeneratedCommand}");

        if (!result.Succeeded && result.ErrorDetail is not null)
        {
            Console.Error.WriteLine($"  error:   {result.ErrorDetail}");
        }
    }

    private static string FormatVideoDetail(MediaStreamInfo stream)
    {
        string resolution = stream.Width.HasValue && stream.Height.HasValue
            ? $"{stream.Width}x{stream.Height} "
            : string.Empty;

        decimal fps = stream.AverageFramesPerSecond;
        string fpsText = fps > 0 ? $"{fps:0.###}fps " : string.Empty;

        string fieldOrder = !string.IsNullOrEmpty(stream.FieldOrder) && stream.FieldOrder != "unknown"
            ? $"{stream.FieldOrder} "
            : string.Empty;

        return $"{resolution}{fpsText}{fieldOrder}";
    }

    private static string FormatAudioDetail(MediaStreamInfo stream)
    {
        string channels = stream.Channels.HasValue ? $"ch={stream.Channels} " : string.Empty;
        string sampleRate = stream.SampleRate.HasValue ? $"{stream.SampleRate}Hz " : string.Empty;
        return $"{channels}{sampleRate}";
    }

    private static string FormatFlags(bool isDefault, bool isForced)
    {
        List<string> flags = [];
        if (isDefault) flags.Add("default");
        if (isForced) flags.Add("forced");
        return flags.Count > 0 ? $"[{string.Join(", ", flags)}]" : string.Empty;
    }

    private static string FormatSize(long? sizeBytes)
    {
        if (!sizeBytes.HasValue) return "unknown";

        return sizeBytes.Value switch
        {
            >= 1_073_741_824 => $"{sizeBytes.Value / 1_073_741_824.0:0.##} GB",
            >= 1_048_576 => $"{sizeBytes.Value / 1_048_576.0:0.##} MB",
            >= 1024 => $"{sizeBytes.Value / 1024.0:0.##} KB",
            _ => $"{sizeBytes.Value} bytes"
        };
    }

    private static string Format(decimal value)
    {
        return value.ToString("0.################", CultureInfo.InvariantCulture);
    }

    private static string FormatNullable(decimal? value, string format)
    {
        return value.HasValue
            ? value.Value.ToString(format, CultureInfo.InvariantCulture)
            : "N/A";
    }
}
