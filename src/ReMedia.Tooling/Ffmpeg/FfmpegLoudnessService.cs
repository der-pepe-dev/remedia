namespace ReMedia.Tooling.Ffmpeg;

using ReMedia.Core.Diagnostics;
using ReMedia.Core.Interfaces;
using ReMedia.Core.Models;
using ReMedia.Core.Validation;
using ReMedia.Tooling.Configuration;

public sealed class FfmpegLoudnessService : ILoudnessService
{
    private readonly ExternalToolPaths _toolPaths;
    private readonly IProcessRunner _processRunner;

    public FfmpegLoudnessService(ExternalToolPaths toolPaths, IProcessRunner processRunner)
    {
        _toolPaths = toolPaths;
        _processRunner = processRunner;
    }

    public async Task<LoudnessAnalysisResult> AnalyzeAsync(
        string inputPath,
        int streamIndex,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNullOrWhiteSpace(inputPath, nameof(inputPath));

        string arguments = FfmpegArgumentBuilder.BuildLoudnessAnalysisArguments(inputPath, streamIndex);
        ProcessExecutionResult result = await _processRunner.RunAsync(_toolPaths.FfmpegPath, arguments, cancellationToken);

        if (!result.Succeeded)
        {
            throw new ProcessExecutionException(result);
        }

        return Ebur128OutputParser.Parse(result.StandardError);
    }

    public ClippingPredictionResult PredictClipping(LoudnessAnalysisResult current, decimal appliedGainDb)
    {
        decimal? predictedSamplePeak = current.SamplePeakDbfs.HasValue
            ? current.SamplePeakDbfs.Value + appliedGainDb
            : null;

        decimal? predictedTruePeak = current.TruePeakDbtp.HasValue
            ? current.TruePeakDbtp.Value + appliedGainDb
            : null;

        bool danger = predictedTruePeak > 0m || predictedSamplePeak > 0m;
        bool warning = !danger && (predictedTruePeak > -1m || predictedSamplePeak > -1m);

        string message = (danger, warning) switch
        {
            (true, _) => $"Clipping expected. Predicted true peak: {predictedTruePeak:0.#} dBTP, sample peak: {predictedSamplePeak:0.#} dBFS.",
            (_, true) => $"Near clipping. Predicted true peak: {predictedTruePeak:0.#} dBTP, sample peak: {predictedSamplePeak:0.#} dBFS.",
            _ => "No clipping expected."
        };

        return new ClippingPredictionResult(warning, danger, appliedGainDb, predictedSamplePeak, predictedTruePeak, message);
    }

    public LoudnessMatchResult MatchToTarget(LoudnessAnalysisResult current, decimal targetLufs, decimal truePeakCeilingDbtp)
    {
        if (!current.IntegratedLufs.HasValue)
        {
            return new LoudnessMatchResult(
                targetLufs, MeasuredLufs: null, RawGainDb: null, RecommendedGainDb: null,
                truePeakCeilingDbtp, CeilingEnforced: false, GainLimitedByCeiling: false,
                AchievedLufs: null, ShortfallDb: null, Clipping: null,
                "No integrated loudness measured; cannot recommend a gain.");
        }

        decimal measured = current.IntegratedLufs.Value;
        decimal rawGain = Round(targetLufs - measured);

        bool ceilingEnforced = current.TruePeakDbtp.HasValue;
        decimal recommendedGain = rawGain;
        if (ceilingEnforced)
        {
            decimal ceilingGain = Round(truePeakCeilingDbtp - current.TruePeakDbtp!.Value);
            recommendedGain = Math.Min(rawGain, ceilingGain);
        }

        bool limited = recommendedGain < rawGain;
        decimal achieved = Round(measured + recommendedGain);
        decimal shortfall = limited ? Round(targetLufs - achieved) : 0m;
        ClippingPredictionResult clipping = PredictClipping(current, recommendedGain);

        string message = BuildMessage(targetLufs, truePeakCeilingDbtp, recommendedGain, achieved, shortfall, ceilingEnforced, limited);

        return new LoudnessMatchResult(
            targetLufs, measured, rawGain, recommendedGain, truePeakCeilingDbtp,
            ceilingEnforced, limited, achieved, shortfall, clipping, message);
    }

    private static string BuildMessage(
        decimal targetLufs, decimal ceiling, decimal recommendedGain,
        decimal achieved, decimal shortfall, bool ceilingEnforced, bool limited)
    {
        if (limited)
        {
            return $"Limited to {recommendedGain:+0.0;-0.0;0} dB by the {ceiling:0.#} dBTP ceiling; "
                + $"reaches {achieved:0.0} LUFS ({shortfall:0.0} dB short of {targetLufs:0.#}).";
        }

        string suffix = ceilingEnforced
            ? string.Empty
            : " (true-peak ceiling not enforced — no true-peak measurement).";
        return $"Apply {recommendedGain:+0.0;-0.0;0} dB to reach {targetLufs:0.#} LUFS.{suffix}";
    }

    private static decimal Round(decimal value)
    {
        return Math.Round(value, 1, MidpointRounding.AwayFromZero);
    }
}
