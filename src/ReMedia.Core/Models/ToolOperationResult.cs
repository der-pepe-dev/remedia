namespace ReMedia.Core.Models;

/// <summary>
/// Captures the outcome of a single tool operation such as a track export,
/// chapter export, or future mux/loudness pass.
/// </summary>
public sealed record ToolOperationResult(
    string OperationName,
    string OutputPath,
    string GeneratedCommand,
    bool Succeeded,
    int ExitCode,
    TimeSpan Duration,
    string? ErrorDetail);
