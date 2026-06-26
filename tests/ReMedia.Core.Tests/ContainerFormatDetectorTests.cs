namespace ReMedia.Core.Tests;

using ReMedia.Core.Models;
using ReMedia.Core.Services;

public sealed class ContainerFormatDetectorTests
{
    [Fact]
    public void Detect_MatroskaHeader_ReturnsMatroska()
    {
        // EBML header: 1A 45 DF A3
        byte[] header = [0x1A, 0x45, 0xDF, 0xA3, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x23];
        using MemoryStream ms = new(header);

        ContainerFormatInfo result = ContainerFormatDetector.Detect(ms);

        Assert.Equal(ContainerFormat.Matroska, result.Format);
    }

    [Fact]
    public void Detect_WebMHeader_ReturnsWebM()
    {
        // EBML header with "webm" doctype somewhere in the first 32 bytes
        byte[] header = new byte[32];
        header[0] = 0x1A; header[1] = 0x45; header[2] = 0xDF; header[3] = 0xA3;
        header[20] = (byte)'w'; header[21] = (byte)'e'; header[22] = (byte)'b'; header[23] = (byte)'m';
        using MemoryStream ms = new(header);

        ContainerFormatInfo result = ContainerFormatDetector.Detect(ms);

        Assert.Equal(ContainerFormat.WebM, result.Format);
    }

    [Fact]
    public void Detect_WebMDoctypeAtHeaderTail_ReturnsWebM()
    {
        // "webm" occupies the last 4 bytes of the 32-byte header — the off-by-one
        // boundary the scan previously missed (falling back to Matroska).
        byte[] header = new byte[32];
        header[0] = 0x1A; header[1] = 0x45; header[2] = 0xDF; header[3] = 0xA3;
        header[28] = (byte)'w'; header[29] = (byte)'e'; header[30] = (byte)'b'; header[31] = (byte)'m';
        using MemoryStream ms = new(header);

        ContainerFormatInfo result = ContainerFormatDetector.Detect(ms);

        Assert.Equal(ContainerFormat.WebM, result.Format);
    }

    [Fact]
    public void Detect_Mp4Header_ReturnsMp4()
    {
        // "ftyp" box at offset 4
        byte[] header = [0x00, 0x00, 0x00, 0x20, (byte)'f', (byte)'t', (byte)'y', (byte)'p',
                         (byte)'i', (byte)'s', (byte)'o', (byte)'m'];
        using MemoryStream ms = new(header);

        ContainerFormatInfo result = ContainerFormatDetector.Detect(ms);

        Assert.Equal(ContainerFormat.Mp4, result.Format);
    }

    [Fact]
    public void Detect_AviHeader_ReturnsAvi()
    {
        byte[] header = [(byte)'R', (byte)'I', (byte)'F', (byte)'F',
                         0x00, 0x00, 0x00, 0x00,
                         (byte)'A', (byte)'V', (byte)'I', (byte)' '];
        using MemoryStream ms = new(header);

        ContainerFormatInfo result = ContainerFormatDetector.Detect(ms);

        Assert.Equal(ContainerFormat.Avi, result.Format);
    }

    [Fact]
    public void Detect_WavHeader_ReturnsWav()
    {
        byte[] header = [(byte)'R', (byte)'I', (byte)'F', (byte)'F',
                         0x00, 0x00, 0x00, 0x00,
                         (byte)'W', (byte)'A', (byte)'V', (byte)'E'];
        using MemoryStream ms = new(header);

        ContainerFormatInfo result = ContainerFormatDetector.Detect(ms);

        Assert.Equal(ContainerFormat.Wav, result.Format);
    }

    [Fact]
    public void Detect_MpegTsHeader_ReturnsMpegTs()
    {
        byte[] header = [0x47, 0x00, 0x11, 0x10];
        using MemoryStream ms = new(header);

        ContainerFormatInfo result = ContainerFormatDetector.Detect(ms);

        Assert.Equal(ContainerFormat.MpegTs, result.Format);
    }

    [Fact]
    public void Detect_MpegPsHeader_ReturnsMpegPs()
    {
        byte[] header = [0x00, 0x00, 0x01, 0xBA];
        using MemoryStream ms = new(header);

        ContainerFormatInfo result = ContainerFormatDetector.Detect(ms);

        Assert.Equal(ContainerFormat.MpegPs, result.Format);
    }

    [Fact]
    public void Detect_FlvHeader_ReturnsFlv()
    {
        byte[] header = [(byte)'F', (byte)'L', (byte)'V', 0x01, 0x05];
        using MemoryStream ms = new(header);

        ContainerFormatInfo result = ContainerFormatDetector.Detect(ms);

        Assert.Equal(ContainerFormat.Flv, result.Format);
    }

    [Fact]
    public void Detect_OggHeader_ReturnsOgg()
    {
        byte[] header = [(byte)'O', (byte)'g', (byte)'g', (byte)'S'];
        using MemoryStream ms = new(header);

        ContainerFormatInfo result = ContainerFormatDetector.Detect(ms);

        Assert.Equal(ContainerFormat.Ogg, result.Format);
    }

    [Fact]
    public void Detect_FlacHeader_ReturnsFlac()
    {
        byte[] header = [(byte)'f', (byte)'L', (byte)'a', (byte)'C'];
        using MemoryStream ms = new(header);

        ContainerFormatInfo result = ContainerFormatDetector.Detect(ms);

        Assert.Equal(ContainerFormat.Flac, result.Format);
    }

    [Fact]
    public void Detect_EmptyStream_ReturnsUnknown()
    {
        using MemoryStream ms = new([]);

        ContainerFormatInfo result = ContainerFormatDetector.Detect(ms);

        Assert.Equal(ContainerFormat.Unknown, result.Format);
    }

    [Fact]
    public void Detect_GarbageBytes_ReturnsUnknown()
    {
        byte[] header = [0xFF, 0xFE, 0xFD, 0xFC, 0xFB, 0xFA, 0xF9, 0xF8];
        using MemoryStream ms = new(header);

        ContainerFormatInfo result = ContainerFormatDetector.Detect(ms);

        Assert.Equal(ContainerFormat.Unknown, result.Format);
    }
}
