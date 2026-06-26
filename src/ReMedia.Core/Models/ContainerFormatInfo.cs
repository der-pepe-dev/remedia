namespace ReMedia.Core.Models;

/// <summary>
/// Identifies a media container format detected from file header magic bytes.
/// </summary>
public sealed record ContainerFormatInfo(
    ContainerFormat Format,
    string ShortName,
    string Description);

/// <summary>
/// Known container formats that can be detected natively.
/// </summary>
public enum ContainerFormat
{
    Unknown = 0,
    Matroska,
    WebM,
    Mp4,
    Avi,
    MpegTs,
    MpegPs,
    Flv,
    Ogg,
    Wav,
    Flac,
}
