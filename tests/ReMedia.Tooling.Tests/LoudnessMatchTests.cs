namespace ReMedia.Tooling.Tests;

using ReMedia.Core.Models;
using ReMedia.Tooling.Ffmpeg;

public sealed class LoudnessMatchTests
{
    private readonly FfmpegLoudnessService _service = new(
        new ReMedia.Tooling.Configuration.ExternalToolPaths("ffprobe", "ffmpeg"),
        null!);

    [Fact]
    public void MatchToTarget_BelowCeiling_RecommendsRawGain()
    {
        // measured -30 -> target -23 needs +7 dB; true peak -10 + 7 = -3 dBTP, under -1 ceiling.
        LoudnessAnalysisResult loudness = new(-30m, 8m, -10m, -10m);

        LoudnessMatchResult result = _service.MatchToTarget(loudness, -23m, -1m);

        Assert.Equal(7m, result.RawGainDb);
        Assert.Equal(7m, result.RecommendedGainDb);
        Assert.False(result.GainLimitedByCeiling);
        Assert.Equal(-23m, result.AchievedLufs);
        Assert.Equal(0m, result.ShortfallDb);
        Assert.NotNull(result.Clipping);
        Assert.False(result.Clipping!.Danger);
    }

    [Fact]
    public void MatchToTarget_GainWouldClip_LimitsToCeiling()
    {
        // measured -30 -> target -16 needs +14 dB, but true peak -3 only allows +2 to hit -1 dBTP.
        LoudnessAnalysisResult loudness = new(-30m, 8m, -3m, -3m);

        LoudnessMatchResult result = _service.MatchToTarget(loudness, -16m, -1m);

        Assert.Equal(14m, result.RawGainDb);
        Assert.Equal(2m, result.RecommendedGainDb);
        Assert.True(result.GainLimitedByCeiling);
        Assert.Equal(-28m, result.AchievedLufs);
        Assert.Equal(12m, result.ShortfallDb);
        Assert.Contains("Limited", result.Message);
    }

    [Fact]
    public void MatchToTarget_RequiresAttenuation_RecommendsNegativeGain()
    {
        // measured -18 -> target -23 needs -5 dB; attenuation never clips.
        LoudnessAnalysisResult loudness = new(-18m, 6m, -2m, -2m);

        LoudnessMatchResult result = _service.MatchToTarget(loudness, -23m, -1m);

        Assert.Equal(-5m, result.RecommendedGainDb);
        Assert.False(result.GainLimitedByCeiling);
        Assert.Equal(-23m, result.AchievedLufs);
    }

    [Fact]
    public void MatchToTarget_NoMeasuredLoudness_ReturnsNoRecommendation()
    {
        LoudnessAnalysisResult loudness = new(null, null, -3m, -3m);

        LoudnessMatchResult result = _service.MatchToTarget(loudness, -23m, -1m);

        Assert.Null(result.RecommendedGainDb);
        Assert.Null(result.Clipping);
        Assert.False(result.CeilingEnforced);
        Assert.Contains("cannot recommend", result.Message);
    }

    [Fact]
    public void MatchToTarget_NoTruePeak_DoesNotEnforceCeiling()
    {
        LoudnessAnalysisResult loudness = new(-30m, 8m, null, null);

        LoudnessMatchResult result = _service.MatchToTarget(loudness, -23m, -1m);

        Assert.Equal(7m, result.RecommendedGainDb);
        Assert.False(result.CeilingEnforced);
        Assert.False(result.GainLimitedByCeiling);
        Assert.Contains("not enforced", result.Message);
    }

    [Fact]
    public void MatchToTarget_RoundsGainToTenthOfDb()
    {
        LoudnessAnalysisResult loudness = new(-23.33m, 8m, -12m, -12m);

        LoudnessMatchResult result = _service.MatchToTarget(loudness, -23m, -1m);

        Assert.Equal(0.3m, result.RecommendedGainDb);
    }
}
