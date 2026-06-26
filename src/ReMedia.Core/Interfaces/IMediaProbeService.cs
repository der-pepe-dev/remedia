namespace ReMedia.Core.Interfaces;

using ReMedia.Core.Models;

public interface IMediaProbeService
{
    Task<MediaProbeResult> ProbeAsync(string inputPath, CancellationToken cancellationToken = default);
}
