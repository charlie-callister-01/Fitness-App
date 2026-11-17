using Core.Model;
using Core.Rules.Badges;
using Core.Tests.Rules.Performance;
using Core.Tests.Rules.Weeks;

namespace Core.Tests.Rules.Badges;

public class BadgeRulesTests
{
    private static DateOnly D(int y, int m, int d) => new(y, m, d);
    private static readonly DateOnly Mon = D(2026, 11, 16);
    private static readonly DateOnly Today = D(2026, 11, 18);

    private static readonly Exercise Squat = new("squat", "Squat", LogKind.Load, null, false);
    private static readonly Exercise Pullup = new("pullup", "Pull-up", LogKind.Reps, null, false);
    private static readonly Exercise Run = new("run5k", "5 km", LogKind.Time, null, true);
    private static readonly Exercise Waist = new("waist", "Waist", LogKind.BodyMeasurementCm, null, false);

    private static BadgeHistory Hist(WeekHistory? weeks = null, History? lifts = null, params WeighIn[] weighIns)
    {
        weeks ??= new WeekHistory();
        return new BadgeHistory(
            weeks.Plan,
            [Squat, Pullup, Run, Waist],
            weeks.Workouts.Concat(lifts?.Workouts ?? []).ToList(),
            weeks.Breaks,
            weighIns);
    }

    private static BadgeProgress P(string metric, double target, BadgeHistory h) =>
        BadgeRules.Progress(new Badge("b", "Badge", metric, target), h, Today);

    [Fact]
    public void Sessions_counts_counted_Workouts_across_weeks_ignoring_benchmark_tests()
    {
        var h = Hist(new WeekHistory().Did(Mon, Mon.AddDays(1), Mon.AddDays(-7)).BenchmarkTest(Mon.AddDays(5)));
        var p = P("sessions", 4, h);
        Assert.Equal(3, p.Value);
        Assert.Equal(75, p.Percent);
        Assert.False(p.Reached);
    }

    [Fact]
    public void Streak_uses_the_best_ever_run_and_best_week_the_busiest_week()
    {
        var h = Hist(new WeekHistory().Did(Mon.AddDays(-14), Mon.AddDays(-13), Mon.AddDays(-7), Mon.AddDays(-6), Mon.AddDays(-5)));
        Assert.Equal(2, P("streak", 2, h).Value);
        Assert.True(P("streak", 2, h).Reached);
        Assert.Equal(3, P("bestweek", 3, h).Value);
        Assert.Equal(3, P("Best Week", 3, h).Value);
    }

    [Fact]
    public void Prs_counts_PRs_across_all_Exercises()
    {
        var lifts = new History()
            .Done("w1", "2026-11-02", "squat", History.Set(80, 5))
            .Done("w2", "2026-11-09", "squat", History.Set(90, 5))
            .Done("w3", "2026-11-02", "pullup", History.Set(reps: 5))
            .Done("w4", "2026-11-09", "pullup", History.Set(reps: 8));
        var p = P("prs", 4, Hist(lifts: lifts));
        Assert.Equal(2, p.Value);
        Assert.Equal(50, p.Percent);
    }

    [Fact]
    public void An_Exercise_metric_uses_its_best_value_in_its_own_measure()
    {
        var lifts = new History().Done("w1", "2026-11-02", "pullup", History.Set(reps: 6), History.Set(reps: 10));
        var p = P("pullup", 10, Hist(lifts: lifts));
        Assert.Equal(10, p.Value);
        Assert.True(p.Reached);
        Assert.Equal(100, p.Percent);
    }

    [Fact]
    public void Progress_is_capped_at_100_and_reached_at_or_past_the_target()
    {
        var lifts = new History().Done("w1", "2026-11-02", "pullup", History.Set(reps: 15));
        var p = P("pullup", 10, Hist(lifts: lifts));
        Assert.Equal(100, p.Percent);
        Assert.True(p.Reached);
    }

    [Fact]
    public void An_Exercise_with_no_history_is_measurable_at_zero()
    {
        var p = P("pullup", 10, Hist());
        Assert.True(p.IsMeasurable);
        Assert.Equal(0, p.Percent);
        Assert.False(p.Reached);
    }

    [Fact]
    public void A_Time_Exercise_is_reached_by_going_at_or_under_the_target()
    {
        var lifts = new History().Done("w1", "2026-11-02", "run5k", History.Set(seconds: 1500));
        var slow = P("run5k", 1200, Hist(lifts: lifts));
        Assert.False(slow.Reached);
        Assert.Equal(80, slow.Percent);
        Assert.True(P("run5k", 1500, Hist(lifts: lifts)).Reached);
    }

    [Fact]
    public void Bodyweight_metric_uses_the_latest_Weigh_in()
    {
        var p = P("bodyweight", 80, Hist(null, null, new WeighIn("a", D(2026, 10, 1), 70), new WeighIn("b", D(2026, 11, 1), 80)));
        Assert.Equal(80, p.Value);
        Assert.True(p.Reached);
    }

    [Fact]
    public void A_negative_bodyweight_target_is_a_drop_from_the_first_Weigh_in()
    {
        var h = Hist(null, null, new WeighIn("a", D(2026, 10, 1), 90), new WeighIn("b", D(2026, 11, 1), 86));
        var p = P("bodyweight", -5, h);
        Assert.Equal(4, p.Value);
        Assert.Equal(80, p.Percent);
        Assert.False(p.Reached);
        Assert.True(P("bodyweight", -4, h).Reached);
    }

    [Fact]
    public void A_negative_target_on_a_measurement_Exercise_is_a_drop_from_its_start()
    {
        var lifts = new History()
            .Done("w1", "2026-10-01", "waist", History.Set(cm: 100))
            .Done("w2", "2026-11-01", "waist", History.Set(cm: 95));
        var p = P("waist", -10, Hist(lifts: lifts));
        Assert.Equal(5, p.Value);
        Assert.Equal(50, p.Percent);
    }

    [Fact]
    public void A_growing_measurement_gives_no_drop()
    {
        var lifts = new History()
            .Done("w1", "2026-10-01", "waist", History.Set(cm: 90))
            .Done("w2", "2026-11-01", "waist", History.Set(cm: 95));
        var p = P("waist", -5, Hist(lifts: lifts));
        Assert.Equal(-5, p.Value);
        Assert.Equal(0, p.Percent);
        Assert.False(p.Reached);
    }

    [Fact]
    public void No_Weigh_ins_means_zero_progress()
    {
        var p = P("bodyweight", -5, Hist());
        Assert.True(p.IsMeasurable);
        Assert.Equal(0, p.Percent);
        Assert.False(p.Reached);
    }

    [Theory]
    [InlineData("maxWeightKg")]
    [InlineData("")]
    [InlineData("no-such-exercise")]
    public void An_unknown_metric_is_not_measurable_and_carries_its_name(string metric)
    {
        var p = P(metric, 100, Hist());
        Assert.False(p.IsMeasurable);
        Assert.Equal(metric, p.Metric);
        Assert.Equal(0, p.Percent);
        Assert.False(p.Reached);
    }
}
