namespace ReMedia.Tooling.Ffmpeg;

using System.Globalization;
using System.Text.RegularExpressions;
using ReMedia.Core.Models;

/// <summary>
/// Parses the EBU R128 summary block from ffmpeg stderr output.
/// The ebur128 filter prints a summary section at the end like:
///   Summary:
///     Integrated loudness:
///       I:         -24.0 LUFS
///       Threshold: -34.0 LUFS
///     Loudness range:
///       LRA:        10.5 LU
///     True peak:
///       Peak:        -1.2 dBTP
///     Sample peak:
///       Peak:        -1.5 dBFS
/// </summary>
public static partial class Ebur128OutputParser
{
    public static LoudnessAnalysisResult Parse(string stderr)
    {
        int summaryIdx = stderr.LastIndexOf("Summary:", StringComparison.OrdinalIgnoreCase);
        string block = summaryIdx >= 0 ? stderr[summaryIdx..] : stderr;

        decimal? integratedLufs = MatchDecimal(IntegratedPattern(), block);
        decimal? loudnessRange = MatchDecimal(LraPattern(), block);
        decimal? truePeakDbtp = MatchDecimal(TruePeakPattern(), block);
        decimal? samplePeakDbfs = MatchDecimal(SamplePeakPattern(), block);

        return new LoudnessAnalysisResult(integratedLufs, loudnessRange, truePeakDbtp, samplePeakDbfs);
    }

    private static decimal? MatchDecimal(Regex pattern, string input)
    {
        Match match = pattern.Match(input);
        if (match.Success && decimal.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
        {
            return result;
        }

        return null;
    }

    [GeneratedRegex(@"I:\s+(-?\d+\.?\d*)\s+LUFS", RegexOptions.Compiled)]
    private static partial Regex IntegratedPattern();

    [GeneratedRegex(@"LRA:\s+(-?\d+\.?\d*)\s+LU", RegexOptions.Compiled)]
    private static partial Regex LraPattern();

    [GeneratedRegex(@"True peak:.*?Peak:\s+(-?\d+\.?\d*)\s+dBTP", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex TruePeakPattern();

    [GeneratedRegex(@"Sample peak:.*?Peak:\s+(-?\d+\.?\d*)\s+dBFS", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex SamplePeakPattern();
}
