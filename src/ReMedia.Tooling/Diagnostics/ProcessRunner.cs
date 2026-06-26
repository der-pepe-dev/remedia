namespace ReMedia.Tooling.Diagnostics;

using ReMedia.Core.Diagnostics;

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessExecutionResult> RunAsync(
        string executablePath,
        string arguments,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset start = DateTimeOffset.UtcNow;

        using System.Diagnostics.Process process = new();
        process.StartInfo.FileName = executablePath;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.Start();

        try
        {
            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            await Task.WhenAll(stdoutTask, stderrTask);
            await process.WaitForExitAsync(cancellationToken);

            TimeSpan duration = DateTimeOffset.UtcNow - start;

            return new ProcessExecutionResult(
                executablePath,
                arguments,
                process.ExitCode,
                stdoutTask.Result,
                stderrTask.Result,
                duration);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
            }

            throw;
        }
    }
}
