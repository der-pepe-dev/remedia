namespace ReMedia.Core.Diagnostics;

/// <summary>
/// Abstraction over external process execution.
/// Implementations live in ReMedia.Tooling; the rest of the solution
/// depends only on this interface.
/// </summary>
public interface IProcessRunner
{
    Task<ProcessExecutionResult> RunAsync(
        string executablePath,
        string arguments,
        CancellationToken cancellationToken = default);
}
