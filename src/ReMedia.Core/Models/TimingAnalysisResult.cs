namespace ReMedia.Core.Models;

public sealed record TimingAnalysisResult(
    TimeSpan OriginalDuration,
    TimeSpan DestinationDuration,
    decimal SourceFps,
    decimal TargetFps,
    decimal StretchFactor,
    decimal AudioTempoFactor);
