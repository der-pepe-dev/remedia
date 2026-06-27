namespace ReMedia.App.Avalonia.Tests;

using Xunit;
using ReMedia.App.Avalonia.ViewModels;
using ReMedia.Core.Interfaces;
using ReMedia.Core.Models;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task ProbeCommand_PopulatesTracksAndLogsFromProbeResult()
    {
        FakeProbeService probe = new(
        [
            Stream(0, MediaAssetType.Video, "h264"),
            Stream(1, MediaAssetType.Audio, "ac3"),
        ]);
        MainWindowViewModel vm = new(probe, filePicker: null) { InputPath = @"C:\in\movie.mkv" };

        await vm.ProbeCommand.ExecuteAsync();

        Assert.Equal(2, vm.Tracks.Count);
        Assert.Equal(0, vm.Tracks[0].StreamIndex);
        Assert.Equal("ac3", vm.Tracks[1].Codec);
        Assert.Contains("Found 2 stream(s)", vm.Log);
    }

    [Fact]
    public async Task ProbeCommand_WhenProbeThrows_LogsErrorAndLeavesTracksEmpty()
    {
        ThrowingProbeService probe = new();
        MainWindowViewModel vm = new(probe, filePicker: null) { InputPath = @"C:\in\bad.mkv" };

        await vm.ProbeCommand.ExecuteAsync();

        Assert.Empty(vm.Tracks);
        Assert.Contains("Probe failed", vm.Log);
    }

    [Fact]
    public void ProbeCommand_CannotExecuteWithoutInputPath()
    {
        MainWindowViewModel vm = new(new FakeProbeService([]), filePicker: null);

        Assert.False(vm.ProbeCommand.CanExecute(null));

        vm.InputPath = "movie.mkv";
        Assert.True(vm.ProbeCommand.CanExecute(null));
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
                inputPath, "matroska", "Matroska", TimeSpan.FromMinutes(90), 1024,
                streams, []));
        }
    }

    private sealed class ThrowingProbeService : IMediaProbeService
    {
        public Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("ffprobe not found");
        }
    }
}
