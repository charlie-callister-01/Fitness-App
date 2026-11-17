using Core.Model;
using Core.Rules.Performance;
using static Core.Tests.Rules.Performance.History;

namespace Core.Tests.Rules.Performance;

public class ExercisePerformanceTests
{
    private static Exercise Ex(LogKind kind, string id = "ex") => new(id, id, kind, null, false);

    private static string[] PrIds(Exercise e, History h) =>
        ExercisePerformance.Prs(e, h.Workouts).Select(p => p.WorkoutId).ToArray();

    // Best ordering

    [Fact]
    public void Load_best_uses_estimated_one_rep_max()
    {
        var squat = Ex(LogKind.Load);
        var h = new History().Done("w1", "2026-01-01", "ex", Set(kg: 100, reps: 5), Set(kg: 110, reps: 3));

        var best = ExercisePerformance.Best(squat, h.Workouts)!;

        Assert.Equal(110, best.Set.WeightKg); // 100x5 = 116.7, 110x3 = 121
        Assert.Equal(3, best.Set.Reps);
    }

    [Fact]
    public void Load_best_picks_higher_estimate_not_higher_weight()
    {
        var squat = Ex(LogKind.Load);
        var h = new History().Done("w1", "2026-01-01", "ex", Set(kg: 120, reps: 1), Set(kg: 100, reps: 8));

        var best = ExercisePerformance.Best(squat, h.Workouts)!;

        Assert.Equal(100, best.Set.WeightKg); // 126.7 vs 124
        Assert.Equal(8, best.Set.Reps);
    }

    [Fact]
    public void Bodyweight_sets_rank_below_any_loaded_set_and_among_themselves_by_reps()
    {
        var pullUp = Ex(LogKind.BodyweightLoad);
        var h = new History()
            .Done("w1", "2026-01-01", "ex", Set(reps: 30), Set(reps: 12))
            .Done("w2", "2026-01-08", "ex", Set(kg: 2.5, reps: 1));

        Assert.Equal(2.5, ExercisePerformance.Best(pullUp, h.Workouts)!.Set.WeightKg);

        var onlyBodyweight = new History().Done("w1", "2026-01-01", "ex", Set(reps: 12), Set(reps: 15));
        Assert.Equal(15, ExercisePerformance.Best(pullUp, onlyBodyweight.Workouts)!.Set.Reps);
    }

    [Fact]
    public void Weighted_bodyweight_sets_rank_by_estimated_one_rep_max()
    {
        var pullUp = Ex(LogKind.BodyweightLoad);
        var h = new History().Done("w1", "2026-01-01", "ex", Set(kg: 10, reps: 3), Set(kg: 5, reps: 45));

        Assert.Equal(5, ExercisePerformance.Best(pullUp, h.Workouts)!.Set.WeightKg); // 5x45 = 12.5 vs 10x3 = 11
    }

    [Fact]
    public void Carry_best_is_heavier_weight_then_longer_distance()
    {
        var carry = Ex(LogKind.Carry);
        var h = new History().Done("w1", "2026-01-01", "ex", Set(kg: 24, m: 100), Set(kg: 32, m: 20), Set(kg: 32, m: 40));

        var best = ExercisePerformance.Best(carry, h.Workouts)!;

        Assert.Equal(32, best.Set.WeightKg);
        Assert.Equal(40, best.Set.DistanceM);
    }

    [Fact]
    public void Time_best_is_the_lowest_time()
    {
        var run = Ex(LogKind.Time);
        var h = new History().Done("w1", "2026-01-01", "ex", Set(seconds: 1500)).Done("w2", "2026-01-08", "ex", Set(seconds: 1420));

        Assert.Equal("w2", ExercisePerformance.Best(run, h.Workouts)!.WorkoutId);
    }

    [Theory]
    [InlineData(LogKind.Reps)]
    [InlineData(LogKind.Seconds)]
    [InlineData(LogKind.Minutes)]
    [InlineData(LogKind.DistanceKm)]
    [InlineData(LogKind.DistanceM)]
    public void Other_measured_kinds_best_is_the_highest_value(LogKind kind)
    {
        var e = Ex(kind);
        var low = kind switch
        {
            LogKind.Reps => Set(reps: 10),
            LogKind.DistanceKm => Set(km: 2),
            LogKind.DistanceM => Set(m: 200),
            _ => Set(seconds: 60),
        };
        var high = kind switch
        {
            LogKind.Reps => Set(reps: 20),
            LogKind.DistanceKm => Set(km: 5),
            LogKind.DistanceM => Set(m: 500),
            _ => Set(seconds: 90),
        };
        var h = new History().Done("w1", "2026-01-01", "ex", high).Done("w2", "2026-01-08", "ex", low);

        Assert.Equal("w1", ExercisePerformance.Best(e, h.Workouts)!.WorkoutId);
    }

    [Theory]
    [InlineData(LogKind.BodyMeasurementKg)]
    [InlineData(LogKind.BodyMeasurementCm)]
    [InlineData(LogKind.Text)]
    [InlineData(LogKind.TickOnly)]
    public void Kinds_without_a_best_have_no_best_and_no_prs(LogKind kind)
    {
        var e = Ex(kind);
        var h = new History()
            .Done("w1", "2026-01-01", "ex", Set(kg: 80))
            .Done("w2", "2026-01-08", "ex", Set(kg: 79));

        Assert.Null(ExercisePerformance.Best(e, h.Workouts));
        Assert.Empty(ExercisePerformance.Prs(e, h.Workouts));
    }

    [Fact]
    public void No_history_has_no_best()
    {
        Assert.Null(ExercisePerformance.Best(Ex(LogKind.Load), []));
    }

    // Max sets

    [Fact]
    public void A_max_set_is_the_best_even_when_a_high_rep_estimate_is_higher()
    {
        var squat = Ex(LogKind.Load);
        var h = new History()
            .Done("w1", "2026-01-01", "ex", Set(kg: 100, reps: 10))
            .Done("w2", "2026-01-08", "ex", Set(kg: 110, reps: 1, max: true));

        var best = ExercisePerformance.Best(squat, h.Workouts)!;

        Assert.True(best.IsMax);
        Assert.Equal(110, best.Set.WeightKg);
    }

    [Fact]
    public void With_several_max_sets_the_best_max_set_wins()
    {
        var squat = Ex(LogKind.Load);
        var h = new History()
            .Done("w1", "2026-01-01", "ex", Set(kg: 120, reps: 1, max: true))
            .Done("w2", "2026-01-08", "ex", Set(kg: 125, reps: 1, max: true), Set(kg: 130, reps: 5));

        Assert.Equal(125, ExercisePerformance.Best(squat, h.Workouts)!.Set.WeightKg);
    }

    [Fact]
    public void An_ordinary_set_above_the_max_set_is_not_a_pr()
    {
        var squat = Ex(LogKind.Load);
        var h = new History()
            .Done("w1", "2026-01-01", "ex", Set(kg: 100, reps: 1, max: true))
            .Done("w2", "2026-01-08", "ex", Set(kg: 100, reps: 10));

        Assert.Empty(PrIds(squat, h));
    }

    // PRs

    [Fact]
    public void First_ever_result_is_a_baseline_not_a_pr_and_a_strict_improvement_is()
    {
        var squat = Ex(LogKind.Load);
        var h = new History()
            .Done("w1", "2026-01-01", "ex", Set(kg: 100, reps: 5))
            .Done("w2", "2026-01-08", "ex", Set(kg: 105, reps: 5));

        var prs = ExercisePerformance.Prs(squat, h.Workouts);

        var pr = Assert.Single(prs);
        Assert.Equal("w2", pr.WorkoutId);
        Assert.Equal(new DateOnly(2026, 1, 8), pr.Date);
        Assert.Equal(105, pr.Set.WeightKg);
    }

    [Fact]
    public void An_equal_repeat_is_not_a_pr()
    {
        var squat = Ex(LogKind.Load);
        var h = new History()
            .Done("w1", "2026-01-01", "ex", Set(kg: 100, reps: 5))
            .Done("w2", "2026-01-08", "ex", Set(kg: 100, reps: 5));

        Assert.Empty(PrIds(squat, h));
    }

    [Fact]
    public void A_lower_time_is_a_pr()
    {
        var run = Ex(LogKind.Time);
        var h = new History()
            .Done("w1", "2026-01-01", "ex", Set(seconds: 1500))
            .Done("w2", "2026-01-08", "ex", Set(seconds: 1600))
            .Done("w3", "2026-01-15", "ex", Set(seconds: 1450));

        Assert.Equal(["w3"], PrIds(run, h));
    }

    [Fact]
    public void A_back_dated_workout_changes_which_later_workouts_are_prs()
    {
        var squat = Ex(LogKind.Load);
        var before = new History()
            .Done("w1", "2026-01-01", "ex", Set(kg: 100, reps: 5))
            .Done("w3", "2026-01-15", "ex", Set(kg: 110, reps: 5));
        Assert.Equal(["w3"], PrIds(squat, before));

        var after = new History()
            .Done("w3", "2026-01-15", "ex", Set(kg: 110, reps: 5))
            .Done("w1", "2026-01-01", "ex", Set(kg: 100, reps: 5))
            .Done("w2", "2026-01-08", "ex", Set(kg: 115, reps: 5)); // back-dated, inserted last

        Assert.Equal(["w2"], PrIds(squat, after)); // w3 no longer beats the best
    }

    [Fact]
    public void Only_the_workouts_best_set_is_compared_and_a_workout_is_one_pr()
    {
        var squat = Ex(LogKind.Load);
        var h = new History()
            .Done("w1", "2026-01-01", "ex", Set(kg: 100, reps: 5))
            .Done("w2", "2026-01-08", "ex", Set(kg: 90, reps: 5), Set(kg: 105, reps: 5), Set(kg: 102, reps: 5));

        var pr = Assert.Single(ExercisePerformance.Prs(squat, h.Workouts));
        Assert.Equal(105, pr.Set.WeightKg);
    }

    // Counting rules

    [Fact]
    public void Skipped_entries_never_count()
    {
        var squat = Ex(LogKind.Load);
        var h = new History()
            .Done("w1", "2026-01-01", "ex", Set(kg: 100, reps: 5))
            .Skipped("w2", "2026-01-08", "ex");

        Assert.Equal("w1", ExercisePerformance.Best(squat, h.Workouts)!.WorkoutId);
        Assert.Empty(PrIds(squat, h));
    }

    [Fact]
    public void A_swapped_entrys_sets_count_for_the_exercise_actually_done_not_the_planned_original()
    {
        var original = Ex(LogKind.Load, "squat");
        var replacement = Ex(LogKind.Load, "legpress");
        var h = new History().Swapped("w1", "2026-01-01", plannedId: "squat", actualId: "legpress", Set(kg: 200, reps: 10));

        Assert.Null(ExercisePerformance.Best(original, h.Workouts));
        Assert.Equal(200, ExercisePerformance.Best(replacement, h.Workouts)!.Set.WeightKg);
    }

    [Fact]
    public void Other_exercises_in_the_history_are_ignored()
    {
        var squat = Ex(LogKind.Load, "squat");
        var h = new History().Done("w1", "2026-01-01", "bench", Set(kg: 80, reps: 5));

        Assert.Null(ExercisePerformance.Best(squat, h.Workouts));
    }

    // Progress points

    [Fact]
    public void Progress_points_give_one_point_per_day_using_the_days_best_set()
    {
        var squat = Ex(LogKind.Load);
        var h = new History()
            .Done("w1", "2026-01-01", "ex", Set(kg: 60, reps: 10), Set(kg: 80, reps: 5))
            .Done("w2", "2026-01-08", "ex", Set(kg: 90, reps: 3));

        var points = ExercisePerformance.ProgressPoints(squat, h.Workouts);

        Assert.Equal(2, points.Count);
        Assert.Equal(new DateOnly(2026, 1, 1), points[0].Date);
        Assert.Equal(80 * (1 + 5 / 30.0), points[0].Value, 6);
        Assert.Equal(90 * (1 + 3 / 30.0), points[1].Value, 6);
    }

    [Fact]
    public void Two_workouts_on_the_same_day_make_one_point_and_one_day_offers_no_series()
    {
        var squat = Ex(LogKind.Load);
        var sameDay = new History()
            .Done("w1", "2026-01-01", "ex", Set(kg: 80, reps: 5))
            .Done("w2", "2026-01-01", "ex", Set(kg: 85, reps: 5));

        Assert.Empty(ExercisePerformance.ProgressPoints(squat, sameDay.Workouts));

        var twoDays = sameDay.Done("w3", "2026-01-02", "ex", Set(kg: 70, reps: 5));
        var points = ExercisePerformance.ProgressPoints(squat, twoDays.Workouts);
        Assert.Equal(2, points.Count);
        Assert.Equal(85, points[0].Set.WeightKg);
    }

    [Fact]
    public void Progress_values_are_in_the_exercises_natural_measure()
    {
        var h = new History()
            .Done("w1", "2026-01-01", "ex", Set(reps: 10, seconds: 60, km: 2))
            .Done("w2", "2026-01-08", "ex", Set(reps: 12, seconds: 75, km: 3));

        Assert.Equal([10d, 12d], ExercisePerformance.ProgressPoints(Ex(LogKind.Reps), h.Workouts).Select(p => p.Value));
        Assert.Equal([60d, 75d], ExercisePerformance.ProgressPoints(Ex(LogKind.Seconds), h.Workouts).Select(p => p.Value));
        Assert.Equal([2d, 3d], ExercisePerformance.ProgressPoints(Ex(LogKind.DistanceKm), h.Workouts).Select(p => p.Value));
    }

    [Fact]
    public void Time_progress_picks_the_lowest_time_of_the_day()
    {
        var h = new History()
            .Done("w1", "2026-01-01", "ex", Set(seconds: 300), Set(seconds: 280))
            .Done("w2", "2026-01-08", "ex", Set(seconds: 270));

        Assert.Equal([280d, 270d], ExercisePerformance.ProgressPoints(Ex(LogKind.Time), h.Workouts).Select(p => p.Value));
    }

    [Fact]
    public void Text_and_tick_only_exercises_have_no_progress_points()
    {
        var h = new History()
            .Done("w1", "2026-01-01", "ex")
            .Done("w2", "2026-01-08", "ex");

        Assert.Empty(ExercisePerformance.ProgressPoints(Ex(LogKind.TickOnly), h.Workouts));
        Assert.Empty(ExercisePerformance.ProgressPoints(Ex(LogKind.Text), h.Workouts));
    }

    // Body measurements

    [Fact]
    public void Body_measurements_report_change_since_the_start_and_never_produce_prs()
    {
        var weight = Ex(LogKind.BodyMeasurementKg);
        var h = new History()
            .Done("w1", "2026-01-01", "ex", Set(kg: 85))
            .Done("w2", "2026-01-08", "ex", Set(kg: 84))
            .Done("w3", "2026-01-15", "ex", Set(kg: 82.5));

        var change = ExercisePerformance.BodyChange(weight, h.Workouts)!;

        Assert.Equal(85, change.Start);
        Assert.Equal(82.5, change.Latest);
        Assert.Equal(-2.5, change.Change);
        Assert.Empty(ExercisePerformance.Prs(weight, h.Workouts));
        Assert.Equal(3, ExercisePerformance.ProgressPoints(weight, h.Workouts).Count);
    }

    [Fact]
    public void Body_change_is_null_for_other_kinds_and_for_no_readings()
    {
        Assert.Null(ExercisePerformance.BodyChange(Ex(LogKind.Load), []));
        Assert.Null(ExercisePerformance.BodyChange(Ex(LogKind.BodyMeasurementKg), []));
    }

    // Snapshot

    [Fact]
    public void Snapshot_returns_last_and_best_together_with_an_empty_target_slot()
    {
        var squat = Ex(LogKind.Load);
        var h = new History()
            .Done("w1", "2026-01-01", "ex", Set(kg: 100, reps: 5))
            .DoneWithRpe("w2", "2026-01-08", "ex", 8, Set(kg: 90, reps: 5), Set(kg: 95, reps: 5))
            .Skipped("w3", "2026-01-15", "ex");

        var snap = ExercisePerformance.Snapshot(squat, h.Workouts);

        Assert.Equal("ex", snap.ExerciseId);
        Assert.Equal("w2", snap.Last!.WorkoutId);
        Assert.Equal(2, snap.Last.Sets.Count);
        Assert.Equal(8, snap.Last.Rpe);
        Assert.Equal(95, snap.Last.BestSet!.WeightKg);
        Assert.Equal("w1", snap.Best!.WorkoutId);
        Assert.Null(snap.Target);
    }

    [Fact]
    public void Snapshot_with_no_history_is_empty()
    {
        var snap = ExercisePerformance.Snapshot(Ex(LogKind.Load), []);

        Assert.Null(snap.Last);
        Assert.Null(snap.Best);
        Assert.Null(snap.Target);
    }

    [Fact]
    public void Snapshot_last_is_the_latest_done_workout_even_for_tick_only()
    {
        var h = new History().Done("w1", "2026-01-01", "ex");

        var snap = ExercisePerformance.Snapshot(Ex(LogKind.TickOnly), h.Workouts);

        Assert.Equal("w1", snap.Last!.WorkoutId);
        Assert.Null(snap.Best);
    }
}
