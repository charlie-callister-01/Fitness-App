using Core.Exchange;
using Core.Model;
using Core.Rules.Insight;
using Core.Rules.Units;
using static Core.Tests.Rules.TestData.HistoryBuilder;
using Core.Tests.Rules.TestData;

namespace Core.Tests.Rules.Insight;

public class ProgressSeriesTests
{
    private static readonly UnitConverter Kg = new(WeightUnit.Kg, DistanceUnit.Km, 2);
    private static readonly UnitConverter Lb = new(WeightUnit.Lb, DistanceUnit.Mi, 2);

    // The default Plan starts Monday 5 Jan 2026, so W1 is 5 to 11 Jan.
    private static readonly Plan Plan = PlanFixture.Make();

    private static Exercise Ex(LogKind kind, string id = "ex") => new(id, id.ToUpperInvariant(), kind, null, false);

    private static ExerciseSeries? Series(Exercise e, HistoryBuilder h, Plan? plan = null, UnitConverter? units = null) =>
        ProgressSeries.Build(e, plan ?? Plan, h.Workouts, units ?? Kg);

    [Theory]
    [InlineData(LogKind.Load, ExerciseGroup.Strength)]
    [InlineData(LogKind.Carry, ExerciseGroup.Carries)]
    [InlineData(LogKind.DistanceKm, ExerciseGroup.Engine)]
    [InlineData(LogKind.DistanceM, ExerciseGroup.Engine)]
    [InlineData(LogKind.Time, ExerciseGroup.Engine)]
    [InlineData(LogKind.Minutes, ExerciseGroup.Engine)]
    [InlineData(LogKind.Reps, ExerciseGroup.Bodyweight)]
    [InlineData(LogKind.Seconds, ExerciseGroup.Bodyweight)]
    [InlineData(LogKind.BodyweightLoad, ExerciseGroup.Bodyweight)]
    [InlineData(LogKind.BodyMeasurementKg, ExerciseGroup.Body)]
    [InlineData(LogKind.BodyMeasurementCm, ExerciseGroup.Body)]
    [InlineData(LogKind.Text, null)]
    [InlineData(LogKind.TickOnly, null)]
    public void Group_comes_from_the_log_kind(LogKind kind, ExerciseGroup? expected) =>
        Assert.Equal(expected, ExerciseGroups.Of(kind));

    [Fact]
    public void Strength_series_plots_estimated_one_rep_max_with_best_and_change_since_first_entry()
    {
        var h = new HistoryBuilder()
            .Done("w1", "2026-01-05", "ex", Set(kg: 100, reps: 5))
            .Done("w2", "2026-01-12", "ex", Set(kg: 100, reps: 8));

        var series = Series(Ex(LogKind.Load), h)!;

        Assert.Equal([116.67, 126.67], series.Points.Select(p => Math.Round(p.Value, 2)));
        Assert.Equal(new DateOnly(2026, 1, 12), series.Best.Date);
        Assert.Equal(10.0, series.Change, 2);
        Assert.Equal("Best 126.7 · +10.0 since W1", series.BestLabel);
        Assert.Equal("kg", series.UnitLabel);
        Assert.Equal(ExerciseGroup.Strength, series.Group);
    }

    [Fact]
    public void An_exercise_with_one_entry_has_no_series()
    {
        var h = new HistoryBuilder().Done("w1", "2026-01-05", "ex", Set(kg: 100, reps: 5));

        Assert.Null(Series(Ex(LogKind.Load), h));
    }

    [Fact]
    public void Text_and_tick_only_exercises_are_never_charted()
    {
        var h = new HistoryBuilder()
            .Done("w1", "2026-01-05", "ex", Set(reps: 5))
            .Done("w2", "2026-01-12", "ex", Set(reps: 6));

        Assert.Null(Series(Ex(LogKind.Text), h));
        Assert.Null(Series(Ex(LogKind.TickOnly), h));
    }

    [Fact]
    public void Points_that_set_a_PR_are_flagged_and_the_first_result_is_only_a_baseline()
    {
        var h = new HistoryBuilder()
            .Done("w1", "2026-01-05", "ex", Set(kg: 100, reps: 5))
            .Done("w2", "2026-01-12", "ex", Set(kg: 100, reps: 8))
            .Done("w3", "2026-01-19", "ex", Set(kg: 90, reps: 5));

        var series = Series(Ex(LogKind.Load), h)!;

        Assert.Equal([false, true, false], series.Points.Select(p => p.IsPr));
    }

    [Fact]
    public void A_long_gap_in_logging_breaks_the_line_and_a_short_one_does_not()
    {
        var h = new HistoryBuilder()
            .Done("w1", "2026-01-05", "ex", Set(kg: 100, reps: 5))
            .Done("w2", "2026-01-19", "ex", Set(kg: 100, reps: 6)) // 14 days: still joined
            .Done("w3", "2026-02-23", "ex", Set(kg: 100, reps: 7)); // 35 days: break

        var series = Series(Ex(LogKind.Load), h)!;

        Assert.Equal([false, false, true], series.Points.Select(p => p.BreaksBefore));
    }

    [Fact]
    public void Each_point_carries_the_Phase_its_date_falls_in()
    {
        var plan = PlanFixture.Make(phases:
        [
            new Phase("p1", "Foundation", 1, 2, [], []),
            new Phase("p2", "Build", 3, 4, [], []),
        ]);
        var h = new HistoryBuilder()
            .Done("w1", "2026-01-05", "ex", Set(kg: 100, reps: 5))
            .Done("w2", "2026-01-19", "ex", Set(kg: 100, reps: 6))
            .Done("w3", "2026-03-30", "ex", Set(kg: 100, reps: 7)); // W13, outside every Phase

        var series = Series(Ex(LogKind.Load), h, plan)!;

        Assert.Equal(["Foundation", "Build", null], series.Points.Select(p => p.PhaseFocus));
    }

    [Fact]
    public void A_lower_is_better_measure_has_its_lowest_value_as_best()
    {
        var h = new HistoryBuilder()
            .Done("w1", "2026-01-05", "ex", Set(seconds: 300))
            .Done("w2", "2026-01-12", "ex", Set(seconds: 280));

        var series = Series(Ex(LogKind.Time), h)!;

        Assert.Equal(280, series.Best.Value);
        Assert.Equal(-20, series.Change);
        Assert.Equal("s", series.UnitLabel);
    }

    [Fact]
    public void Weights_follow_the_owners_units()
    {
        var h = new HistoryBuilder()
            .Done("w1", "2026-01-05", "ex", Set(kg: 100, reps: 30))
            .Done("w2", "2026-01-12", "ex", Set(kg: 50, reps: 30));

        var series = Series(Ex(LogKind.Load), h, units: Lb)!;

        Assert.Equal("lb", series.UnitLabel);
        Assert.Equal(440.92, series.Points[0].Value, 2); // 200 kg estimated 1RM
        Assert.Equal(220.46, series.Points[1].Value, 2);
    }

    [Fact]
    public void A_body_measurement_plots_its_own_readings()
    {
        var h = new HistoryBuilder()
            .Done("w1", "2026-01-05", "ex", Set(cm: 92.5))
            .Done("w2", "2026-01-12", "ex", Set(cm: 91.0));

        var series = Series(Ex(LogKind.BodyMeasurementCm), h)!;

        Assert.Equal([92.5, 91.0], series.Points.Select(p => p.Value));
        Assert.Equal("cm", series.UnitLabel);
        Assert.Equal(ExerciseGroup.Body, series.Group);
        Assert.All(series.Points, p => Assert.False(p.IsPr));
    }

    [Fact]
    public void With_no_Plan_the_label_says_since_first_entry()
    {
        var h = new HistoryBuilder()
            .Done("w1", "2026-01-05", "ex", Set(reps: 5))
            .Done("w2", "2026-01-12", "ex", Set(reps: 8));

        var series = ProgressSeries.Build(Ex(LogKind.Reps), null, h.Workouts, Kg)!;

        Assert.Equal("Best 8 · +3 since first entry", series.BestLabel);
    }

    // A chosen range shows part of the history but the records stay the records.

    private static HistoryBuilder RangedHistory() => new HistoryBuilder()
        .Done("w1", "2026-01-05", "ex", Set(reps: 8))
        .Done("w2", "2026-01-12", "ex", Set(reps: 5))
        .Done("w3", "2026-01-19", "ex", Set(reps: 6))
        .Done("w4", "2026-01-26", "ex", Set(reps: 9));

    [Fact]
    public void A_range_never_makes_its_first_point_a_PR_it_was_not()
    {
        var range = new DateRange(new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 31));

        var series = ProgressSeries.Build(Ex(LogKind.Reps), Plan, RangedHistory().Workouts, Kg, range)!;

        Assert.Equal(3, series.Points.Count);
        Assert.False(series.Points[0].IsPr);
        Assert.True(series.Points[2].IsPr);
    }

    [Fact]
    public void A_range_takes_best_change_and_label_from_the_visible_points()
    {
        var range = new DateRange(new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 31));

        var series = ProgressSeries.Build(Ex(LogKind.Reps), Plan, RangedHistory().Workouts, Kg, range)!;

        Assert.Equal(9, series.Best.Value);
        Assert.Equal(4, series.Change);
        Assert.Equal(2, series.FirstPlanWeek);
        Assert.Equal("Best 9 · +4 since W2", series.BestLabel);
    }

    [Fact]
    public void A_range_with_one_point_leaves_the_Exercise_as_needing_two()
    {
        var range = new DateRange(new DateOnly(2026, 1, 25), new DateOnly(2026, 1, 31));

        Assert.Null(ProgressSeries.Build(Ex(LogKind.Reps), Plan, RangedHistory().Workouts, Kg, range));
        var list = ProgressList.Build([Ex(LogKind.Reps, "ex")], Plan, RangedHistory().Workouts, Kg, range);
        Assert.Single(list.Groups[0].NeedsTwo);
    }

    [Fact]
    public void A_range_with_no_Plan_says_since_first_entry()
    {
        var range = new DateRange(new DateOnly(2026, 1, 12), new DateOnly(2026, 1, 31));

        var series = ProgressSeries.Build(Ex(LogKind.Reps), null, RangedHistory().Workouts, Kg, range)!;

        Assert.Equal("Best 9 · +4 since first entry", series.BestLabel);
    }

    // The progress list

    [Fact]
    public void The_list_splits_chartable_from_needs_two_and_groups_in_chip_order()
    {
        var exercises = new[]
        {
            Ex(LogKind.Load, "squat"), Ex(LogKind.Load, "press"), Ex(LogKind.Seconds, "plank"),
            Ex(LogKind.Text, "journal"), Ex(LogKind.Load, "unused"),
        };
        var h = new HistoryBuilder()
            .Done("w1", "2026-01-05", "squat", Set(kg: 100, reps: 5))
            .Done("w2", "2026-01-12", "squat", Set(kg: 100, reps: 6))
            .Done("w3", "2026-01-12", "press", Set(kg: 50, reps: 5))
            .Done("w4", "2026-01-05", "plank", Set(seconds: 30))
            .Done("w5", "2026-01-12", "plank", Set(seconds: 40))
            .Done("w6", "2026-01-12", "journal", Set());

        var list = ProgressList.Build(exercises, Plan, h.Workouts, Kg);

        Assert.False(list.IsEmpty);
        Assert.Equal([ExerciseGroup.Strength, ExerciseGroup.Bodyweight], list.Groups.Select(g => g.Group));
        var strength = list.Groups[0];
        Assert.Equal(["squat"], strength.Charts.Select(c => c.ExerciseId));
        Assert.Equal([("press", "PRESS")], strength.NeedsTwo.Select(r => (r.ExerciseId, r.Name)));
        Assert.Equal(["plank"], list.Groups[1].Charts.Select(c => c.ExerciseId));
    }

    [Fact]
    public void Two_entries_on_the_same_day_are_one_entry()
    {
        var exercises = new[] { Ex(LogKind.Load, "squat") };
        var h = new HistoryBuilder()
            .Done("w1", "2026-01-05", "squat", Set(kg: 100, reps: 5))
            .Done("w2", "2026-01-05", "squat", Set(kg: 100, reps: 6));

        var list = ProgressList.Build(exercises, Plan, h.Workouts, Kg);

        Assert.Empty(list.Groups[0].Charts);
        Assert.Single(list.Groups[0].NeedsTwo);
    }

    [Fact]
    public void With_no_history_the_list_is_empty()
    {
        var list = ProgressList.Build([Ex(LogKind.Load)], Plan, [], Kg);

        Assert.True(list.IsEmpty);
        Assert.Empty(list.Groups);
    }
}
