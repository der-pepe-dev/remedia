namespace ReMedia.Core.Services;

using ReMedia.Core.Models;

/// <summary>
/// Applies a stretch factor to subtitle cue timestamps.
/// Used when exporting subtitles alongside retimed audio.
/// </summary>
public static class SubtitleRetimingService
{
    public static IReadOnlyList<SubtitleCue> Retime(
        IReadOnlyList<SubtitleCue> cues,
        decimal stretchFactor)
    {
        if (stretchFactor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stretchFactor), "Stretch factor must be greater than zero.");
        }

        if (stretchFactor == 1m)
        {
            return cues;
        }

        return cues.Select(cue => new SubtitleCue(
            cue.Index,
            ScaleTimeSpan(cue.Start, stretchFactor),
            ScaleTimeSpan(cue.End, stretchFactor),
            cue.Text)).ToArray();
    }

    private static TimeSpan ScaleTimeSpan(TimeSpan value, decimal factor)
    {
        double scaledTicks = value.Ticks * (double)factor;
        return TimeSpan.FromTicks(Convert.ToInt64(Math.Round(scaledTicks, MidpointRounding.AwayFromZero)));
    }
}
