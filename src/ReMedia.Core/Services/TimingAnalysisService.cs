namespace ReMedia.Core.Services;

using ReMedia.Core.Interfaces;
using ReMedia.Core.Models;

public sealed class TimingAnalysisService : ITimingAnalysisService
{
    public TimingAnalysisResult Analyze(TimingAnalysisRequest request)
    {
        if (request.SourceFps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Source FPS must be greater than zero.");
        }

        if (request.TargetFps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "Target FPS must be greater than zero.");
        }

        decimal stretchFactor = request.SourceFps / request.TargetFps;
        decimal audioTempoFactor = request.TargetFps / request.SourceFps;

        double destinationTicks = request.OriginalDuration.Ticks * (double)stretchFactor;
        TimeSpan destinationDuration = TimeSpan.FromTicks(Convert.ToInt64(Math.Round(destinationTicks, MidpointRounding.AwayFromZero)));

        return new TimingAnalysisResult(
            request.OriginalDuration,
            destinationDuration,
            request.SourceFps,
            request.TargetFps,
            stretchFactor,
            audioTempoFactor);
    }
}
