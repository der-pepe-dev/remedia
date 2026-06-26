namespace ReMedia.Core.Tests;

using ReMedia.Core.Models;
using ReMedia.Core.Services;

public sealed class VttWriterTests
{
    [Fact]
    public void Write_ProducesValidVtt()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4), "Hello"),
            new(2, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(8), "World"),
        ];

        string output = VttWriter.Write(cues);

        Assert.StartsWith("WEBVTT", output);
        Assert.Contains("00:00:01.000 --> 00:00:04.000", output);
        Assert.Contains("Hello", output);
        Assert.Contains("00:00:05.000 --> 00:00:08.000", output);
    }

    [Fact]
    public void Write_UsesPeriodNotComma()
    {
        List<SubtitleCue> cues =
        [
            new(1, new TimeSpan(0, 0, 1, 23, 456), new TimeSpan(0, 0, 2, 34, 789), "Test"),
        ];

        string output = VttWriter.Write(cues);

        Assert.Contains("00:01:23.456 --> 00:02:34.789", output);
        Assert.DoesNotContain(",", output.Replace("WEBVTT", ""));
    }

    [Fact]
    public void Write_EmptyList_HasHeaderOnly()
    {
        string output = VttWriter.Write([]);

        Assert.StartsWith("WEBVTT", output);
    }

    [Fact]
    public void RoundTrip_ParseThenWrite_PreservesContent()
    {
        string original = "WEBVTT\n\n1\n00:00:01.000 --> 00:00:04.000\nHello\n\n2\n00:00:05.000 --> 00:00:08.500\nWorld\n";

        IReadOnlyList<SubtitleCue> cues = VttParser.Parse(original);
        string roundTripped = VttWriter.Write(cues);
        IReadOnlyList<SubtitleCue> reparsed = VttParser.Parse(roundTripped);

        Assert.Equal(cues.Count, reparsed.Count);
        for (int i = 0; i < cues.Count; i++)
        {
            Assert.Equal(cues[i].Start, reparsed[i].Start);
            Assert.Equal(cues[i].End, reparsed[i].End);
            Assert.Equal(cues[i].Text, reparsed[i].Text);
        }
    }

    [Fact]
    public void RoundTrip_RetimeVtt()
    {
        string original = "WEBVTT\n\n1\n00:00:10.000 --> 00:00:14.000\nHello\n";

        IReadOnlyList<SubtitleCue> cues = VttParser.Parse(original);
        decimal stretch = 25m / (24000m / 1001m);
        IReadOnlyList<SubtitleCue> retimed = SubtitleRetimingService.Retime(cues, stretch);
        string output = VttWriter.Write(retimed);
        IReadOnlyList<SubtitleCue> reparsed = VttParser.Parse(output);

        Assert.Single(reparsed);
        Assert.True(reparsed[0].Start > TimeSpan.FromSeconds(10));
    }
}
