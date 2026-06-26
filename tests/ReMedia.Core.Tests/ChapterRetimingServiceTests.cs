namespace ReMedia.Core.Tests;

using ReMedia.Core.Models;
using ReMedia.Core.Services;

public sealed class ChapterRetimingServiceTests
{
    [Fact]
    public void Retime_AppliesStretchFactorToStartAndEnd()
    {
        List<MediaChapterInfo> chapters =
        [
            new(0, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(300), "Chapter 1"),
            new(1, TimeSpan.FromSeconds(300), TimeSpan.FromSeconds(600), "Chapter 2"),
        ];

        decimal stretchFactor = 25m / (24000m / 1001m);
        IReadOnlyList<MediaChapterInfo> retimed = ChapterRetimingService.Retime(chapters, stretchFactor);

        Assert.Equal(2, retimed.Count);
        Assert.True(retimed[0].End > TimeSpan.FromSeconds(300));
        Assert.True(retimed[1].End > TimeSpan.FromSeconds(600));
        Assert.Equal("Chapter 1", retimed[0].Title);
        Assert.Equal("Chapter 2", retimed[1].Title);
    }

    [Fact]
    public void Retime_WithStretchFactorOne_ReturnsSameChapters()
    {
        List<MediaChapterInfo> chapters =
        [
            new(0, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20), "Ch"),
        ];

        IReadOnlyList<MediaChapterInfo> retimed = ChapterRetimingService.Retime(chapters, 1m);

        Assert.Same(chapters, retimed);
    }

    [Fact]
    public void Retime_PreservesChapterIds()
    {
        List<MediaChapterInfo> chapters =
        [
            new(42, TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(100), "Test"),
        ];

        IReadOnlyList<MediaChapterInfo> retimed = ChapterRetimingService.Retime(chapters, 1.5m);

        Assert.Equal(42, retimed[0].Id);
    }

    [Fact]
    public void Retime_WithZeroStretchFactor_Throws()
    {
        List<MediaChapterInfo> chapters = [new(0, TimeSpan.Zero, TimeSpan.FromSeconds(1), "Ch")];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChapterRetimingService.Retime(chapters, 0m));
    }

    [Fact]
    public void Retime_WithEmptyList_ReturnsEmpty()
    {
        IReadOnlyList<MediaChapterInfo> retimed = ChapterRetimingService.Retime([], 1.5m);

        Assert.Empty(retimed);
    }

    [Fact]
    public void Retime_PalToNtsc_ProducesLongerDurations()
    {
        decimal stretchFactor = 25m / (24000m / 1001m);

        List<MediaChapterInfo> chapters =
        [
            new(0, TimeSpan.Zero, TimeSpan.FromMinutes(90), "Feature"),
        ];

        IReadOnlyList<MediaChapterInfo> retimed = ChapterRetimingService.Retime(chapters, stretchFactor);

        Assert.True(retimed[0].End > TimeSpan.FromMinutes(90));
        Assert.Equal(TimeSpan.Zero, retimed[0].Start);
    }
}
