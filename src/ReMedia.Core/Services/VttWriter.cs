namespace ReMedia.Core.Services;

using System.Globalization;
using System.Text;
using ReMedia.Core.Models;

/// <summary>
/// Writes <see cref="SubtitleCue"/> records to WebVTT (.vtt) format.
/// </summary>
public static class VttWriter
{
    public static string Write(IReadOnlyList<SubtitleCue> cues)
    {
        StringBuilder sb = new();
        sb.AppendLine("WEBVTT");
        sb.AppendLine();

        for (int i = 0; i < cues.Count; i++)
        {
            SubtitleCue cue = cues[i];

            sb.AppendLine(cue.Index.ToString(CultureInfo.InvariantCulture));
            sb.Append(FormatTimestamp(cue.Start));
            sb.Append(" --> ");
            sb.AppendLine(FormatTimestamp(cue.End));
            sb.AppendLine(cue.Text);

            if (i < cues.Count - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    public static async Task WriteToFileAsync(
        IReadOnlyList<SubtitleCue> cues,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        string content = Write(cues);
        await File.WriteAllTextAsync(outputPath, content, Encoding.UTF8, cancellationToken);
    }

    private static string FormatTimestamp(TimeSpan value)
    {
        return $"{(int)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}.{value.Milliseconds:000}";
    }
}
