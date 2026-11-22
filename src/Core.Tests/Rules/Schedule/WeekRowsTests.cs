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

    [Fact]
    public void A_one_weekly_Track_Plan_hides_the_Track_name_on_day_rows()
    {
        var rows = WeekRows.Build(Plan, [], [], [], Mon);

        Assert.All(rows, r =>
        {
            Assert.Equal("Main", r.TrackName);
            Assert.False(r.ShowTrackName);
        });
    }

    [Fact]
    public void Extra_weekly_Tracks_appear_on_their_days_in_date_order_with_Track_names_shown_and_Missed_per_Track()
    {
        var gym = new Track("gym", "Gym", new TrackRhythm.Weekly(2, 2, false), []);
        var cardio = new Track("cardio", "Cardio", new TrackRhythm.Weekly(1, 1, false), []);
        var mobility = new Track("mob", "Mobility", new TrackRhythm.Daily(75), []);
        var plan = PlanFixture.Make(start: D(2026, 1, 5), tracks: [gym, cardio, mobility], sessions:
        [
            PlanFixture.Session("lower", DayOfWeek.Monday, name: "Lower", trackId: "gym"),
            PlanFixture.Session("tempo", DayOfWeek.Tuesday, SessionType.Cardio, name: "Tempo Run", trackId: "cardio"),
            PlanFixture.Session("upper", DayOfWeek.Wednesday, name: "Upper", trackId: "gym"),
            PlanFixture.Session("stretch", DefaultDay.Daily, SessionType.Mobility, name: "Morning Stretch", trackId: "mob"),
        ]);

        var wed = Mon.AddDays(2);
        var rows = WeekRows.Build(plan, [], [], [Finished(Mon, "lower")], wed);

        Assert.Equal(["lower", "tempo", "upper"], rows.Select(r => r.SessionId));
        Assert.Equal(["Gym", "Cardio", "Gym"], rows.Select(r => r.TrackName));
        Assert.All(rows, r => Assert.True(r.ShowTrackName));
        Assert.Equal([WeekRowChip.Done, WeekRowChip.Missed, WeekRowChip.Today], Chips(rows));
    }

    [Fact]
    public void Daily_Sessions_are_returned_separately_with_NotStarted_InProgress_or_Done_and_never_in_day_rows()
    {
        var gym = new Track("gym", "Gym", new TrackRhythm.Weekly(1, 1, false), []);
        var mobility = new Track("mob", "Mobility", new TrackRhythm.Daily(75), []);
        var row1 = PlanFixture.Row("hip", sets: 1);
        var row2 = PlanFixture.Row("ankle", sets: 1);
        var row3 = PlanFixture.Row("spine", sets: 1);
        var row4 = PlanFixture.Row("shoulder", sets: 1);
        var stretch = PlanFixture.Session("stretch", DefaultDay.Daily, SessionType.Mobility, name: "Morning Stretch", trackId: "mob",
            exercises: [row1, row2, row3, row4]);
        var plan = PlanFixture.Make(start: D(2026, 1, 5), tracks: [gym, mobility], sessions:
        [
            PlanFixture.Session("lower", DayOfWeek.Monday, name: "Lower", trackId: "gym"),
            stretch,
        ]);

        // After days pass without a Workout, the daily row is still NotStarted (never Missed) and never in day rows.
        var fri = Mon.AddDays(4);
        Assert.DoesNotContain(WeekRows.Build(plan, [], [], [], fri), r => r.SessionId == "stretch");
        var notStarted = Assert.Single(WeekRows.BuildDaily(plan, [], fri));
        Assert.Equal(("stretch", "Morning Stretch", "mob", "Mobility", DailyRowState.NotStarted),
            (notStarted.SessionId, notStarted.Name, notStarted.TrackId, notStarted.TrackName, notStarted.State));
        Assert.Null(notStarted.InProgressWorkout);
        Assert.Null(notStarted.DoneWorkout);

        // An open Workout today puts the daily row InProgress.
        var open = new Workout("w-open", fri, "stretch", false, new DateTimeOffset(2026, 11, 20, 8, 0, 0, TimeSpan.Zero), null,
            [row1, row2, row3, row4], [], null);
        var inProgress = Assert.Single(WeekRows.BuildDaily(plan, [open], fri));
        Assert.Equal(DailyRowState.InProgress, inProgress.State);
        Assert.Equal(open, inProgress.InProgressWorkout);
        Assert.Null(inProgress.DoneWorkout);

        // A finished Workout below dayDonePercent (1 of 4 = 25% < 75%) does not count as Done.
        var partial = open with
        {
            Id = "w-partial",
            EndedAt = new DateTimeOffset(2026, 11, 20, 8, 15, 0, TimeSpan.Zero),
            Entries = [new WorkoutEntry("hip", EntryStatus.Done, null, null, null, [new WorkoutSet("s1", null, 1, null, null, null, false)])],
        };
        var belowThreshold = Assert.Single(WeekRows.BuildDaily(plan, [partial], fri));
        Assert.Equal(DailyRowState.NotStarted, belowThreshold.State);

        // A finished Workout meeting dayDonePercent (3 of 4 = 75%) puts the daily row Done.
        var done = partial with
        {
            Id = "w-done",
            Entries =
            [
                new WorkoutEntry("hip", EntryStatus.Done, null, null, null, [new WorkoutSet("s1", null, 1, null, null, null, false)]),
                new WorkoutEntry("ankle", EntryStatus.Done, null, null, null, [new WorkoutSet("s2", null, 1, null, null, null, false)]),
                new WorkoutEntry("spine", EntryStatus.Done, null, null, null, [new WorkoutSet("s3", null, 1, null, null, null, false)]),
            ],
        };
        var doneRow = Assert.Single(WeekRows.BuildDaily(plan, [done], fri));
        Assert.Equal(DailyRowState.Done, doneRow.State);
        Assert.Equal(done, doneRow.DoneWorkout);
        Assert.Null(doneRow.InProgressWorkout);
    }
}
