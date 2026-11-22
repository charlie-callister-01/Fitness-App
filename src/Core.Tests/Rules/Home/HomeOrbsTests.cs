using Core.Model;
using Core.Rules.Home;
using Core.Storage;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;

namespace Core.Tests.Rules.Home;

public class HomeOrbsTests
{
    // Mon is 2026-11-16 (Monday).

    [Fact]
    public void Compute_returns_null_when_no_active_plan()
    {
        var plan = PlanFixture.Make(Mon) with { IsActive = false };
        var snapshot = new DataSnapshot { Plans = [plan] };

        var orbs = HomeOrbs.Compute(snapshot, Mon);

        Assert.Null(orbs);
    }

    [Fact]
    public void StreakOrb_computes_current_this_week_count_minimum_and_fraction()
    {
        // Target 3, StreakMin 2. 1 workout logged this week.
        var plan = PlanFixture.Make(Mon, target: 3, minPerWeek: 2);
        var row = PlanFixture.Row("ex1");
        var w = new Workout("w1", Mon, PlanFixture.Strength, false, null, null, [row],
            [new WorkoutEntry("ex1", EntryStatus.Done, null, null, null, [HistoryBuilder.Set(10, 10)])], null);

        var snapshot = new DataSnapshot
        {
            Plans = [plan],
            Workouts = [w],
        };

        var orbs = HomeOrbs.Compute(snapshot, Mon);

        Assert.NotNull(orbs);
        Assert.Equal(0, orbs.Streak.Current); // week still in progress with 1 < 2 workouts
        Assert.Equal(1, orbs.Streak.ThisWeekCount);
        Assert.Equal(2, orbs.Streak.Minimum);
        Assert.False(orbs.Streak.OnBreak);
        Assert.Equal(0.5, orbs.Streak.Fraction);
    }

    [Fact]
    public void StreakOrb_fraction_is_zero_when_minimum_is_zero()
    {
        var plan = PlanFixture.Make(Mon, minPerWeek: 0);
        var snapshot = new DataSnapshot { Plans = [plan] };

        var orbs = HomeOrbs.Compute(snapshot, Mon);

        Assert.NotNull(orbs);
        Assert.Equal(0, orbs.Streak.Minimum);
        Assert.Equal(0.0, orbs.Streak.Fraction);
    }

    [Fact]
    public void StreakOrb_on_break_is_true_on_break_day_and_current_number_kept()
    {
        // 2 past qualifying weeks -> streak is 2. Today is in a Break.
        var plan = PlanFixture.Make(D(2026, 11, 2), minPerWeek: 2);
        var row = PlanFixture.Row("ex1");
        Workout W(string id, DateOnly d) => new(id, d, PlanFixture.Strength, false, null, null, [row],
            [new WorkoutEntry("ex1", EntryStatus.Done, null, null, null, [HistoryBuilder.Set(10, 10)])], null);

        var workouts = new List<Workout>
        {
            W("w1", D(2026, 11, 2)),
            W("w2", D(2026, 11, 3)),
            W("w3", D(2026, 11, 9)),
            W("w4", D(2026, 11, 10)),
        };

        var breaks = new List<Break> { HistoryBuilder.BreakOf(Mon, Mon.AddDays(1)) };

        var snapshot = new DataSnapshot
        {
            Plans = [plan],
            Workouts = workouts,
            Breaks = breaks,
        };

        var orbs = HomeOrbs.Compute(snapshot, Mon);

        Assert.NotNull(orbs);
        Assert.Equal(2, orbs.Streak.Current);
        Assert.True(orbs.Streak.OnBreak);
    }

    [Fact]
    public void PhaseOrb_returns_week_in_phase_and_phase_length_when_in_phase()
    {
        // Plan starts Mon. Phase 1 is weeks 1-4. Today is in week 2.
        var phase = new Phase("p1", "Focus", 1, 4, [], []);
        var plan = PlanFixture.Make(Mon, phases: [phase]);
        var snapshot = new DataSnapshot { Plans = [plan] };

        var today = Mon.AddDays(7); // Week 2
        var orbs = HomeOrbs.Compute(snapshot, today);

        Assert.NotNull(orbs);
        Assert.NotNull(orbs.Phase);
        Assert.Equal(2, orbs.Phase.WeekInPhase);
        Assert.Equal(4, orbs.Phase.PhaseLengthWeeks);
    }

    [Fact]
    public void PhaseOrb_returns_null_before_start_after_end_and_in_gap()
    {
        // Plan starts Mon. Phase 1 is weeks 2-3.
        var phase = new Phase("p1", "Focus", 2, 3, [], []);
        var plan = PlanFixture.Make(Mon, phases: [phase]);
        var snapshot = new DataSnapshot { Plans = [plan] };

        // Before start date
        var orbsBefore = HomeOrbs.Compute(snapshot, Mon.AddDays(-1));
        Assert.NotNull(orbsBefore);
        Assert.Null(orbsBefore.Phase);

        // In week 1 (gap before phase 1 starts in week 2)
        var orbsGap = HomeOrbs.Compute(snapshot, Mon);
        Assert.NotNull(orbsGap);
        Assert.Null(orbsGap.Phase);

        // After week 3 (week 4)
        var orbsAfter = HomeOrbs.Compute(snapshot, Mon.AddDays(21));
        Assert.NotNull(orbsAfter);
        Assert.Null(orbsAfter.Phase);
    }

    [Fact]
    public void RaceOrb_returns_null_when_no_goal_date()
    {
        var plan = PlanFixture.Make(Mon) with { GoalDate = null };
        var snapshot = new DataSnapshot { Plans = [plan] };

        var orbs = HomeOrbs.Compute(snapshot, Mon);

        Assert.NotNull(orbs);
        Assert.Null(orbs.Race);
    }

    [Fact]
    public void RaceOrb_computes_days_to_go_and_clamped_elapsed_share()
    {
        // Start: Mon (2026-11-16), Goal: Mon + 10 (2026-11-26). Today: Mon + 5 (2026-11-21).
        var plan = PlanFixture.Make(Mon) with { GoalDate = Mon.AddDays(10) };
        var snapshot = new DataSnapshot { Plans = [plan] };

        var orbs = HomeOrbs.Compute(snapshot, Mon.AddDays(5));

        Assert.NotNull(orbs);
        Assert.NotNull(orbs.Race);
        Assert.Equal(5, orbs.Race.DaysToGo);
        Assert.Equal(0.5, orbs.Race.ElapsedShare);
        Assert.Equal(0.5, orbs.Race.Fraction);
    }

    [Fact]
    public void RaceOrb_days_to_go_is_never_negative_and_elapsed_share_is_clamped_to_one_when_past_goal()
    {
        var plan = PlanFixture.Make(Mon) with { GoalDate = Mon.AddDays(10) };
        var snapshot = new DataSnapshot { Plans = [plan] };

        var orbs = HomeOrbs.Compute(snapshot, Mon.AddDays(15));

        Assert.NotNull(orbs);
        Assert.NotNull(orbs.Race);
        Assert.Equal(0, orbs.Race.DaysToGo);
        Assert.Equal(1.0, orbs.Race.ElapsedShare);
    }

    [Fact]
    public void RaceOrb_elapsed_share_is_zero_for_zero_or_negative_span()
    {
        // GoalDate <= StartDate
        var plan = PlanFixture.Make(Mon) with { GoalDate = Mon.AddDays(-2) };
        var snapshot = new DataSnapshot { Plans = [plan] };

        var orbs = HomeOrbs.Compute(snapshot, Mon);

        Assert.NotNull(orbs);
        Assert.NotNull(orbs.Race);
        Assert.Equal(0, orbs.Race.DaysToGo);
        Assert.Equal(0.0, orbs.Race.ElapsedShare);
    }

    [Fact]
    public void RaceOrb_elapsed_share_is_zero_when_today_is_before_start_date()
    {
        var plan = PlanFixture.Make(Mon) with { GoalDate = Mon.AddDays(10) };
        var snapshot = new DataSnapshot { Plans = [plan] };

        var orbs = HomeOrbs.Compute(snapshot, Mon.AddDays(-5));

        Assert.NotNull(orbs);
        Assert.NotNull(orbs.Race);
        Assert.Equal(15, orbs.Race.DaysToGo);
        Assert.Equal(0.0, orbs.Race.ElapsedShare);
    }
}
