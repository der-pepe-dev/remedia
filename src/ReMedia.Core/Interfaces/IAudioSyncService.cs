namespace ReMedia.Core.Interfaces;

using ReMedia.Core.Models;

public interface IAudioSyncService
{
    Task<AudioSyncAnalysisResult> AnalyzeSyncAsync(
        string sourcePath,
        int sourceStreamIndex,
        string destinationPath,
        int destinationStreamIndex,
        decimal stretchFactor = 1m,
        CancellationToken cancellationToken = default);
}
