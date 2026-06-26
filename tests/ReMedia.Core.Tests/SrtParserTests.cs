namespace ReMedia.Core.Tests;

using ReMedia.Core.Models;
using ReMedia.Core.Services;

public sealed class SrtParserTests
{
    [Fact]
    public void Parse_BasicSrt_ReturnsCues()
    {
        string input = """
            1
            00:00:01,000 --> 00:00:04,000
            Hello, world!

            2
            00:00:05,000 --> 00:00:08,000
            Second subtitle.

            """;

        IReadOnlyList<SubtitleCue> cues = SrtParser.Parse(input);

        Assert.Equal(2, cues.Count);
        Assert.Equal(1, cues[0].Index);
        Assert.Equal(TimeSpan.FromSeconds(1), cues[0].Start);
        Assert.Equal(TimeSpan.FromSeconds(4), cues[0].End);
        Assert.Equal("Hello, world!", cues[0].Text);
        Assert.Equal(2, cues[1].Index);
        Assert.Equal("Second subtitle.", cues[1].Text);
    }

    [Fact]
    public void Parse_MultiLineText_PreservesLineBreaks()
    {
        string input = """
            1
            00:00:01,000 --> 00:00:04,000
            Line one
            Line two

            """;

        IReadOnlyList<SubtitleCue> cues = SrtParser.Parse(input);

        Assert.Single(cues);
        Assert.Contains("Line one", cues[0].Text);
        Assert.Contains("Line two", cues[0].Text);
        Assert.Contains(Environment.NewLine, cues[0].Text);
    }

    [Fact]
    public void Parse_WithBom_IgnoresBom()
    {
        string input = "\uFEFF1\n00:00:01,000 --> 00:00:02,000\nTest\n";

        IReadOnlyList<SubtitleCue> cues = SrtParser.Parse(input);

        Assert.Single(cues);
        Assert.Equal(1, cues[0].Index);
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(SrtParser.Parse(""));
        Assert.Empty(SrtParser.Parse("   "));
    }

    [Fact]
    public void Parse_WindowsLineEndings_Works()
    {
        string input = "1\r\n00:00:01,000 --> 00:00:02,000\r\nHello\r\n\r\n";

        IReadOnlyList<SubtitleCue> cues = SrtParser.Parse(input);

        Assert.Single(cues);
        Assert.Equal("Hello", cues[0].Text);
    }

    [Fact]
    public void Parse_ExtraBlankLinesBetweenCues_Works()
    {
        string input = """
            1
            00:00:01,000 --> 00:00:02,000
            First



            2
            00:00:03,000 --> 00:00:04,000
            Second

            """;

        IReadOnlyList<SubtitleCue> cues = SrtParser.Parse(input);

        Assert.Equal(2, cues.Count);
    }

    [Fact]
    public void Parse_MillisecondPrecision_PreservedExactly()
    {
        string input = "1\n00:01:23,456 --> 00:02:34,789\nText\n";

        IReadOnlyList<SubtitleCue> cues = SrtParser.Parse(input);

        Assert.Single(cues);
        Assert.Equal(new TimeSpan(0, 0, 1, 23, 456), cues[0].Start);
        Assert.Equal(new TimeSpan(0, 0, 2, 34, 789), cues[0].End);
    }

    [Fact]
    public void Parse_HoursOverTwentyFour_Works()
    {
        string input = "1\n25:00:00,000 --> 25:00:05,000\nLong video\n";

        IReadOnlyList<SubtitleCue> cues = SrtParser.Parse(input);

        Assert.Single(cues);
        Assert.Equal(TimeSpan.FromHours(25), cues[0].Start);
    }
}
