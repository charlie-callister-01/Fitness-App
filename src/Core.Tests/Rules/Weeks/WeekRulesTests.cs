using Core.Model;
using Core.Rules.Tracks;
using Core.Rules.Weeks;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;

namespace Core.Tests.Rules.Weeks;

public class WeekRulesTests
{

    // Default Plan starts Mon 2026-01-05; weekly target 3, Streak minimum 2. 2026-11-16 is a Monday.

    // ---- counted Workouts

    [Fact]
    public void Counted_workouts_include_partial_and_swapped_but_not_empty_or_all_skipped()
    {
        var h = new HistoryBuilder().Did(Mon).Swapped(Mon.AddDays(1)).AllSkipped(Mon.AddDays(2)).EmptyWorkout(Mon.AddDays(3));
        Assert.Equal(2, WeekRules.CountedWorkouts(h.Plan, TrackRules.Main(h.Plan), h.Workouts, Mon));
    }

    [Fact]
    public void Benchmark_test_Sessions_do_not_count()
    {
        var h = new HistoryBuilder().Did(Mon).BenchmarkTest(Mon.AddDays(5));
        Assert.Equal(1, WeekRules.CountedWorkouts(h.Plan, TrackRules.Main(h.Plan), h.Workouts, Mon));
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public void Custom_workouts_count_only_when_the_Plan_rule_says_so(bool customCounts, int expected)
    {
        var h = new HistoryBuilder().WithPlan(D(2026, 1, 5), customCounts: customCounts).Did(Mon).Custom(Mon.AddDays(1));
        Assert.Equal(expected, WeekRules.CountedWorkouts(h.Plan, TrackRules.Main(h.Plan), h.Workouts, Mon));
    }

    [Fact]
    public void Only_Workouts_inside_the_Monday_to_Sunday_week_count()
    {
        var h = new HistoryBuilder().Did(Mon.AddDays(-1), Mon, Mon.AddDays(6), Mon.AddDays(7));
        Assert.Equal(2, WeekRules.CountedWorkouts(h.Plan, TrackRules.Main(h.Plan), h.Workouts, Mon));
    }

    // ---- Streak

    [Fact]
    public void Streak_counts_consecutive_qualifying_completed_weeks()
    {
        var h = new HistoryBuilder().Did(D(2026, 11, 2), D(2026, 11, 3), D(2026, 11, 9), D(2026, 11, 10));
        Assert.Equal(2, WeekRules.Streak(h.Plan, TrackRules.Main(h.Plan), h.Workouts, h.Breaks, D(2026, 11, 18)).Current);
    }

    [Fact]
    public void An_unqualifying_completed_week_breaks_the_Streak()
    {
        // Nov 2 qualifies, Nov 9 has one, Nov 16 (current) qualifies: the failed week resets the run.
        var h = new HistoryBuilder().Did(D(2026, 11, 2), D(2026, 11, 3), D(2026, 11, 9), Mon, D(2026, 11, 17));
        var s = WeekRules.Streak(h.Plan, TrackRules.Main(h.Plan), h.Workouts, h.Breaks, D(2026, 11, 18));
        Assert.Equal(1, s.Current);
        Assert.Equal(1, s.BestRun);
    }

    [Fact]
    public void The_current_week_never_breaks_the_Streak_while_in_progress()
    {
        var h = new HistoryBuilder().Did(D(2026, 11, 2), D(2026, 11, 3), D(2026, 11, 9), D(2026, 11, 10));
        Assert.Equal(2, WeekRules.Streak(h.Plan, TrackRules.Main(h.Plan), h.Workouts, h.Breaks, Mon).Current); // Monday, nothing logged yet
    }

    [Fact]
    public void The_current_week_joins_the_Streak_the_moment_it_qualifies()
    {
        var h = new HistoryBuilder().Did(D(2026, 11, 9), D(2026, 11, 10), Mon, Mon.AddDays(1));
        Assert.Equal(2, WeekRules.Streak(h.Plan, TrackRules.Main(h.Plan), h.Workouts, h.Breaks, Mon.AddDays(1)).Current);
    }

    [Fact]
    public void A_week_touched_by_a_Break_neither_breaks_nor_adds()
    {
        var h = new HistoryBuilder()
            .Did(D(2026, 11, 2), D(2026, 11, 3))
            .Break(D(2026, 11, 12), D(2026, 11, 12)) // week of Nov 9 has no workouts but is excused
            .Did(Mon, Mon.AddDays(1));
        var s = WeekRules.Streak(h.Plan, TrackRules.Main(h.Plan), h.Workouts, h.Breaks, Mon.AddDays(2));
        Assert.Equal(2, s.Current);
        Assert.Equal(2, s.BestRun);
    }

    [Fact]
    public void A_week_that_qualifies_despite_a_Break_does_not_add()
    {
        var h = new HistoryBuilder().Break(D(2026, 11, 12), D(2026, 11, 12)).Did(D(2026, 11, 9), D(2026, 11, 10));
        Assert.Equal(0, WeekRules.Streak(h.Plan, TrackRules.Main(h.Plan), h.Workouts, h.Breaks, Mon).Current);
    }

    [Fact]
    public void Weeks_before_the_Plan_start_are_ignored()
    {
        var h = new HistoryBuilder().WithPlan(D(2026, 11, 9)).Did(D(2026, 11, 9), D(2026, 11, 10));
        Assert.Equal(1, WeekRules.Streak(h.Plan, TrackRules.Main(h.Plan), h.Workouts, h.Breaks, Mon).Current);
    }

    [Fact]
    public void A_new_Plan_has_no_Streak_before_its_first_week_completes()
    {
        var h = new HistoryBuilder().WithPlan(Mon);
        Assert.Equal(0, WeekRules.Streak(h.Plan, TrackRules.Main(h.Plan), h.Workouts, h.Breaks, Mon.AddDays(3)).Current);
    }

    [Fact]
    public void Best_run_and_best_week_count_are_exposed_for_Badges()
    {
        var h = new HistoryBuilder()
            .Did(D(2026, 10, 5), D(2026, 10, 6), D(2026, 10, 7), D(2026, 10, 8)) // 4 in a week
            .Did(D(2026, 10, 12), D(2026, 10, 13))
            .Did(D(2026, 10, 19)) // fails
            .Did(D(2026, 10, 26), D(2026, 10, 27));
        var s = WeekRules.Streak(h.Plan, TrackRules.Main(h.Plan), h.Workouts, h.Breaks, D(2026, 11, 3));
        Assert.Equal(2, s.BestRun);
        Assert.Equal(4, s.BestWeekCount);
        Assert.Equal(1, s.Current);
    }

    [Fact]
    public void A_week_across_the_year_boundary_is_one_week()
    {
        var h = new HistoryBuilder()
            .Did(D(2026, 12, 29), D(2027, 1, 1)) // Tue and Fri of the same ISO week
            .Did(D(2027, 1, 5), D(2027, 1, 6));
        Assert.Equal(2, WeekRules.Streak(h.Plan, TrackRules.Main(h.Plan), h.Workouts, h.Breaks, D(2027, 1, 11)).Current);
    }

    // ---- Consistency

    [Fact]
    public void Target_hit_when_the_target_is_reached()
    {
        var h = new HistoryBuilder().Did(Mon, Mon.AddDays(1), Mon.AddDays(2));
        Assert.Equal(ConsistencyKind.TargetHit, WeekRules.Consistency(h.Plan, TrackRules.Main(h.Plan), h.Workouts, h.Breaks, Mon.AddDays(3)).Kind);
    }

    [Fact]
    public void On_track_with_a_spare_day_and_a_reason()
    {
        var h = new HistoryBuilder();
        var c = WeekRules.Consistency(h.Plan, TrackRules.Main(h.Plan), h.Workouts, h.Breaks, Mon);
        Assert.Equal(ConsistencyKind.OnTrack, c.Kind);
        Assert.False(string.IsNullOrWhiteSpace(c.Message));
    }

    [Fact]
    public void Behind_when_every_remaining_day_is_needed()
    {
        var h = new HistoryBuilder(); // 3 needed, Fri/Sat/Sun left
        Assert.Equal(ConsistencyKind.Behind, WeekRules.Consistency(h.Plan, TrackRules.Main(h.Plan), h.Workouts, h.Breaks, Mon.AddDays(4)).Kind);
    }

    [Fact]
    public void Behind_on_Saturday_and_on_track_on_Friday_with_one_done()
    {
        var h = new HistoryBuilder().Did(Mon);
        Assert.Equal(ConsistencyKind.Behind, WeekRules.Consistency(h.Plan, TrackRules.Main(h.Plan), h.Workouts, h.Breaks, Mon.AddDays(5)).Kind); // Sat, Sun for 2
        Assert.Equal(ConsistencyKind.OnTrack, WeekRules.Consistency(h.Plan, TrackRules.Main(h.Plan), h.Workouts, h.Breaks, Mon.AddDays(4)).Kind); // Fri-Sun for 2
    }

    [Fact]
    public void Out_of_reach_when_more_remain_than_days_left()
    {
        var h = new HistoryBuilder().Did(Mon);
        Assert.Equal(ConsistencyKind.TargetOutOfReach, WeekRules.Consistency(h.Plan, TrackRules.Main(h.Plan), h.Workouts, h.Breaks, Mon.AddDays(6)).Kind);
    }

    [Fact]
    public void Today_counts_as_an_available_day()
    {
        var h = new HistoryBuilder().Did(Mon, Mon.AddDays(1)); // one to go, only Sunday left
        Assert.Equal(ConsistencyKind.Behind, WeekRules.Consistency(h.Plan, TrackRules.Main(h.Plan), h.Workouts, h.Breaks, Mon.AddDays(6)).Kind);
    }

    [Fact]
    public void Break_days_are_not_available_days()
    {
        var h = new HistoryBuilder().Break(Mon.AddDays(2), Mon.AddDays(6)); // Wed to Sun excused
        // Tuesday: one available day left, 3 needed.
        Assert.Equal(ConsistencyKind.TargetOutOfReach, WeekRules.Consistency(h.Plan, TrackRules.Main(h.Plan), h.Workouts, h.Breaks, Mon.AddDays(1)).Kind);
    }

    // ---- Plan week number

    [Theory]
    [InlineData(2026, 1, 5, 1)]
    [InlineData(2026, 1, 11, 1)]
    [InlineData(2026, 1, 12, 2)]
    [InlineData(2027, 1, 4, 53)]
    public void Plan_week_number_counts_from_one_at_the_start_week(int y, int m, int d, int expected)
    {
        Assert.Equal(expected, WeekRules.PlanWeekNumber(new HistoryBuilder().Plan, new DateOnly(y, m, d)));
    }

    [Fact]
    public void Plan_week_number_is_null_before_the_Plan_starts()
    {
        Assert.Null(WeekRules.PlanWeekNumber(new HistoryBuilder().Plan, new DateOnly(2026, 1, 4)));
    }

    [Fact]
    public void A_mid_week_start_date_makes_that_whole_week_week_one()
    {
        var plan = new HistoryBuilder().WithPlan(new DateOnly(2026, 1, 8)).Plan; // Thursday
        Assert.Equal(1, WeekRules.PlanWeekNumber(plan, new DateOnly(2026, 1, 8)));
        Assert.Equal(2, WeekRules.PlanWeekNumber(plan, new DateOnly(2026, 1, 12)));
    }

    // ---- Multi-Track

    private static Workout DoneSession(DateOnly date, string? sessionId) =>
        new($"w-{date.DayNumber}-{sessionId}", date, sessionId, false, null, null, [],
            [new WorkoutEntry("squat", EntryStatus.Done, null, null, null, [])], null);

    [Fact]
    public void Two_weekly_Tracks_are_counted_separately_for_counts_Streak_best_week_and_consistency()
    {
        var gym = new Track("gym", "Gym", new TrackRhythm.Weekly(3, 2, false), []);
        var cardio = new Track("cardio", "Cardio", new TrackRhythm.Weekly(2, 1, false), []);
        var plan = PlanFixture.Make(start: D(2026, 1, 5), tracks: [gym, cardio], sessions:
        [
            PlanFixture.Session("s-gym", DayOfWeek.Monday, trackId: "gym"),
            PlanFixture.Session("s-run", DayOfWeek.Tuesday, SessionType.Cardio, trackId: "cardio"),
        ]);

        var workouts = new[]
        {
            // Week of Nov 9: 2 gym, 1 cardio
            DoneSession(D(2026, 11, 9), "s-gym"),
            DoneSession(D(2026, 11, 11), "s-gym"),
            DoneSession(D(2026, 11, 10), "s-run"),
            // Week of Mon (Nov 16): 1 gym, 2 cardio
            DoneSession(Mon, "s-gym"),
            DoneSession(Mon.AddDays(1), "s-run"),
            DoneSession(Mon.AddDays(3), "s-run"),
        };

        Assert.Equal(1, WeekRules.CountedWorkouts(plan, gym, workouts, Mon));
        Assert.Equal(2, WeekRules.CountedWorkouts(plan, cardio, workouts, Mon));
        Assert.Equal(2, WeekRules.CountedWorkoutsByWeek(plan, gym, workouts)[D(2026, 11, 9)]);
        Assert.Equal(1, WeekRules.CountedWorkoutsByWeek(plan, cardio, workouts)[D(2026, 11, 9)]);

        var gymStreak = WeekRules.Streak(plan, gym, workouts, [], Mon.AddDays(4));
        var cardioStreak = WeekRules.Streak(plan, cardio, workouts, [], Mon.AddDays(4));
        Assert.Equal((1, 1, 2), (gymStreak.Current, gymStreak.BestRun, gymStreak.BestWeekCount));
        Assert.Equal((2, 2, 2), (cardioStreak.Current, cardioStreak.BestRun, cardioStreak.BestWeekCount));

        Assert.Equal(ConsistencyKind.OnTrack, WeekRules.Consistency(plan, gym, workouts, [], Mon.AddDays(4)).Kind);
        Assert.Equal(ConsistencyKind.Behind, WeekRules.Consistency(plan, gym, workouts, [], Mon.AddDays(5)).Kind);
        Assert.Equal(ConsistencyKind.TargetHit, WeekRules.Consistency(plan, cardio, workouts, [], Mon.AddDays(5)).Kind);
    }

    [Fact]
    public void Custom_workouts_count_for_the_main_Track_only_even_when_a_second_Track_allows_them()
    {
        var gym = new Track("gym", "Gym", new TrackRhythm.Weekly(2, 1, true), []);
        var cardio = new Track("cardio", "Cardio", new TrackRhythm.Weekly(2, 1, true), []);
        var plan = PlanFixture.Make(start: D(2026, 1, 5), tracks: [gym, cardio], sessions:
        [
            PlanFixture.Session("s-gym", DayOfWeek.Monday, trackId: "gym"),
            PlanFixture.Session("s-run", DayOfWeek.Tuesday, SessionType.Cardio, trackId: "cardio"),
        ]);
        var workouts = new[] { DoneSession(Mon, null) };

        Assert.Equal(1, WeekRules.CountedWorkouts(plan, gym, workouts, Mon));
        Assert.Equal(0, WeekRules.CountedWorkouts(plan, cardio, workouts, Mon));
    }

    [Fact]
    public void Breaks_excuse_every_weekly_Track()
    {
        var gym = new Track("gym", "Gym", new TrackRhythm.Weekly(2, 2, false), []);
        var cardio = new Track("cardio", "Cardio", new TrackRhythm.Weekly(2, 2, false), []);
        var plan = PlanFixture.Make(start: D(2026, 1, 5), tracks: [gym, cardio], sessions:
        [
            PlanFixture.Session("s-gym", DayOfWeek.Monday, trackId: "gym"),
            PlanFixture.Session("s-run", DayOfWeek.Tuesday, SessionType.Cardio, trackId: "cardio"),
        ]);
        var workouts = new[]
        {
            DoneSession(D(2026, 11, 2), "s-gym"), DoneSession(D(2026, 11, 3), "s-gym"),
            DoneSession(D(2026, 11, 2), "s-run"), DoneSession(D(2026, 11, 3), "s-run"),
            DoneSession(Mon, "s-gym"), DoneSession(Mon.AddDays(1), "s-gym"),
            DoneSession(Mon, "s-run"), DoneSession(Mon.AddDays(1), "s-run"),
        };
        var breaks = new[] { HistoryBuilder.BreakOf(D(2026, 11, 12), D(2026, 11, 12)) };

        Assert.Equal(2, WeekRules.Streak(plan, gym, workouts, breaks, Mon.AddDays(2)).Current);
        Assert.Equal(2, WeekRules.Streak(plan, cardio, workouts, breaks, Mon.AddDays(2)).Current);
    }

    [Fact]
    public void A_daily_Track_has_no_weekly_counted_Workouts_or_weekly_Streak()
    {
        var gym = new Track("gym", "Gym", new TrackRhythm.Weekly(2, 1, false), []);
        var mobility = new Track("mob", "Mobility", new TrackRhythm.Daily(75), []);
        var plan = PlanFixture.Make(start: D(2026, 1, 5), tracks: [gym, mobility], sessions:
        [
            PlanFixture.Session("s-gym", DayOfWeek.Monday, trackId: "gym"),
            PlanFixture.Session("s-mob", DefaultDay.Daily, SessionType.Mobility, trackId: "mob"),
        ]);
        var workouts = new[] { DoneSession(Mon, "s-mob") };

        Assert.Equal(0, WeekRules.CountedWorkouts(plan, mobility, workouts, Mon));
        Assert.Equal(0, WeekRules.CountedWorkouts(plan, gym, workouts, Mon));
        Assert.Equal(new StreakResult(0, 0, 0), WeekRules.Streak(plan, mobility, workouts, [], Mon.AddDays(7)));
    }
}
