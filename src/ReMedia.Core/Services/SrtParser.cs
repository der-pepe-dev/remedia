namespace ReMedia.Core.Services;

using System.Globalization;
using System.Text;
using ReMedia.Core.Models;

/// <summary>
/// Parses SubRip (.srt) subtitle files into <see cref="SubtitleCue"/> records.
/// Handles BOM, Windows/Unix line endings, blank lines in cue text, and
/// cues separated by one or more blank lines.
/// </summary>
public static class SrtParser
{
    public static IReadOnlyList<SubtitleCue> Parse(string content)
    {
        return Parse(content, out _);
    }

    /// <summary>
    /// Parses SubRip content, also reporting cues that were dropped because their timing
    /// line could not be parsed. Per the domain rule "never hide timing-mismatch warnings".
    /// </summary>
    public static IReadOnlyList<SubtitleCue> Parse(string content, out IReadOnlyList<string> warnings)
    {
        List<string> warningList = [];
        warnings = warningList;

        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        content = content.TrimStart('\uFEFF');

        string[] lines = content.Split('\n');
        List<SubtitleCue> cues = [];

        int i = 0;
        while (i < lines.Length)
        {
            while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
            {
                i++;
            }

            if (i >= lines.Length)
            {
                break;
            }

            string indexLine = lines[i].Trim();
            if (!int.TryParse(indexLine, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
            {
                i++;
                continue;
            }

            i++;

            if (i >= lines.Length)
            {
                break;
            }

            string timingLine = lines[i].Trim();
            i++;

            if (!TryParseTimingLine(timingLine, out TimeSpan start, out TimeSpan end))
            {
                warningList.Add($"Skipped cue with unparseable timing line: \"{timingLine}\"");
                continue;
            }

            StringBuilder textBuilder = new();
            while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
            {
                if (textBuilder.Length > 0)
                {
                    textBuilder.AppendLine();
                }

                textBuilder.Append(lines[i].TrimEnd('\r'));
                i++;
            }

            cues.Add(new SubtitleCue(index, start, end, textBuilder.ToString()));
        }

        return cues;
    }

    public static IReadOnlyList<SubtitleCue> ParseFile(string filePath)
    {
        string content = File.ReadAllText(filePath, Encoding.UTF8);
        return Parse(content);
    }

    private static bool TryParseTimingLine(string line, out TimeSpan start, out TimeSpan end)
    {
        start = TimeSpan.Zero;
        end = TimeSpan.Zero;

        int arrowIndex = line.IndexOf("-->", StringComparison.Ordinal);
        if (arrowIndex < 0)
        {
            return false;
        }

        string startText = line[..arrowIndex].Trim();
        string endText = line[(arrowIndex + 3)..].Trim();

        return TryParseSrtTimestamp(startText, out start) && TryParseSrtTimestamp(endText, out end);
    }

    private static bool TryParseSrtTimestamp(string text, out TimeSpan result)
    {
        result = TimeSpan.Zero;

        string[] parts = text.Split(':', ',');
        if (parts.Length != 4)
        {
            return false;
        }

        if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int hours) ||
            !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int minutes) ||
            !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds) ||
            !SubtitleTimeParsing.TryParseFractionalMs(parts[3], out int milliseconds))
        {
            return false;
        }

        result = new TimeSpan(0, hours, minutes, seconds, milliseconds);
        return true;
    }
}
