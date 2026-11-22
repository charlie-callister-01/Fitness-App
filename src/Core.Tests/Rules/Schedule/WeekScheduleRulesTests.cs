using Core.Model;
using Core.Rules.Schedule;
using Core.Rules.Weeks;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.Schedule;

public class WeekScheduleRulesTests
{
    // Plan starts Mon 2026-01-05. Sessions: Lower Mon, Upper Wed, Run Fri, benchmark Test Sat every 4 weeks.
    // Benchmark weeks are Plan weeks 1, 5, 9 ... so BenchMon (week 45) is one and Mon (week 46) is not.
    private static readonly DateOnly Start = D(2026, 1, 5);
    private static readonly DateOnly BenchMon = D(2026, 11, 9);

    private static Plan Plan(DateOnly? start = null) => PlanFixture.Make(start: start ?? Start, sessions:
    [
        PlanFixture.Session("lower", DayOfWeek.Monday, name: "Lower"),
        PlanFixture.Session("upper", DayOfWeek.Wednesday, name: "Upper"),
        PlanFixture.Session("run", DayOfWeek.Friday, SessionType.Cardio, name: "Run"),
        PlanFixture.Session("test", DayOfWeek.Saturday, SessionType.Test, name: "Test", benchmarkEveryWeeks: 4),
    ]);

    private static WeekSchedule Move(DateOnly monday, string id, DateOnly to) => new(monday, [new SessionMove(id, to)]);

    private static List<string> Ids(WeekLayout l) => l.Sessions.Select(s => s.SessionId).ToList();

    // ---- layout

    [Fact]
    public void Sessions_sit_on_their_default_days_in_date_order()
    {
        var l = WeekScheduleRules.Layout(Plan(), [], [], Mon.AddDays(2));
        Assert.Equal(Mon, l.Monday);
        Assert.Equal(["lower", "upper", "run"], Ids(l));
        Assert.Equal([Mon, Mon.AddDays(2), Mon.AddDays(4)], l.Sessions.Select(s => s.Date));
        Assert.All(l.Sessions, s => Assert.False(s.IsMoved));
    }

    [Fact]
    public void A_move_for_that_week_overrides_the_default_day_and_is_flagged()
    {
        var l = WeekScheduleRules.Layout(Plan(), [Move(Mon, "lower", Mon.AddDays(5))], [], Mon);
        var lower = l.Sessions.Single(s => s.SessionId == "lower");
        Assert.Equal(Mon.AddDays(5), lower.Date);
        Assert.True(lower.IsMoved);
        Assert.Equal(["upper", "run", "lower"], Ids(l));
    }

    [Fact]
    public void A_move_in_another_week_or_to_a_date_outside_the_week_is_ignored()
    {
        var other = Move(Mon.AddDays(7), "lower", Mon.AddDays(8));
        var outside = Move(Mon, "upper", Mon.AddDays(8));
        var l = WeekScheduleRules.Layout(Plan(), [other, outside], [], Mon);
        Assert.Equal([Mon, Mon.AddDays(2), Mon.AddDays(4)], l.Sessions.Select(s => s.Date));
    }

    [Fact]
    public void Break_days_are_marked_unavailable_but_Sessions_stay_listed()
    {
        var l = WeekScheduleRules.Layout(Plan(), [], [BreakOf(Mon.AddDays(1), Mon.AddDays(2))], Mon);
        Assert.Equal([Mon.AddDays(1), Mon.AddDays(2)], l.BreakDays);
        Assert.Equal([false, true, false], l.Sessions.Select(s => s.IsBreakDay));
    }

    [Fact]
    public void A_Session_moved_onto_a_Break_day_is_marked()
    {
        var l = WeekScheduleRules.Layout(Plan(), [Move(Mon, "lower", Mon.AddDays(3))], [BreakOf(Mon.AddDays(3), Mon.AddDays(3))], Mon);
        Assert.True(l.Sessions.Single(s => s.SessionId == "lower").IsBreakDay);
    }

    [Fact]
    public void Benchmark_Session_appears_only_in_its_interval_weeks()
    {
        Assert.DoesNotContain("test", Ids(WeekScheduleRules.Layout(Plan(), [], [], Mon)));
        var bench = WeekScheduleRules.Layout(Plan(), [], [], BenchMon);
        Assert.Contains(bench.Sessions, s => s is { SessionId: "test" } && BenchmarkWeeks.IsBenchmark(s.Session));
        Assert.Contains("test", Ids(WeekScheduleRules.Layout(Plan(), [], [], WeekCalendar.AddWeeks(BenchMon, 4))));
        Assert.DoesNotContain("test", Ids(WeekScheduleRules.Layout(Plan(), [], [], WeekCalendar.AddWeeks(BenchMon, 2))));
    }

    [Fact]
    public void Benchmark_scheduling_rule_is_reusable_on_its_own()
    {
        var plan = Plan();
        var test = plan.Sessions.Single(s => s.Id == "test");
        Assert.True(BenchmarkWeeks.IsBenchmark(test));
        Assert.True(BenchmarkWeeks.IsScheduledInWeek(plan, test, Start));
        Assert.False(BenchmarkWeeks.IsScheduledInWeek(plan, test, Start.AddDays(7)));
        Assert.True(BenchmarkWeeks.IsScheduledInWeek(plan, test, Start.AddDays(28)));
        Assert.False(BenchmarkWeeks.IsScheduledInWeek(plan, test, Start.AddDays(-28)));
        Assert.True(BenchmarkWeeks.IsScheduledInWeek(plan, plan.Sessions[0], Start.AddDays(7)));
    }

    [Fact]
    public void A_test_session_with_a_zero_interval_is_not_a_benchmark_and_is_always_scheduled()
    {
        var plan = Plan();
        var odd = PlanFixture.Session("odd", DefaultDay.Saturday, SessionType.Test, name: "Odd", benchmarkEveryWeeks: 0);
        Assert.False(BenchmarkWeeks.IsBenchmark(odd));
        Assert.True(BenchmarkWeeks.IsScheduledInWeek(plan, odd, Start.AddDays(7)));
    }

    [Fact]
    public void Missed_sessions_carry_the_session_name()
    {
        var missed = WeekScheduleRules.MissedInWeek(Plan(), [], [], [], Mon, Mon.AddDays(1));
        Assert.Equal("Lower", Assert.Single(missed).SessionName);
    }

    [Fact]
    public void Weeks_before_the_Plan_start_are_empty()
    {
        var l = WeekScheduleRules.Layout(Plan(Mon), [], [], Mon.AddDays(-7));
        Assert.Empty(l.Sessions);
    }

    // ---- missed sessions

    private static List<string> MissedIds(Plan plan, IReadOnlyList<Workout> w, DateOnly today,
        IReadOnlyList<WeekSchedule>? schedules = null, IReadOnlyList<Break>? breaks = null) =>
        WeekScheduleRules.MissedInWeek(plan, schedules ?? [], breaks ?? [], w, Mon, today).Select(m => m.SessionId).ToList();

    [Fact]
    public void A_Session_is_missed_once_its_day_has_passed_with_no_Workout()
    {
        Assert.Equal(["lower"], MissedIds(Plan(), [], Mon.AddDays(1)));
        Assert.Equal(["lower", "upper"], MissedIds(Plan(), [], Mon.AddDays(3)));
    }

    [Fact]
    public void Todays_Session_is_not_missed()
    {
        Assert.Empty(MissedIds(Plan(), [], Mon));
        Assert.Equal(["lower"], MissedIds(Plan(), [], Mon.AddDays(2)));
    }

    [Fact]
    public void A_Session_made_up_on_a_different_day_that_week_is_not_missed()
    {
        Assert.Empty(MissedIds(Plan(), [Logged(Mon.AddDays(1), "lower")], Mon.AddDays(2)));
    }

    [Fact]
    public void A_Workout_from_a_different_week_does_not_clear_a_Session()
    {
        var w = new[] { Logged(Mon.AddDays(-2), "lower"), Logged(Mon.AddDays(7), "lower") };
        Assert.Equal(["lower"], MissedIds(Plan(), w, Mon.AddDays(1)));
    }

    [Fact]
    public void A_Session_stays_missed_when_its_Exercises_were_done_in_a_Custom_workout()
    {
        Assert.Equal(["lower"], MissedIds(Plan(), [Logged(Mon.AddDays(1), null)], Mon.AddDays(2)));
    }

    [Fact]
    public void Sessions_on_Break_days_are_not_missed()
    {
        Assert.Equal(["lower"], MissedIds(Plan(), [], Mon.AddDays(5), breaks: [BreakOf(Mon.AddDays(2), Mon.AddDays(4))]));
    }

    [Fact]
    public void A_moved_Session_is_missed_by_its_moved_day_not_its_default()
    {
        var s = new[] { Move(Mon, "lower", Mon.AddDays(3)) };
        Assert.Empty(MissedIds(Plan(), [], Mon.AddDays(2), s));
        Assert.Equal(["upper"], MissedIds(Plan(), [], Mon.AddDays(3), s));
        Assert.Equal(["upper", "lower"], MissedIds(Plan(), [], Mon.AddDays(4), s));
    }

    [Fact]
    public void Benchmark_Session_is_missed_only_in_a_benchmark_week()
    {
        var plan = Plan();
        var inBenchWeek = WeekScheduleRules.MissedInWeek(plan, [], [], [], BenchMon, BenchMon.AddDays(7));
        Assert.Contains(inBenchWeek, m => m.SessionId == "test");
        Assert.DoesNotContain("test", MissedIds(plan, [], Mon.AddDays(7)));
    }

    [Fact]
    public void Sessions_before_the_Plan_start_date_are_not_missed()
    {
        // Plan starts Wednesday of the week: Lower (Mon) predates it.
        var plan = Plan(Mon.AddDays(2));
        Assert.Equal(["upper"], MissedIds(plan, [], Mon.AddDays(3)));
        Assert.Empty(WeekScheduleRules.MissedInWeek(plan, [], [], [], Mon.AddDays(-7), Mon.AddDays(3)));
    }

    [Fact]
    public void Missed_lists_every_past_week_from_the_Plan_start_oldest_first()
    {
        var plan = Plan(Mon);
        var w = new[] { Logged(Mon, "lower"), Logged(Mon.AddDays(7), "upper") };
        var missed = WeekScheduleRules.Missed(plan, [], [], w, Mon.AddDays(8));
        Assert.Equal(
            [("upper", Mon.AddDays(2)), ("run", Mon.AddDays(4)), ("test", Mon.AddDays(5)), ("lower", Mon.AddDays(7))],
            missed.Select(m => (m.SessionId, m.Date)));
    }
}
