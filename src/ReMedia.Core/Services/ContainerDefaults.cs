namespace ReMedia.Core.Services;

using ReMedia.Core.Models;

/// <summary>
/// Maps codec names to reasonable default output containers for same-format export.
/// </summary>
public static class ContainerDefaults
{
    public static string GetDefaultContainer(MediaAssetType assetType, string? codecName)
    {
        return assetType switch
        {
            MediaAssetType.Audio => GetAudioContainer(codecName),
            MediaAssetType.Subtitle => GetSubtitleContainer(codecName),
            _ => ".bin"
        };
    }

    private static string GetAudioContainer(string? codecName)
    {
        return codecName?.ToLowerInvariant() switch
        {
            "ac3" => ".ac3",
            "eac3" => ".eac3",
            "flac" => ".flac",
            "aac" => ".m4a",
            "mp3" or "mp3float" or "libmp3lame" => ".mp3",
            "opus" or "libopus" => ".opus",
            "vorbis" or "libvorbis" => ".ogg",
            "pcm_s16le" or "pcm_s24le" or "pcm_s32le" or "pcm_f32le" or "pcm_f64le" => ".wav",
            "truehd" => ".thd",
            "dts" => ".dts",
            _ => ".mka"
        };
    }

    private static string GetSubtitleContainer(string? codecName)
    {
        return codecName?.ToLowerInvariant() switch
        {
            "subrip" or "srt" => ".srt",
            "ass" or "ssa" => ".ass",
            "webvtt" => ".vtt",
            "dvd_subtitle" or "dvdsub" => ".sup",
            "hdmv_pgs_subtitle" or "pgssub" => ".sup",
            _ => ".mks"
        };
    }
}
