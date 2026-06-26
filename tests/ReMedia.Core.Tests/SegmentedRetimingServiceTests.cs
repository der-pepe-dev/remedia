namespace ReMedia.Core.Tests;

using ReMedia.Core.Models;
using ReMedia.Core.Services;

public sealed class SegmentedRetimingServiceTests
{
    [Fact]
    public void RetimeSubtitles_SingleSegmentCoveringAll_BehavesLikeUniform()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(14), "Hello"),
            new(2, TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(24), "World"),
        ];

        decimal stretch = 25m / (24000m / 1001m);
        TimingSegment segment = new(TimeSpan.Zero, null, stretch);

        IReadOnlyList<SubtitleCue> segmented = SegmentedRetimingService.RetimeSubtitles(cues, [segment]);
        IReadOnlyList<SubtitleCue> uniform = SubtitleRetimingService.Retime(cues, stretch);

        Assert.Equal(uniform[0].Start, segmented[0].Start);
        Assert.Equal(uniform[0].End, segmented[0].End);
        Assert.Equal(uniform[1].Start, segmented[1].Start);
        Assert.Equal(uniform[1].End, segmented[1].End);
    }

    [Fact]
    public void RetimeSubtitles_TwoSegments_AppliesDifferentFactors()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(8), "In first segment"),
            new(2, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(18), "In second segment"),
        ];

        List<TimingSegment> segments =
        [
            new(TimeSpan.Zero, TimeSpan.FromSeconds(10), 2m),       // 2x stretch (0-10s)
            new(TimeSpan.FromSeconds(10), null, 1m),                 // 1x stretch (10s+)
        ];

        IReadOnlyList<SubtitleCue> result = SegmentedRetimingService.RetimeSubtitles(cues, segments);

        // Cue 1: start=5s in 2x segment -> 10s, end=8s in 2x segment -> 16s
        Assert.Equal(TimeSpan.FromSeconds(10), result[0].Start);
        Assert.Equal(TimeSpan.FromSeconds(16), result[0].End);

        // Cue 2: start=15s -> first 10s at 2x (=20s) + 5s at 1x (=5s) = 25s
        Assert.Equal(TimeSpan.FromSeconds(25), result[1].Start);
    }

    [Fact]
    public void RetimeSubtitles_GapBetweenSegments_GapPassesThroughAt1x()
    {
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(6), "In gap"),
        ];

        List<TimingSegment> segments =
        [
            new(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20), 2m),
        ];

        IReadOnlyList<SubtitleCue> result = SegmentedRetimingService.RetimeSubtitles(cues, segments);

        // 5s is before the segment, so it passes through at 1x
        Assert.Equal(TimeSpan.FromSeconds(5), result[0].Start);
    }

    [Fact]
    public void RetimeChapters_AppliesSegments()
    {
        List<MediaChapterInfo> chapters =
        [
            new(0, TimeSpan.Zero, TimeSpan.FromSeconds(10), "Intro"),
            new(1, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), "Main"),
        ];

        List<TimingSegment> segments =
        [
            new(TimeSpan.Zero, null, 1.5m),
        ];

        IReadOnlyList<MediaChapterInfo> result = SegmentedRetimingService.RetimeChapters(chapters, segments);

        Assert.Equal(TimeSpan.Zero, result[0].Start);
        Assert.Equal(TimeSpan.FromSeconds(15), result[0].End);
        Assert.Equal(TimeSpan.FromSeconds(15), result[1].Start);
        Assert.Equal(TimeSpan.FromSeconds(45), result[1].End);
    }

    [Fact]
    public void RetimeSubtitles_EmptySegments_Throws()
    {
        List<SubtitleCue> cues = [new(1, TimeSpan.Zero, TimeSpan.FromSeconds(1), "X")];

        Assert.Throws<ArgumentException>(() =>
            SegmentedRetimingService.RetimeSubtitles(cues, []));
    }

    [Fact]
    public void RetimeSubtitles_InvalidStretchFactor_Throws()
    {
        List<SubtitleCue> cues = [new(1, TimeSpan.Zero, TimeSpan.FromSeconds(1), "X")];
        List<TimingSegment> segments = [new(TimeSpan.Zero, null, 0m)];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SegmentedRetimingService.RetimeSubtitles(cues, segments));
    }

    [Fact]
    public void RetimeSubtitles_UnsortedSegments_Throws()
    {
        List<SubtitleCue> cues = [new(1, TimeSpan.Zero, TimeSpan.FromSeconds(1), "X")];
        List<TimingSegment> segments =
        [
            new(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20), 1m),
            new(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 1m),
        ];

        Assert.Throws<ArgumentException>(() =>
            SegmentedRetimingService.RetimeSubtitles(cues, segments));
    }

    [Fact]
    public void RetimeSubtitles_CueSpanningTwoSegments_CorrectlySplitsCalculation()
    {
        // Cue from 5s to 15s, with segment boundary at 10s
        List<SubtitleCue> cues =
        [
            new(1, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15), "Spanning"),
        ];

        List<TimingSegment> segments =
        [
            new(TimeSpan.Zero, TimeSpan.FromSeconds(10), 2m),
            new(TimeSpan.FromSeconds(10), null, 0.5m),
        ];

        IReadOnlyList<SubtitleCue> result = SegmentedRetimingService.RetimeSubtitles(cues, segments);

        // Start: 5s in 2x segment -> 10s
        Assert.Equal(TimeSpan.FromSeconds(10), result[0].Start);

        // End: first 10s at 2x (=20s) + 5s at 0.5x (=2.5s) = 22.5s
        Assert.Equal(TimeSpan.FromSeconds(22.5), result[0].End);
    }

    [Fact]
    public void TimingSegment_FromFps_CalculatesCorrectStretch()
    {
        TimingSegment segment = TimingSegment.FromFps(TimeSpan.Zero, null, 25m, 24000m / 1001m);

        Assert.Equal(25m / (24000m / 1001m), segment.StretchFactor);
    }

    [Fact]
    public void TimingSegment_Contains_ReturnsCorrectly()
    {
        TimingSegment segment = new(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 1m);

        Assert.False(segment.Contains(TimeSpan.FromSeconds(4)));
        Assert.True(segment.Contains(TimeSpan.FromSeconds(5)));
        Assert.True(segment.Contains(TimeSpan.FromSeconds(7)));
        Assert.False(segment.Contains(TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public void TimingSegment_Contains_NullEnd_MatchesEverythingAfterStart()
    {
        TimingSegment segment = new(TimeSpan.FromSeconds(5), null, 1m);

        Assert.False(segment.Contains(TimeSpan.FromSeconds(4)));
        Assert.True(segment.Contains(TimeSpan.FromSeconds(5)));
        Assert.True(segment.Contains(TimeSpan.FromHours(100)));
    }
}
