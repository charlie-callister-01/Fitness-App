namespace Ui.Tests;

public class BuildIdentifierTests
{
    [Theory]
    [InlineData("d8d8ea1f3c0b9a7e6d5c4b3a291807f6e5d4c3b2", "d8d8ea1")]
    [InlineData("d8d8ea1f3c0b9a7e6d5c4b3a291807f6e5d4c3b2+d8d8ea1f3c0b", "d8d8ea1")]
    [InlineData("abc", "abc")]
    [InlineData("1.0.0+abcdef", "1.0.0")]
    [InlineData("", "dev")]
    [InlineData("   ", "dev")]
    [InlineData("+abc", "dev")]
    [InlineData(null, "dev")]
    public void Shorten_gives_a_short_id_without_sdk_suffix(string? version, string expected)
    {
        Assert.Equal(expected, BuildIdentifier.Shorten(version));
    }
}
