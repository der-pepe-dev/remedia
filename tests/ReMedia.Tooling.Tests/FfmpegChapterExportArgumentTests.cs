namespace ReMedia.Tooling.Tests;

using ReMedia.Tooling.Ffmpeg;

public sealed class FfmpegChapterExportArgumentTests
{
    [Fact]
    public void BuildChapterExportArguments_ContainsInputPath()
    {
        string arguments = FfmpegArgumentBuilder.BuildChapterExportArguments(
            @"C:\in\movie.mkv",
            @"C:\out\chapters.txt");

        Assert.Contains(@"""C:\in\movie.mkv""", arguments);
    }

    [Fact]
    public void BuildChapterExportArguments_ContainsOutputPath()
    {
        string arguments = FfmpegArgumentBuilder.BuildChapterExportArguments(
            @"C:\in\movie.mkv",
            @"C:\out\chapters.txt");

        Assert.Contains(@"""C:\out\chapters.txt""", arguments);
    }

    [Fact]
    public void BuildChapterExportArguments_UsesFfmetadataFormat()
    {
        string arguments = FfmpegArgumentBuilder.BuildChapterExportArguments(
            @"C:\in\movie.mkv",
            @"C:\out\chapters.txt");

        Assert.Contains("-f ffmetadata", arguments);
    }

    [Fact]
    public void BuildChapterExportArguments_IncludesOverwriteFlag()
    {
        string arguments = FfmpegArgumentBuilder.BuildChapterExportArguments(
            @"C:\in\movie.mkv",
            @"C:\out\chapters.txt");

        Assert.StartsWith("-y", arguments);
    }
}
