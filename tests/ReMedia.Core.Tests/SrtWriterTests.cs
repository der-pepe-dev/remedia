namespace ReMedia.Core.Tests;

using ReMedia.Core.Models;
using ReMedia.Core.Services;

public sealed class SrtWriterTests
{
    [Fact]
    public void Write_ProducesValidSrt()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4), "Hello"),
            new(2, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(8), "World"),
        ];

        string output = SrtWriter.Write(cues);

        Assert.Contains("1", output);
        Assert.Contains("00:00:01,000 --> 00:00:04,000", output);
        Assert.Contains("Hello", output);
        Assert.Contains("2", output);
        Assert.Contains("00:00:05,000 --> 00:00:08,000", output);
        Assert.Contains("World", output);
    }

    [Fact]
    public void Write_PreservesMultiLineText()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4), "Line 1\nLine 2"),
        ];

        string output = SrtWriter.Write(cues);

        Assert.Contains("Line 1\nLine 2", output);
    }

    [Fact]
    public void Write_FormatsMillisecondsWithComma()
    {
        List<SubtitleCue> cues =
        [
            new(1, new TimeSpan(0, 0, 1, 23, 456), new TimeSpan(0, 0, 2, 34, 789), "Test"),
        ];

        string output = SrtWriter.Write(cues);

        Assert.Contains("00:01:23,456 --> 00:02:34,789", output);
    }

    [Fact]
    public void Write_EmptyList_ReturnsEmpty()
    {
        string output = SrtWriter.Write([]);

        Assert.Equal(string.Empty, output);
    }

    [Fact]
    public void RoundTrip_ParseThenWrite_PreservesContent()
    {
        string original = "1\r\n00:00:01,000 --> 00:00:04,000\r\nHello, world!\r\n\r\n2\r\n00:00:05,000 --> 00:00:08,500\r\nSecond line\r\n";

        IReadOnlyList<SubtitleCue> cues = SrtParser.Parse(original);
        string roundTripped = SrtWriter.Write(cues);
        IReadOnlyList<SubtitleCue> reparsed = SrtParser.Parse(roundTripped);

        Assert.Equal(cues.Count, reparsed.Count);
        for (int i = 0; i < cues.Count; i++)
        {
            Assert.Equal(cues[i].Index, reparsed[i].Index);
            Assert.Equal(cues[i].Start, reparsed[i].Start);
            Assert.Equal(cues[i].End, reparsed[i].End);
            Assert.Equal(cues[i].Text, reparsed[i].Text);
        }
    }

    [Fact]
    public void Write_HoursOverTwentyFour_FormatsCorrectly()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.FromHours(25), TimeSpan.FromHours(25) + TimeSpan.FromSeconds(5), "Late"),
        ];

        string output = SrtWriter.Write(cues);

        Assert.Contains("25:00:00,000 --> 25:00:05,000", output);
    }
}
