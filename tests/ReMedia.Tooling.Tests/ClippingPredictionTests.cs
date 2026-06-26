namespace ReMedia.Tooling.Tests;

using ReMedia.Core.Models;
using ReMedia.Tooling.Ffmpeg;

public sealed class ClippingPredictionTests
{
    private readonly FfmpegLoudnessService _service;

    public ClippingPredictionTests()
    {
        _service = new FfmpegLoudnessService(
            new ReMedia.Tooling.Configuration.ExternalToolPaths("ffprobe", "ffmpeg"),
            null!);
    }

    [Fact]
    public void PredictClipping_WithSafeGain_ReturnsNoDanger()
    {
        LoudnessAnalysisResult loudness = new(-24m, 10m, -6m, -6m);

        ClippingPredictionResult result = _service.PredictClipping(loudness, 2m);

        Assert.False(result.Danger);
        Assert.False(result.Warning);
        Assert.Equal(2m, result.AppliedGainDb);
        Assert.Equal(-4m, result.PredictedTruePeakDbtp);
        Assert.Equal(-4m, result.PredictedSamplePeakDbfs);
    }

    [Fact]
    public void PredictClipping_WithClippingGain_ReturnsDanger()
    {
        LoudnessAnalysisResult loudness = new(-24m, 10m, -2m, -2m);

        ClippingPredictionResult result = _service.PredictClipping(loudness, 5m);

        Assert.True(result.Danger);
        Assert.Equal(3m, result.PredictedTruePeakDbtp);
        Assert.Equal(3m, result.PredictedSamplePeakDbfs);
        Assert.Contains("Clipping expected", result.Message);
    }

    [Fact]
    public void PredictClipping_WithNearClipping_ReturnsWarning()
    {
        LoudnessAnalysisResult loudness = new(-24m, 10m, -1.5m, -1.5m);

        ClippingPredictionResult result = _service.PredictClipping(loudness, 1m);

        Assert.True(result.Warning);
        Assert.False(result.Danger);
        Assert.Equal(-0.5m, result.PredictedTruePeakDbtp);
        Assert.Contains("Near clipping", result.Message);
    }

    [Fact]
    public void PredictClipping_WithNullPeaks_HandlesGracefully()
    {
        LoudnessAnalysisResult loudness = new(-24m, 10m, null, null);

        ClippingPredictionResult result = _service.PredictClipping(loudness, 10m);

        Assert.False(result.Danger);
        Assert.False(result.Warning);
        Assert.Null(result.PredictedTruePeakDbtp);
        Assert.Null(result.PredictedSamplePeakDbfs);
    }

    [Fact]
    public void PredictClipping_WithZeroGain_ReturnsOriginalPeaks()
    {
        LoudnessAnalysisResult loudness = new(-24m, 10m, -3m, -3m);

        ClippingPredictionResult result = _service.PredictClipping(loudness, 0m);

        Assert.Equal(-3m, result.PredictedTruePeakDbtp);
        Assert.Equal(-3m, result.PredictedSamplePeakDbfs);
        Assert.Contains("No clipping", result.Message);
    }

    [Fact]
    public void PredictClipping_WithNegativeGain_LowersPeaks()
    {
        LoudnessAnalysisResult loudness = new(-24m, 10m, -1m, -1m);

        ClippingPredictionResult result = _service.PredictClipping(loudness, -3m);

        Assert.Equal(-4m, result.PredictedTruePeakDbtp);
        Assert.False(result.Danger);
        Assert.False(result.Warning);
    }
}
