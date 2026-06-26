namespace ReMedia.Core.Interfaces;

using ReMedia.Core.Models;

/// <summary>
/// Exports chapter metadata from a media file.
/// </summary>
public interface IChapterExportService
{
    Task<ToolOperationResult> ExportAsync(
        string inputPath,
        string outputPath,
        CancellationToken cancellationToken = default);
}
