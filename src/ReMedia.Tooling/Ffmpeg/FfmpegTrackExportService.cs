namespace ReMedia.Tooling.Ffmpeg;

using ReMedia.Core.Diagnostics;
using ReMedia.Core.Interfaces;
using ReMedia.Core.Models;
using ReMedia.Core.Validation;
using ReMedia.Tooling.Configuration;

public sealed class FfmpegTrackExportService : ITrackExportService
{
    private readonly ExternalToolPaths _toolPaths;
    private readonly IProcessRunner _processRunner;

    public FfmpegTrackExportService(ExternalToolPaths toolPaths, IProcessRunner processRunner)
    {
        _toolPaths = toolPaths;
        _processRunner = processRunner;
    }

    public async Task<IReadOnlyList<ToolOperationResult>> ExportAsync(
        string inputPath,
        IReadOnlyCollection<TrackExportOptions> tracks,
        CancellationToken cancellationToken = default,
        bool concatDemuxer = false)
    {
        Guard.AgainstNullOrWhiteSpace(inputPath, nameof(inputPath));

        List<ToolOperationResult> results = [];

        foreach (TrackExportOptions track in tracks)
        {
            string arguments = FfmpegArgumentBuilder.BuildTrackExportArguments(inputPath, track, concatDemuxer);
            string command = $"{_toolPaths.FfmpegPath} {arguments}";

            try
            {
                ProcessExecutionResult result = await _processRunner.RunAsync(_toolPaths.FfmpegPath, arguments, cancellationToken);

                results.Add(new ToolOperationResult(
                    $"stream {track.StreamIndex}",
                    track.OutputPath,
                    command,
                    result.Succeeded,
                    result.ExitCode,
                    result.Duration,
                    result.Succeeded ? null : result.StandardError));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                results.Add(new ToolOperationResult(
                    $"stream {track.StreamIndex}",
                    track.OutputPath,
                    command,
                    Succeeded: false,
                    ExitCode: -1,
                    Duration: TimeSpan.Zero,
                    ex.Message));
            }
        }

        return results;
    }
}
