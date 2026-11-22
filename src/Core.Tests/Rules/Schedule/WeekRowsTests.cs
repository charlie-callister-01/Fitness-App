using Core.Model;
using Core.Rules.Schedule;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.Schedule;

public class WeekRowsTests
{
    // Mon is 2026-11-16. Plan: Lower Mon, Upper Wed, Run Fri.
    private static readonly Plan Plan = PlanFixture.Make(start: D(2026, 1, 5), sessions:
    [
        PlanFixture.Session("lower", DayOfWeek.Monday, name: "Lower"),
        PlanFixture.Session("upper", DayOfWeek.Wednesday, name: "Upper"),
        PlanFixture.Session("run", DayOfWeek.Friday, SessionType.Cardio, name: "Run"),
    ]);

    private static Workout Finished(DateOnly date, string sessionId) =>
        Logged(date, sessionId) with { EndedAt = new DateTimeOffset(date.Year, date.Month, date.Day, 10, 0, 0, TimeSpan.Zero) };

    private static IReadOnlyList<WeekRowChip> Chips(IReadOnlyList<WeekRow> rows) => [.. rows.Select(r => r.Chip)];

    [Fact]
    public void Rows_are_in_day_order_with_Today_on_todays_row_and_no_chip_on_later_days()
    {
        var rows = WeekRows.Build(Plan, [], [], [], Mon);

        Assert.Equal(["Lower", "Upper", "Run"], rows.Select(r => r.Name));
        Assert.Equal([WeekRowChip.Today, WeekRowChip.None, WeekRowChip.None], Chips(rows));
    }

    [Fact]
    public void A_finished_Workout_makes_the_row_Done_even_on_today()
    {
        var rows = WeekRows.Build(Plan, [], [], [Finished(Mon, "lower")], Mon);

        Assert.Equal(WeekRowChip.Done, rows[0].Chip);
    }

    [Fact]
    public void A_Session_whose_day_passed_with_no_Workout_is_Missed_and_a_done_one_is_not()
    {
        var wed = Mon.AddDays(2);

        var rows = WeekRows.Build(Plan, [], [], [Finished(Mon.AddDays(1), "upper")], wed);

        Assert.Equal([WeekRowChip.Missed, WeekRowChip.Done, WeekRowChip.None], Chips(rows));
    }

    [Fact]
    public void A_moved_row_says_so_and_a_Break_day_is_marked_and_never_Missed()
    {
        var moved = new WeekSchedule(Mon, [new SessionMove("lower", Mon.AddDays(1))]);
        var brk = new Break("b", Mon.AddDays(1), Mon.AddDays(1), "");

        var rows = WeekRows.Build(Plan, [moved], [brk], [], Mon.AddDays(2));

        var lower = rows.Single(r => r.SessionId == "lower");
        Assert.True(lower.IsMoved);
        Assert.True(lower.IsBreakDay);
        Assert.Equal(WeekRowChip.None, lower.Chip);
    }

    [Fact]
    public void A_Break_touches_the_week_only_when_its_dates_reach_into_it()
    {
        Assert.True(WeekRows.BreakTouchesWeek([new Break("b", Mon.AddDays(-3), Mon.AddDays(1), "")], Mon.AddDays(3)));
        Assert.True(WeekRows.BreakTouchesWeek([new Break("b", Mon.AddDays(6), Mon.AddDays(9), "")], Mon));
        Assert.False(WeekRows.BreakTouchesWeek([new Break("b", Mon.AddDays(-9), Mon.AddDays(-1), "")], Mon));
        Assert.False(WeekRows.BreakTouchesWeek([], Mon));
    }
}
