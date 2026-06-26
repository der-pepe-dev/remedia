namespace ReMedia.Core.Services;

using System.Text;

/// <summary>
/// Writes an ffmpeg concat demuxer file from a list of media file paths.
/// The concat demuxer allows treating multiple files as a single continuous source.
/// Format:
///   file '/path/to/part1.mkv'
///   file '/path/to/part2.mkv'
/// </summary>
public static class ConcatFileWriter
{
    public static string Write(IReadOnlyList<string> filePaths)
    {
        if (filePaths.Count == 0)
        {
            throw new ArgumentException("At least one file path is required.", nameof(filePaths));
        }

        StringBuilder sb = new();
        foreach (string path in filePaths)
        {
            // Inside the concat demuxer's single quotes, backslash is literal (Windows
            // paths are safe as-is); only a single quote is special, escaped as the
            // standard close/escape/reopen sequence.
            string escaped = path.Replace("'", "'\\''");
            sb.AppendLine($"file '{escaped}'");
        }

        return sb.ToString();
    }

    public static async Task<string> WriteToFileAsync(
        IReadOnlyList<string> filePaths,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        string content = Write(filePaths);
        await File.WriteAllTextAsync(outputPath, content, Encoding.UTF8, cancellationToken);
        return outputPath;
    }
}
