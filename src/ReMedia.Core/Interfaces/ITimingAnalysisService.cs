namespace ReMedia.Core.Interfaces;

using ReMedia.Core.Models;

public interface ITimingAnalysisService
{
    TimingAnalysisResult Analyze(TimingAnalysisRequest request);
}
