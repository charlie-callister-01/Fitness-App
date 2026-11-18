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
        var step = StepRules.For(kind, Metric, 2, WeightUnit.Kg);

        Assert.Equal(new StepSize(StepField.First, 2, "kg"), step);
    }

    [Fact]
    public void The_increment_shows_in_pounds_when_units_are_lb()
    {
        var step = StepRules.For(LogKind.Load, Imperial, 2.5, WeightUnit.Lb);

        Assert.Equal(new StepSize(StepField.First, 5.5, "lb"), step);
    }

    [Fact]
    public void Bodyweight_steps_one_rep_in_the_second_field()
    {
        Assert.Equal(new StepSize(StepField.Second, 1, "rep"), StepRules.For(LogKind.BodyweightLoad, Metric, 2, WeightUnit.Kg));
    }

    [Fact]
    public void Seconds_step_five_seconds()
    {
        Assert.Equal(new StepSize(StepField.First, 5, "s"), StepRules.For(LogKind.Seconds, Metric, 2, WeightUnit.Kg));
    }

    [Theory]
    [InlineData(LogKind.Time)]
    [InlineData(LogKind.DistanceKm)]
    [InlineData(LogKind.DistanceM)]
    [InlineData(LogKind.Text)]
    [InlineData(LogKind.TickOnly)]
    public void Time_and_distance_kinds_have_no_steppers(LogKind kind)
    {
        Assert.Null(StepRules.For(kind, Metric, 2, WeightUnit.Kg));
    }
}
