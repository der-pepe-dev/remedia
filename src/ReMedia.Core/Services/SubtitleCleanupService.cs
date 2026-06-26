namespace ReMedia.Core.Services;

using System.Text.RegularExpressions;
using ReMedia.Core.Models;

/// <summary>
/// Cleans up subtitle cues by stripping SDH annotations, HTML tags,
/// fixing overlapping cue timing, and removing empty cues.
/// Each operation is independent and can be combined.
/// </summary>
public static partial class SubtitleCleanupService
{
    /// <summary>
    /// Applies all cleanup operations: strip SDH, strip HTML, fix overlaps, remove empty.
    /// </summary>
    public static IReadOnlyList<SubtitleCue> CleanAll(IReadOnlyList<SubtitleCue> cues)
    {
        IReadOnlyList<SubtitleCue> result = StripSdh(cues);
        result = StripHtmlTags(result);
        result = FixOverlaps(result);
        result = RemoveEmpty(result);
        return result;
    }

    /// <summary>
    /// Strips SDH (Subtitles for the Deaf and Hard-of-hearing) annotations.
    /// Removes text in square brackets like [music], [laughing], (door slams), etc.
    /// </summary>
    public static IReadOnlyList<SubtitleCue> StripSdh(IReadOnlyList<SubtitleCue> cues)
    {
        return cues.Select(cue =>
        {
            string cleaned = SquareBracketPattern().Replace(cue.Text, "");
            cleaned = RoundBracketPattern().Replace(cleaned, "");
            cleaned = CleanBlankLines(cleaned);
            return cue with { Text = cleaned };
        }).ToArray();
    }

    /// <summary>
    /// Strips HTML/rich-text tags like &lt;i&gt;, &lt;b&gt;, &lt;font&gt;, etc.
    /// </summary>
    public static IReadOnlyList<SubtitleCue> StripHtmlTags(IReadOnlyList<SubtitleCue> cues)
    {
        return cues.Select(cue =>
        {
            string cleaned = HtmlTagPattern().Replace(cue.Text, "");
            return cue with { Text = cleaned };
        }).ToArray();
    }

    /// <summary>
    /// Fixes overlapping cues by trimming the end of a cue to not exceed
    /// the start of the next cue (with a minimum 1ms gap).
    /// </summary>
    public static IReadOnlyList<SubtitleCue> FixOverlaps(IReadOnlyList<SubtitleCue> cues)
    {
        if (cues.Count < 2)
        {
            return cues;
        }

        List<SubtitleCue> result = [cues[0]];

        for (int i = 1; i < cues.Count; i++)
        {
            SubtitleCue prev = result[^1];
            SubtitleCue current = cues[i];

            if (prev.End > current.Start)
            {
                TimeSpan fixedEnd = current.Start - TimeSpan.FromMilliseconds(1);
                if (fixedEnd < prev.Start)
                {
                    fixedEnd = prev.Start;
                }

                result[^1] = prev with { End = fixedEnd };
            }

            result.Add(current);
        }

        return result;
    }

    /// <summary>
    /// Removes cues that have empty or whitespace-only text after cleanup.
    /// Re-indexes the remaining cues sequentially starting from 1.
    /// </summary>
    public static IReadOnlyList<SubtitleCue> RemoveEmpty(IReadOnlyList<SubtitleCue> cues)
    {
        int index = 1;
        return cues
            .Where(c => !string.IsNullOrWhiteSpace(c.Text))
            .Select(c => c with { Index = index++ })
            .ToArray();
    }

    private static string CleanBlankLines(string text)
    {
        string[] lines = text.Split('\n');
        IEnumerable<string> nonEmpty = lines
            .Select(l => l.TrimEnd('\r').Trim())
            .Where(l => l.Length > 0);
        return string.Join(Environment.NewLine, nonEmpty);
    }

    [GeneratedRegex(@"\[.*?\]", RegexOptions.Singleline)]
    private static partial Regex SquareBracketPattern();

    [GeneratedRegex(@"\(.*?\)", RegexOptions.Singleline)]
    private static partial Regex RoundBracketPattern();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagPattern();
}
