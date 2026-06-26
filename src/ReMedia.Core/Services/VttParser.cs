namespace ReMedia.Core.Services;

using System.Globalization;
using System.Text;
using ReMedia.Core.Models;

/// <summary>
/// Parses WebVTT (.vtt) subtitle files into <see cref="SubtitleCue"/> records.
/// Handles the WEBVTT header, BOM, optional cue IDs, cue settings,
/// and cues separated by one or more blank lines.
/// </summary>
public static class VttParser
{
    public static IReadOnlyList<SubtitleCue> Parse(string content)
    {
        return Parse(content, out _);
    }

    /// <summary>
    /// Parses WebVTT content, also reporting cues that were dropped because their timing
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
        int cueIndex = 1;

        // Skip WEBVTT header and any header metadata
        while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
        {
            i++;
        }

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

            // A line containing "-->" is a timing line; otherwise it might be a cue ID
            string currentLine = lines[i].Trim();
            if (!currentLine.Contains("-->", StringComparison.Ordinal))
            {
                // This is a cue ID line — skip it and move to the timing line
                if (int.TryParse(currentLine, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedIndex))
                {
                    cueIndex = parsedIndex;
                }

                i++;
                if (i >= lines.Length)
                {
                    break;
                }

                currentLine = lines[i].Trim();
            }

            if (!TryParseTimingLine(currentLine, out TimeSpan start, out TimeSpan end))
            {
                warningList.Add($"Skipped cue with unparseable timing line: \"{currentLine}\"");
                i++;
                continue;
            }

            i++;

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

            cues.Add(new SubtitleCue(cueIndex, start, end, textBuilder.ToString()));
            cueIndex++;
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

        // End timestamp may be followed by cue settings (e.g., "position:10%")
        string remainder = line[(arrowIndex + 3)..].Trim();
        int spaceIdx = remainder.IndexOf(' ');
        string endText = spaceIdx >= 0 ? remainder[..spaceIdx] : remainder;

        return TryParseVttTimestamp(startText, out start) && TryParseVttTimestamp(endText, out end);
    }

    private static bool TryParseVttTimestamp(string text, out TimeSpan result)
    {
        result = TimeSpan.Zero;

        // WebVTT allows HH:MM:SS.mmm or MM:SS.mmm
        string[] colonParts = text.Split(':');
        if (colonParts.Length is not (2 or 3))
        {
            return false;
        }

        int hours = 0;
        int minuteIdx;

        if (colonParts.Length == 3)
        {
            if (!int.TryParse(colonParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out hours))
            {
                return false;
            }

            minuteIdx = 1;
        }
        else
        {
            minuteIdx = 0;
        }

        if (!int.TryParse(colonParts[minuteIdx], NumberStyles.Integer, CultureInfo.InvariantCulture, out int minutes))
        {
            return false;
        }

        string[] secParts = colonParts[minuteIdx + 1].Split('.');
        if (secParts.Length != 2)
        {
            return false;
        }

        if (!int.TryParse(secParts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int seconds) ||
            !SubtitleTimeParsing.TryParseFractionalMs(secParts[1], out int milliseconds))
        {
            return false;
        }

        result = new TimeSpan(0, hours, minutes, seconds, milliseconds);
        return true;
    }
}
