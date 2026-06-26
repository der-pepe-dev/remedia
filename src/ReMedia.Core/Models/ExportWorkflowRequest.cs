namespace ReMedia.Core.Models;

/// <summary>
/// User-provided selections for an export workflow.
/// When <see cref="AdditionalInputPaths"/> is non-empty, the input files
/// are concatenated using the ffmpeg concat demuxer before processing.
/// </summary>
public sealed record ExportWorkflowRequest(
    string InputPath,
    string OutputFolder,
    IReadOnlyList<ExportTrackSelection> Tracks,
    bool ExportChapters,
    IReadOnlyList<MediaChapterInfo> Chapters,
    decimal? SourceFps = null,
    decimal? TargetFps = null,
    bool MuxToMkv = false,
    string? DestinationMasterPath = null,
    IReadOnlyList<string>? AdditionalInputPaths = null)
{
    /// <summary>
    /// Returns all input paths (primary + additional) as a single list.
    /// </summary>
    public IReadOnlyList<string> AllInputPaths =>
        AdditionalInputPaths is { Count: > 0 }
            ? [InputPath, .. AdditionalInputPaths]
            : [InputPath];

    /// <summary>
    /// True when the source is split across multiple files.
    /// </summary>
    public bool IsMultiPart => AdditionalInputPaths is { Count: > 0 };
}

/// <summary>
/// Describes one track the user wants to export.
/// </summary>
public sealed record ExportTrackSelection(
    int StreamIndex,
    MediaAssetType AssetType,
    string? CodecName,
    string OutputCodec,
    string OutputContainer,
    bool CopyStream,
    decimal? AppliedGainDb = null,
    decimal? AudioSyncOffsetMs = null);
