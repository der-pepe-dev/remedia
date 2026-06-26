namespace ReMedia.Core.Models;

/// <summary>
/// Defines a time range with its own stretch factor.
/// Used for segment-based timing where different parts of a file
/// have different PAL/NTSC characteristics (e.g., hybrid sources,
/// commercial cuts, or speed-corrected segments).
/// </summary>
/// <param name="Start">Start time in the source timeline (inclusive).</param>
/// <param name="End">End time in the source timeline (exclusive). Null means end of file.</param>
/// <param name="StretchFactor">Stretch factor for this segment (sourceFps / targetFps).</param>
public sealed record TimingSegment(
    TimeSpan Start,
    TimeSpan? End,
    decimal StretchFactor)
{
    /// <summary>
    /// Creates a segment from FPS values.
    /// </summary>
    public static TimingSegment FromFps(TimeSpan start, TimeSpan? end, decimal sourceFps, decimal targetFps)
    {
        if (sourceFps <= 0) throw new ArgumentOutOfRangeException(nameof(sourceFps));
        if (targetFps <= 0) throw new ArgumentOutOfRangeException(nameof(targetFps));

        return new TimingSegment(start, end, sourceFps / targetFps);
    }

    /// <summary>
    /// Returns true if the given time falls within this segment.
    /// </summary>
    public bool Contains(TimeSpan time) =>
        time >= Start && (End is null || time < End.Value);
}
