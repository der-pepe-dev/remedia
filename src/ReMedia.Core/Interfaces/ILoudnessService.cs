namespace ReMedia.Core.Interfaces;

using ReMedia.Core.Models;

public interface ILoudnessService
{
    Task<LoudnessAnalysisResult> AnalyzeAsync(string inputPath, int streamIndex, CancellationToken cancellationToken = default);

    ClippingPredictionResult PredictClipping(LoudnessAnalysisResult current, decimal appliedGainDb);

    /// <summary>
    /// Recommends a gain that brings <paramref name="current"/> to <paramref name="targetLufs"/>,
    /// reduced as needed so the true peak stays at or below <paramref name="truePeakCeilingDbtp"/>.
    /// </summary>
    LoudnessMatchResult MatchToTarget(LoudnessAnalysisResult current, decimal targetLufs, decimal truePeakCeilingDbtp);
}
