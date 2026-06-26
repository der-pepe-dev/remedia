namespace ReMedia.Core.Models;

public readonly record struct Rational(int Numerator, int Denominator)
{
    public static readonly Rational Zero = new(0, 1);

    public decimal ToDecimal()
    {
        return Denominator == 0 ? 0m : (decimal)Numerator / Denominator;
    }

    public override string ToString()
    {
        return $"{Numerator}/{Denominator}";
    }

    public static Rational ParseOrZero(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Zero;
        }

        string[] parts = value.Split('/', StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            return Zero;
        }

        if (!int.TryParse(parts[0], out int numerator) || !int.TryParse(parts[1], out int denominator))
        {
            return Zero;
        }

        // A zero denominator is not a valid rate; never silently rewrite it to a
        // valid-looking value. Return Zero so bad FPS data is caught downstream.
        if (denominator == 0)
        {
            return Zero;
        }

        return new Rational(numerator, denominator);
    }
}
