namespace ReMedia.Core.Models;

public sealed record MediaProbeResult(
    string InputPath,
    string? FormatName,
    string? FormatLongName,
    TimeSpan? Duration,
    long? SizeBytes,
    IReadOnlyList<MediaStreamInfo> Streams,
    IReadOnlyList<MediaChapterInfo> Chapters,
    ContainerFormatInfo? NativeFormat = null)
{
    public MediaStreamInfo? GetPrimaryVideoStream()
    {
        return Streams.FirstOrDefault(stream => stream.AssetType == MediaAssetType.Video);
    }
}
