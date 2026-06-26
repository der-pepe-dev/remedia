namespace ReMedia.Core.Services;

using System.Globalization;
using System.Text;
using ReMedia.Core.Models;

/// <summary>
/// Writes chapter data in ffmetadata format.
/// This is a pure .NET writer — no external tools required.
/// </summary>
public static class FfmetadataWriter
{
    public static string WriteChapters(IReadOnlyList<MediaChapterInfo> chapters)
    {
        StringBuilder sb = new();
        sb.AppendLine(";FFMETADATA1");

        foreach (MediaChapterInfo chapter in chapters)
        {
            sb.AppendLine("[CHAPTER]");
            sb.AppendLine("TIMEBASE=1/1000");
            sb.Append("START=");
            sb.AppendLine(ToMillis(chapter.Start).ToString(CultureInfo.InvariantCulture));
            sb.Append("END=");
            sb.AppendLine(ToMillis(chapter.End).ToString(CultureInfo.InvariantCulture));

            if (!string.IsNullOrEmpty(chapter.Title))
            {
                sb.Append("title=");
                sb.AppendLine(EscapeMetadataValue(chapter.Title));
            }
        }

        return sb.ToString();
    }

    public static async Task WriteChaptersToFileAsync(
        IReadOnlyList<MediaChapterInfo> chapters,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        string content = WriteChapters(chapters);
        await File.WriteAllTextAsync(outputPath, content, Encoding.UTF8, cancellationToken);
    }

    private static long ToMillis(TimeSpan time)
    {
        return (long)Math.Round(time.TotalMilliseconds, MidpointRounding.AwayFromZero);
    }

    private static string EscapeMetadataValue(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("=", "\\=")
            .Replace(";", "\\;")
            .Replace("#", "\\#")
            .Replace("\n", "\\\n");
    }
}
