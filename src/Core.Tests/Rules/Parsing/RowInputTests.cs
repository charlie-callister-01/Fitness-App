using Core.Model;
using Core.Rules.LogKinds;
using Core.Rules.Parsing;
using Core.Rules.Units;

namespace Core.Tests.Rules.Parsing;

public class RowInputTests
{
    private static InputParser Metric => new(new UnitConverter(WeightUnit.Kg, DistanceUnit.Km, 2.5));

    private static ParseResult Parse(LogKind kind, string? first, string? second = null) =>
        Metric.Parse(RowInput.Join(kind, first, second), kind);

    private static ParsedSet Ok(LogKind kind, string? first, string? second = null) =>
        Assert.IsType<ParseResult.Success>(Parse(kind, first, second)).Set;

    private static string Fail(LogKind kind, string? first, string? second = null) =>
        Assert.IsType<ParseResult.Failure>(Parse(kind, first, second)).Error;

    [Fact]
    public void Load_joins_weight_and_reps_with_comma_decimals()
    {
        var set = Ok(LogKind.Load, "22,5", "8");

        Assert.Equal(22.5, set.WeightKg);
        Assert.Equal(8, set.Reps);
    }

    [Fact]
    public void Bodyweight_row_with_bw_has_reps_only()
    {
        var set = Ok(LogKind.BodyweightLoad, "bw", "12");

        Assert.Null(set.WeightKg);
        Assert.Equal(12, set.Reps);
    }

    [Fact]
    public void Bodyweight_row_with_added_weight_keeps_it()
    {
        var set = Ok(LogKind.BodyweightLoad, "10", "8");

        Assert.Equal(10, set.WeightKg);
        Assert.Equal(8, set.Reps);
    }

    [Fact]
    public void Carry_joins_weight_and_metres()
    {
        var set = Ok(LogKind.Carry, "32", "40");

        Assert.Equal(32, set.WeightKg);
        Assert.Equal(40, set.DistanceM);
    }

    [Theory]
    [InlineData(LogKind.Reps, "12", 12)]
    [InlineData(LogKind.Seconds, "45s", 45)]
    [InlineData(LogKind.Seconds, "45", 45)]
    [InlineData(LogKind.Minutes, "3min", 180)]
    [InlineData(LogKind.Time, "1:48", 108)]
    [InlineData(LogKind.Time, "108", 108)]
    [InlineData(LogKind.Time, "1:04:10", 3850)]
    public void Single_field_kinds_pass_the_field_unchanged(LogKind kind, string text, int expected)
    {
        var set = Ok(kind, text, "ignored");

        Assert.Equal(expected, set.Reps ?? set.DurationSeconds);
    }

    [Fact]
    public void Distance_kinds_pass_the_field_unchanged()
    {
        Assert.Equal(5, Ok(LogKind.DistanceKm, "5km").DistanceKm);
        Assert.Equal(400, Ok(LogKind.DistanceM, "400").DistanceM);
    }

    [Fact]
    public void Tick_only_needs_no_field()
    {
        Assert.IsType<ParseResult.Success>(Parse(LogKind.TickOnly, null));
    }

    [Theory]
    [InlineData(LogKind.Load, "", "8")]
    [InlineData(LogKind.Load, "20", "")]
    [InlineData(LogKind.Load, null, null)]
    [InlineData(LogKind.BodyweightLoad, "bw", "")]
    [InlineData(LogKind.BodyweightLoad, "", "8")]
    [InlineData(LogKind.Carry, "32", "")]
    [InlineData(LogKind.Carry, "", "40")]
    [InlineData(LogKind.Reps, "", null)]
    [InlineData(LogKind.Time, "", null)]
    public void Blank_field_gives_the_parsers_own_message(LogKind kind, string? first, string? second)
    {
        var expected = Assert.IsType<ParseResult.Failure>(Metric.Parse("", kind)).Error;

        Assert.Equal(expected, Fail(kind, first, second));
    }

    [Fact]
    public void Check_returns_the_set_for_a_good_row()
    {
        var check = Assert.IsType<RowCheck.Valid>(RowInput.Check(LogKind.Load, "22,5", "8", Metric));

        Assert.Equal(22.5, check.Set.WeightKg);
        Assert.Equal(8, check.Set.Reps);
    }

    [Theory]
    [InlineData(LogKind.Load, "abc", "8", RowField.First)]
    [InlineData(LogKind.Load, "20", "many", RowField.Second)]
    [InlineData(LogKind.Load, "", "", RowField.First)]
    [InlineData(LogKind.Carry, "32", "", RowField.Second)]
    [InlineData(LogKind.Reps, "lots", null, RowField.First)]
    public void Check_names_the_field_to_blame_and_gives_the_parsers_message(LogKind kind, string first, string? second, RowField field)
    {
        var check = Assert.IsType<RowCheck.Invalid>(RowInput.Check(kind, first, second, Metric));

        Assert.Equal(field, check.Field);
        Assert.Equal(Fail(kind, first, second), check.Error);
    }

    [Fact]
    public void Bad_number_gives_the_same_message_as_typing_it_whole()
    {
        var joined = Fail(LogKind.Load, "abc", "8");
        var typed = Assert.IsType<ParseResult.Failure>(Metric.Parse("abcx8", LogKind.Load)).Error;

        Assert.Equal(typed, joined);
    }
}
