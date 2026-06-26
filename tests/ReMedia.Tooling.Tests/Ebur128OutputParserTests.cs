namespace ReMedia.Tooling.Tests;

using ReMedia.Core.Models;
using ReMedia.Tooling.Ffmpeg;

public sealed class Ebur128OutputParserTests
{
    private const string SampleOutput = """
        [Parsed_ebur128_0 @ 0x1234] Summary:

          Integrated loudness:
            I:         -24.0 LUFS
            Threshold: -34.0 LUFS

          Loudness range:
            LRA:        10.5 LU
            LRA low:   -30.0 LUFS
            LRA high:  -19.5 LUFS

          True peak:
            Peak:        -1.2 dBTP

          Sample peak:
            Peak:        -1.5 dBFS
        """;

    [Fact]
    public void Parse_ExtractsIntegratedLufs()
    {
        LoudnessAnalysisResult result = Ebur128OutputParser.Parse(SampleOutput);

        Assert.Equal(-24.0m, result.IntegratedLufs);
    }

    [Fact]
    public void Parse_ExtractsLoudnessRange()
    {
        LoudnessAnalysisResult result = Ebur128OutputParser.Parse(SampleOutput);

        Assert.Equal(10.5m, result.LoudnessRange);
    }

    [Fact]
    public void Parse_ExtractsTruePeak()
    {
        LoudnessAnalysisResult result = Ebur128OutputParser.Parse(SampleOutput);

        Assert.Equal(-1.2m, result.TruePeakDbtp);
    }

    [Fact]
    public void Parse_ExtractsSamplePeak()
    {
        LoudnessAnalysisResult result = Ebur128OutputParser.Parse(SampleOutput);

        Assert.Equal(-1.5m, result.SamplePeakDbfs);
    }

    [Fact]
    public void Parse_WithEmptyInput_ReturnsAllNulls()
    {
        LoudnessAnalysisResult result = Ebur128OutputParser.Parse(string.Empty);

        Assert.Null(result.IntegratedLufs);
        Assert.Null(result.LoudnessRange);
        Assert.Null(result.TruePeakDbtp);
        Assert.Null(result.SamplePeakDbfs);
    }

    [Fact]
    public void Parse_WithPartialOutput_ReturnsAvailableValues()
    {
        string partial = """
            Integrated loudness:
              I:         -20.0 LUFS
            """;

        LoudnessAnalysisResult result = Ebur128OutputParser.Parse(partial);

        Assert.Equal(-20.0m, result.IntegratedLufs);
        Assert.Null(result.LoudnessRange);
        Assert.Null(result.TruePeakDbtp);
        Assert.Null(result.SamplePeakDbfs);
    }

    [Fact]
    public void Parse_WithNonNumericValue_ReturnsNull()
    {
        string malformed = """
            Integrated loudness:
              I:         n/a LUFS
            """;

        LoudnessAnalysisResult result = Ebur128OutputParser.Parse(malformed);

        Assert.Null(result.IntegratedLufs);
    }

    [Fact]
    public void Parse_WithPositivePeak_ParsesCorrectly()
    {
        string loud = """
            True peak:
              Peak:         0.3 dBTP
            Sample peak:
              Peak:         0.1 dBFS
            """;

        LoudnessAnalysisResult result = Ebur128OutputParser.Parse(loud);

        Assert.Equal(0.3m, result.TruePeakDbtp);
        Assert.Equal(0.1m, result.SamplePeakDbfs);
    }
}
