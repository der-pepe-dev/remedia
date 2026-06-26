namespace ReMedia.Core.Tests;

using ReMedia.Core.Models;
using ReMedia.Core.Services;

public sealed class ContainerDefaultsTests
{
    [Theory]
    [InlineData("ac3", ".ac3")]
    [InlineData("eac3", ".eac3")]
    [InlineData("flac", ".flac")]
    [InlineData("aac", ".m4a")]
    [InlineData("mp3", ".mp3")]
    [InlineData("opus", ".opus")]
    [InlineData("vorbis", ".ogg")]
    [InlineData("pcm_s16le", ".wav")]
    [InlineData("pcm_s24le", ".wav")]
    [InlineData("truehd", ".thd")]
    [InlineData("dts", ".dts")]
    [InlineData("some_unknown_codec", ".mka")]
    [InlineData(null, ".mka")]
    public void GetDefaultContainer_ForAudio_ReturnsExpected(string? codecName, string expected)
    {
        string result = ContainerDefaults.GetDefaultContainer(MediaAssetType.Audio, codecName);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("subrip", ".srt")]
    [InlineData("srt", ".srt")]
    [InlineData("ass", ".ass")]
    [InlineData("ssa", ".ass")]
    [InlineData("webvtt", ".vtt")]
    [InlineData("dvd_subtitle", ".sup")]
    [InlineData("hdmv_pgs_subtitle", ".sup")]
    [InlineData("some_unknown_sub", ".mks")]
    [InlineData(null, ".mks")]
    public void GetDefaultContainer_ForSubtitle_ReturnsExpected(string? codecName, string expected)
    {
        string result = ContainerDefaults.GetDefaultContainer(MediaAssetType.Subtitle, codecName);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetDefaultContainer_ForVideo_ReturnsBin()
    {
        string result = ContainerDefaults.GetDefaultContainer(MediaAssetType.Video, "h264");
        Assert.Equal(".bin", result);
    }

    [Theory]
    [InlineData("flac", ".flac")]
    [InlineData("pcm_s16le", ".wav")]
    [InlineData("ac3", ".ac3")]
    [InlineData("aac", ".m4a")]
    [InlineData("libopus", ".opus")]
    [InlineData("libmp3lame", ".mp3")]
    [InlineData("libvorbis", ".ogg")]
    public void GetDefaultContainer_ForEncoderCodecNames_ReturnsExpected(string codecName, string expected)
    {
        string result = ContainerDefaults.GetDefaultContainer(MediaAssetType.Audio, codecName);
        Assert.Equal(expected, result);
    }
}
