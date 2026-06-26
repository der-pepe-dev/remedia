namespace ReMedia.Core.Models;

/// <summary>
/// A single subtitle cue (entry) with timing and text.
/// Used for text-based subtitle formats like SRT and WebVTT.
/// </summary>
public sealed record SubtitleCue(
    int Index,
    TimeSpan Start,
    TimeSpan End,
    string Text);
