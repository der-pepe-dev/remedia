namespace ReMedia.Core.Models;

public sealed record TimingAnalysisRequest(
    TimeSpan OriginalDuration,
    decimal SourceFps,
    decimal TargetFps);
