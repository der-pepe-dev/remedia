namespace ReMedia.Core.Interfaces;

using ReMedia.Core.Models;

public interface ILoudnessService
{
    Task<LoudnessAnalysisResult> AnalyzeAsync(string inputPath, int streamIndex, CancellationToken cancellationToken = default);

    ClippingPredictionResult PredictClipping(LoudnessAnalysisResult current, decimal appliedGainDb);
}
