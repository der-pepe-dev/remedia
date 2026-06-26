namespace ReMedia.Core.Interfaces;

using ReMedia.Core.Models;

public interface IMuxService
{
    Task<ToolOperationResult> MuxAsync(MuxRequest request, CancellationToken cancellationToken = default);
}
