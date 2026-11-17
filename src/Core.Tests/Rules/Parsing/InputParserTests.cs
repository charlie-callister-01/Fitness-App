using System.Globalization;
using Core.Model;
using Core.Rules.Parsing;
using Core.Rules.Units;

namespace Core.Tests.Rules.Parsing;

public class InputParserTests
{
    private static InputParser Metric => new(new UnitConverter(WeightUnit.Kg, DistanceUnit.Km, 2.5));
    private static InputParser Imperial => new(new UnitConverter(WeightUnit.Lb, DistanceUnit.Mi, 2.5));

    private static ParsedSet Ok(InputParser parser, string text, LogKind kind)
    {
        var result = parser.Parse(text, kind);
        Assert.True(result.IsSuccess, $"'{text}' as {kind}: {result.Error}");
        Assert.Null(result.Error);
        return result.Set!;
    }

    private static string Fail(InputParser parser, string? text, LogKind kind)
    {
        var result = parser.Parse(text, kind);
        Assert.False(result.IsSuccess, $"'{text}' as {kind} should fail");
        Assert.Null(result.Set);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
        return result.Error!;
    }

    [Theory]
    [InlineData("20x10", 20, 10)]
    [InlineData("20 x 10", 20, 10)]
    [InlineData("20×10", 20, 10)]
    [InlineData("20*10", 20, 10)]
    [InlineData("20X10", 20, 10)]
    [InlineData("22,5x8", 22.5, 8)]
    [InlineData("22.5x8", 22.5, 8)]
    [InlineData("  22 , 5 x 8  ", 22.5, 8)]
    [InlineData(".5x8", 0.5, 8)]
    public void Load_parses_weight_and_reps(string text, double kg, int reps)
    {
        var set = Ok(Metric, text, LogKind.Load);
        Assert.Equal(kg, set.WeightKg);
        Assert.Equal(reps, set.Reps);
    }

    [Fact]
    public void Load_in_lb_is_converted_to_canonical_kg()
    {
        var set = Ok(Imperial, "100x5", LogKind.Load);
        Assert.Equal(45.359237, set.WeightKg!.Value, 9);
        Assert.Equal(5, set.Reps);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("20")]
    [InlineData("20x")]
    [InlineData("x10")]
    [InlineData("20x10x3")]
    [InlineData("20x10.5")]
    [InlineData("20x0")]
    [InlineData("0x10")]
    [InlineData("-20x10")]
    [InlineData("abcx10")]
    [InlineData("1,000.5x10")]
    [InlineData("1.2.3x10")]
    [InlineData("20x99999999999")]
    [InlineData("٢٠x10")]
    public void Load_rejects_with_expected_format_message(string text) =>
        Assert.Equal("Expected weight x reps, like 20x10", Fail(Metric, text, LogKind.Load));

    [Fact]
    public void Null_input_fails_without_throwing() =>
        Assert.Equal("Expected weight x reps, like 20x10", Fail(Metric, null, LogKind.Load));

    [Theory]
    [InlineData("bwx8", null, 8)]
    [InlineData("BW x 8", null, 8)]
    [InlineData("bw*8", null, 8)]
    [InlineData("10x8", 10.0, 8)]
    [InlineData("12,5×6", 12.5, 6)]
    public void BodyweightLoad_takes_bodyweight_or_added_weight(string text, double? kg, int reps)
    {
        var set = Ok(Metric, text, LogKind.BodyweightLoad);
        Assert.Equal(kg, set.WeightKg);
        Assert.Equal(reps, set.Reps);
    }

    [Theory]
    [InlineData("8")]
    [InlineData("bwx")]
    [InlineData("bwx0")]
    [InlineData("bw")]
    [InlineData("bwxx8")]
    [InlineData("-5x8")]
    public void BodyweightLoad_rejects(string text) =>
        Assert.Equal("Expected bwxreps or added weight x reps, like bwx8 or 10x8", Fail(Metric, text, LogKind.BodyweightLoad));

    [Fact]
    public void BodyweightLoad_added_weight_in_lb_is_converted() =>
        Assert.Equal(50 * 0.45359237, Ok(Imperial, "50x5", LogKind.BodyweightLoad).WeightKg!.Value, 9);

    [Theory]
    [InlineData("32x40", 32, 40)]
    [InlineData("32 * 40", 32, 40)]
    [InlineData("27,5x12.5", 27.5, 12.5)]
    public void Carry_is_weight_per_hand_and_metres(string text, double kg, double metres)
    {
        var set = Ok(Metric, text, LogKind.Carry);
        Assert.Equal(kg, set.WeightKg);
        Assert.Equal(metres, set.DistanceM);
        Assert.Null(set.DistanceKm);
    }

    [Theory]
    [InlineData("32")]
    [InlineData("32x0")]
    [InlineData("bwx40")]
    [InlineData("32x40m")]
    public void Carry_rejects(string text) =>
        Assert.Equal("Expected weight x metres, like 32x40", Fail(Metric, text, LogKind.Carry));

    [Fact]
    public void Carry_weight_in_lb_is_converted_but_metres_are_not()
    {
        var set = Ok(Imperial, "70x40", LogKind.Carry);
        Assert.Equal(70 * 0.45359237, set.WeightKg!.Value, 9);
        Assert.Equal(40, set.DistanceM);
    }

    [Theory]
    [InlineData("4:32", 272)]
    [InlineData("04:32", 272)]
    [InlineData("1:04:10", 3850)]
    [InlineData("0:45", 45)]
    [InlineData("4 : 32", 272)]
    [InlineData("272", 272)]
    [InlineData("45,5", 46)]
    [InlineData("59:59", 3599)]
    public void Time_accepts_clock_forms_and_plain_seconds(string text, int seconds) =>
        Assert.Equal(seconds, Ok(Metric, text, LogKind.Time).DurationSeconds);

    [Theory]
    [InlineData("")]
    [InlineData("4:60")]
    [InlineData("1:60:00")]
    [InlineData("1:2:3:4")]
    [InlineData("4:")]
    [InlineData(":30")]
    [InlineData("4:5")]
    [InlineData("0")]
    [InlineData("0:00")]
    [InlineData("4:32.5")]
    [InlineData("fast")]
    [InlineData("-3")]
    public void Time_rejects(string text) =>
        Assert.Equal("Expected a time like 4:32, 1:04:10 or seconds", Fail(Metric, text, LogKind.Time));

    [Theory]
    [InlineData("45", LogKind.Seconds, 45)]
    [InlineData("45s", LogKind.Seconds, 45)]
    [InlineData("45 sec", LogKind.Seconds, 45)]
    [InlineData("2min", LogKind.Seconds, 120)]
    [InlineData("3", LogKind.Minutes, 180)]
    [InlineData("3min", LogKind.Minutes, 180)]
    [InlineData("1,5 min", LogKind.Minutes, 90)]
    [InlineData("90s", LogKind.Minutes, 90)]
    public void Seconds_and_minutes_kinds_store_canonical_seconds(string text, LogKind kind, int seconds) =>
        Assert.Equal(seconds, Ok(Metric, text, kind).DurationSeconds);

    [Theory]
    [InlineData("", LogKind.Seconds, "Expected seconds, like 45 or 45s")]
    [InlineData("0", LogKind.Seconds, "Expected seconds, like 45 or 45s")]
    [InlineData("5km", LogKind.Seconds, "Expected seconds, like 45 or 45s")]
    [InlineData("4:32", LogKind.Seconds, "Expected seconds, like 45 or 45s")]
    [InlineData("", LogKind.Minutes, "Expected minutes, like 3 or 3min")]
    [InlineData("abc", LogKind.Minutes, "Expected minutes, like 3 or 3min")]
    [InlineData("-1", LogKind.Minutes, "Expected minutes, like 3 or 3min")]
    public void Duration_kinds_reject(string text, LogKind kind, string message) =>
        Assert.Equal(message, Fail(Metric, text, kind));

    [Theory]
    [InlineData("5", 5.0)]
    [InlineData("5km", 5.0)]
    [InlineData("5,5 km", 5.5)]
    [InlineData("400m", 0.4)]
    [InlineData("10 K M", 10.0)]
    public void DistanceKm_takes_bare_number_in_display_unit_and_unambiguous_suffix(string text, double km) =>
        Assert.Equal(km, Ok(Metric, text, LogKind.DistanceKm).DistanceKm!.Value, 9);

    [Fact]
    public void DistanceKm_bare_number_in_miles_is_converted_but_km_suffix_is_not()
    {
        Assert.Equal(16.09344, Ok(Imperial, "10", LogKind.DistanceKm).DistanceKm!.Value, 9);
        Assert.Equal(10, Ok(Imperial, "10km", LogKind.DistanceKm).DistanceKm!.Value, 9);
    }

    [Theory]
    [InlineData("400", 400.0)]
    [InlineData("400m", 400.0)]
    [InlineData("1,5km", 1500.0)]
    [InlineData("0.5 km", 500.0)]
    public void DistanceM_is_metres_whatever_the_display_unit(string text, double metres)
    {
        var set = Ok(Imperial, text, LogKind.DistanceM);
        Assert.Equal(metres, set.DistanceM!.Value, 9);
        Assert.Null(set.DistanceKm);
    }

    [Theory]
    [InlineData("", LogKind.DistanceKm, "Expected a distance in km, like 5 or 5km")]
    [InlineData("0", LogKind.DistanceKm, "Expected a distance in km, like 5 or 5km")]
    [InlineData("5kg", LogKind.DistanceKm, "Expected a distance in km, like 5 or 5km")]
    [InlineData("5x5", LogKind.DistanceKm, "Expected a distance in km, like 5 or 5km")]
    [InlineData("", LogKind.DistanceM, "Expected a distance in metres, like 400 or 400m")]
    [InlineData("m400", LogKind.DistanceM, "Expected a distance in metres, like 400 or 400m")]
    public void Distance_kinds_reject(string text, LogKind kind, string message) =>
        Assert.Equal(message, Fail(Metric, text, kind));

    [Theory]
    [InlineData("12", 12)]
    [InlineData(" 1 2 ", 12)]
    public void Reps_is_an_integer(string text, int reps) =>
        Assert.Equal(reps, Ok(Metric, text, LogKind.Reps).Reps);

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("12.5")]
    [InlineData("12,5")]
    [InlineData("-3")]
    [InlineData("12x3")]
    [InlineData("abc")]
    public void Reps_rejects(string text) =>
        Assert.Equal("Expected a whole number of reps, like 12", Fail(Metric, text, LogKind.Reps));

    [Theory]
    [InlineData("82.3", 82.3)]
    [InlineData("82,3", 82.3)]
    public void BodyMeasurementKg_is_a_weight(string text, double kg) =>
        Assert.Equal(kg, Ok(Metric, text, LogKind.BodyMeasurementKg).WeightKg!.Value, 9);

    [Fact]
    public void BodyMeasurementKg_in_lb_is_converted() =>
        Assert.Equal(181 * 0.45359237, Ok(Imperial, "181", LogKind.BodyMeasurementKg).WeightKg!.Value, 9);

    [Theory]
    [InlineData("92", 92.0)]
    [InlineData("92,5", 92.5)]
    public void BodyMeasurementCm_is_a_length_never_converted(string text, double cm)
    {
        var set = Ok(Imperial, text, LogKind.BodyMeasurementCm);
        Assert.Equal(cm, set.MeasurementCm);
        Assert.Null(set.WeightKg);
    }

    [Theory]
    [InlineData("", LogKind.BodyMeasurementKg, "Expected a weight, like 82.3")]
    [InlineData("0", LogKind.BodyMeasurementKg, "Expected a weight, like 82.3")]
    [InlineData("82x3", LogKind.BodyMeasurementKg, "Expected a weight, like 82.3")]
    [InlineData("", LogKind.BodyMeasurementCm, "Expected a length in cm, like 92.5")]
    [InlineData("tall", LogKind.BodyMeasurementCm, "Expected a length in cm, like 92.5")]
    public void Body_measurements_reject(string text, LogKind kind, string message) =>
        Assert.Equal(message, Fail(Metric, text, kind));

    [Theory]
    [InlineData("")]
    [InlineData("done")]
    [InlineData("anything at all")]
    public void TickOnly_needs_no_value(string text) =>
        Assert.Equal(ParsedSet.Empty, Ok(Metric, text, LogKind.TickOnly));

    [Fact]
    public void Text_keeps_inner_spacing_and_trims_ends()
    {
        Assert.Equal("felt  strong", Ok(Metric, "  felt  strong ", LogKind.Text).Text);
        Assert.Equal("Expected some text", Fail(Metric, "   ", LogKind.Text));
        Assert.Equal("Expected some text", Fail(Metric, null, LogKind.Text));
    }

    [Fact]
    public void Undefined_log_kind_fails_without_throwing() =>
        Assert.False(Metric.Parse("5", (LogKind)999).IsSuccess);

    [Theory]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    [InlineData("ar-SA")]
    public void Results_do_not_depend_on_the_current_culture(string cultureName)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo(cultureName);
            Assert.Equal(22.5, Ok(Metric, "22,5x8", LogKind.Load).WeightKg);
            Assert.Equal(22.5, Ok(Metric, "22.5x8", LogKind.Load).WeightKg);
            Assert.Equal(5.5, Ok(Metric, "5.5km", LogKind.DistanceKm).DistanceKm);
            Assert.Equal(272, Ok(Metric, "4:32", LogKind.Time).DurationSeconds);
            Assert.Equal(82.3, Ok(Metric, "82,3", LogKind.BodyMeasurementKg).WeightKg);
            Fail(Metric, "1.000,5x8", LogKind.Load);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }
}
