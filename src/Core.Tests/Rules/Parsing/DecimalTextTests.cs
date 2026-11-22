using Core.Rules.Parsing;

namespace Core.Tests.Rules.Parsing;

public class DecimalTextTests
{
    [Theory]
    [InlineData("86.5", 86.5)]
    [InlineData("86,5", 86.5)]
    [InlineData("  2.5 ", 2.5)]
    [InlineData("80", 80)]
    public void A_comma_or_a_dot_is_the_decimal_mark(string text, double expected)
    {
        Assert.True(DecimalText.TryParse(text, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("abc")]
    public void Anything_else_is_not_a_number(string? text)
    {
        Assert.False(DecimalText.TryParse(text, out var value));
        Assert.Equal(0, value);
    }
}
