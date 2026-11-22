using Core.Model;
using static Core.Tests.Rules.TestData.Dates;
using Core.Rules.Badges;
using Core.Tests.Rules.TestData;

namespace Core.Tests.Rules.Badges;

public class BadgeRulesTests
{
    private static readonly DateOnly Today = D(2026, 11, 18);

    private static readonly Exercise Squat = new("squat", "Squat", LogKind.Load, null, false);
    private static readonly Exercise Pullup = new("pullup", "Pull-up", LogKind.Reps, null, false);
    private static readonly Exercise Run = new("run5k", "5 km", LogKind.Time, null, true);
    private static readonly Exercise Waist = new("waist", "Waist", LogKind.BodyMeasurementCm, null, false);

    private static BadgeHistory Hist(HistoryBuilder? weeks = null, HistoryBuilder? lifts = null, params WeighIn[] weighIns)
    {
        weeks ??= new HistoryBuilder();
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
        var h = Hist(new HistoryBuilder().Did(Mon, Mon.AddDays(1), Mon.AddDays(-7)).BenchmarkTest(Mon.AddDays(5)));
        var p = P("sessions", 4, h);
        Assert.Equal(3, p.Value);
        Assert.Equal(75, p.Percent);
        Assert.False(p.Reached);
    }

    [Fact]
    public void Streak_uses_the_best_ever_run_and_best_week_the_busiest_week()
    {
        var h = Hist(new HistoryBuilder().Did(Mon.AddDays(-14), Mon.AddDays(-13), Mon.AddDays(-7), Mon.AddDays(-6), Mon.AddDays(-5)));
        Assert.Equal(2, P("streak", 2, h).Value);
        Assert.True(P("streak", 2, h).Reached);
        Assert.Equal(3, P("bestweek", 3, h).Value);
        Assert.Equal(3, P("Best Week", 3, h).Value);
    }

    [Fact]
    public void Prs_counts_PRs_across_all_Exercises()
    {
        var lifts = new HistoryBuilder()
            .Done("w1", "2026-11-02", "squat", HistoryBuilder.Set(80, 5))
            .Done("w2", "2026-11-09", "squat", HistoryBuilder.Set(90, 5))
            .Done("w3", "2026-11-02", "pullup", HistoryBuilder.Set(reps: 5))
            .Done("w4", "2026-11-09", "pullup", HistoryBuilder.Set(reps: 8));
        var p = P("prs", 4, Hist(lifts: lifts));
        Assert.Equal(2, p.Value);
        Assert.Equal(50, p.Percent);
    }

    [Fact]
    public void An_Exercise_metric_uses_its_best_value_in_its_own_measure()
    {
        var lifts = new HistoryBuilder().Done("w1", "2026-11-02", "pullup", HistoryBuilder.Set(reps: 6), HistoryBuilder.Set(reps: 10));
        var p = P("pullup", 10, Hist(lifts: lifts));
        Assert.Equal(10, p.Value);
        Assert.True(p.Reached);
        Assert.Equal(100, p.Percent);
    }

    [Fact]
    public void Progress_is_capped_at_100_and_reached_at_or_past_the_target()
    {
        var lifts = new HistoryBuilder().Done("w1", "2026-11-02", "pullup", HistoryBuilder.Set(reps: 15));
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
        var lifts = new HistoryBuilder().Done("w1", "2026-11-02", "run5k", HistoryBuilder.Set(seconds: 1500));
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
        var lifts = new HistoryBuilder()
            .Done("w1", "2026-10-01", "waist", HistoryBuilder.Set(cm: 100))
            .Done("w2", "2026-11-01", "waist", HistoryBuilder.Set(cm: 95));
        var p = P("waist", -10, Hist(lifts: lifts));
        Assert.Equal(5, p.Value);
        Assert.Equal(50, p.Percent);
    }

    [Fact]
    public void A_growing_measurement_gives_no_drop()
    {
        var lifts = new HistoryBuilder()
            .Done("w1", "2026-10-01", "waist", HistoryBuilder.Set(cm: 90))
            .Done("w2", "2026-11-01", "waist", HistoryBuilder.Set(cm: 95));
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
    [InlineData("streak:unknown")]
    [InlineData("days:unknown")]
    [InlineData("bestweek:unknown")]
    [InlineData("streak:")]
    [InlineData("days:")]
    [InlineData("bestweek:")]
    [InlineData("unknown:main")]
    public void An_unknown_metric_is_not_measurable_and_carries_its_name(string metric)
    {
        var p = P(metric, 100, Hist());
        Assert.False(p.IsMeasurable);
        Assert.Equal(metric, p.Metric);
        Assert.Equal(0, p.Percent);
        Assert.False(p.Reached);
    }

    [Fact]
    public void Qualified_metrics_measure_a_weekly_Track_and_unqualified_metrics_measure_the_main_Track()
    {
        var (history, reordered) = MultiTrackHistories();

        Assert.Equal(5, P("sessions", 10, history).Value);
        Assert.Equal(2, P("streak", 4, history).Value);
        Assert.Equal(3, P("bestweek", 4, history).Value);

        Assert.Equal(3, P("streak:cardio", 3, history).Value);
        Assert.True(P("streak:cardio", 3, history).Reached);
        Assert.Equal(5, P("days:cardio", 10, history).Value);
        Assert.Equal(2, P("bestweek:cardio", 4, history).Value);

        // Reordering Tracks leaves qualified metrics measuring the named Track by ID.
        Assert.Equal(3, P("streak:cardio", 3, reordered).Value);
        Assert.Equal(5, P("days:cardio", 10, reordered).Value);
        Assert.Equal(2, P("bestweek:cardio", 4, reordered).Value);
        Assert.Equal(2, P("streak:main", 4, reordered).Value);
        Assert.Equal(5, P("days:main", 10, reordered).Value);
    }

    [Fact]
    public void Qualified_metrics_measure_a_daily_Track_and_bestweek_is_not_measurable()
    {
        var (history, _) = MultiTrackHistories();

        Assert.Equal(4, P("streak:mobility", 4, history).Value);
        Assert.True(P("streak:mobility", 4, history).Reached);
        Assert.Equal(5, P("days:mobility", 10, history).Value);

        var bestWeek = P("bestweek:mobility", 5, history);
        Assert.False(bestWeek.IsMeasurable);
        Assert.Equal("bestweek:mobility", bestWeek.Metric);
        Assert.Equal(0, bestWeek.Value);
        Assert.Equal(0, bestWeek.Percent);
        Assert.False(bestWeek.Reached);
    }

    [Theory]
    [InlineData("STREAK:Cardio", 3)]
    [InlineData("DaYs:CARDIO", 5)]
    [InlineData("BestWeek:CaRdIo", 2)]
    [InlineData("STREAK:MOBILITY", 4)]
    [InlineData("Days:Mobility", 5)]
    public void Qualified_metrics_match_prefix_and_Track_ID_case_insensitively(string metric, double expected)
    {
        var (history, _) = MultiTrackHistories();
        var progress = P(metric, 10, history);
        Assert.True(progress.IsMeasurable);
        Assert.Equal(expected, progress.Value);
    }

    private static (BadgeHistory Normal, BadgeHistory Reordered) MultiTrackHistories()
    {
        var main = new Track("main", "Main", new TrackRhythm.Weekly(3, 2, false), []);
        var cardio = new Track("cardio", "Cardio", new TrackRhythm.Weekly(2, 1, false), []);
        var mobility = new Track("mobility", "Mobility", new TrackRhythm.Daily(75), []);
        IReadOnlyList<Session> sessions =
        [
            PlanFixture.Session("s-main", DefaultDay.Monday, trackId: "main"),
            PlanFixture.Session("s-cardio", DefaultDay.Tuesday, SessionType.Cardio, trackId: "cardio"),
            PlanFixture.Session("s-mob", DefaultDay.Daily, SessionType.Mobility, trackId: "mobility"),
        ];
        var start = D(2026, 10, 26);
        var plan = PlanFixture.Make(start: start, tracks: [main, cardio, mobility], sessions: sessions);
        var reorderedPlan = plan with { Tracks = [cardio, main, mobility] };

        Workout DoneSession(string id, DateOnly date, string sessionId) =>
            new(id, date, sessionId, false, null, null, [],
                [new WorkoutEntry("squat", EntryStatus.Done, null, null, null, [])], null);

        Workout DailyWorkout(string id, DateOnly date, int doneRows, int totalRows)
        {
            var planned = Enumerable.Range(1, totalRows)
                .Select(i => new SessionExercise($"ex{i}", 1, null, null, null))
                .ToList();
            var entries = Enumerable.Range(1, totalRows)
                .Select(i => new WorkoutEntry($"ex{i}", i <= doneRows ? EntryStatus.Done : EntryStatus.Skipped,
                    i <= doneRows ? null : "skip", null, null, []))
                .ToList();
            return new(id, date, "s-mob", false, null, null, planned, entries, null);
        }

        IReadOnlyList<Workout> workouts =
        [
            // Main track: 2 in week -2, 3 in week -1 => 5 sessions, streak 2, bestweek 3
            DoneSession("m1", Mon.AddDays(-14), "s-main"),
            DoneSession("m2", Mon.AddDays(-13), "s-main"),
            DoneSession("m3", Mon.AddDays(-7), "s-main"),
            DoneSession("m4", Mon.AddDays(-6), "s-main"),
            DoneSession("m5", Mon.AddDays(-5), "s-main"),
            // Cardio track: 1 in week -3, 1 in week -2, 2 in week -1, 1 in week 0 => 5 days, streak 4, bestweek 2
            DoneSession("c1", Mon.AddDays(-21), "s-cardio"),
            DoneSession("c2", Mon.AddDays(-14), "s-cardio"),
            DoneSession("c3", Mon.AddDays(-7), "s-cardio"),
            DoneSession("c4", Mon.AddDays(-6), "s-cardio"),
            DoneSession("c5", Mon, "s-cardio"),
            // Mobility track: 4 done days in a run (3/4 = 75% across a Break day), 1 failed day (2/4 = 50%), 1 earlier done day => days 5, streak 4
            DailyWorkout("d0", D(2026, 11, 1), 3, 4),
            DailyWorkout("d-fail", D(2026, 11, 10), 2, 4),
            DailyWorkout("d1", D(2026, 11, 13), 3, 4),
            DailyWorkout("d2", D(2026, 11, 14), 4, 4),
            DailyWorkout("d3", D(2026, 11, 15), 3, 4),
            // 2026-11-16 is a Break day (neutral)
            DailyWorkout("d4", D(2026, 11, 17), 3, 4),
        ];
        IReadOnlyList<Break> breaks = [new("b1", D(2026, 11, 16), D(2026, 11, 16), "Rest")];

        return (
            new BadgeHistory(plan, [Squat, Pullup, Run, Waist], workouts, breaks, []),
            new BadgeHistory(reorderedPlan, [Squat, Pullup, Run, Waist], workouts, breaks, []));
    }
}
