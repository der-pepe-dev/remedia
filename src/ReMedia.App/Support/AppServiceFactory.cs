namespace ReMedia.App.Support;

using ReMedia.Core.Diagnostics;
using ReMedia.Core.Interfaces;
using ReMedia.Core.Services;
using ReMedia.Tooling.Configuration;
using ReMedia.Tooling.Diagnostics;
using ReMedia.Tooling.Ffmpeg;
using ReMedia.Tooling.Ffprobe;

/// <summary>
/// Composition root for the desktop app.
/// Creates all services once and exposes them as interfaces.
/// Only this class references ReMedia.Tooling concrete types.
/// </summary>
public sealed class AppServiceFactory
{
    public AppServiceFactory(IToolLogger? logger = null)
    {
        ExternalToolPaths toolPaths = ResolveToolPaths();
        IProcessRunner processRunner = new ProcessRunner();

        ProbeService = new FfprobeMediaProbeService(toolPaths, processRunner);
        TrackExportService = new FfmpegTrackExportService(toolPaths, processRunner);
        ChapterExportService = new FfmpegChapterExportService(toolPaths, processRunner);
        LoudnessService = new FfmpegLoudnessService(toolPaths, processRunner);
        MuxService = new FfmpegMuxService(toolPaths, processRunner);
        AudioSyncService = new FfmpegAudioSyncService(toolPaths, processRunner);
        TimingAnalysisService = new TimingAnalysisService();
        ExportWorkflowService = new ExportWorkflowService(TrackExportService, ChapterExportService, logger, MuxService);
    }

    public IMediaProbeService ProbeService { get; }
    public ITrackExportService TrackExportService { get; }
    public IChapterExportService ChapterExportService { get; }
    public ILoudnessService LoudnessService { get; }
    public IMuxService MuxService { get; }
    public IAudioSyncService AudioSyncService { get; }
    public ITimingAnalysisService TimingAnalysisService { get; }
    public ExportWorkflowService ExportWorkflowService { get; }

    private static ExternalToolPaths ResolveToolPaths()
    {
        return ExternalToolPaths.ResolveFromEnvironment();
    }
}
