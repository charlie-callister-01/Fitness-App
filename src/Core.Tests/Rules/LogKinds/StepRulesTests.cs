using Core.Model;
using Core.Rules.LogKinds;
using Core.Rules.Units;

namespace Core.Tests.Rules.LogKinds;

public class StepRulesTests
{
    private static readonly UnitConverter Metric = new(WeightUnit.Kg, DistanceUnit.Km, 2);
    private static readonly UnitConverter Imperial = new(WeightUnit.Lb, DistanceUnit.Mi, 2);

    [Theory]
    [InlineData(LogKind.Load)]
    [InlineData(LogKind.Carry)]
    public void Loaded_kinds_step_the_first_field_by_the_plans_increment_in_kg(LogKind kind)
    {
        var step = RowSpec.For(kind)?.StepFor(Metric, 2);

        Assert.Equal(new StepSize(StepField.First, 2, "kg"), step);
    }

    [Fact]
    public void The_increment_shows_in_pounds_when_units_are_lb()
    {
        var step = RowSpec.For(LogKind.Load)?.StepFor(Imperial, 2.5);

        Assert.Equal(new StepSize(StepField.First, 5.5, "lb"), step);
    }

    [Fact]
    public void Bodyweight_steps_one_rep_in_the_second_field()
    {
        Assert.Equal(new StepSize(StepField.Second, 1, "rep"), RowSpec.For(LogKind.BodyweightLoad)?.StepFor(Metric, 2));
    }

    [Fact]
    public void Seconds_step_five_seconds()
    {
        Assert.Equal(new StepSize(StepField.First, 5, "s"), RowSpec.For(LogKind.Seconds)?.StepFor(Metric, 2));
    }

    [Theory]
    [InlineData("20", 1, "22")]
    [InlineData("22,5", -1, "20.5")]
    [InlineData("", 1, "2")]
    [InlineData("abc", -1, "0")]
    [InlineData("1", -1, "0")]
    public void Applying_a_step_reads_the_typed_number_and_never_goes_below_zero(string typed, int direction, string expected)
    {
        Assert.Equal(expected, new StepSize(StepField.First, 2, "kg").Apply(typed, direction));
    }

    [Fact]
    public void Every_loggable_kind_has_a_row_and_text_has_none()
    {
        Assert.False(RowSpec.IsLoggable(LogKind.Text));
        Assert.Null(RowSpec.For(LogKind.Text));
        Assert.All(Enum.GetValues<LogKind>().Where(k => k != LogKind.Text), k => Assert.True(RowSpec.IsLoggable(k), k.ToString()));
    }

    [Fact]
    public void A_bodyweight_row_hints_bw_or_the_display_weight_unit_and_a_pair_row_hints_nothing_else()
    {
        Assert.Equal("bw or kg", RowSpec.For(LogKind.BodyweightLoad)!.FirstHint(Metric));
        Assert.Equal("bw or lb", RowSpec.For(LogKind.BodyweightLoad)!.FirstHint(Imperial));
        Assert.Null(RowSpec.For(LogKind.Load)!.FirstHint(Metric));
        Assert.Equal("mi", RowSpec.For(LogKind.DistanceKm)!.FirstHint(Imperial));
    }

    [Theory]
    [InlineData(LogKind.Time)]
    [InlineData(LogKind.DistanceKm)]
    [InlineData(LogKind.DistanceM)]
    [InlineData(LogKind.Text)]
    [InlineData(LogKind.TickOnly)]
    public void Time_and_distance_kinds_have_no_steppers(LogKind kind)
    {
        Assert.Null(RowSpec.For(kind)?.StepFor(Metric, 2));
    }
}
