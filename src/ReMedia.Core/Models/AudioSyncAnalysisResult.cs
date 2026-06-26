namespace ReMedia.Core.Models;

public sealed record AudioSyncAnalysisResult(
    TimeSpan DetectedOffset,
    float Confidence,
    IReadOnlyList<AudioPeak> SourcePeaks,
    IReadOnlyList<AudioPeak> DestinationPeaks);
