namespace ReMedia.Core.Services;

using ReMedia.Core.Models;

/// <summary>
/// Detects media container format from file header magic bytes.
/// This is the first step toward a native probe layer that doesn't
/// depend on ffprobe. Currently detects format only; detailed stream
/// and chapter parsing will be added in future phases.
/// </summary>
public static class ContainerFormatDetector
{
    private const int HeaderSize = 32;

    /// <summary>
    /// Detects the container format of a file by reading its header.
    /// </summary>
    public static ContainerFormatInfo Detect(string filePath)
    {
        using FileStream fs = File.OpenRead(filePath);
        return Detect(fs);
    }

    /// <summary>
    /// Detects the container format from a stream (reads up to 32 bytes from current position).
    /// </summary>
    public static ContainerFormatInfo Detect(Stream stream)
    {
        Span<byte> header = stackalloc byte[HeaderSize];
        int bytesRead = stream.Read(header);
        if (bytesRead < 4)
        {
            return Unknown;
        }

        ReadOnlySpan<byte> buf = header[..bytesRead];

        // EBML header (Matroska / WebM): 0x1A 0x45 0xDF 0xA3
        if (buf.Length >= 4 && buf[0] == 0x1A && buf[1] == 0x45 && buf[2] == 0xDF && buf[3] == 0xA3)
        {
            return DetectEbml(buf);
        }

        // RIFF (AVI / WAV): "RIFF"
        if (buf.Length >= 12 && buf[0] == 'R' && buf[1] == 'I' && buf[2] == 'F' && buf[3] == 'F')
        {
            return DetectRiff(buf);
        }

        // ISO Base Media / MP4: check for "ftyp" box
        if (buf.Length >= 8 && buf[4] == 'f' && buf[5] == 't' && buf[6] == 'y' && buf[7] == 'p')
        {
            return Mp4;
        }

        // MPEG-TS: 0x47 sync byte (check first and 188th byte if possible)
        if (buf[0] == 0x47)
        {
            return MpegTs;
        }

        // MPEG-PS: 0x00 0x00 0x01 0xBA
        if (buf.Length >= 4 && buf[0] == 0x00 && buf[1] == 0x00 && buf[2] == 0x01 && buf[3] == 0xBA)
        {
            return MpegPs;
        }

        // FLV: "FLV" 0x01
        if (buf.Length >= 4 && buf[0] == 'F' && buf[1] == 'L' && buf[2] == 'V')
        {
            return Flv;
        }

        // OGG: "OggS"
        if (buf.Length >= 4 && buf[0] == 'O' && buf[1] == 'g' && buf[2] == 'g' && buf[3] == 'S')
        {
            return Ogg;
        }

        // FLAC: "fLaC"
        if (buf.Length >= 4 && buf[0] == 'f' && buf[1] == 'L' && buf[2] == 'a' && buf[3] == 'C')
        {
            return FlacFormat;
        }

        return Unknown;
    }

    private static ContainerFormatInfo DetectEbml(ReadOnlySpan<byte> buf)
    {
        // Look for "webm" or "matroska" doctype in the header region.
        // A full EBML parse would be needed for certainty, but a quick
        // byte scan for the doctype string covers practical files.
        for (int i = 4; i < buf.Length - 4; i++)
        {
            if (buf[i] == 'w' && buf[i + 1] == 'e' && buf[i + 2] == 'b' && buf[i + 3] == 'm')
            {
                return WebM;
            }
        }

        return Matroska;
    }

    private static ContainerFormatInfo DetectRiff(ReadOnlySpan<byte> buf)
    {
        // Bytes 8-11 are the RIFF form type: "AVI " or "WAVE"
        if (buf[8] == 'A' && buf[9] == 'V' && buf[10] == 'I' && buf[11] == ' ')
        {
            return Avi;
        }

        if (buf[8] == 'W' && buf[9] == 'A' && buf[10] == 'V' && buf[11] == 'E')
        {
            return Wav;
        }

        return Unknown;
    }

    private static readonly ContainerFormatInfo Unknown = new(ContainerFormat.Unknown, "unknown", "Unknown format");
    private static readonly ContainerFormatInfo Matroska = new(ContainerFormat.Matroska, "matroska", "Matroska (MKV/MKA)");
    private static readonly ContainerFormatInfo WebM = new(ContainerFormat.WebM, "webm", "WebM");
    private static readonly ContainerFormatInfo Mp4 = new(ContainerFormat.Mp4, "mp4", "MPEG-4 Part 14 (MP4/M4A)");
    private static readonly ContainerFormatInfo Avi = new(ContainerFormat.Avi, "avi", "Audio Video Interleave");
    private static readonly ContainerFormatInfo MpegTs = new(ContainerFormat.MpegTs, "mpegts", "MPEG Transport Stream");
    private static readonly ContainerFormatInfo MpegPs = new(ContainerFormat.MpegPs, "mpeg", "MPEG Program Stream");
    private static readonly ContainerFormatInfo Flv = new(ContainerFormat.Flv, "flv", "Flash Video");
    private static readonly ContainerFormatInfo Ogg = new(ContainerFormat.Ogg, "ogg", "OGG");
    private static readonly ContainerFormatInfo Wav = new(ContainerFormat.Wav, "wav", "Waveform Audio");
    private static readonly ContainerFormatInfo FlacFormat = new(ContainerFormat.Flac, "flac", "Free Lossless Audio Codec");
}
