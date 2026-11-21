using Core.Model;
using Core.Rules.Home;
using Core.Storage;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;

namespace Core.Tests.Rules.Home;

public class HomeWeekRulesTests
{
    // Mon is 2026-11-16 (Monday).

    [Fact]
    public void Strip_returns_empty_when_no_active_plan()
    {
        var plan = PlanFixture.Make(Mon) with { IsActive = false };
        var snapshot = new DataSnapshot { Plans = [plan] };

        var tiles = HomeWeekRules.Strip(snapshot, Mon);

        Assert.Empty(tiles);
    }

    [Fact]
    public void Strip_returns_seven_tiles_Monday_to_Sunday_with_correct_weekday_labels_and_IsToday()
    {
        var plan = PlanFixture.Make(Mon);
        var snapshot = new DataSnapshot { Plans = [plan] };
        var wednesday = Mon.AddDays(2);

        var tiles = HomeWeekRules.Strip(snapshot, wednesday);

        Assert.Equal(7, tiles.Count);
        string[] expectedDays = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
        for (var i = 0; i < 7; i++)
        {
            var date = Mon.AddDays(i);
            Assert.Equal(date, tiles[i].Date);
            Assert.Equal(expectedDays[i], tiles[i].Weekday);
            Assert.Equal(expectedDays[i], tiles[i].WeekdayLabel);
            Assert.Equal(date == wednesday, tiles[i].IsToday);
        }
    }

    [Fact]
    public void Strip_covers_all_TileStates_Done_Partial_Missed_BreakDay_Upcoming_Rest()
    {
        // Monday (Done), Tuesday (Partial), Wednesday (Missed), Thursday (BreakDay), Friday (Upcoming), Saturday (Rest), Sunday (Rest)
        var row = PlanFixture.Row("ex1");
        var row2 = PlanFixture.Row("ex2");

        var sMon = PlanFixture.Session("s-mon", DayOfWeek.Monday, name: "A: Mon", exercises: [row]);
        var sTue = PlanFixture.Session("s-tue", DayOfWeek.Tuesday, name: "B: Tue", exercises: [row, row2]);
        var sWed = PlanFixture.Session("s-wed", DayOfWeek.Wednesday, name: "C: Wed", exercises: [row]);
        var sThu = PlanFixture.Session("s-thu", DayOfWeek.Thursday, name: "D: Thu", exercises: [row]);
        var sFri = PlanFixture.Session("s-fri", DayOfWeek.Friday, name: "E: Fri", exercises: [row]);

        var plan = PlanFixture.Make(Mon, sessions: [sMon, sTue, sWed, sThu, sFri]);

        // Mon done: full workout
        var wMon = new Workout("w-mon", Mon, sMon.Id, false, null, DateTimeOffset.UnixEpoch, [row],
            [new WorkoutEntry("ex1", EntryStatus.Done, null, null, null, [HistoryBuilder.Set(10, 10)])], null);

        // Tue partial: 2 planned rows, 1 skipped
        var wTue = new Workout("w-tue", Mon.AddDays(1), sTue.Id, false, null, DateTimeOffset.UnixEpoch, [row, row2],
            [
                new WorkoutEntry("ex1", EntryStatus.Done, null, null, null, [HistoryBuilder.Set(10, 10)]),
                new WorkoutEntry("ex2", EntryStatus.Skipped, "tired", null, null, []),
            ], null);

        // Thu is a break day
        var breaks = new List<Break> { HistoryBuilder.BreakOf(Mon.AddDays(3), Mon.AddDays(3)) };

        var snapshot = new DataSnapshot
        {
            Plans = [plan],
            Workouts = [wMon, wTue],
            Breaks = breaks,
        };

        // Today is Friday (Mon.AddDays(4))
        var today = Mon.AddDays(4);
        var tiles = HomeWeekRules.Strip(snapshot, today);

        Assert.Equal(TileState.Done, tiles[0].State);
        Assert.Equal("A", tiles[0].Code);
        Assert.Equal(sMon.Id, tiles[0].Session?.Id);

        Assert.Equal(TileState.Partial, tiles[1].State);
        Assert.Equal("B", tiles[1].Code);

        Assert.Equal(TileState.Missed, tiles[2].State);
        Assert.Equal("C", tiles[2].Code);

        Assert.Equal(TileState.BreakDay, tiles[3].State);
        Assert.Equal("D", tiles[3].Code);

        Assert.Equal(TileState.Upcoming, tiles[4].State);
        Assert.Equal("E", tiles[4].Code);

        Assert.Equal(TileState.Rest, tiles[5].State);
        Assert.Null(tiles[5].Session);
        Assert.Null(tiles[5].Code);

        Assert.Equal(TileState.Rest, tiles[6].State);
        Assert.Null(tiles[6].Session);
    }

    [Fact]
    public void Strip_break_day_is_never_missed_and_marked_as_break_day()
    {
        // Session on Monday, Monday is a Break day, no workout logged, today is Wednesday
        var sMon = PlanFixture.Session("s-mon", DayOfWeek.Monday, name: "A: Mon");
        var plan = PlanFixture.Make(Mon, sessions: [sMon]);
        var breaks = new List<Break> { HistoryBuilder.BreakOf(Mon, Mon) };

        var snapshot = new DataSnapshot
        {
            Plans = [plan],
            Breaks = breaks,
        };

        var tiles = HomeWeekRules.Strip(snapshot, Mon.AddDays(2));

        Assert.Equal(TileState.BreakDay, tiles[0].State);
        Assert.NotEqual(TileState.Missed, tiles[0].State);
    }

    [Fact]
    public void Strip_moved_session_shows_on_its_moved_day()
    {
        // Session originally on Monday, moved to Wednesday
        var sMon = PlanFixture.Session("s-mon", DayOfWeek.Monday, name: "A: Mon");
        var plan = PlanFixture.Make(Mon, sessions: [sMon]);

        var schedules = new List<WeekSchedule>
        {
            new(Mon, [new SessionMove(sMon.Id, Mon.AddDays(2))]),
        };

        var snapshot = new DataSnapshot
        {
            Plans = [plan],
            WeekSchedules = schedules,
        };

        var tiles = HomeWeekRules.Strip(snapshot, Mon);

        // Monday is now a Rest day
        Assert.Equal(TileState.Rest, tiles[0].State);
        Assert.Null(tiles[0].Session);

        // Wednesday has the moved session
        Assert.Equal(TileState.Upcoming, tiles[2].State);
        Assert.Equal(sMon.Id, tiles[2].Session?.Id);
        Assert.Equal("A", tiles[2].Code);
    }

    [Fact]
    public void Strip_when_two_sessions_on_same_day_takes_first_in_layout_order()
    {
        var s1 = PlanFixture.Session("s1", DayOfWeek.Monday, name: "First");
        var s2 = PlanFixture.Session("s2", DayOfWeek.Monday, name: "Second");
        var plan = PlanFixture.Make(Mon, sessions: [s1, s2]);

        var snapshot = new DataSnapshot { Plans = [plan] };
        var tiles = HomeWeekRules.Strip(snapshot, Mon);

        Assert.Equal(s1.Id, tiles[0].Session?.Id);
        Assert.Equal("F", tiles[0].Code);
    }

    [Theory]
    [InlineData("A: Lower + core", "A")]
    [InlineData("Full body", "F")]
    [InlineData("AB: x", "AB")]
    [InlineData("Long name: x", "L")]
    [InlineData("ABC: test", "ABC")]
    [InlineData("ABCD: test", "A")]
    [InlineData("lower", "L")]
    public void SessionCode_computes_expected_codes(string name, string expected)
    {
        Assert.Equal(expected, HomeWeekRules.SessionCode(name));
    }

    [Fact]
    public void Line_returns_null_when_no_active_plan()
    {
        var plan = PlanFixture.Make(Mon) with { IsActive = false };
        var snapshot = new DataSnapshot { Plans = [plan] };

        var line = HomeWeekRules.Line(snapshot, Mon);

        Assert.Null(line);
    }

    [Fact]
    public void Line_computes_done_scheduled_target_and_missed_codes_in_date_order()
    {
        var row = PlanFixture.Row("ex1");
        var sMon = PlanFixture.Session("s-mon", DayOfWeek.Monday, name: "A: Mon", exercises: [row]);
        var sTue = PlanFixture.Session("s-tue", DayOfWeek.Tuesday, name: "B: Tue", exercises: [row]);
        var sWed = PlanFixture.Session("s-wed", DayOfWeek.Wednesday, name: "C: Wed", exercises: [row]);
        var sThu = PlanFixture.Session("s-thu", DayOfWeek.Thursday, name: "D: Thu", exercises: [row]);
        var sFri = PlanFixture.Session("s-fri", DayOfWeek.Friday, name: "E: Fri", exercises: [row]);

        var plan = PlanFixture.Make(Mon, target: 5, sessions: [sMon, sTue, sWed, sThu, sFri]);

        // Wednesday was done; Monday and Tuesday were missed
        var wWed = new Workout("w-wed", Mon.AddDays(2), sWed.Id, false, null, null, [row],
            [new WorkoutEntry("ex1", EntryStatus.Done, null, null, null, [HistoryBuilder.Set(10, 10)])], null);

        var snapshot = new DataSnapshot
        {
            Plans = [plan],
            Workouts = [wWed],
        };

        // Today is Thursday (Mon.AddDays(3))
        var today = Mon.AddDays(3);
        var line = HomeWeekRules.Line(snapshot, today);

        Assert.NotNull(line);
        Assert.Equal(1, line.Done);
        Assert.Equal(5, line.Scheduled);
        Assert.Equal(5, line.Target);
        Assert.Equal(["A", "B"], line.MissedCodes);
    }

    [Fact]
    public void Line_zero_missed_gives_empty_list()
    {
        var row = PlanFixture.Row("ex1");
        var sMon = PlanFixture.Session("s-mon", DayOfWeek.Monday, name: "A: Mon", exercises: [row]);
        var plan = PlanFixture.Make(Mon, target: 3, sessions: [sMon]);

        // Logged on Monday
        var wMon = new Workout("w-mon", Mon, sMon.Id, false, null, null, [row],
            [new WorkoutEntry("ex1", EntryStatus.Done, null, null, null, [HistoryBuilder.Set(10, 10)])], null);

        var snapshot = new DataSnapshot
        {
            Plans = [plan],
            Workouts = [wMon],
        };

        var line = HomeWeekRules.Line(snapshot, Mon.AddDays(2));

        Assert.NotNull(line);
        Assert.Equal(1, line.Done);
        Assert.Empty(line.MissedCodes);
    }
}
