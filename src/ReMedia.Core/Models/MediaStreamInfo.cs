namespace ReMedia.Core.Models;

public sealed record MediaStreamInfo(
    int Index,
    MediaAssetType AssetType,
    string? CodecName,
    string? CodecLongName,
    string? Language,
    string? Title,
    bool Default,
    bool Forced,
    int? Channels,
    int? SampleRate,
    int? Width,
    int? Height,
    Rational AvgFrameRate,
    Rational RealFrameRate,
    string? FieldOrder)
{
    public decimal AverageFramesPerSecond => AvgFrameRate.ToDecimal();
    public decimal RealFramesPerSecond => RealFrameRate.ToDecimal();
}
