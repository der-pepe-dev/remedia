namespace ReMedia.Core.Services;

using System.Globalization;

/// <summary>
/// Common frame rates used in PAL, NTSC, film, and web workflows.
/// Each entry has an exact decimal value and a display label.
/// </summary>
public static class FpsPresets
{
    public static IReadOnlyList<FpsPreset> All { get; } =
    [
        new("23.976 (NTSC Film)", 24000m / 1001m),
        new("24 (Film)", 24m),
        new("25 (PAL)", 25m),
        new("29.97 (NTSC)", 30000m / 1001m),
        new("30", 30m),
        new("48", 48m),
        new("50 (PAL High)", 50m),
        new("59.94 (NTSC High)", 60000m / 1001m),
        new("60", 60m),
    ];
}

public sealed record FpsPreset(string DisplayName, decimal Value)
{
    public string ValueText { get; } = Value.ToString("0.################", CultureInfo.InvariantCulture);

    public override string ToString() => DisplayName;
}
