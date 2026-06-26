namespace ReMedia.Tooling.Tests;

using ReMedia.Core.Diagnostics;
using ReMedia.Core.Models;
using ReMedia.Tooling.Configuration;
using ReMedia.Tooling.Ffprobe;

public sealed class FfprobeMediaProbeServiceTests
{
    private const string SampleFfprobeJson = """
        {
          "streams": [
            {
              "index": 0,
              "codec_type": "video",
              "codec_name": "h264",
              "codec_long_name": "H.264 / AVC",
              "width": 1920,
              "height": 1080,
              "avg_frame_rate": "24000/1001",
              "r_frame_rate": "24000/1001",
              "field_order": "progressive",
              "disposition": { "default": 1, "forced": 0 },
              "tags": {}
            },
            {
              "index": 1,
              "codec_type": "audio",
              "codec_name": "ac3",
              "codec_long_name": "ATSC A/52A (AC-3)",
              "channels": 6,
              "sample_rate": "48000",
              "avg_frame_rate": "0/0",
              "r_frame_rate": "0/0",
              "disposition": { "default": 1, "forced": 0 },
              "tags": { "language": "eng", "title": "Surround 5.1" }
            },
            {
              "index": 2,
              "codec_type": "subtitle",
              "codec_name": "subrip",
              "codec_long_name": "SubRip subtitle",
              "avg_frame_rate": "0/0",
              "r_frame_rate": "0/0",
              "disposition": { "default": 0, "forced": 1 },
              "tags": { "language": "eng", "title": "Forced" }
            }
          ],
          "chapters": [
            { "id": 0, "start_time": "0.000000", "end_time": "300.500000", "tags": { "title": "Chapter 1" } },
            { "id": 1, "start_time": "300.500000", "end_time": "600.000000", "tags": { "title": "Chapter 2" } }
          ],
          "format": {
            "filename": "movie.mkv",
            "format_name": "matroska,webm",
            "format_long_name": "Matroska / WebM",
            "duration": "5541.123000",
            "size": "4294967296"
          }
        }
        """;

    [Fact]
    public async Task ProbeAsync_MapsFormatCorrectly()
    {
        FakeProcessRunner runner = new(SampleFfprobeJson);
        FfprobeMediaProbeService service = new(new ExternalToolPaths("ffprobe", "ffmpeg"), runner);

        MediaProbeResult result = await service.ProbeAsync("movie.mkv", TestContext.Current.CancellationToken);

        Assert.Equal("matroska,webm", result.FormatName);
        Assert.Equal("Matroska / WebM", result.FormatLongName);
        Assert.NotNull(result.Duration);
        Assert.Equal(4294967296L, result.SizeBytes);
    }

    [Fact]
    public async Task ProbeAsync_MapsVideoStream()
    {
        FakeProcessRunner runner = new(SampleFfprobeJson);
        FfprobeMediaProbeService service = new(new ExternalToolPaths("ffprobe", "ffmpeg"), runner);

        MediaProbeResult result = await service.ProbeAsync("movie.mkv", TestContext.Current.CancellationToken);

        MediaStreamInfo video = result.Streams[0];
        Assert.Equal(MediaAssetType.Video, video.AssetType);
        Assert.Equal("h264", video.CodecName);
        Assert.Equal(1920, video.Width);
        Assert.Equal(1080, video.Height);
        Assert.Equal(24000m / 1001m, video.AverageFramesPerSecond);
        Assert.True(video.Default);
        Assert.False(video.Forced);
    }

    [Fact]
    public async Task ProbeAsync_MapsAudioStreamWithTags()
    {
        FakeProcessRunner runner = new(SampleFfprobeJson);
        FfprobeMediaProbeService service = new(new ExternalToolPaths("ffprobe", "ffmpeg"), runner);

        MediaProbeResult result = await service.ProbeAsync("movie.mkv", TestContext.Current.CancellationToken);

        MediaStreamInfo audio = result.Streams[1];
        Assert.Equal(MediaAssetType.Audio, audio.AssetType);
        Assert.Equal("ac3", audio.CodecName);
        Assert.Equal(6, audio.Channels);
        Assert.Equal(48000, audio.SampleRate);
        Assert.Equal("eng", audio.Language);
        Assert.Equal("Surround 5.1", audio.Title);
    }

    [Fact]
    public async Task ProbeAsync_MapsForcedSubtitleFlag()
    {
        FakeProcessRunner runner = new(SampleFfprobeJson);
        FfprobeMediaProbeService service = new(new ExternalToolPaths("ffprobe", "ffmpeg"), runner);

        MediaProbeResult result = await service.ProbeAsync("movie.mkv", TestContext.Current.CancellationToken);

        MediaStreamInfo subtitle = result.Streams[2];
        Assert.Equal(MediaAssetType.Subtitle, subtitle.AssetType);
        Assert.False(subtitle.Default);
        Assert.True(subtitle.Forced);
    }

    [Fact]
    public async Task ProbeAsync_MapsChapters()
    {
        FakeProcessRunner runner = new(SampleFfprobeJson);
        FfprobeMediaProbeService service = new(new ExternalToolPaths("ffprobe", "ffmpeg"), runner);

        MediaProbeResult result = await service.ProbeAsync("movie.mkv", TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Chapters.Count);
        Assert.Equal("Chapter 1", result.Chapters[0].Title);
        Assert.Equal("Chapter 2", result.Chapters[1].Title);
        Assert.Equal(TimeSpan.Zero, result.Chapters[0].Start);
    }

    [Fact]
    public async Task ProbeAsync_WithNoStreams_ReturnsEmptyList()
    {
        string json = """{ "streams": [], "chapters": [], "format": { "format_name": "wav" } }""";
        FakeProcessRunner runner = new(json);
        FfprobeMediaProbeService service = new(new ExternalToolPaths("ffprobe", "ffmpeg"), runner);

        MediaProbeResult result = await service.ProbeAsync("silence.wav", TestContext.Current.CancellationToken);

        Assert.Empty(result.Streams);
        Assert.Empty(result.Chapters);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ProbeAsync_WithEmptyInputPath_Throws(string? inputPath)
    {
        FakeProcessRunner runner = new("{}");
        FfprobeMediaProbeService service = new(new ExternalToolPaths("ffprobe", "ffmpeg"), runner);

        await Assert.ThrowsAsync<ArgumentException>(() => service.ProbeAsync(inputPath!, TestContext.Current.CancellationToken));
    }

    private sealed class FakeProcessRunner : IProcessRunner
    {
        private readonly string _stdout;

        public FakeProcessRunner(string stdout)
        {
            _stdout = stdout;
        }

        public Task<ProcessExecutionResult> RunAsync(
            string executablePath,
            string arguments,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ProcessExecutionResult(
                executablePath,
                arguments,
                ExitCode: 0,
                _stdout,
                StandardError: string.Empty,
                TimeSpan.FromMilliseconds(50)));
        }
    }
}
