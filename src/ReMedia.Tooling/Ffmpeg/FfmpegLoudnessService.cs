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
}
