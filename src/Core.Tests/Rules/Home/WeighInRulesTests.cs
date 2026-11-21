using System.Globalization;
using Core.Model;
using Core.Rules.Home;
using Core.Rules.Units;
using Core.Tests.Ids;
using Core.Tests.Rules.TestData;

namespace Core.Tests.Rules.Home;

public class WeighInRulesTests
{
    private static UnitConverter Metric => new(WeightUnit.Kg, DistanceUnit.Km, 2.5);
    private static UnitConverter Imperial => new(WeightUnit.Lb, DistanceUnit.Mi, 2.5);

    [Fact]
    public void Create_with_valid_kg_value_returns_valid_weigh_in()
    {
        var ids = new SequentialIdGenerator("w");
        var result = WeighInRules.Create("86.3", Metric, Dates.Mon, ids);

        var valid = Assert.IsType<WeighInResult.Valid>(result);
        Assert.Equal("w-1", valid.WeighIn.Id);
        Assert.Equal(Dates.Mon, valid.WeighIn.Date);
        Assert.Equal(86.3, valid.WeighIn.WeightKg);
    }

    [Fact]
    public void Create_with_valid_lb_value_converts_to_kg_and_rounds_to_2_dp()
    {
        var ids = new SequentialIdGenerator("w");
        // 86.3 lb * 0.45359237 = 39.1450215... rounds to 39.15 kg
        var result = WeighInRules.Create("86.3", Imperial, Dates.Mon, ids);

        var valid = Assert.IsType<WeighInResult.Valid>(result);
        Assert.Equal(39.15, valid.WeighIn.WeightKg);
    }

    [Fact]
    public void Create_with_comma_decimal_separator_is_accepted()
    {
        var ids = new SequentialIdGenerator("w");
        var result = WeighInRules.Create("86,3", Metric, Dates.Mon, ids);

        var valid = Assert.IsType<WeighInResult.Valid>(result);
        Assert.Equal(86.3, valid.WeighIn.WeightKg);
    }

    [Fact]
    public void Create_with_surrounding_whitespace_is_accepted()
    {
        var ids = new SequentialIdGenerator("w");
        var result = WeighInRules.Create("   86.3   ", Metric, Dates.Mon, ids);

        var valid = Assert.IsType<WeighInResult.Valid>(result);
        Assert.Equal(86.3, valid.WeighIn.WeightKg);
    }

    [Fact]
    public void Create_rounds_kg_to_two_decimal_places()
    {
        var ids = new SequentialIdGenerator("w");
        var result = WeighInRules.Create("86.345", Metric, Dates.Mon, ids);

        var valid = Assert.IsType<WeighInResult.Valid>(result);
        Assert.Equal(86.35, valid.WeighIn.WeightKg);
    }

    [Theory]
    [InlineData("20", 20.0)]
    [InlineData("400", 400.0)]
    public void Create_with_boundary_values_20_and_400_are_valid(string input, double expectedKg)
    {
        var ids = new SequentialIdGenerator("w");
        var result = WeighInRules.Create(input, Metric, Dates.Mon, ids);

        var valid = Assert.IsType<WeighInResult.Valid>(result);
        Assert.Equal(expectedKg, valid.WeighIn.WeightKg);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("86.3.3")]
    [InlineData("kg")]
    public void Create_with_unparsable_or_empty_inputs_returns_invalid_with_unparsable_message(string? input)
    {
        var ids = new SequentialIdGenerator("w");
        var result = WeighInRules.Create(input, Metric, Dates.Mon, ids);

        var invalid = Assert.IsType<WeighInResult.Invalid>(result);
        Assert.Equal("Enter your weight as a number, for example 86.3.", invalid.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("19.9")]
    [InlineData("400.1")]
    public void Create_with_out_of_range_inputs_returns_invalid_with_out_of_range_message(string input)
    {
        var ids = new SequentialIdGenerator("w");
        var result = WeighInRules.Create(input, Metric, Dates.Mon, ids);

        var invalid = Assert.IsType<WeighInResult.Invalid>(result);
        Assert.Equal("That doesn't look like a body weight. Check the number.", invalid.Message);
    }

    [Fact]
    public void Create_under_comma_decimal_culture_works_identically()
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var ids = new SequentialIdGenerator("w");

            var dotResult = WeighInRules.Create("86.3", Metric, Dates.Mon, ids);
            var commaResult = WeighInRules.Create("86,3", Metric, Dates.Mon, ids);

            var validDot = Assert.IsType<WeighInResult.Valid>(dotResult);
            var validComma = Assert.IsType<WeighInResult.Valid>(commaResult);

            Assert.Equal(86.3, validDot.WeighIn.WeightKg);
            Assert.Equal(86.3, validComma.WeighIn.WeightKg);
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }
}
