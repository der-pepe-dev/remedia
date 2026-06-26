namespace ReMedia.Tooling.Configuration;

public sealed record ExternalToolPaths(
    string FfprobePath,
    string FfmpegPath)
{
    public static ExternalToolPaths ResolveFromEnvironment()
    {
        string ffprobe = Environment.GetEnvironmentVariable("REMEDIA_FFPROBE_PATH") ?? "ffprobe";
        string ffmpeg = Environment.GetEnvironmentVariable("REMEDIA_FFMPEG_PATH") ?? "ffmpeg";
        return new ExternalToolPaths(ffprobe, ffmpeg);
    }
}
