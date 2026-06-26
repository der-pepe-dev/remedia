namespace ReMedia.Core.Models;

/// <summary>
/// Recommendation for bringing a stream to a target integrated loudness, built on a
/// measured <see cref="LoudnessAnalysisResult"/>.
/// <para>
/// <c>RawGainDb</c> is the gain to hit the target exactly (target - measured);
/// <c>RecommendedGainDb</c> is that gain reduced as needed to keep the true peak at or
/// below <c>TruePeakCeilingDbtp</c>. <c>CeilingEnforced</c> is false when there is no
/// true-peak measurement to limit against; <c>GainLimitedByCeiling</c> is true when the
/// recommendation was reduced below the raw gain. <c>AchievedLufs</c> is measured plus
/// recommended, and <c>ShortfallDb</c> is how far short of the target that lands (0 when
/// not limited). <c>Clipping</c> is the prediction at the recommended gain, null when no
/// gain could be recommended (e.g. no measured loudness).
/// </para>
/// </summary>
public sealed record LoudnessMatchResult(
    decimal TargetLufs,
    decimal? MeasuredLufs,
    decimal? RawGainDb,
    decimal? RecommendedGainDb,
    decimal TruePeakCeilingDbtp,
    bool CeilingEnforced,
    bool GainLimitedByCeiling,
    decimal? AchievedLufs,
    decimal? ShortfallDb,
    ClippingPredictionResult? Clipping,
    string Message);
