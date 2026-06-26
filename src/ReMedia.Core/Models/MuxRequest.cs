namespace ReMedia.Core.Models;

/// <summary>
/// Describes a single asset to include in a mux operation.
/// This can be a file exported from the source (audio, subtitle, chapters)
/// or a stream from an existing container (the destination master).
/// </summary>
public sealed record MuxInputAsset(
    string FilePath,
    int InputIndex,
    int StreamIndex,
    MediaAssetType AssetType,
    string? Language,
    string? Title,
    bool IsDefault,
    bool IsForced);

/// <summary>
/// Request to mux multiple assets into a single output container.
/// If <see cref="DestinationMasterPath"/> is set, all streams from that file
/// are included first, then the additional assets are appended.
/// </summary>
public sealed record MuxRequest(
    string OutputPath,
    string? DestinationMasterPath,
    IReadOnlyList<MuxInputAsset> Assets,
    string? ChaptersFilePath);
