namespace ReMedia.Core.Models;

public sealed record MediaChapterInfo(
    long Id,
    TimeSpan Start,
    TimeSpan End,
    string? Title);
