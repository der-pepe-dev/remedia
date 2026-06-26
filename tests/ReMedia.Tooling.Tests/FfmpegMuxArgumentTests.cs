namespace ReMedia.Tooling.Tests;

using ReMedia.Core.Models;
using ReMedia.Tooling.Ffmpeg;

public sealed class FfmpegMuxArgumentTests
{
    [Fact]
    public void BuildMuxArguments_WithAssetsOnly_ProducesCorrectCommand()
    {
        MuxRequest request = new(
            OutputPath: @"C:\out\output.mkv",
            DestinationMasterPath: null,
            Assets:
            [
                new(@"C:\out\audio.flac", 0, 0, MediaAssetType.Audio, "eng", "English 5.1", IsDefault: true, IsForced: false),
                new(@"C:\out\subs.srt", 1, 0, MediaAssetType.Subtitle, "eng", null, IsDefault: false, IsForced: false),
            ],
            ChaptersFilePath: null);

        string args = FfmpegArgumentBuilder.BuildMuxArguments(request);

        Assert.Contains(@"""C:\out\audio.flac""", args);
        Assert.Contains(@"""C:\out\subs.srt""", args);
        Assert.Contains("-c copy", args);
        Assert.Contains(@"""C:\out\output.mkv""", args);
        Assert.Contains("-map_chapters -1", args);
        Assert.Contains("language=eng", args);
    }

    [Fact]
    public void BuildMuxArguments_WithDestinationMaster_MapsItFirst()
    {
        MuxRequest request = new(
            OutputPath: @"C:\out\output.mkv",
            DestinationMasterPath: @"C:\in\dest.mkv",
            Assets:
            [
                new(@"C:\out\audio.flac", 1, 0, MediaAssetType.Audio, "jpn", null, IsDefault: false, IsForced: false),
            ],
            ChaptersFilePath: null);

        string args = FfmpegArgumentBuilder.BuildMuxArguments(request);

        Assert.Contains(@"""C:\in\dest.mkv""", args);
        Assert.Contains("-map 0", args);
        Assert.Contains("-map 1:0", args);
    }

    [Fact]
    public void BuildMuxArguments_WithChapters_AddsMapChapters()
    {
        MuxRequest request = new(
            OutputPath: @"C:\out\output.mkv",
            DestinationMasterPath: null,
            Assets:
            [
                new(@"C:\out\audio.flac", 1, 0, MediaAssetType.Audio, null, null, IsDefault: false, IsForced: false),
            ],
            ChaptersFilePath: @"C:\out\chapters.txt");

        string args = FfmpegArgumentBuilder.BuildMuxArguments(request);

        Assert.Contains(@"""C:\out\chapters.txt""", args);
        Assert.Contains("-map_chapters 0", args);
        Assert.DoesNotContain("-map_chapters -1", args);
    }

    [Fact]
    public void BuildMuxArguments_WithDestinationAndChapters_CorrectInputOrder()
    {
        MuxRequest request = new(
            OutputPath: @"C:\out\output.mkv",
            DestinationMasterPath: @"C:\in\dest.mkv",
            Assets:
            [
                new(@"C:\out\audio.flac", 2, 0, MediaAssetType.Audio, null, null, IsDefault: false, IsForced: false),
            ],
            ChaptersFilePath: @"C:\out\chapters.txt");

        string args = FfmpegArgumentBuilder.BuildMuxArguments(request);

        Assert.Contains(@"-i ""C:\in\dest.mkv""", args);
        Assert.Contains(@"-i ""C:\out\chapters.txt""", args);
        Assert.Contains(@"-i ""C:\out\audio.flac""", args);
        Assert.Contains("-map 0", args);
        Assert.Contains("-map 2:0", args);
        Assert.Contains("-map_chapters 1", args);
    }

    [Fact]
    public void BuildMuxArguments_WithDefaultDisposition_SetsDisposition()
    {
        MuxRequest request = new(
            OutputPath: @"C:\out\output.mkv",
            DestinationMasterPath: null,
            Assets:
            [
                new(@"C:\out\audio.flac", 0, 0, MediaAssetType.Audio, null, null, IsDefault: true, IsForced: false),
            ],
            ChaptersFilePath: null);

        string args = FfmpegArgumentBuilder.BuildMuxArguments(request);

        Assert.Contains("-disposition:", args);
        Assert.Contains("default", args);
    }

    [Fact]
    public void BuildMuxArguments_TitleWithDoubleQuote_EscapesInnerQuote()
    {
        MuxRequest request = new(
            OutputPath: @"C:\out\output.mkv",
            DestinationMasterPath: null,
            Assets:
            [
                new(@"C:\out\audio.flac", 0, 0, MediaAssetType.Audio, null, @"My ""Best"" Track", IsDefault: false, IsForced: false),
            ],
            ChaptersFilePath: null);

        string args = FfmpegArgumentBuilder.BuildMuxArguments(request);

        // Inner quotes are backslash-escaped so they can't terminate the argument early.
        Assert.Contains(@"\""Best\""", args);
    }

    [Fact]
    public void BuildMuxArguments_WithNoAssets_ProducesMinimalCommand()
    {
        MuxRequest request = new(
            OutputPath: @"C:\out\output.mkv",
            DestinationMasterPath: @"C:\in\dest.mkv",
            Assets: [],
            ChaptersFilePath: null);

        string args = FfmpegArgumentBuilder.BuildMuxArguments(request);

        Assert.Contains("-map 0", args);
        Assert.Contains("-c copy", args);
    }
}
