namespace ReMedia.Core.Tests;

using ReMedia.Core.Interfaces;
using ReMedia.Core.Models;
using ReMedia.Core.Services;

public sealed class ExportWorkflowServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WithTracks_DelegatesAndReturnsResults()
    {
        FakeTrackExportService trackExport = new();
        FakeChapterExportService chapterExport = new();
        ExportWorkflowService service = new(trackExport, chapterExport);

        ExportWorkflowRequest request = new(
            InputPath: @"C:\in\movie.mkv",
            OutputFolder: Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            Tracks:
            [
                new ExportTrackSelection(1, MediaAssetType.Audio, "ac3", "copy", ".ac3", CopyStream: true),
                new ExportTrackSelection(2, MediaAssetType.Subtitle, "srt", "copy", ".srt", CopyStream: true),
            ],
            ExportChapters: false,
            Chapters: []);

        ExportWorkflowResult result = await service.ExecuteAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.TrackResults.Count);
        Assert.True(result.TrackResults.All(r => r.Succeeded));
        Assert.Null(result.ChapterResult);
        Assert.False(result.HasFailures);
    }

    [Fact]
    public async Task ExecuteAsync_WithChapters_ExportsChapters()
    {
        FakeTrackExportService trackExport = new();
        FakeChapterExportService chapterExport = new();
        ExportWorkflowService service = new(trackExport, chapterExport);

        ExportWorkflowRequest request = new(
            InputPath: @"C:\in\movie.mkv",
            OutputFolder: Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            Tracks: [],
            ExportChapters: true,
            Chapters: [new MediaChapterInfo(0, TimeSpan.Zero, TimeSpan.FromMinutes(5), "Ch1")]);

        ExportWorkflowResult result = await service.ExecuteAsync(request, TestContext.Current.CancellationToken);

        Assert.Empty(result.TrackResults);
        Assert.NotNull(result.ChapterResult);
        Assert.True(result.ChapterResult.Succeeded);
        Assert.False(result.HasFailures);
    }

    [Fact]
    public async Task ExecuteAsync_SkipsVideoTracks()
    {
        FakeTrackExportService trackExport = new();
        FakeChapterExportService chapterExport = new();
        ExportWorkflowService service = new(trackExport, chapterExport);

        ExportWorkflowRequest request = new(
            InputPath: @"C:\in\movie.mkv",
            OutputFolder: Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            Tracks:
            [
                new ExportTrackSelection(0, MediaAssetType.Video, "h264", "copy", ".mkv", CopyStream: true),
                new ExportTrackSelection(1, MediaAssetType.Audio, "ac3", "copy", ".ac3", CopyStream: true),
            ],
            ExportChapters: false,
            Chapters: []);

        ExportWorkflowResult result = await service.ExecuteAsync(request, TestContext.Current.CancellationToken);

        Assert.Single(result.TrackResults);
        Assert.Contains("stream 1", result.TrackResults[0].OperationName);
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedTrack_ReportsHasFailures()
    {
        FakeTrackExportService trackExport = new(simulateFailure: true);
        FakeChapterExportService chapterExport = new();
        ExportWorkflowService service = new(trackExport, chapterExport);

        ExportWorkflowRequest request = new(
            InputPath: @"C:\in\movie.mkv",
            OutputFolder: Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            Tracks: [new ExportTrackSelection(1, MediaAssetType.Audio, "ac3", "copy", ".ac3", CopyStream: true)],
            ExportChapters: false,
            Chapters: []);

        ExportWorkflowResult result = await service.ExecuteAsync(request, TestContext.Current.CancellationToken);

        Assert.True(result.HasFailures);
    }

    [Fact]
    public async Task ExecuteAsync_WithChaptersDisabled_SkipsChapterExport()
    {
        FakeTrackExportService trackExport = new();
        FakeChapterExportService chapterExport = new();
        ExportWorkflowService service = new(trackExport, chapterExport);

        ExportWorkflowRequest request = new(
            InputPath: @"C:\in\movie.mkv",
            OutputFolder: Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            Tracks: [],
            ExportChapters: false,
            Chapters: [new MediaChapterInfo(0, TimeSpan.Zero, TimeSpan.FromMinutes(5), "Ch1")]);

        ExportWorkflowResult result = await service.ExecuteAsync(request, TestContext.Current.CancellationToken);

        Assert.Null(result.ChapterResult);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoChaptersInFile_SkipsChapterExport()
    {
        FakeTrackExportService trackExport = new();
        FakeChapterExportService chapterExport = new();
        ExportWorkflowService service = new(trackExport, chapterExport);

        ExportWorkflowRequest request = new(
            InputPath: @"C:\in\movie.mkv",
            OutputFolder: Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            Tracks: [],
            ExportChapters: true,
            Chapters: []);

        ExportWorkflowResult result = await service.ExecuteAsync(request, TestContext.Current.CancellationToken);

        Assert.Null(result.ChapterResult);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimingConversion_SetsTimingOnAudioTracks()
    {
        CapturingTrackExportService trackExport = new();
        FakeChapterExportService chapterExport = new();
        ExportWorkflowService service = new(trackExport, chapterExport);

        ExportWorkflowRequest request = new(
            InputPath: @"C:\in\movie.mkv",
            OutputFolder: Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            Tracks:
            [
                new ExportTrackSelection(1, MediaAssetType.Audio, "ac3", "flac", ".flac", CopyStream: false),
                new ExportTrackSelection(2, MediaAssetType.Subtitle, "srt", "copy", ".srt", CopyStream: true),
            ],
            ExportChapters: false,
            Chapters: [],
            SourceFps: 25m,
            TargetFps: 24000m / 1001m);

        await service.ExecuteAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(2, trackExport.CapturedTracks.Count);

        TrackExportOptions audioTrack = trackExport.CapturedTracks.First(t => t.StreamIndex == 1);
        Assert.True(audioTrack.ApplyTimingConversion);
        Assert.Equal(25m, audioTrack.SourceFps);
        Assert.Equal(24000m / 1001m, audioTrack.TargetFps);

        TrackExportOptions subtitleTrack = trackExport.CapturedTracks.First(t => t.StreamIndex == 2);
        Assert.False(subtitleTrack.ApplyTimingConversion);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimingConversion_DoesNotApplyToAudioCopy()
    {
        CapturingTrackExportService trackExport = new();
        FakeChapterExportService chapterExport = new();
        ExportWorkflowService service = new(trackExport, chapterExport);

        ExportWorkflowRequest request = new(
            InputPath: @"C:\in\movie.mkv",
            OutputFolder: Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            Tracks: [new ExportTrackSelection(1, MediaAssetType.Audio, "ac3", "copy", ".ac3", CopyStream: true)],
            ExportChapters: false,
            Chapters: [],
            SourceFps: 25m,
            TargetFps: 24000m / 1001m);

        await service.ExecuteAsync(request, TestContext.Current.CancellationToken);

        TrackExportOptions audioTrack = trackExport.CapturedTracks.Single();
        Assert.False(audioTrack.ApplyTimingConversion);
        Assert.True(audioTrack.CopyStream);
    }

    [Fact]
    public async Task ExecuteAsync_WithSameFps_DoesNotApplyTiming()
    {
        CapturingTrackExportService trackExport = new();
        FakeChapterExportService chapterExport = new();
        ExportWorkflowService service = new(trackExport, chapterExport);

        ExportWorkflowRequest request = new(
            InputPath: @"C:\in\movie.mkv",
            OutputFolder: Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            Tracks: [new ExportTrackSelection(1, MediaAssetType.Audio, "ac3", "flac", ".flac", CopyStream: false)],
            ExportChapters: false,
            Chapters: [],
            SourceFps: 25m,
            TargetFps: 25m);

        await service.ExecuteAsync(request, TestContext.Current.CancellationToken);

        TrackExportOptions audioTrack = trackExport.CapturedTracks.Single();
        Assert.False(audioTrack.ApplyTimingConversion);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoFps_DoesNotApplyTiming()
    {
        CapturingTrackExportService trackExport = new();
        FakeChapterExportService chapterExport = new();
        ExportWorkflowService service = new(trackExport, chapterExport);

        ExportWorkflowRequest request = new(
            InputPath: @"C:\in\movie.mkv",
            OutputFolder: Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            Tracks: [new ExportTrackSelection(1, MediaAssetType.Audio, "ac3", "flac", ".flac", CopyStream: false)],
            ExportChapters: false,
            Chapters: []);

        await service.ExecuteAsync(request, TestContext.Current.CancellationToken);

        TrackExportOptions audioTrack = trackExport.CapturedTracks.Single();
        Assert.False(audioTrack.ApplyTimingConversion);
    }

    [Fact]
    public async Task ExecuteAsync_WithTimingAndChapters_WritesRetimedChapters()
    {
        FakeTrackExportService trackExport = new();
        FakeChapterExportService chapterExport = new();
        ExportWorkflowService service = new(trackExport, chapterExport);

        string outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        ExportWorkflowRequest request = new(
            InputPath: @"C:\in\movie.mkv",
            OutputFolder: outputFolder,
            Tracks: [],
            ExportChapters: true,
            Chapters:
            [
                new MediaChapterInfo(0, TimeSpan.Zero, TimeSpan.FromSeconds(300), "Chapter 1"),
                new MediaChapterInfo(1, TimeSpan.FromSeconds(300), TimeSpan.FromSeconds(600), "Chapter 2"),
            ],
            SourceFps: 25m,
            TargetFps: 24000m / 1001m);

        ExportWorkflowResult result = await service.ExecuteAsync(request, TestContext.Current.CancellationToken);

        Assert.NotNull(result.ChapterResult);
        Assert.True(result.ChapterResult.Succeeded);
        Assert.Contains("retimed", result.ChapterResult.OperationName);

        string chapterPath = Path.Combine(outputFolder, "chapters.txt");
        Assert.True(File.Exists(chapterPath));

        string content = await File.ReadAllTextAsync(chapterPath, TestContext.Current.CancellationToken);
        Assert.StartsWith(";FFMETADATA1", content);
        Assert.Contains("[CHAPTER]", content);
        Assert.Contains("title=Chapter 1", content);

        long endMs = (long)TimeSpan.FromSeconds(300).TotalMilliseconds;
        Assert.DoesNotContain($"END={endMs}", content);

        Directory.Delete(outputFolder, recursive: true);
    }

    [Fact]
    public async Task ExecuteAsync_WithChaptersButNoTiming_UsesOriginalExport()
    {
        FakeTrackExportService trackExport = new();
        FakeChapterExportService chapterExport = new();
        ExportWorkflowService service = new(trackExport, chapterExport);

        ExportWorkflowRequest request = new(
            InputPath: @"C:\in\movie.mkv",
            OutputFolder: Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            Tracks: [],
            ExportChapters: true,
            Chapters: [new MediaChapterInfo(0, TimeSpan.Zero, TimeSpan.FromSeconds(60), "Ch1")]);

        ExportWorkflowResult result = await service.ExecuteAsync(request, TestContext.Current.CancellationToken);

        Assert.NotNull(result.ChapterResult);
        Assert.True(result.ChapterResult.Succeeded);
        Assert.DoesNotContain("retimed", result.ChapterResult.OperationName);
    }

    [Fact]
    public async Task ExecuteAsync_WithMux_MapsAssetTypesToMatchingSelections()
    {
        string outputFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(outputFolder);

        WritingTrackExportService trackExport = new();
        FakeChapterExportService chapterExport = new();
        CapturingMuxService muxService = new();
        ExportWorkflowService service = new(trackExport, chapterExport, muxService: muxService);

        ExportWorkflowRequest request = new(
            InputPath: @"C:\in\movie.mkv",
            OutputFolder: outputFolder,
            Tracks:
            [
                new ExportTrackSelection(1, MediaAssetType.Audio, "ac3", "copy", ".ac3", CopyStream: true),
                new ExportTrackSelection(2, MediaAssetType.Subtitle, "srt", "copy", ".srt", CopyStream: true),
            ],
            ExportChapters: false,
            Chapters: [],
            MuxToMkv: true);

        await service.ExecuteAsync(request, TestContext.Current.CancellationToken);

        try
        {
            Assert.NotNull(muxService.LastRequest);
            Assert.Equal(2, muxService.LastRequest!.Assets.Count);
            MuxInputAsset audio = muxService.LastRequest.Assets.Single(a => a.FilePath.EndsWith(".ac3", StringComparison.Ordinal));
            MuxInputAsset subtitle = muxService.LastRequest.Assets.Single(a => a.FilePath.EndsWith(".srt", StringComparison.Ordinal));
            Assert.Equal(MediaAssetType.Audio, audio.AssetType);
            Assert.Equal(MediaAssetType.Subtitle, subtitle.AssetType);
        }
        finally
        {
            Directory.Delete(outputFolder, recursive: true);
        }
    }

    private sealed class WritingTrackExportService : ITrackExportService
    {
        public Task<IReadOnlyList<ToolOperationResult>> ExportAsync(
            string inputPath,
            IReadOnlyCollection<TrackExportOptions> tracks,
            CancellationToken cancellationToken = default,
            bool concatDemuxer = false)
        {
            List<ToolOperationResult> results = [];
            foreach (TrackExportOptions t in tracks)
            {
                File.WriteAllText(t.OutputPath, "stub");
                results.Add(new ToolOperationResult(
                    $"stream {t.StreamIndex}",
                    t.OutputPath,
                    $"ffmpeg -i \"{inputPath}\" ...",
                    Succeeded: true,
                    ExitCode: 0,
                    Duration: TimeSpan.FromMilliseconds(10),
                    ErrorDetail: null));
            }

            return Task.FromResult<IReadOnlyList<ToolOperationResult>>(results);
        }
    }

    private sealed class CapturingMuxService : IMuxService
    {
        public MuxRequest? LastRequest { get; private set; }

        public Task<ToolOperationResult> MuxAsync(MuxRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new ToolOperationResult(
                "mux",
                request.OutputPath,
                "ffmpeg mux",
                Succeeded: true,
                ExitCode: 0,
                Duration: TimeSpan.Zero,
                ErrorDetail: null));
        }
    }

    private sealed class FakeTrackExportService : ITrackExportService
    {
        private readonly bool _simulateFailure;

        public FakeTrackExportService(bool simulateFailure = false)
        {
            _simulateFailure = simulateFailure;
        }

        public Task<IReadOnlyList<ToolOperationResult>> ExportAsync(
            string inputPath,
            IReadOnlyCollection<TrackExportOptions> tracks,
            CancellationToken cancellationToken = default,
            bool concatDemuxer = false)
        {
            List<ToolOperationResult> results = tracks
                .Select(t => new ToolOperationResult(
                    $"stream {t.StreamIndex}",
                    t.OutputPath,
                    $"ffmpeg -i \"{inputPath}\" ...",
                    Succeeded: !_simulateFailure,
                    ExitCode: _simulateFailure ? 1 : 0,
                    Duration: TimeSpan.FromMilliseconds(100),
                    _simulateFailure ? "simulated failure" : null))
                .ToList();

            return Task.FromResult<IReadOnlyList<ToolOperationResult>>(results);
        }
    }

    private sealed class FakeChapterExportService : IChapterExportService
    {
        public Task<ToolOperationResult> ExportAsync(
            string inputPath,
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ToolOperationResult(
                "chapters",
                outputPath,
                $"ffmpeg -i \"{inputPath}\" -f ffmetadata \"{outputPath}\"",
                Succeeded: true,
                ExitCode: 0,
                Duration: TimeSpan.FromMilliseconds(50),
                ErrorDetail: null));
        }
    }

    private sealed class CapturingTrackExportService : ITrackExportService
    {
        public List<TrackExportOptions> CapturedTracks { get; } = [];

        public Task<IReadOnlyList<ToolOperationResult>> ExportAsync(
            string inputPath,
            IReadOnlyCollection<TrackExportOptions> tracks,
            CancellationToken cancellationToken = default,
            bool concatDemuxer = false)
        {
            CapturedTracks.AddRange(tracks);

            List<ToolOperationResult> results = tracks
                .Select(t => new ToolOperationResult(
                    $"stream {t.StreamIndex}",
                    t.OutputPath,
                    $"ffmpeg -i \"{inputPath}\" ...",
                    Succeeded: true,
                    ExitCode: 0,
                    Duration: TimeSpan.FromMilliseconds(100),
                    ErrorDetail: null))
                .ToList();

            return Task.FromResult<IReadOnlyList<ToolOperationResult>>(results);
        }
    }
}
