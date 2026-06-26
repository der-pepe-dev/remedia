namespace ReMedia.Core.Tests;

using ReMedia.Core.Models;
using ReMedia.Core.Services;

public sealed class TimingAnalysisServiceTests
{
    [Fact]
    public void Analyze_ForTwentyFiveToTwentyThreePointNineSevenSix_ComputesExpectedStretch()
    {
        TimingAnalysisService service = new();

        TimingAnalysisResult result = service.Analyze(new TimingAnalysisRequest(
            TimeSpan.FromMinutes(100),
            25m,
            24000m / 1001m));

        Assert.Equal(25m / (24000m / 1001m), result.StretchFactor);
        Assert.Equal((24000m / 1001m) / 25m, result.AudioTempoFactor);
        Assert.True(result.DestinationDuration > result.OriginalDuration);
    }

    [Fact]
    public void Analyze_WithInvalidSourceFps_Throws()
    {
        TimingAnalysisService service = new();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            service.Analyze(new TimingAnalysisRequest(TimeSpan.FromMinutes(1), 0m, 23.976m)));
    }
}
