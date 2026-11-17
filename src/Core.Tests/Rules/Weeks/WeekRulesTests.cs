using Core.Rules.Weeks;

namespace Core.Tests.Rules.Weeks;

public class WeekRulesTests
{
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    // Default Plan starts Mon 2026-01-05; weekly target 3, Streak minimum 2. 2026-11-16 is a Monday.
    private static readonly DateOnly Mon = D(2026, 11, 16);

    // ---- counted Workouts

    [Fact]
    public void Counted_workouts_include_partial_and_swapped_but_not_empty_or_all_skipped()
    {
        var h = new WeekHistory().Did(Mon).Swapped(Mon.AddDays(1)).AllSkipped(Mon.AddDays(2)).EmptyWorkout(Mon.AddDays(3));
        Assert.Equal(2, WeekRules.CountedWorkouts(h.Plan, h.Workouts, Mon));
    }

    [Fact]
    public void Benchmark_test_Sessions_do_not_count()
    {
        var h = new WeekHistory().Did(Mon).BenchmarkTest(Mon.AddDays(5));
        Assert.Equal(1, WeekRules.CountedWorkouts(h.Plan, h.Workouts, Mon));
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public void Custom_workouts_count_only_when_the_Plan_rule_says_so(bool customCounts, int expected)
    {
        var h = new WeekHistory().WithPlan(D(2026, 1, 5), customCounts: customCounts).Did(Mon).Custom(Mon.AddDays(1));
        Assert.Equal(expected, WeekRules.CountedWorkouts(h.Plan, h.Workouts, Mon));
    }

    [Fact]
    public void Only_Workouts_inside_the_Monday_to_Sunday_week_count()
    {
        var h = new WeekHistory().Did(Mon.AddDays(-1), Mon, Mon.AddDays(6), Mon.AddDays(7));
        Assert.Equal(2, WeekRules.CountedWorkouts(h.Plan, h.Workouts, Mon));
    }

    // ---- Streak

    [Fact]
    public void Streak_counts_consecutive_qualifying_completed_weeks()
    {
        var h = new WeekHistory().Did(D(2026, 11, 2), D(2026, 11, 3), D(2026, 11, 9), D(2026, 11, 10));
        Assert.Equal(2, WeekRules.Streak(h.Plan, h.Workouts, h.Breaks, D(2026, 11, 18)).Current);
    }

    [Fact]
    public void An_unqualifying_completed_week_breaks_the_Streak()
    {
        // Nov 2 qualifies, Nov 9 has one, Nov 16 (current) qualifies: the failed week resets the run.
        var h = new WeekHistory().Did(D(2026, 11, 2), D(2026, 11, 3), D(2026, 11, 9), Mon, D(2026, 11, 17));
        var s = WeekRules.Streak(h.Plan, h.Workouts, h.Breaks, D(2026, 11, 18));
        Assert.Equal(1, s.Current);
        Assert.Equal(1, s.BestRun);
    }

    [Fact]
    public void The_current_week_never_breaks_the_Streak_while_in_progress()
    {
        var h = new WeekHistory().Did(D(2026, 11, 2), D(2026, 11, 3), D(2026, 11, 9), D(2026, 11, 10));
        Assert.Equal(2, WeekRules.Streak(h.Plan, h.Workouts, h.Breaks, Mon).Current); // Monday, nothing logged yet
    }

    [Fact]
    public void The_current_week_joins_the_Streak_the_moment_it_qualifies()
    {
        var h = new WeekHistory().Did(D(2026, 11, 9), D(2026, 11, 10), Mon, Mon.AddDays(1));
        Assert.Equal(2, WeekRules.Streak(h.Plan, h.Workouts, h.Breaks, Mon.AddDays(1)).Current);
    }

    [Fact]
    public void A_week_touched_by_a_Break_neither_breaks_nor_adds()
    {
        var h = new WeekHistory()
            .Did(D(2026, 11, 2), D(2026, 11, 3))
            .Break(D(2026, 11, 12), D(2026, 11, 12)) // week of Nov 9 has no workouts but is excused
            .Did(Mon, Mon.AddDays(1));
        var s = WeekRules.Streak(h.Plan, h.Workouts, h.Breaks, Mon.AddDays(2));
        Assert.Equal(2, s.Current);
        Assert.Equal(2, s.BestRun);
    }

    [Fact]
    public void A_week_that_qualifies_despite_a_Break_does_not_add()
    {
        var h = new WeekHistory().Break(D(2026, 11, 12), D(2026, 11, 12)).Did(D(2026, 11, 9), D(2026, 11, 10));
        Assert.Equal(0, WeekRules.Streak(h.Plan, h.Workouts, h.Breaks, Mon).Current);
    }

    [Fact]
    public void Weeks_before_the_Plan_start_are_ignored()
    {
        var h = new WeekHistory().WithPlan(D(2026, 11, 9)).Did(D(2026, 11, 9), D(2026, 11, 10));
        Assert.Equal(1, WeekRules.Streak(h.Plan, h.Workouts, h.Breaks, Mon).Current);
    }

    [Fact]
    public void A_new_Plan_has_no_Streak_before_its_first_week_completes()
    {
        var h = new WeekHistory().WithPlan(Mon);
        Assert.Equal(0, WeekRules.Streak(h.Plan, h.Workouts, h.Breaks, Mon.AddDays(3)).Current);
    }

    [Fact]
    public void Best_run_and_best_week_count_are_exposed_for_Badges()
    {
        var h = new WeekHistory()
            .Did(D(2026, 10, 5), D(2026, 10, 6), D(2026, 10, 7), D(2026, 10, 8)) // 4 in a week
            .Did(D(2026, 10, 12), D(2026, 10, 13))
            .Did(D(2026, 10, 19)) // fails
            .Did(D(2026, 10, 26), D(2026, 10, 27));
        var s = WeekRules.Streak(h.Plan, h.Workouts, h.Breaks, D(2026, 11, 3));
        Assert.Equal(2, s.BestRun);
        Assert.Equal(4, s.BestWeekCount);
        Assert.Equal(1, s.Current);
    }

    [Fact]
    public void A_week_across_the_year_boundary_is_one_week()
    {
        var h = new WeekHistory()
            .Did(D(2026, 12, 29), D(2027, 1, 1)) // Tue and Fri of the same ISO week
            .Did(D(2027, 1, 5), D(2027, 1, 6));
        Assert.Equal(2, WeekRules.Streak(h.Plan, h.Workouts, h.Breaks, D(2027, 1, 11)).Current);
    }

    // ---- Consistency

    [Fact]
    public void Target_hit_when_the_target_is_reached()
    {
        var h = new WeekHistory().Did(Mon, Mon.AddDays(1), Mon.AddDays(2));
        Assert.Equal(ConsistencyKind.TargetHit, WeekRules.Consistency(h.Plan, h.Workouts, h.Breaks, Mon.AddDays(3)).Kind);
    }

    [Fact]
    public void On_track_with_a_spare_day_and_a_reason()
    {
        var h = new WeekHistory();
        var c = WeekRules.Consistency(h.Plan, h.Workouts, h.Breaks, Mon);
        Assert.Equal(ConsistencyKind.OnTrack, c.Kind);
        Assert.False(string.IsNullOrWhiteSpace(c.Message));
    }

    [Fact]
    public void Behind_when_every_remaining_day_is_needed()
    {
        var h = new WeekHistory(); // 3 needed, Fri/Sat/Sun left
        Assert.Equal(ConsistencyKind.Behind, WeekRules.Consistency(h.Plan, h.Workouts, h.Breaks, Mon.AddDays(4)).Kind);
    }

    [Fact]
    public void Behind_on_Saturday_and_on_track_on_Friday_with_one_done()
    {
        var h = new WeekHistory().Did(Mon);
        Assert.Equal(ConsistencyKind.Behind, WeekRules.Consistency(h.Plan, h.Workouts, h.Breaks, Mon.AddDays(5)).Kind); // Sat, Sun for 2
        Assert.Equal(ConsistencyKind.OnTrack, WeekRules.Consistency(h.Plan, h.Workouts, h.Breaks, Mon.AddDays(4)).Kind); // Fri-Sun for 2
    }

    [Fact]
    public void Out_of_reach_when_more_remain_than_days_left()
    {
        var h = new WeekHistory().Did(Mon);
        Assert.Equal(ConsistencyKind.TargetOutOfReach, WeekRules.Consistency(h.Plan, h.Workouts, h.Breaks, Mon.AddDays(6)).Kind);
    }

    [Fact]
    public void Today_counts_as_an_available_day()
    {
        var h = new WeekHistory().Did(Mon, Mon.AddDays(1)); // one to go, only Sunday left
        Assert.Equal(ConsistencyKind.Behind, WeekRules.Consistency(h.Plan, h.Workouts, h.Breaks, Mon.AddDays(6)).Kind);
    }

    [Fact]
    public void Break_days_are_not_available_days()
    {
        var h = new WeekHistory().Break(Mon.AddDays(2), Mon.AddDays(6)); // Wed to Sun excused
        // Tuesday: one available day left, 3 needed.
        Assert.Equal(ConsistencyKind.TargetOutOfReach, WeekRules.Consistency(h.Plan, h.Workouts, h.Breaks, Mon.AddDays(1)).Kind);
    }

    // ---- Plan week number

    [Theory]
    [InlineData(2026, 1, 5, 1)]
    [InlineData(2026, 1, 11, 1)]
    [InlineData(2026, 1, 12, 2)]
    [InlineData(2027, 1, 4, 53)]
    public void Plan_week_number_counts_from_one_at_the_start_week(int y, int m, int d, int expected)
    {
        Assert.Equal(expected, WeekRules.PlanWeekNumber(new WeekHistory().Plan, new DateOnly(y, m, d)));
    }

    [Fact]
    public void Plan_week_number_is_null_before_the_Plan_starts()
    {
        Assert.Null(WeekRules.PlanWeekNumber(new WeekHistory().Plan, new DateOnly(2026, 1, 4)));
    }

    [Fact]
    public void A_mid_week_start_date_makes_that_whole_week_week_one()
    {
        var plan = new WeekHistory().WithPlan(new DateOnly(2026, 1, 8)).Plan; // Thursday
        Assert.Equal(1, WeekRules.PlanWeekNumber(plan, new DateOnly(2026, 1, 8)));
        Assert.Equal(2, WeekRules.PlanWeekNumber(plan, new DateOnly(2026, 1, 12)));
    }
}
