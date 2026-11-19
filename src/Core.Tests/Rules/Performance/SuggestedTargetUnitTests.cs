using Core.Model;
using Core.Rules.Performance;
using Core.Rules.Units;
using Core.Tests.Rules.TestData;

namespace Core.Tests.Rules.Performance;

/// <summary>The Suggested target reason is written in the owner's display unit (the target's own numbers stay in canonical kg).</summary>
public class SuggestedTargetUnitTests
{
    private const double KgPerLb = 0.45359237;

    private static readonly Exercise Ex = new("ex", "ex", LogKind.Load, null, false);
    private static readonly Exercise CarryEx = new("ex", "ex", LogKind.Carry, null, false);

    private static UnitConverter Lb => new(WeightUnit.Lb, DistanceUnit.Mi, 2);
    private static UnitConverter Kg => new(WeightUnit.Kg, DistanceUnit.Km, 2);

    private static Plan PlanOf(double incrementKg) =>
        PlanFixture.Make(new DateOnly(2026, 1, 1), incrementKg: incrementKg, sessions:
        [
            PlanFixture.Session("s1", DayOfWeek.Monday, exercises: [PlanFixture.Row("ex", reps: 8, repsMax: 12)]),
        ]);

    private static SuggestedTarget? TargetFor(Exercise exercise, UnitConverter units, double incrementKg, WorkoutSet set) =>
        ExercisePerformance.Snapshot(exercise, new HistoryBuilder().DoneWithRpe("w1", "2026-01-01", "ex", 7, set).Workouts, PlanOf(incrementKg), units).Target;

    [Fact]
    public void Lb_user_reads_the_added_weight_in_lb()
    {
        var t = TargetFor(Ex, Lb, 5 * KgPerLb, HistoryBuilder.Set(kg: 100 * KgPerLb, reps: 12))!;

        Assert.Equal("Hit 12 of 12 reps at RPE 7: add 5 lb, back to 10 reps", t.Reason);
        Assert.Equal(105 * KgPerLb, t.WeightKg!.Value, 6);
    }

    [Fact]
    public void Lb_user_reads_a_carry_weight_and_the_added_weight_in_lb()
    {
        var t = TargetFor(CarryEx, Lb, 5 * KgPerLb, HistoryBuilder.Set(kg: 70 * KgPerLb, m: 40))!;

        Assert.Equal("Carried 70 lb for 40 m at RPE 7: add 5 lb, back to 20 m", t.Reason);
    }

    [Fact]
    public void Kg_user_keeps_the_existing_wording()
    {
        var t = TargetFor(Ex, Kg, 2, HistoryBuilder.Set(kg: 20, reps: 12))!;

        Assert.Equal("Hit 12 of 12 reps at RPE 7: add 2 kg, back to 10 reps", t.Reason);
    }

    [Fact]
    public void Without_a_unit_converter_the_reason_is_in_kg()
    {
        var history = new HistoryBuilder().DoneWithRpe("w1", "2026-01-01", "ex", 7, HistoryBuilder.Set(kg: 20, reps: 12)).Workouts;

        var t = ExercisePerformance.Snapshot(Ex, history, PlanOf(2)).Target!;

        Assert.Equal("Hit 12 of 12 reps at RPE 7: add 2 kg, back to 10 reps", t.Reason);
    }
}
