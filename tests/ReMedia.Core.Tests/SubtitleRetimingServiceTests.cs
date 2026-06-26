namespace ReMedia.Core.Tests;

using ReMedia.Core.Models;
using ReMedia.Core.Services;

public sealed class SubtitleRetimingServiceTests
{
    [Fact]
    public void Retime_AppliesStretchFactorToStartAndEnd()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(14), "Hello"),
            new(2, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(24), "World"),
        ];

        decimal stretchFactor = 25m / (24000m / 1001m);
        IReadOnlyList<SubtitleCue> retimed = SubtitleRetimingService.Retime(cues, stretchFactor);

        Assert.Equal(2, retimed.Count);
        Assert.True(retimed[0].Start > TimeSpan.FromSeconds(10));
        Assert.True(retimed[0].End > TimeSpan.FromSeconds(14));
        Assert.Equal("Hello", retimed[0].Text);
        Assert.Equal(1, retimed[0].Index);
    }

    [Fact]
    public void Retime_WithStretchFactorOne_ReturnsSameList()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), "Same"),
        ];

        IReadOnlyList<SubtitleCue> retimed = SubtitleRetimingService.Retime(cues, 1m);

        Assert.Same(cues, retimed);
    }

    [Fact]
    public void Retime_WithZeroStretchFactor_Throws()
    {
        List<SubtitleCue> cues = [new(1, TimeSpan.Zero, TimeSpan.FromSeconds(1), "X")];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SubtitleRetimingService.Retime(cues, 0m));
    }

    [Fact]
    public void Retime_PreservesTextContent()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5), "Line 1\nLine 2"),
        ];

        IReadOnlyList<SubtitleCue> retimed = SubtitleRetimingService.Retime(cues, 1.5m);

        Assert.Equal("Line 1\nLine 2", retimed[0].Text);
    }

    [Fact]
    public void Retime_EmptyList_ReturnsEmpty()
    {
        IReadOnlyList<SubtitleCue> retimed = SubtitleRetimingService.Retime([], 1.5m);

        Assert.Empty(retimed);
    }

    [Fact]
    public void Retime_PalToNtsc_ProducesLongerTimestamps()
    {
        decimal stretchFactor = 25m / (24000m / 1001m);

        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.FromMinutes(60), TimeSpan.FromMinutes(60) + TimeSpan.FromSeconds(3), "Late cue"),
        ];

        IReadOnlyList<SubtitleCue> retimed = SubtitleRetimingService.Retime(cues, stretchFactor);

        Assert.True(retimed[0].Start > TimeSpan.FromMinutes(60));
    }

    [Fact]
    public void Retime_FullRoundTrip_ParseRetimeWrite()
    {
        string original = "1\n00:00:10,000 --> 00:00:14,000\nHello\n\n2\n00:00:20,000 --> 00:00:24,000\nWorld\n";

        IReadOnlyList<SubtitleCue> cues = SrtParser.Parse(original);
        decimal stretch = 25m / (24000m / 1001m);
        IReadOnlyList<SubtitleCue> retimed = SubtitleRetimingService.Retime(cues, stretch);
        string output = SrtWriter.Write(retimed);

        IReadOnlyList<SubtitleCue> reparsed = SrtParser.Parse(output);

        Assert.Equal(2, reparsed.Count);
        Assert.True(reparsed[0].Start > TimeSpan.FromSeconds(10));
        Assert.True(reparsed[1].Start > TimeSpan.FromSeconds(20));
        Assert.Equal("Hello", reparsed[0].Text);
        Assert.Equal("World", reparsed[1].Text);
    }
}
