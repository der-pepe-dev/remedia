namespace ReMedia.Core.Tests;

using ReMedia.Core.Models;
using ReMedia.Core.Services;

public sealed class FfmetadataWriterTests
{
    [Fact]
    public void WriteChapters_ProducesValidFfmetadataHeader()
    {
        List<MediaChapterInfo> chapters =
        [
            new(0, TimeSpan.Zero, TimeSpan.FromSeconds(60), "Intro"),
        ];

        string output = FfmetadataWriter.WriteChapters(chapters);

        Assert.StartsWith(";FFMETADATA1", output);
    }

    [Fact]
    public void WriteChapters_ContainsChapterSection()
    {
        List<MediaChapterInfo> chapters =
        [
            new(0, TimeSpan.Zero, TimeSpan.FromSeconds(60), "Intro"),
        ];

        string output = FfmetadataWriter.WriteChapters(chapters);

        Assert.Contains("[CHAPTER]", output);
        Assert.Contains("TIMEBASE=1/1000", output);
        Assert.Contains("START=0", output);
        Assert.Contains("END=60000", output);
        Assert.Contains("title=Intro", output);
    }

    [Fact]
    public void WriteChapters_WithMultipleChapters_WritesAll()
    {
        List<MediaChapterInfo> chapters =
        [
            new(0, TimeSpan.Zero, TimeSpan.FromSeconds(30), "One"),
            new(1, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60), "Two"),
            new(2, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(90), "Three"),
        ];

        string output = FfmetadataWriter.WriteChapters(chapters);

        int chapterCount = output.Split("[CHAPTER]").Length - 1;
        Assert.Equal(3, chapterCount);
        Assert.Contains("title=One", output);
        Assert.Contains("title=Two", output);
        Assert.Contains("title=Three", output);
    }

    [Fact]
    public void WriteChapters_WithNullTitle_OmitsTitleLine()
    {
        List<MediaChapterInfo> chapters =
        [
            new(0, TimeSpan.Zero, TimeSpan.FromSeconds(10), null),
        ];

        string output = FfmetadataWriter.WriteChapters(chapters);

        Assert.DoesNotContain("title=", output);
    }

    [Fact]
    public void WriteChapters_EscapesSpecialCharacters()
    {
        List<MediaChapterInfo> chapters =
        [
            new(0, TimeSpan.Zero, TimeSpan.FromSeconds(10), "Test=Value;Special#Chars"),
        ];

        string output = FfmetadataWriter.WriteChapters(chapters);

        Assert.Contains(@"title=Test\=Value\;Special\#Chars", output);
    }

    [Fact]
    public void WriteChapters_WithEmptyList_WritesHeaderOnly()
    {
        string output = FfmetadataWriter.WriteChapters([]);

        Assert.StartsWith(";FFMETADATA1", output);
        Assert.DoesNotContain("[CHAPTER]", output);
    }

    [Fact]
    public void WriteChapters_WithFractionalMilliseconds_TruncatesToWholeMs()
    {
        List<MediaChapterInfo> chapters =
        [
            new(0, TimeSpan.FromTicks(5001), TimeSpan.FromTicks(TimeSpan.TicksPerSecond + 5001), null),
        ];

        string output = FfmetadataWriter.WriteChapters(chapters);

        Assert.Contains("START=0", output);
        Assert.Contains("END=1000", output);
    }
}
