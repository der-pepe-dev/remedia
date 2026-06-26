namespace ReMedia.Core.Interfaces;

using ReMedia.Core.Models;

public interface ITrackExportService
{
    Task<IReadOnlyList<ToolOperationResult>> ExportAsync(
        string inputPath,
        IReadOnlyCollection<TrackExportOptions> tracks,
        CancellationToken cancellationToken = default,
        bool concatDemuxer = false);
}
