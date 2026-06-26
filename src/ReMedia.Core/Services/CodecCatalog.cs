namespace ReMedia.Core.Services;

using ReMedia.Core.Models;

public static class CodecCatalog
{
    public static IReadOnlyList<CodecOption> Audio { get; } =
    [
        new("flac", "FLAC", MediaAssetType.Audio, ".flac", Lossless: true, SupportsRetiming: true),
        new("pcm_s16le", "PCM 16-bit WAV", MediaAssetType.Audio, ".wav", Lossless: true, SupportsRetiming: true),
        new("ac3", "AC-3", MediaAssetType.Audio, ".ac3", Lossless: false, SupportsRetiming: true),
        new("eac3", "E-AC-3", MediaAssetType.Audio, ".eac3", Lossless: false, SupportsRetiming: true),
        new("aac", "AAC", MediaAssetType.Audio, ".m4a", Lossless: false, SupportsRetiming: true),
        new("libopus", "Opus", MediaAssetType.Audio, ".opus", Lossless: false, SupportsRetiming: true),
        new("libmp3lame", "MP3", MediaAssetType.Audio, ".mp3", Lossless: false, SupportsRetiming: true),
    ];

    public static IReadOnlyList<CodecOption> Subtitle { get; } =
    [
        new("copy", "Copy Original", MediaAssetType.Subtitle, ".sub", Lossless: true, SupportsRetiming: false),
        new("srt", "SubRip (.srt)", MediaAssetType.Subtitle, ".srt", Lossless: false, SupportsRetiming: true),
        new("webvtt", "WebVTT (.vtt)", MediaAssetType.Subtitle, ".vtt", Lossless: false, SupportsRetiming: true),
    ];
}
