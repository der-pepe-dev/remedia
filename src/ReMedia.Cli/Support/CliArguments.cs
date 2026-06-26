namespace ReMedia.Cli.Support;

using System.Globalization;

internal static class CliArguments
{
    public static string? ReadString(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    public static decimal? ReadDecimal(string[] args, string name)
    {
        string? value = ReadString(args, name);
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed) ? parsed : null;
    }

    public static TimeSpan? ReadTimeSpan(string[] args, string name)
    {
        string? value = ReadString(args, name);
        return TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out TimeSpan parsed) ? parsed : null;
    }

    public static List<string> ReadManyStrings(string[] args, string name)
    {
        List<string> values = [];

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                values.Add(args[i + 1]);
            }
        }

        return values;
    }

    public static List<int> ReadManyIntegers(string[] args, string name)
    {
        List<int> values = [];

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (int.TryParse(args[i + 1], out int parsed))
            {
                values.Add(parsed);
            }
        }

        return values;
    }

    /// <summary>True if the named flag appears in <paramref name="args"/> (value not required).</summary>
    public static bool HasFlag(string[] args, string name)
    {
        return Array.Exists(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Reads all integer values for a repeated flag, but fails (returns false) if any
    /// occurrence has a missing or non-integer value rather than silently dropping it.
    /// </summary>
    public static bool TryReadManyIntegers(string[] args, string name, out List<int> values)
    {
        values = [];

        for (int i = 0; i < args.Length; i++)
        {
            if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out int parsed))
            {
                return false;
            }

            values.Add(parsed);
        }

        return true;
    }
}
