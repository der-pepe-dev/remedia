namespace ReMedia.Tooling.Ffmpeg;

using ReMedia.Core.Diagnostics;
using ReMedia.Core.Interfaces;
using ReMedia.Core.Models;
using ReMedia.Core.Validation;
using ReMedia.Tooling.Configuration;

public sealed class FfmpegMuxService : IMuxService
{
    private readonly ExternalToolPaths _toolPaths;
    private readonly IProcessRunner _processRunner;

    public FfmpegMuxService(ExternalToolPaths toolPaths, IProcessRunner processRunner)
    {
        _toolPaths = toolPaths;
        _processRunner = processRunner;
    }

    public async Task<ToolOperationResult> MuxAsync(MuxRequest request, CancellationToken cancellationToken = default)
    {
        Guard.AgainstNullOrWhiteSpace(request.OutputPath, nameof(request.OutputPath));

        string arguments = FfmpegArgumentBuilder.BuildMuxArguments(request);
        ProcessExecutionResult result = await _processRunner.RunAsync(_toolPaths.FfmpegPath, arguments, cancellationToken);

        return new ToolOperationResult(
            "mux",
            request.OutputPath,
            $"{_toolPaths.FfmpegPath} {arguments}",
            result.Succeeded,
            result.ExitCode,
            result.Duration,
            result.Succeeded ? null : result.StandardError);
    }
}
