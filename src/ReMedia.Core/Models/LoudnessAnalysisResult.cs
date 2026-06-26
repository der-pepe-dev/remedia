namespace ReMedia.Core.Models;

public sealed record LoudnessAnalysisResult(
    decimal? IntegratedLufs,
    decimal? LoudnessRange,
    decimal? TruePeakDbtp,
    decimal? SamplePeakDbfs);
