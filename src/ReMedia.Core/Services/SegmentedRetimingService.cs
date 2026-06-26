namespace ReMedia.Core.Services;

using ReMedia.Core.Models;

/// <summary>
/// Applies segment-based timing to timestamps. Each segment defines a time range
/// with its own stretch factor. Timestamps are scaled by the factor of the segment
/// they fall into. Gaps between segments use a factor of 1 (no change).
/// Timestamps that fall after all segments use the last segment's accumulated offset.
/// </summary>
public static class SegmentedRetimingService
{
    /// <summary>
    /// Retimes subtitle cues using per-segment stretch factors.
    /// </summary>
    public static IReadOnlyList<SubtitleCue> RetimeSubtitles(
        IReadOnlyList<SubtitleCue> cues,
        IReadOnlyList<TimingSegment> segments)
    {
        ValidateSegments(segments);

        return cues.Select(cue => new SubtitleCue(
            cue.Index,
            ScaleTimestamp(cue.Start, segments),
            ScaleTimestamp(cue.End, segments),
            cue.Text)).ToArray();
    }

    /// <summary>
    /// Retimes chapters using per-segment stretch factors.
    /// </summary>
    public static IReadOnlyList<MediaChapterInfo> RetimeChapters(
        IReadOnlyList<MediaChapterInfo> chapters,
        IReadOnlyList<TimingSegment> segments)
    {
        ValidateSegments(segments);

        return chapters.Select(chapter => new MediaChapterInfo(
            chapter.Id,
            ScaleTimestamp(chapter.Start, segments),
            ScaleTimestamp(chapter.End, segments),
            chapter.Title)).ToArray();
    }

    /// <summary>
    /// Scales a single timestamp through the segment chain.
    /// The output time is computed by walking through segments in order,
    /// accumulating the offset difference caused by each segment's stretch.
    /// </summary>
    internal static TimeSpan ScaleTimestamp(TimeSpan sourceTime, IReadOnlyList<TimingSegment> segments)
    {
        // Walk through time from 0 to sourceTime, applying each segment's factor
        // to the portion of time that overlaps with it.
        double outputTicks = 0;
        TimeSpan cursor = TimeSpan.Zero;

        foreach (TimingSegment segment in segments)
        {
            if (cursor >= sourceTime)
            {
                break;
            }

            // Gap before this segment: passes through at factor 1.0
            if (segment.Start > cursor)
            {
                TimeSpan gapEnd = sourceTime < segment.Start ? sourceTime : segment.Start;
                outputTicks += (gapEnd - cursor).Ticks;
                cursor = gapEnd;

                if (cursor >= sourceTime)
                {
                    break;
                }
            }

            // Segment range
            TimeSpan segEnd = segment.End ?? TimeSpan.MaxValue;
            TimeSpan rangeEnd = sourceTime < segEnd ? sourceTime : segEnd;

            if (rangeEnd > cursor)
            {
                double segmentTicks = (rangeEnd - cursor).Ticks;
                outputTicks += segmentTicks * (double)segment.StretchFactor;
                cursor = rangeEnd;
            }
        }

        // Time after all segments: passes through at factor 1.0
        if (cursor < sourceTime)
        {
            outputTicks += (sourceTime - cursor).Ticks;
        }

        return TimeSpan.FromTicks(Convert.ToInt64(Math.Round(outputTicks, MidpointRounding.AwayFromZero)));
    }

    private static void ValidateSegments(IReadOnlyList<TimingSegment> segments)
    {
        if (segments.Count == 0)
        {
            throw new ArgumentException("At least one timing segment is required.", nameof(segments));
        }

        for (int i = 0; i < segments.Count; i++)
        {
            if (segments[i].StretchFactor <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(segments), $"Segment {i} has invalid stretch factor.");
            }

            if (segments[i].End.HasValue && segments[i].End.Value <= segments[i].Start)
            {
                throw new ArgumentException($"Segment {i} end must be after start.", nameof(segments));
            }

            if (i > 0 && segments[i].Start < segments[i - 1].Start)
            {
                throw new ArgumentException("Segments must be ordered by start time.", nameof(segments));
            }
        }
    }
}
