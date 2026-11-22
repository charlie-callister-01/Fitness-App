using Core.Model;
using Core.Rules.Insight;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.Insight;

public class CalendarViewTests
{
    // Plan starts Mon 5 Jan 2026: Strength "a-lower" (named Lower) on Mondays, benchmark Test on Saturdays every 6 weeks.
    private static readonly DateOnly Start = new(2026, 1, 5);
    private static readonly DateOnly Wed = new(2026, 1, 14);
    private static readonly Exercise Squat = new("squat", "Squat", LogKind.Load, null, false);

    private static Workout Finished(string id, DateOnly date, string? session, params WorkoutSet[] sets) =>
        new(id, date, session, false, null, new DateTimeOffset(date.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero),
            [new SessionExercise("squat", 1, 5, null, null)],
            [new WorkoutEntry("squat", EntryStatus.Done, null, null, null, sets)], null);

    private static CalendarView Build(Plan plan, DateOnly today, IReadOnlyList<Workout>? workouts = null, IReadOnlyList<Break>? breaks = null) =>
        CalendarViewBuilder.Build(plan, [], breaks ?? [], workouts ?? [], [Squat], today);

    private static CalendarDay Day(CalendarView v, DateOnly d) => v.Weeks.SelectMany(w => w.Days).Single(x => x.Date == d);

    [Fact]
    public void A_finished_Session_day_is_Done_with_its_type_workout_PR_flag_and_label()
    {
        var view = Build(PlanFixture.Make(Start), Wed,
            [Finished("w0", Start, "a-lower", Set(kg: 20, reps: 5)), Finished("w1", Start.AddDays(7), "a-lower", Set(kg: 30, reps: 5))]);
        var day = Day(view, Start.AddDays(7));

        Assert.Equal(CalendarDayState.Done, day.State);
        Assert.Equal(SessionType.Strength, day.SessionType);
        Assert.Equal("w1", day.WorkoutId);
        Assert.Equal("Mon 12 Jan: Lower, done, 1 PR", day.Label);
        Assert.True(day.HasPr);
    }

    [Fact]
    public void The_first_ever_result_is_a_baseline_not_a_PR()
    {
        var day = Day(Build(PlanFixture.Make(Start), Wed, [Finished("w0", Start, "a-lower", Set(kg: 20, reps: 5))]), Start);

        Assert.Equal(0, day.PrCount);
        Assert.Equal("Mon 5 Jan: Lower, done", day.Label);
    }

    [Fact]
    public void A_Workout_with_a_skipped_row_is_Partial()
    {
        var partial = Finished("w1", Start, "a-lower") with
        {
            Entries = [new WorkoutEntry("squat", EntryStatus.Skipped, "time", null, null, [])],
        };
        var day = Day(Build(PlanFixture.Make(Start), Wed, [partial]), Start);

        Assert.Equal(CalendarDayState.Partial, day.State);
        Assert.Equal("Mon 5 Jan: Lower, partial", day.Label);
    }

    [Fact]
    public void A_past_scheduled_Session_with_no_Workout_is_Missed()
    {
        var day = Day(Build(PlanFixture.Make(Start), Wed), Start);

        Assert.Equal(CalendarDayState.Missed, day.State);
        Assert.Null(day.WorkoutId);
        Assert.Equal("Mon 5 Jan: Lower, missed", day.Label);
    }

    [Fact]
    public void A_Break_over_a_Missed_day_shows_as_a_Break_day()
    {
        var day = Day(Build(PlanFixture.Make(Start), Wed, breaks: [BreakOf(Start, Start.AddDays(3))]), Start);

        Assert.Equal(CalendarDayState.BreakDay, day.State);
        Assert.Equal("Mon 5 Jan: Break", day.Label);
    }

    [Fact]
    public void Today_and_later_days_with_nothing_are_Future_and_today_is_flagged()
    {
        var view = Build(PlanFixture.Make(Start), Wed);

        Assert.Equal(CalendarDayState.Future, Day(view, Wed).State);
        Assert.True(Day(view, Wed).IsToday);
        Assert.EndsWith(", today", Day(view, Wed).Label);
        Assert.Equal(CalendarDayState.Future, Day(view, Wed.AddDays(1)).State);
        Assert.False(Day(view, Wed.AddDays(1)).IsToday);
    }

    [Fact]
    public void A_past_rest_day_is_Empty()
    {
        Assert.Equal(CalendarDayState.Empty, Day(Build(PlanFixture.Make(Start), Wed), Start.AddDays(1)).State);
    }

    [Fact]
    public void A_Custom_workout_day_is_Done_with_no_Session_type()
    {
        var day = Day(Build(PlanFixture.Make(Start), Wed, [Finished("c1", Start.AddDays(1), null, Set(kg: 20, reps: 5))]), Start.AddDays(1));

        Assert.Equal(CalendarDayState.Done, day.State);
        Assert.Null(day.SessionType);
        Assert.StartsWith("Tue 6 Jan: Custom workout, done", day.Label);
    }

    [Fact]
    public void Weeks_run_Monday_to_Sunday_from_the_Plan_start_to_the_Goal_date_numbered_from_1()
    {
        var plan = PlanFixture.Make(Start) with { GoalDate = new DateOnly(2026, 2, 1) };
        var view = Build(plan, Wed);

        Assert.Equal(4, view.Weeks.Count);
        Assert.Equal([1, 2, 3, 4], view.Weeks.Select(w => w.Number));
        Assert.All(view.Weeks, w => Assert.Equal(7, w.Days.Count));
        Assert.Equal(Start, view.Weeks[0].Monday);
        Assert.Equal(new DateOnly(2026, 2, 1), view.Weeks[^1].Days[^1].Date);
    }

    [Fact]
    public void A_Plan_with_no_Phases_and_no_Goal_date_runs_to_this_week_with_an_empty_Phase_strip()
    {
        var view = Build(PlanFixture.Make(Start), Wed);

        Assert.Equal(2, view.Weeks.Count);
        Assert.Empty(view.Phases);
        Assert.False(view.HasHistory);
    }

    [Fact]
    public void The_Phase_strip_lists_Phases_in_week_order_with_name_and_length_and_extends_the_grid()
    {
        static Phase P(string id, string focus, int s, int e) => new(id, focus, s, e, [], []);
        var plan = PlanFixture.Make(Start, phases: [P("b", "Build", 5, 8), P("a", "Base", 1, 4)]);
        var view = Build(plan, Wed);

        Assert.Equal(["Base", "Build"], view.Phases.Select(p => p.Name));
        Assert.Equal([4, 4], view.Phases.Select(p => p.Weeks));
        Assert.Equal(8, view.Weeks.Count);
    }

    [Fact]
    public void History_is_reported_once_a_Workout_is_finished()
    {
        Assert.True(Build(PlanFixture.Make(Start), Wed, [Finished("w", Start, "a-lower", Set(kg: 20, reps: 5))]).HasHistory);
    }
}
