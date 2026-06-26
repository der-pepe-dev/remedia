namespace ReMedia.Core.Diagnostics;

/// <summary>
/// Captures the full outcome of running an external process.
/// </summary>
public sealed record ProcessExecutionResult(
    string ExecutablePath,
    string Arguments,
    int ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Duration)
{
    public bool Succeeded => ExitCode == 0;
}
