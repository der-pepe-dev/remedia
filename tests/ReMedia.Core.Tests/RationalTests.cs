namespace ReMedia.Core.Tests;

using ReMedia.Core.Models;

public sealed class RationalTests
{
    [Fact]
    public void ParseOrZero_WithValidFraction_ReturnsExpected()
    {
        Rational result = Rational.ParseOrZero("24000/1001");
        Assert.Equal(24000, result.Numerator);
        Assert.Equal(1001, result.Denominator);
    }

    [Fact]
    public void ParseOrZero_WithZeroDenominator_ReturnsZero()
    {
        // A zero denominator is not a valid rate; never silently rewrite it.
        Rational result = Rational.ParseOrZero("25/0");
        Assert.Equal(Rational.Zero, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseOrZero_WithNullOrWhitespace_ReturnsZero(string? value)
    {
        Rational result = Rational.ParseOrZero(value);
        Assert.Equal(Rational.Zero, result);
    }

    [Theory]
    [InlineData("not/valid")]
    [InlineData("25")]
    [InlineData("25/a/b")]
    [InlineData("2//3")]
    public void ParseOrZero_WithMalformed_ReturnsZero(string value)
    {
        Rational result = Rational.ParseOrZero(value);
        Assert.Equal(Rational.Zero, result);
    }

    [Fact]
    public void ToDecimal_WithZeroDenominator_ReturnsZero()
    {
        Rational r = new(25, 0);
        Assert.Equal(0m, r.ToDecimal());
    }

    [Fact]
    public void ToDecimal_WithValidFraction_ReturnsExpected()
    {
        Rational r = new(24000, 1001);
        Assert.Equal(24000m / 1001m, r.ToDecimal());
    }

    [Fact]
    public void ToString_ReturnsSlashFormat()
    {
        Rational r = new(24000, 1001);
        Assert.Equal("24000/1001", r.ToString());
    }
}
