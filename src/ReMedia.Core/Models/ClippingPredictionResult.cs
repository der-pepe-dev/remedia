namespace ReMedia.Core.Models;

public sealed record ClippingPredictionResult(
    bool Warning,
    bool Danger,
    decimal AppliedGainDb,
    decimal? PredictedSamplePeakDbfs,
    decimal? PredictedTruePeakDbtp,
    string Message);
