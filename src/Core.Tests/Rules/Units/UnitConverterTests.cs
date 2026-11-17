using System.Globalization;
using Core.Model;
using Core.Rules.Units;

namespace Core.Tests.Rules.Units;

public class UnitConverterTests
{
    private static UnitConverter Metric => new(WeightUnit.Kg, DistanceUnit.Km, 2.5);
    private static UnitConverter Imperial => new(WeightUnit.Lb, DistanceUnit.Mi, 2.5);

    [Fact]
    public void Weight_in_kg_displays_unchanged_and_in_lb_converted_and_rounded()
    {
        Assert.Equal(100, Metric.DisplayWeight(100));
        Assert.Equal(220.5, Imperial.DisplayWeight(100)); // 220.462 lb rounds to nearest 0.5
    }

    [Theory]
    [InlineData(100.12, 100.0)]
    [InlineData(100.13, 100.25)]
    [InlineData(62.6, 62.5)]
    public void Kg_display_rounds_to_quarter_kg(double kg, double expected) =>
        Assert.Equal(expected, Metric.DisplayWeight(kg));

    [Fact]
    public void Distance_displays_to_two_decimals_in_either_unit()
    {
        Assert.Equal(5.12, Metric.DisplayDistance(5.123));
        Assert.Equal(3.11, Imperial.DisplayDistance(5.0)); // 3.1069
        Assert.Equal(0.12, Imperial.DisplayDistance(0.2)); // 0.1243
    }

    [Fact]
    public void Display_input_converts_to_canonical_without_rounding()
    {
        Assert.Equal(45.359237, Imperial.WeightFromDisplay(100), 9);
        Assert.Equal(16.09344, Imperial.DistanceFromDisplay(10), 9);
        Assert.Equal(82.3, Metric.WeightFromDisplay(82.3));
        Assert.Equal(7.777, Metric.DistanceFromDisplay(7.777));
    }

    [Fact]
    public void Conversion_does_not_round_the_stored_value()
    {
        var kg = Imperial.WeightFromDisplay(135.5);
        Assert.NotEqual(Math.Round(kg, 2), kg);
    }

    [Fact]
    public void Both_units_are_available_for_weight_and_distance()
    {
        var w = Metric.BothWeights(100);
        Assert.Equal(100, w.Kg);
        Assert.Equal(220.5, w.Lb);

        var d = Imperial.BothDistances(10);
        Assert.Equal(10, d.Km);
        Assert.Equal(6.21, d.Mi);
    }

    [Fact]
    public void Display_then_reenter_is_stable_for_weights_and_distances()
    {
        foreach (var conv in new[] { Metric, Imperial })
        {
            for (var kg = 0.5; kg < 300; kg += 0.37)
            {
                var shown = conv.DisplayWeight(kg);
                var stored = conv.WeightFromDisplay(shown);
                Assert.Equal(shown, conv.DisplayWeight(stored));
                Assert.True(Math.Abs(stored - kg) <= 0.5 * 0.45359237 + 1e-9);
            }

            for (var km = 0.1; km < 60; km += 0.173)
            {
                var shown = conv.DisplayDistance(km);
                var stored = conv.DistanceFromDisplay(shown);
                Assert.Equal(shown, conv.DisplayDistance(stored));
            }
        }
    }

    [Fact]
    public void Repeated_edits_do_not_drift()
    {
        var first = Imperial.WeightFromDisplay(Imperial.DisplayWeight(83.7));
        var current = first;
        for (var i = 0; i < 1000; i++)
            current = Imperial.WeightFromDisplay(Imperial.DisplayWeight(current));

        Assert.Equal(first, current, 9);
    }

    [Fact]
    public void Weight_increment_stays_in_kg_whatever_the_display_unit()
    {
        Assert.Equal(2.5, Metric.WeightIncrementKg);
        Assert.Equal(2.5, Imperial.WeightIncrementKg);
        var settings = new Settings(WeightUnit.Lb, DistanceUnit.Mi, 2.5, 90, null, 3, true);
        Assert.Equal(2.5, new UnitConverter(settings).WeightIncrementKg);
    }

    [Fact]
    public void Results_are_identical_under_a_comma_decimal_culture()
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            var expected = (Imperial.DisplayWeight(100.3), Imperial.DisplayDistance(5.0), Imperial.WeightFromDisplay(135.5));
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var actual = (Imperial.DisplayWeight(100.3), Imperial.DisplayDistance(5.0), Imperial.WeightFromDisplay(135.5));
            Assert.Equal(expected, actual);
        }
        finally { CultureInfo.CurrentCulture = before; }
    }
}
