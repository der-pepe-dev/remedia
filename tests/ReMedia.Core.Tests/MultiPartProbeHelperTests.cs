namespace ReMedia.Core.Tests;

using ReMedia.Core.Interfaces;
using ReMedia.Core.Models;
using ReMedia.Core.Services;

public sealed class MultiPartProbeHelperTests
{
    [Fact]
    public async Task ProbeAllAsync_SinglePart_ReturnsUnmodified()
    {
        FakeProbeService probe = new(
        [
            CreateProbeResult("part1.mkv", TimeSpan.FromMinutes(60),
            [
                new(0, TimeSpan.Zero, TimeSpan.FromMinutes(30), "Chapter 1"),
                new(1, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(60), "Chapter 2"),
            ]),
        ]);

        MultiPartProbeResult result = await MultiPartProbeHelper.ProbeAllAsync(probe, ["part1.mkv"], TestContext.Current.CancellationToken);

        Assert.Equal(TimeSpan.FromMinutes(60), result.Combined.Duration);
        Assert.Equal(2, result.Combined.Chapters.Count);
        Assert.Single(result.PartDurations);
        Assert.Equal(TimeSpan.FromMinutes(60), result.PartDurations[0]);
    }

    [Fact]
    public async Task ProbeAllAsync_TwoParts_CombinesDuration()
    {
        FakeProbeService probe = new(
        [
            CreateProbeResult("part1.mkv", TimeSpan.FromMinutes(60), []),
            CreateProbeResult("part2.mkv", TimeSpan.FromMinutes(45), []),
        ]);

        MultiPartProbeResult result = await MultiPartProbeHelper.ProbeAllAsync(probe, ["part1.mkv", "part2.mkv"], TestContext.Current.CancellationToken);

        Assert.Equal(TimeSpan.FromMinutes(105), result.Combined.Duration);
        Assert.Equal(2, result.PartDurations.Count);
    }

    [Fact]
    public async Task ProbeAllAsync_TwoParts_ShiftsChaptersFromPart2()
    {
        FakeProbeService probe = new(
        [
            CreateProbeResult("part1.mkv", TimeSpan.FromMinutes(60),
            [
                new(0, TimeSpan.Zero, TimeSpan.FromMinutes(30), "P1-Ch1"),
                new(1, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(60), "P1-Ch2"),
            ]),
            CreateProbeResult("part2.mkv", TimeSpan.FromMinutes(45),
            [
                new(0, TimeSpan.Zero, TimeSpan.FromMinutes(20), "P2-Ch1"),
                new(1, TimeSpan.FromMinutes(20), TimeSpan.FromMinutes(45), "P2-Ch2"),
            ]),
        ]);

        MultiPartProbeResult result = await MultiPartProbeHelper.ProbeAllAsync(probe, ["part1.mkv", "part2.mkv"], TestContext.Current.CancellationToken);

        Assert.Equal(4, result.Combined.Chapters.Count);

        // Part 1 chapters: unchanged
        Assert.Equal(TimeSpan.Zero, result.Combined.Chapters[0].Start);
        Assert.Equal(TimeSpan.FromMinutes(30), result.Combined.Chapters[0].End);
        Assert.Equal("P1-Ch1", result.Combined.Chapters[0].Title);

        Assert.Equal(TimeSpan.FromMinutes(30), result.Combined.Chapters[1].Start);
        Assert.Equal(TimeSpan.FromMinutes(60), result.Combined.Chapters[1].End);

        // Part 2 chapters: shifted by 60 minutes
        Assert.Equal(TimeSpan.FromMinutes(60), result.Combined.Chapters[2].Start);
        Assert.Equal(TimeSpan.FromMinutes(80), result.Combined.Chapters[2].End);
        Assert.Equal("P2-Ch1", result.Combined.Chapters[2].Title);

        Assert.Equal(TimeSpan.FromMinutes(80), result.Combined.Chapters[3].Start);
        Assert.Equal(TimeSpan.FromMinutes(105), result.Combined.Chapters[3].End);
    }

    [Fact]
    public async Task ProbeAllAsync_ThreeParts_ShiftsCorrectly()
    {
        FakeProbeService probe = new(
        [
            CreateProbeResult("p1.mkv", TimeSpan.FromMinutes(30),
                [new(0, TimeSpan.Zero, TimeSpan.FromMinutes(30), "A")]),
            CreateProbeResult("p2.mkv", TimeSpan.FromMinutes(20),
                [new(0, TimeSpan.Zero, TimeSpan.FromMinutes(20), "B")]),
            CreateProbeResult("p3.mkv", TimeSpan.FromMinutes(10),
                [new(0, TimeSpan.Zero, TimeSpan.FromMinutes(10), "C")]),
        ]);

        MultiPartProbeResult result = await MultiPartProbeHelper.ProbeAllAsync(
            probe, ["p1.mkv", "p2.mkv", "p3.mkv"], TestContext.Current.CancellationToken);

        Assert.Equal(TimeSpan.FromMinutes(60), result.Combined.Duration);
        Assert.Equal(3, result.Combined.Chapters.Count);

        Assert.Equal(TimeSpan.Zero, result.Combined.Chapters[0].Start);             // Part 1: 0
        Assert.Equal(TimeSpan.FromMinutes(30), result.Combined.Chapters[1].Start);   // Part 2: offset by 30
        Assert.Equal(TimeSpan.FromMinutes(50), result.Combined.Chapters[2].Start);   // Part 3: offset by 50
    }

    [Fact]
    public async Task ProbeAllAsync_ChapterIdsAreSequential()
    {
        FakeProbeService probe = new(
        [
            CreateProbeResult("p1.mkv", TimeSpan.FromMinutes(30),
                [new(0, TimeSpan.Zero, TimeSpan.FromMinutes(30), "A")]),
            CreateProbeResult("p2.mkv", TimeSpan.FromMinutes(30),
                [new(0, TimeSpan.Zero, TimeSpan.FromMinutes(30), "B")]),
        ]);

        MultiPartProbeResult result = await MultiPartProbeHelper.ProbeAllAsync(probe, ["p1.mkv", "p2.mkv"], TestContext.Current.CancellationToken);

        Assert.Equal(0, result.Combined.Chapters[0].Id);
        Assert.Equal(1, result.Combined.Chapters[1].Id);
    }

    [Fact]
    public async Task ProbeAllAsync_Part2HasNoChapters_OnlyPart1Chapters()
    {
        FakeProbeService probe = new(
        [
            CreateProbeResult("p1.mkv", TimeSpan.FromMinutes(60),
                [new(0, TimeSpan.Zero, TimeSpan.FromMinutes(60), "Intro")]),
            CreateProbeResult("p2.mkv", TimeSpan.FromMinutes(40), []),
        ]);

        MultiPartProbeResult result = await MultiPartProbeHelper.ProbeAllAsync(probe, ["p1.mkv", "p2.mkv"], TestContext.Current.CancellationToken);

        Assert.Equal(TimeSpan.FromMinutes(100), result.Combined.Duration);
        Assert.Single(result.Combined.Chapters);
        Assert.Equal("Intro", result.Combined.Chapters[0].Title);
    }

    [Fact]
    public void ShiftChapters_AppliesOffset()
    {
        List<MediaChapterInfo> chapters =
        [
            new(0, TimeSpan.Zero, TimeSpan.FromMinutes(10), "Ch1"),
            new(1, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(20), "Ch2"),
        ];

        IReadOnlyList<MediaChapterInfo> shifted = MultiPartProbeHelper.ShiftChapters(
            chapters, TimeSpan.FromMinutes(60), startId: 5);

        Assert.Equal(5, shifted[0].Id);
        Assert.Equal(TimeSpan.FromMinutes(60), shifted[0].Start);
        Assert.Equal(TimeSpan.FromMinutes(70), shifted[0].End);

        Assert.Equal(6, shifted[1].Id);
        Assert.Equal(TimeSpan.FromMinutes(70), shifted[1].Start);
        Assert.Equal(TimeSpan.FromMinutes(80), shifted[1].End);
    }

    [Fact]
    public async Task ProbeAllAsync_EmptyPaths_Throws()
    {
        FakeProbeService probe = new([]);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            MultiPartProbeHelper.ProbeAllAsync(probe, [], TestContext.Current.CancellationToken));
    }

    private static MediaProbeResult CreateProbeResult(
        string inputPath, TimeSpan duration, IReadOnlyList<MediaChapterInfo> chapters)
    {
        return new MediaProbeResult(inputPath, "matroska", "Matroska", duration, null, [], chapters);
    }

    private sealed class FakeProbeService : IMediaProbeService
    {
        private readonly Dictionary<string, MediaProbeResult> _results;

        public FakeProbeService(IReadOnlyList<MediaProbeResult> results)
        {
            _results = results.ToDictionary(r => r.InputPath);
        }

        public Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_results[inputPath]);
        }
    }
}
