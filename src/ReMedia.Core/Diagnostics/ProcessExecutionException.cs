namespace ReMedia.Core.Diagnostics;

public sealed class ProcessExecutionException : Exception
{
    public ProcessExecutionException(ProcessExecutionResult result)
        : base($"Process execution failed with exit code {result.ExitCode}: {result.ExecutablePath} {result.Arguments}")
    {
        Result = result;
    }

    public ProcessExecutionResult Result { get; }
}
