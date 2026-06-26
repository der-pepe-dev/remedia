namespace ReMedia.Tooling.Tests;

using ReMedia.Core.Models;
using ReMedia.Tooling.Ffmpeg;

public sealed class FfmpegArgumentBuilderTests
{
    [Fact]
    public void BuildTrackExportArguments_ForCopyExport_MapsRequestedStream()
    {
        TrackExportOptions options = new(
            StreamIndex: 2,
            AssetType: MediaAssetType.Audio,
            OutputContainer: ".mka",
            OutputCodec: "copy",
            OutputPath: @"C:\out\stream-2.mka",
            CopyStream: true);

        string arguments = FfmpegArgumentBuilder.BuildTrackExportArguments(@"C:\in\movie.mkv", options);

        Assert.Contains("-map 0:2", arguments);
        Assert.Contains("-c copy", arguments);
        Assert.Contains(@"""C:\out\stream-2.mka""", arguments);
    }

    [Fact]
    public void BuildTrackExportArguments_ForTimingConversion_AddsAtempo()
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
            TargetFps: 24000m / 1001m);

        string arguments = FfmpegArgumentBuilder.BuildTrackExportArguments(@"C:\in\movie.mkv", options);

        Assert.Contains("-filter:a", arguments);
        Assert.Contains("atempo=", arguments);
        Assert.Contains("-c:a flac", arguments);
    }

    [Fact]
    public void BuildTrackExportArguments_CopyWithTimingConversion_Throws()
    {
        TrackExportOptions options = new(
            StreamIndex: 1,
            AssetType: MediaAssetType.Audio,
            OutputContainer: ".mka",
            OutputCodec: "copy",
            OutputPath: @"C:\out\audio.mka",
            CopyStream: true,
            ApplyTimingConversion: true,
            SourceFps: 25m,
            TargetFps: 24000m / 1001m);

        Assert.Throws<ArgumentException>(() =>
            FfmpegArgumentBuilder.BuildTrackExportArguments(@"C:\in\movie.mkv", options));
    }

    [Fact]
    public void BuildTrackExportArguments_WithNonAudioTimingConversion_DoesNotAddAtempo()
    {
        TrackExportOptions options = new(
            StreamIndex: 3,
            AssetType: MediaAssetType.Subtitle,
            OutputContainer: ".srt",
            OutputCodec: "srt",
            OutputPath: @"C:\out\sub.srt",
            CopyStream: false,
            ApplyTimingConversion: true,
            SourceFps: 25m,
            TargetFps: 24000m / 1001m);

        string arguments = FfmpegArgumentBuilder.BuildTrackExportArguments(@"C:\in\movie.mkv", options);

        Assert.DoesNotContain("atempo", arguments);
        Assert.Contains("-c:s srt", arguments);
    }

    [Fact]
    public void BuildTrackExportArguments_ForExplicitCodecWithoutTiming_UsesCodecSpecifier()
    {
        TrackExportOptions options = new(
            StreamIndex: 1,
            AssetType: MediaAssetType.Audio,
            OutputContainer: ".flac",
            OutputCodec: "flac",
            OutputPath: @"C:\out\audio.flac",
            CopyStream: false);

        string arguments = FfmpegArgumentBuilder.BuildTrackExportArguments(@"C:\in\movie.mkv", options);

        Assert.Contains("-c:a flac", arguments);
        Assert.DoesNotContain("atempo", arguments);
        Assert.DoesNotContain("-c copy", arguments);
    }

    [Fact]
    public void BuildTrackExportArguments_WithGain_AddsVolumeFilter()
    {
        TrackExportOptions options = new(
            StreamIndex: 1,
            AssetType: MediaAssetType.Audio,
            OutputContainer: ".flac",
            OutputCodec: "flac",
            OutputPath: @"C:\out\audio.flac",
            CopyStream: false,
            AppliedGainDb: 3.5m);

        string arguments = FfmpegArgumentBuilder.BuildTrackExportArguments(@"C:\in\movie.mkv", options);

        Assert.Contains("volume=3.5dB", arguments);
        Assert.Contains("-filter:a", arguments);
        Assert.Contains("-c:a flac", arguments);
    }

    [Fact]
    public void BuildTrackExportArguments_WithGainAndTiming_ChainsFilters()
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

        string arguments = FfmpegArgumentBuilder.BuildTrackExportArguments(@"C:\in\movie.mkv", options);

        Assert.Contains("atempo=", arguments);
        Assert.Contains("volume=-2dB", arguments);
        Assert.Contains(",", arguments);
    }

    [Fact]
    public void BuildTrackExportArguments_WithGainAndCopy_Throws()
    {
        TrackExportOptions options = new(
            StreamIndex: 1,
            AssetType: MediaAssetType.Audio,
            OutputContainer: ".ac3",
            OutputCodec: "copy",
            OutputPath: @"C:\out\audio.ac3",
            CopyStream: true,
            AppliedGainDb: 3m);

        Assert.Throws<ArgumentException>(() =>
            FfmpegArgumentBuilder.BuildTrackExportArguments(@"C:\in\movie.mkv", options));
    }

    [Fact]
    public void BuildTrackExportArguments_WithNegativeGain_FormatsCorrectly()
    {
        TrackExportOptions options = new(
            StreamIndex: 1,
            AssetType: MediaAssetType.Audio,
            OutputContainer: ".flac",
            OutputCodec: "flac",
            OutputPath: @"C:\out\audio.flac",
            CopyStream: false,
            AppliedGainDb: -5.2m);

        string arguments = FfmpegArgumentBuilder.BuildTrackExportArguments(@"C:\in\movie.mkv", options);

        Assert.Contains("volume=-5.2dB", arguments);
    }

    [Fact]
    public void BuildLoudnessAnalysisArguments_ContainsEbur128Filter()
    {
        string arguments = FfmpegArgumentBuilder.BuildLoudnessAnalysisArguments(@"C:\in\movie.mkv", 1);

        Assert.Contains(@"""C:\in\movie.mkv""", arguments);
        Assert.Contains("-map 0:1", arguments);
        Assert.Contains("-af ebur128=peak=true", arguments);
        Assert.Contains("-f null", arguments);
    }
}
