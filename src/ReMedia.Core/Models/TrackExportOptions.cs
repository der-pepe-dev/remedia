namespace ReMedia.Core.Models;

public sealed record TrackExportOptions(
    int StreamIndex,
    MediaAssetType AssetType,
    string OutputContainer,
    string OutputCodec,
    string OutputPath,
    bool CopyStream,
    bool ApplyTimingConversion = false,
    decimal? SourceFps = null,
    decimal? TargetFps = null,
    decimal? AppliedGainDb = null,
    decimal? AudioSyncOffsetMs = null);
