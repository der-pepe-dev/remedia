namespace ReMedia.Tooling.Ffmpeg;

using ReMedia.Core.Diagnostics;
using ReMedia.Core.Interfaces;
using ReMedia.Core.Models;
using ReMedia.Core.Validation;
using ReMedia.Tooling.Configuration;

public sealed class FfmpegChapterExportService : IChapterExportService
{
    private readonly ExternalToolPaths _toolPaths;
    private readonly IProcessRunner _processRunner;

    public FfmpegChapterExportService(ExternalToolPaths toolPaths, IProcessRunner processRunner)
    {
        _toolPaths = toolPaths;
        _processRunner = processRunner;
    }

    public async Task<ToolOperationResult> ExportAsync(
        string inputPath,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        Guard.AgainstNullOrWhiteSpace(inputPath, nameof(inputPath));

        string arguments = FfmpegArgumentBuilder.BuildChapterExportArguments(inputPath, outputPath);
        string command = $"{_toolPaths.FfmpegPath} {arguments}";

        try
        {
            ProcessExecutionResult result = await _processRunner.RunAsync(_toolPaths.FfmpegPath, arguments, cancellationToken);

            return new ToolOperationResult(
                "chapters",
                outputPath,
                command,
                result.Succeeded,
                result.ExitCode,
                result.Duration,
                result.Succeeded ? null : result.StandardError);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ToolOperationResult(
                "chapters",
                outputPath,
                command,
                Succeeded: false,
                ExitCode: -1,
                Duration: TimeSpan.Zero,
                ex.Message);
        }
    }
}
