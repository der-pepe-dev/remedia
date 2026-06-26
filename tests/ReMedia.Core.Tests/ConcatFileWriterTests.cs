namespace ReMedia.Core.Tests;

using ReMedia.Core.Services;

public sealed class ConcatFileWriterTests
{
    [Fact]
    public void Write_SingleFile_ProducesCorrectContent()
    {
        string result = ConcatFileWriter.Write([@"C:\movies\part1.mkv"]);

        Assert.Contains("file 'C:\\movies\\part1.mkv'", result);
    }

    [Fact]
    public void Write_TwoParts_ListsBothFiles()
    {
        string result = ConcatFileWriter.Write([@"C:\part1.mkv", @"C:\part2.mkv"]);

        string[] lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Contains("part1.mkv", lines[0]);
        Assert.Contains("part2.mkv", lines[1]);
    }

    [Fact]
    public void Write_EscapesSingleQuotes()
    {
        string result = ConcatFileWriter.Write([@"C:\movie's file.mkv"]);

        Assert.Contains(@"file 'C:\movie'\''s file.mkv'", result);
    }

    [Fact]
    public void Write_EmptyList_Throws()
    {
        Assert.Throws<ArgumentException>(() => ConcatFileWriter.Write([]));
    }

    [Fact]
    public void Write_ThreeParts_ListsAllFiles()
    {
        string result = ConcatFileWriter.Write(["a.mkv", "b.mkv", "c.mkv"]);

        string[] lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
    }
}
