namespace ReMedia.Core.Models;

public sealed record CodecOption(
    string Id,
    string DisplayName,
    MediaAssetType AssetType,
    string SuggestedContainer,
    bool Lossless,
    bool SupportsRetiming);
