namespace ReMedia.Core.Services;

using System.Globalization;

/// <summary>
/// Shared helpers for parsing subtitle timestamps. Centralizes the fractional-seconds
/// handling so SRT and VTT parsers treat <c>.5</c> as 500ms (not 5ms).
/// </summary>
internal static class SubtitleTimeParsing
{
    /// <summary>
    /// Parses a fractional-seconds field (the part after the seconds separator) into
    /// milliseconds. The field is interpreted as a decimal fraction of a second, so
    /// "5" => 500ms, "50" => 500ms, "500" => 500ms. Accepts 1-3 digits only; anything
    /// else (empty, too long, non-digit, signed) is rejected.
    /// </summary>
    public static bool TryParseFractionalMs(string fraction, out int milliseconds)
    {
        milliseconds = 0;

        if (fraction.Length is < 1 or > 3)
        {
            return false;
        }

        if (!int.TryParse(fraction, NumberStyles.None, CultureInfo.InvariantCulture, out int value))
        {
            return false;
        }

        // Scale to thousandths: a 1-digit field is tenths, 2-digit is hundredths, 3-digit is already ms.
        for (int i = fraction.Length; i < 3; i++)
        {
            value *= 10;
        }

        milliseconds = value;
        return true;
    }
}
