namespace ReMedia.Tooling.Ffprobe;

using System.Globalization;
using System.Text.Json;
using ReMedia.Core.Diagnostics;
using ReMedia.Core.Interfaces;
using ReMedia.Core.Models;
using ReMedia.Core.Services;
using ReMedia.Core.Validation;
using ReMedia.Tooling.Configuration;

public sealed class FfprobeMediaProbeService : IMediaProbeService
{
    private readonly ExternalToolPaths _toolPaths;
    private readonly IProcessRunner _processRunner;

    public FfprobeMediaProbeService(ExternalToolPaths toolPaths, IProcessRunner processRunner)
    {
        _toolPaths = toolPaths;
        _processRunner = processRunner;
    }

    public async Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNullOrWhiteSpace(inputPath, nameof(inputPath));

        string arguments =
            $"-v error -of json " +
            $"-show_entries format=filename,format_name,format_long_name,duration,size:" +
            $"stream=index,codec_type,codec_name,codec_long_name,avg_frame_rate,r_frame_rate,width,height,field_order,channels,sample_rate:" +
            $"stream_disposition=default,forced:" +
            $"stream_tags=language,title:chapter=id,start_time,end_time:chapter_tags=title " +
            $"-show_streams -show_chapters \"{inputPath}\"";

        ProcessExecutionResult result = await _processRunner.RunAsync(_toolPaths.FfprobePath, arguments, cancellationToken);
        if (!result.Succeeded)
        {
            throw new ProcessExecutionException(result);
        }

        FfprobeRootDto? root = JsonSerializer.Deserialize<FfprobeRootDto>(result.StandardOutput);
        if (root is null)
        {
            throw new InvalidOperationException("ffprobe returned no data.");
        }

        ContainerFormatInfo? nativeFormat = null;
        try
        {
            nativeFormat = ContainerFormatDetector.Detect(inputPath);
        }
        catch
        {
            // Non-fatal: native detection is best-effort
        }

        return new MediaProbeResult(
            inputPath,
            root.Format?.FormatName,
            root.Format?.FormatLongName,
            ParseDuration(root.Format?.Duration),
            ParseNullableInt64(root.Format?.Size),
            MapStreams(root.Streams),
            MapChapters(root.Chapters),
            nativeFormat);
    }

    private static IReadOnlyList<MediaStreamInfo> MapStreams(IReadOnlyCollection<FfprobeStreamDto>? streams)
    {
        if (streams is null || streams.Count == 0)
        {
            return [];
        }

        return streams.Select(stream => new MediaStreamInfo(
            stream.Index,
            MapAssetType(stream.CodecType),
            stream.CodecName,
            stream.CodecLongName,
            stream.Tags is not null && stream.Tags.TryGetValue("language", out string? language) ? language : null,
            stream.Tags is not null && stream.Tags.TryGetValue("title", out string? title) ? title : null,
            stream.Disposition?.Default == 1,
            stream.Disposition?.Forced == 1,
            stream.Channels,
            ParseNullableInt32(stream.SampleRate),
            stream.Width,
            stream.Height,
            Rational.ParseOrZero(stream.AvgFrameRate),
            Rational.ParseOrZero(stream.RealFrameRate),
            stream.FieldOrder)).ToArray();
    }

    private static IReadOnlyList<MediaChapterInfo> MapChapters(IReadOnlyCollection<FfprobeChapterDto>? chapters)
    {
        if (chapters is null || chapters.Count == 0)
        {
            return [];
        }

        return chapters.Select(chapter => new MediaChapterInfo(
            chapter.Id,
            ParseDuration(chapter.StartTime) ?? TimeSpan.Zero,
            ParseDuration(chapter.EndTime) ?? TimeSpan.Zero,
            chapter.Tags is not null && chapter.Tags.TryGetValue("title", out string? title) ? title : null)).ToArray();
    }

    private static MediaAssetType MapAssetType(string? codecType)
    {
        return codecType?.ToLowerInvariant() switch
        {
            "video" => MediaAssetType.Video,
            "audio" => MediaAssetType.Audio,
            "subtitle" => MediaAssetType.Subtitle,
            "data" => MediaAssetType.Data,
            "attachment" => MediaAssetType.Attachment,
            _ => MediaAssetType.Unknown
        };
    }

    private static TimeSpan? ParseDuration(string? value)
    {
        if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal seconds))
        {
            return null;
        }

        return TimeSpan.FromSeconds((double)seconds);
    }

    private static int? ParseNullableInt32(string? value)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) ? parsed : null;
    }

    private static long? ParseNullableInt64(string? value)
    {
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed) ? parsed : null;
    }
}
