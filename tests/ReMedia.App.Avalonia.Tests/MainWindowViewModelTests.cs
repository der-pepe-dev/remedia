namespace ReMedia.App.Avalonia.Tests;

using Xunit;
using ReMedia.App.Avalonia.ViewModels;
using ReMedia.Core.Interfaces;
using ReMedia.Core.Models;
using ReMedia.Core.Services;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task ProbeCommand_PopulatesTracksAndLogsFromProbeResult()
    {
        MainWindowViewModel vm = CreateViewModel(
            new FakeProbeService([Stream(0, MediaAssetType.Video, "h264"), Stream(1, MediaAssetType.Audio, "ac3")]));
        vm.InputPath = @"C:\in\movie.mkv";

        await vm.ProbeCommand.ExecuteAsync();

        Assert.Equal(2, vm.Tracks.Count);
        Assert.Equal("ac3", vm.Tracks[1].Codec);
        Assert.Contains("Found 2 stream(s)", vm.Log);
    }

    [Fact]
    public async Task ProbeCommand_WhenProbeThrows_LogsErrorAndLeavesTracksEmpty()
    {
        MainWindowViewModel vm = CreateViewModel(new ThrowingProbeService());
        vm.InputPath = @"C:\in\bad.mkv";

        await vm.ProbeCommand.ExecuteAsync();

        Assert.Empty(vm.Tracks);
        Assert.Contains("Probe failed", vm.Log);
    }

    [Fact]
    public void ExportCommand_RequiresInputOutputAndASelectedTrack()
    {
        FakeTrackExportService track = new();
        MainWindowViewModel vm = CreateViewModel(
            new FakeProbeService([Stream(1, MediaAssetType.Audio, "ac3")]), track);

        Assert.False(vm.ExportCommand.CanExecute(null));

        vm.InputPath = "movie.mkv";
        vm.OutputFolder = "out";
        vm.Tracks.Add(new TrackRowViewModel(Stream(1, MediaAssetType.Audio, "ac3")));
        Assert.True(vm.ExportCommand.CanExecute(null));

        vm.Tracks[0].Selected = false;
        Assert.False(vm.ExportCommand.CanExecute(null));
    }

    [Fact]
    public async Task ExportCommand_ExportsSelectedNonVideoTracks()
    {
        FakeTrackExportService track = new();
        MainWindowViewModel vm = CreateViewModel(
            new FakeProbeService([Stream(0, MediaAssetType.Video, "h264"), Stream(1, MediaAssetType.Audio, "ac3")]),
            track);

        string outputFolder = Path.Join(Path.GetTempPath(), Guid.NewGuid().ToString());
        vm.InputPath = @"C:\in\movie.mkv";
        vm.OutputFolder = outputFolder;

        try
        {
            await vm.ProbeCommand.ExecuteAsync();
            await vm.ExportCommand.ExecuteAsync();

            // Only the audio track is exported; the video track is excluded.
            Assert.Single(track.ExportedStreamIndexes);
            Assert.Equal(1, track.ExportedStreamIndexes[0]);
            Assert.Contains("Export completed", vm.Log);
        }
        finally
        {
            if (Directory.Exists(outputFolder))
            {
                Directory.Delete(outputFolder, recursive: true);
            }
        }
    }

    private static MainWindowViewModel CreateViewModel(IMediaProbeService probe, ITrackExportService? track = null)
    {
        ExportWorkflowService workflow = new(track ?? new FakeTrackExportService(), new FakeChapterExportService());
        return new MainWindowViewModel(probe, workflow, filePicker: null);
    }

    private static MediaStreamInfo Stream(int index, MediaAssetType type, string codec)
    {
        return new MediaStreamInfo(
            index, type, codec, codec, "eng", null, Default: false, Forced: false,
            Channels: type == MediaAssetType.Audio ? 6 : null,
            SampleRate: type == MediaAssetType.Audio ? 48000 : null,
            Width: null, Height: null,
            AvgFrameRate: Rational.Zero, RealFrameRate: Rational.Zero, FieldOrder: null);
    }

    private sealed class FakeProbeService(IReadOnlyList<MediaStreamInfo> streams) : IMediaProbeService
    {
        public Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new MediaProbeResult(
                inputPath, "matroska", "Matroska", TimeSpan.FromMinutes(90), 1024, streams, []));
        }
    }

    private sealed class ThrowingProbeService : IMediaProbeService
    {
        public Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("ffprobe not found");
        }
    }

    private sealed class FakeTrackExportService : ITrackExportService
    {
        public List<int> ExportedStreamIndexes { get; } = [];

        public Task<IReadOnlyList<ToolOperationResult>> ExportAsync(
            string inputPath,
            IReadOnlyCollection<TrackExportOptions> tracks,
            CancellationToken cancellationToken = default,
            bool concatDemuxer = false)
        {
            List<ToolOperationResult> results = [];
            foreach (TrackExportOptions t in tracks)
            {
                ExportedStreamIndexes.Add(t.StreamIndex);
                results.Add(new ToolOperationResult(
                    $"stream {t.StreamIndex}", t.OutputPath, "fake", Succeeded: true,
                    ExitCode: 0, Duration: TimeSpan.Zero, ErrorDetail: null));
            }

            return Task.FromResult<IReadOnlyList<ToolOperationResult>>(results);
        }
    }

    private sealed class FakeChapterExportService : IChapterExportService
    {
        public Task<ToolOperationResult> ExportAsync(string inputPath, string outputPath, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ToolOperationResult(
                "chapters", outputPath, "fake", Succeeded: true, ExitCode: 0, Duration: TimeSpan.Zero, ErrorDetail: null));
        }
    }
}
