namespace ReMedia.Core.Services;

using ReMedia.Core.Models;

/// <summary>
/// Applies a stretch factor to chapter timestamps.
/// Used when exporting chapters alongside retimed audio.
/// </summary>
public static class ChapterRetimingService
{
    public static IReadOnlyList<MediaChapterInfo> Retime(
        IReadOnlyList<MediaChapterInfo> chapters,
        decimal stretchFactor)
    {
        if (stretchFactor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stretchFactor), "Stretch factor must be greater than zero.");
        }

        if (stretchFactor == 1m)
        {
            return chapters;
        }

        return chapters.Select(chapter => new MediaChapterInfo(
            chapter.Id,
            ScaleTimeSpan(chapter.Start, stretchFactor),
            ScaleTimeSpan(chapter.End, stretchFactor),
            chapter.Title)).ToArray();
    }

    private static TimeSpan ScaleTimeSpan(TimeSpan value, decimal factor)
    {
        double scaledTicks = value.Ticks * (double)factor;
        return TimeSpan.FromTicks(Convert.ToInt64(Math.Round(scaledTicks, MidpointRounding.AwayFromZero)));
    }
}
