namespace ReMedia.Tooling.Tests;

using ReMedia.Core.Models;
using ReMedia.Tooling.Ffmpeg;

public sealed class FfmpegConcatArgumentTests
{
    [Fact]
    public void BuildTrackExportArguments_WithConcatDemuxer_AddsConcatFlags()
    {
        TrackExportOptions options = new(
            StreamIndex: 1,
            AssetType: MediaAssetType.Audio,
            OutputContainer: ".flac",
            OutputCodec: "copy",
            OutputPath: @"C:\out\audio.flac",
            CopyStream: true);

        string arguments = FfmpegArgumentBuilder.BuildTrackExportArguments(@"C:\out\concat.txt", options, concatDemuxer: true);

        Assert.Contains("-f concat", arguments);
        Assert.Contains("-safe 0", arguments);
        Assert.Contains(@"""C:\out\concat.txt""", arguments);
        Assert.Contains("-map 0:1", arguments);
    }

    [Fact]
    public void BuildTrackExportArguments_WithoutConcatDemuxer_NoConcatFlags()
    {
        TrackExportOptions options = new(
            StreamIndex: 1,
            AssetType: MediaAssetType.Audio,
            OutputContainer: ".flac",
            OutputCodec: "copy",
            OutputPath: @"C:\out\audio.flac",
            CopyStream: true);

        string arguments = FfmpegArgumentBuilder.BuildTrackExportArguments(@"C:\in\movie.mkv", options, concatDemuxer: false);

        Assert.DoesNotContain("-f concat", arguments);
        Assert.DoesNotContain("-safe", arguments);
    }

    [Fact]
    public void BuildTrackExportArguments_ConcatWithTimingAndGain_ChainsCorrectly()
    {
        TrackExportOptions options = new(
            StreamIndex: 1,
            AssetType: MediaAssetType.Audio,
            OutputContainer: ".flac",
            OutputCodec: "flac",
            OutputPath: @"C:\out\audio.flac",
            CopyStream: false,
            ApplyTimingConversion: true,
            SourceFps: 25m,
            TargetFps: 24000m / 1001m,
            AppliedGainDb: -2m);

        string arguments = FfmpegArgumentBuilder.BuildTrackExportArguments(@"C:\out\concat.txt", options, concatDemuxer: true);

        Assert.Contains("-f concat", arguments);
        Assert.Contains("-safe 0", arguments);
        Assert.Contains("atempo=", arguments);
        Assert.Contains("volume=-2dB", arguments);
    }
}
