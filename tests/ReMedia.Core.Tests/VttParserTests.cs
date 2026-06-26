namespace ReMedia.Core.Tests;

using ReMedia.Core.Models;
using ReMedia.Core.Services;

public sealed class VttParserTests
{
    [Fact]
    public void Parse_BasicVtt_ReturnsCues()
    {
        string input = """
            WEBVTT

            1
            00:00:01.000 --> 00:00:04.000
            Hello, world!

            2
            00:00:05.000 --> 00:00:08.000
            Second subtitle.
            """;

        IReadOnlyList<SubtitleCue> cues = VttParser.Parse(input);

        Assert.Equal(2, cues.Count);
        Assert.Equal(1, cues[0].Index);
        Assert.Equal(TimeSpan.FromSeconds(1), cues[0].Start);
        Assert.Equal(TimeSpan.FromSeconds(4), cues[0].End);
        Assert.Equal("Hello, world!", cues[0].Text);
    }

    [Fact]
    public void Parse_WithoutCueIds_AssignsSequentialIndexes()
    {
        string input = "WEBVTT\n\n00:00:01.000 --> 00:00:02.000\nFirst\n\n00:00:03.000 --> 00:00:04.000\nSecond\n";

        IReadOnlyList<SubtitleCue> cues = VttParser.Parse(input);

        Assert.Equal(2, cues.Count);
        Assert.Equal(1, cues[0].Index);
        Assert.Equal(2, cues[1].Index);
    }

    [Fact]
    public void Parse_ShortTimestamp_WithoutHours()
    {
        string input = "WEBVTT\n\n01:23.456 --> 02:34.789\nShort\n";

        IReadOnlyList<SubtitleCue> cues = VttParser.Parse(input);

        Assert.Single(cues);
        Assert.Equal(new TimeSpan(0, 0, 1, 23, 456), cues[0].Start);
        Assert.Equal(new TimeSpan(0, 0, 2, 34, 789), cues[0].End);
    }

    [Fact]
    public void Parse_WithCueSettings_IgnoresSettings()
    {
        string input = "WEBVTT\n\n00:00:01.000 --> 00:00:04.000 position:10% align:start\nStyled cue\n";

        IReadOnlyList<SubtitleCue> cues = VttParser.Parse(input);

        Assert.Single(cues);
        Assert.Equal(TimeSpan.FromSeconds(4), cues[0].End);
        Assert.Equal("Styled cue", cues[0].Text);
    }

    [Fact]
    public void Parse_WithBom_IgnoresBom()
    {
        string input = "\uFEFFWEBVTT\n\n00:00:01.000 --> 00:00:02.000\nTest\n";

        IReadOnlyList<SubtitleCue> cues = VttParser.Parse(input);

        Assert.Single(cues);
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(VttParser.Parse(""));
        Assert.Empty(VttParser.Parse("   "));
    }

    [Fact]
    public void Parse_MultiLineText_PreservesLineBreaks()
    {
        string input = "WEBVTT\n\n00:00:01.000 --> 00:00:04.000\nLine one\nLine two\n";

        IReadOnlyList<SubtitleCue> cues = VttParser.Parse(input);

        Assert.Single(cues);
        Assert.Contains("Line one", cues[0].Text);
        Assert.Contains("Line two", cues[0].Text);
    }

    [Fact]
    public void Parse_HeaderWithMetadata_SkipsHeader()
    {
        string input = "WEBVTT\nKind: captions\nLanguage: en\n\n00:00:01.000 --> 00:00:02.000\nHello\n";

        IReadOnlyList<SubtitleCue> cues = VttParser.Parse(input);

        Assert.Single(cues);
        Assert.Equal("Hello", cues[0].Text);
    }

    [Theory]
    [InlineData("00:00:01.5", 1, 500)]
    [InlineData("00:00:01.50", 1, 500)]
    [InlineData("00:00:01.500", 1, 500)]
    [InlineData("00:00:01.05", 1, 50)]
    public void Parse_FractionalSeconds_ScaledToMilliseconds(string startStamp, int expectSeconds, int expectMs)
    {
        string input = $"WEBVTT\n\n{startStamp} --> 00:00:09.000\nHi\n";

        IReadOnlyList<SubtitleCue> cues = VttParser.Parse(input);

        Assert.Single(cues);
        Assert.Equal(new TimeSpan(0, 0, 0, expectSeconds, expectMs), cues[0].Start);
    }

    [Fact]
    public void Parse_TooManyFractionalDigits_DropsCueAndWarns()
    {
        // ".5000" has 4 fractional digits — rejected rather than silently mis-scaled.
        string input = "WEBVTT\n\n00:00:01.5000 --> 00:00:02.000\nBad\n";

        IReadOnlyList<SubtitleCue> cues = VttParser.Parse(input, out IReadOnlyList<string> warnings);

        Assert.Empty(cues);
        Assert.NotEmpty(warnings);
    }

    [Fact]
    public void Parse_UnparseableTimingLine_ReportsWarningAndKeepsValidCue()
    {
        string input = "WEBVTT\n\n00:00:0x.000 --> 00:00:02.000\nBad\n\n00:00:03.000 --> 00:00:04.000\nGood\n";

        IReadOnlyList<SubtitleCue> cues = VttParser.Parse(input, out IReadOnlyList<string> warnings);

        Assert.Single(cues);
        Assert.Equal("Good", cues[0].Text);
        Assert.NotEmpty(warnings);
    }
}
