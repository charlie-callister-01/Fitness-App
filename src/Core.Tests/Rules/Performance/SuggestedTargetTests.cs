using Core.Model;
using Core.Rules.Performance;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.Performance;

public class SuggestedTargetTests
{
    private static Exercise Ex(LogKind kind) => new("ex", "ex", kind, null, false);

    private static Plan PlanWith(int? reps = 8, int? repsMax = 12, double increment = 2, params string[] otherFirst) =>
        PlanFixture.Make(start: new DateOnly(2026, 1, 1), incrementKg: increment, sessions:
        [
            .. otherFirst.Select(id => PlanFixture.Session("o" + id, DayOfWeek.Monday, name: "o", exercises: [PlanFixture.Row(id, reps: 1, repsMax: 1)])),
            PlanFixture.Session("s1", DayOfWeek.Monday, exercises: [PlanFixture.Row("ex", reps: reps, repsMax: repsMax)]),
            PlanFixture.Session("s2", DayOfWeek.Friday, exercises: [PlanFixture.Row("ex", reps: 1, repsMax: 3)]),
        ]);

    private static SuggestedTarget? Target(LogKind kind, HistoryBuilder h, Plan? plan = null) =>
        ExercisePerformance.Snapshot(Ex(kind), h.Workouts, plan ?? PlanWith()).Target;

    private static HistoryBuilder Last(string date, int? rpe, params WorkoutSet[] sets) =>
        rpe is { } r ? new HistoryBuilder().DoneWithRpe("w1", date, "ex", r, sets) : new HistoryBuilder().Done("w1", date, "ex", sets);

    // Load

    [Fact]
    public void Load_below_top_of_range_adds_one_rep()
    {
        var t = Target(LogKind.Load, Last("2026-01-01", 7, Set(kg: 20, reps: 10)))!;

        Assert.Equal(20, t.WeightKg);
        Assert.Equal(11, t.Reps);
        Assert.Equal("Hit 10 of 12 reps at RPE 7: add 1 rep", t.Reason);
    }

    [Fact]
    public void Load_at_top_of_range_adds_increment_and_drops_two_reps()
    {
        var t = Target(LogKind.Load, Last("2026-01-01", 7, Set(kg: 20, reps: 12)))!;

        Assert.Equal(22, t.WeightKg);
        Assert.Equal(10, t.Reps);
        Assert.Equal("Hit 12 of 12 reps at RPE 7: add 2 kg, back to 10 reps", t.Reason);
    }

    [Fact]
    public void Load_without_rpe_progresses_normally()
    {
        var t = Target(LogKind.Load, Last("2026-01-01", null, Set(kg: 20, reps: 10)))!;

        Assert.Equal(11, t.Reps);
        Assert.Equal("Hit 10 of 12 reps: add 1 rep", t.Reason);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(10)]
    public void Load_rpe_9_or_10_holds(int rpe)
    {
        var t = Target(LogKind.Load, Last("2026-01-01", rpe, Set(kg: 20, reps: 12)))!;

        Assert.Equal(20, t.WeightKg);
        Assert.Equal(12, t.Reps);
        Assert.Equal($"Hit 12 of 12 reps at RPE {rpe}: hold the same weight and reps", t.Reason);
    }

    [Fact]
    public void Load_rpe_8_is_normal_progression()
    {
        Assert.Equal(11, Target(LogKind.Load, Last("2026-01-01", 8, Set(kg: 20, reps: 10)))!.Reps);
    }

    [Fact]
    public void Load_rpe_6_adds_two_reps()
    {
        var t = Target(LogKind.Load, Last("2026-01-01", 6, Set(kg: 20, reps: 8)))!;

        Assert.Equal(10, t.Reps);
        Assert.Equal("Hit 8 of 12 reps at RPE 6: easy, add 2 reps", t.Reason);
    }

    [Fact]
    public void Load_rpe_5_at_top_doubles_the_increment()
    {
        var t = Target(LogKind.Load, Last("2026-01-01", 5, Set(kg: 20, reps: 12)))!;

        Assert.Equal(24, t.WeightKg);
        Assert.Equal(10, t.Reps);
        Assert.Equal("Hit 12 of 12 reps at RPE 5: easy, add 4 kg, back to 10 reps", t.Reason);
    }

    [Fact]
    public void Load_target_is_based_on_the_last_workout_where_it_was_done_not_a_skip()
    {
        var h = new HistoryBuilder()
            .Done("w1", "2026-01-01", "ex", Set(kg: 20, reps: 10))
            .Skipped("w2", "2026-01-08", "ex");

        Assert.Equal(11, Target(LogKind.Load, h)!.Reps);
    }

    [Fact]
    public void Load_uses_the_first_plan_row_for_the_rep_range_and_the_plan_increment()
    {
        var plan = PlanWith(reps: 5, repsMax: null, increment: 2.5, "other");

        var t = Target(LogKind.Load, Last("2026-01-01", 7, Set(kg: 100, reps: 5)), plan)!;

        Assert.Equal(102.5, t.WeightKg);
        Assert.Equal(3, t.Reps);
        Assert.Equal("Hit 5 of 5 reps at RPE 7: add 2.5 kg, back to 3 reps", t.Reason);
    }

    [Fact]
    public void Bodyweight_exercise_at_top_adds_the_increment_as_extra_weight()
    {
        var t = Target(LogKind.BodyweightLoad, Last("2026-01-01", 7, Set(reps: 12)))!;

        Assert.Equal(2, t.WeightKg);
        Assert.Equal(10, t.Reps);
        Assert.Equal("Hit 12 of 12 reps at RPE 7: add 2 kg, back to 10 reps", t.Reason);
    }

    // Carry

    [Fact]
    public void Carry_adds_ten_metres_below_forty()
    {
        var t = Target(LogKind.Carry, Last("2026-01-01", 7, Set(kg: 32, m: 20)))!;

        Assert.Equal(32, t.WeightKg);
        Assert.Equal(30, t.DistanceM);
        Assert.Equal("Carried 32 kg for 20 m at RPE 7: add 10 m", t.Reason);
    }

    [Fact]
    public void Carry_at_forty_adds_weight_and_resets_distance_to_twenty()
    {
        var t = Target(LogKind.Carry, Last("2026-01-01", 7, Set(kg: 32, m: 40)))!;

        Assert.Equal(34, t.WeightKg);
        Assert.Equal(20, t.DistanceM);
        Assert.Equal("Carried 32 kg for 40 m at RPE 7: add 2 kg, back to 20 m", t.Reason);
    }

    [Fact]
    public void Carry_rpe_9_holds()
    {
        var t = Target(LogKind.Carry, Last("2026-01-01", 9, Set(kg: 32, m: 30)))!;

        Assert.Equal(32, t.WeightKg);
        Assert.Equal(30, t.DistanceM);
        Assert.Equal("Carried 32 kg for 30 m at RPE 9: hold the same weight and distance", t.Reason);
    }

    [Fact]
    public void Carry_rpe_6_steps_twice()
    {
        var t = Target(LogKind.Carry, Last("2026-01-01", 6, Set(kg: 32, m: 30)))!;

        Assert.Equal(34, t.WeightKg);
        Assert.Equal(20, t.DistanceM);
        Assert.Equal("Carried 32 kg for 30 m at RPE 6: easy, add 2 kg, back to 20 m", t.Reason);
    }

    // Time and other kinds

    [Fact]
    public void Time_target_beats_the_best_by_five_seconds()
    {
        var h = new HistoryBuilder()
            .Done("w1", "2026-01-01", "ex", Set(seconds: 1500))
            .Done("w2", "2026-01-08", "ex", Set(seconds: 1560));

        var t = Target(LogKind.Time, h)!;

        Assert.Equal(1495, t.DurationSeconds);
        Assert.Equal("Best 25:00; beat it by 5 s", t.Reason);
    }

    [Fact]
    public void Time_rpe_6_beats_best_by_ten_seconds()
    {
        var t = Target(LogKind.Time, Last("2026-01-01", 6, Set(seconds: 1500)))!;

        Assert.Equal(1490, t.DurationSeconds);
        Assert.Equal("Best 25:00; RPE 6 last time, so beat it by 10 s", t.Reason);
    }

    [Fact]
    public void Time_rpe_9_holds_the_last_result()
    {
        var h = new HistoryBuilder()
            .Done("w1", "2026-01-01", "ex", Set(seconds: 1500))
            .DoneWithRpe("w2", "2026-01-08", "ex", 9, Set(seconds: 1560));

        var t = Target(LogKind.Time, h)!;

        Assert.Equal(1560, t.DurationSeconds);
        Assert.Equal("Last 26:00 at RPE 9: hold the same time", t.Reason);
    }

    [Fact]
    public void Reps_adds_one_to_the_best()
    {
        var t = Target(LogKind.Reps, Last("2026-01-01", null, Set(reps: 15)))!;

        Assert.Equal(16, t.Reps);
        Assert.Equal("Best 15 reps; add 1 rep", t.Reason);
    }

    [Fact]
    public void Seconds_adds_five_to_the_best()
    {
        var t = Target(LogKind.Seconds, Last("2026-01-01", null, Set(seconds: 60)))!;

        Assert.Equal(65, t.DurationSeconds);
        Assert.Equal("Best 60 s; add 5 s", t.Reason);
    }

    [Fact]
    public void Minutes_adds_one_to_the_best()
    {
        var t = Target(LogKind.Minutes, Last("2026-01-01", null, Set(seconds: 180)))!;

        Assert.Equal(240, t.DurationSeconds);
        Assert.Equal("Best 3 min; add 1 min", t.Reason);
    }

    [Fact]
    public void Km_adds_half_a_kilometre_to_the_best()
    {
        var t = Target(LogKind.DistanceKm, Last("2026-01-01", null, Set(km: 5)))!;

        Assert.Equal(5.5, t.DistanceKm);
        Assert.Equal("Best 5 km; add 0.5 km", t.Reason);
    }

    [Fact]
    public void Distance_in_metres_adds_ten_to_the_best()
    {
        var t = Target(LogKind.DistanceM, Last("2026-01-01", null, Set(m: 400)))!;

        Assert.Equal(410, t.DistanceM);
        Assert.Equal("Best 400 m; add 10 m", t.Reason);
    }

    [Fact]
    public void Reps_rpe_6_adds_two_reps()
    {
        var t = Target(LogKind.Reps, Last("2026-01-01", 6, Set(reps: 15)))!;

        Assert.Equal(17, t.Reps);
        Assert.Equal("Best 15 reps; RPE 6 last time, so add 2 reps", t.Reason);
    }

    // No target

    [Fact]
    public void No_history_means_no_target()
    {
        Assert.Null(Target(LogKind.Load, new HistoryBuilder()));
    }

    [Fact]
    public void Only_skips_means_no_target()
    {
        Assert.Null(Target(LogKind.Load, new HistoryBuilder().Skipped("w1", "2026-01-01", "ex")));
    }

    [Theory]
    [InlineData(LogKind.TickOnly)]
    [InlineData(LogKind.Text)]
    [InlineData(LogKind.BodyMeasurementKg)]
    public void Kinds_without_a_target_get_none(LogKind kind)
    {
        Assert.Null(Target(kind, Last("2026-01-01", null, Set(kg: 80))));
    }

    [Fact]
    public void Snapshot_without_a_plan_has_no_target()
    {
        var h = Last("2026-01-01", 7, Set(kg: 20, reps: 10));

        Assert.Null(ExercisePerformance.Snapshot(Ex(LogKind.Load), h.Workouts).Target);
    }

    [Fact]
    public void Snapshot_carries_last_best_and_target_together()
    {
        var snap = ExercisePerformance.Snapshot(Ex(LogKind.Load), Last("2026-01-01", 7, Set(kg: 20, reps: 10)).Workouts, PlanWith());

        Assert.NotNull(snap.Last);
        Assert.NotNull(snap.Best);
        Assert.Equal(11, snap.Target!.Reps);
    }
}
