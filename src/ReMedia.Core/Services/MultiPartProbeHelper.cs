namespace ReMedia.Core.Services;

using ReMedia.Core.Interfaces;
using ReMedia.Core.Models;

/// <summary>
/// Combines probe results from multiple source parts into a single coherent result.
/// Chapters from each part are shifted by the cumulative duration of all prior parts.
/// Chapter IDs are renumbered sequentially across all parts.
/// Duration is the sum of all parts.
/// Streams are taken from the primary (first) part only.
/// </summary>
public static class MultiPartProbeHelper
{
    /// <summary>
    /// Probes all parts and returns a combined result.
    /// </summary>
    public static async Task<MultiPartProbeResult> ProbeAllAsync(
        IMediaProbeService probeService,
        IReadOnlyList<string> allPaths,
        CancellationToken cancellationToken = default)
    {
        if (allPaths.Count == 0)
        {
            throw new ArgumentException("At least one input path is required.", nameof(allPaths));
        }

        MediaProbeResult primary = await probeService.ProbeAsync(allPaths[0], cancellationToken);

        if (allPaths.Count == 1)
        {
            return new MultiPartProbeResult(primary, [primary.Duration ?? TimeSpan.Zero]);
        }

        List<TimeSpan> partDurations = [primary.Duration ?? TimeSpan.Zero];
        List<MediaChapterInfo> allChapters = [.. primary.Chapters];
        TimeSpan cumulativeOffset = primary.Duration ?? TimeSpan.Zero;
        long nextChapterId = allChapters.Count > 0 ? allChapters[^1].Id + 1 : 1;

        for (int i = 1; i < allPaths.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            MediaProbeResult partResult = await probeService.ProbeAsync(allPaths[i], cancellationToken);
            TimeSpan partDuration = partResult.Duration ?? TimeSpan.Zero;
            partDurations.Add(partDuration);

            foreach (MediaChapterInfo chapter in partResult.Chapters)
            {
                allChapters.Add(new MediaChapterInfo(
                    nextChapterId++,
                    chapter.Start + cumulativeOffset,
                    chapter.End + cumulativeOffset,
                    chapter.Title));
            }

            cumulativeOffset += partDuration;
        }

        TimeSpan totalDuration = partDurations.Aggregate(TimeSpan.Zero, (sum, d) => sum + d);

        MediaProbeResult combined = primary with
        {
            Duration = totalDuration,
            Chapters = allChapters,
        };

        return new MultiPartProbeResult(combined, partDurations);
    }

    /// <summary>
    /// Shifts chapter timestamps by an offset. Useful for manually merging chapters
    /// from a specific part into an existing list.
    /// </summary>
    public static IReadOnlyList<MediaChapterInfo> ShiftChapters(
        IReadOnlyList<MediaChapterInfo> chapters,
        TimeSpan offset,
        int startId = 1)
    {
        return chapters.Select((ch, idx) => new MediaChapterInfo(
            startId + idx,
            ch.Start + offset,
            ch.End + offset,
            ch.Title)).ToArray();
    }
}

/// <summary>
/// Result of probing a multi-part source.
/// Contains the combined probe result and individual part durations
/// (useful for offset calculations in subtitle post-processing).
/// </summary>
public sealed record MultiPartProbeResult(
    MediaProbeResult Combined,
    IReadOnlyList<TimeSpan> PartDurations);
