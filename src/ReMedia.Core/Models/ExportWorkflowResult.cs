namespace ReMedia.Core.Models;

/// <summary>
/// Aggregated result of a full export workflow (tracks + optional chapters + optional mux).
/// </summary>
public sealed record ExportWorkflowResult(
    IReadOnlyList<ToolOperationResult> TrackResults,
    ToolOperationResult? ChapterResult,
    ToolOperationResult? MuxResult,
    bool HasFailures);
