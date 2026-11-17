using Core.Model;
using Core.Rules.Weeks;

namespace Core.Rules.Schedule;

/// <summary>Week schedule computation and Missed sessions. "Today" is always passed in.</summary>
public static class WeekScheduleRules
{
    /// <summary>
    /// The Sessions placed in the week containing <paramref name="weekDate"/>: default day, then that week's moves
    /// (a move to a date outside the week is ignored), with Break days marked. Benchmark Sessions appear only in
    /// their interval weeks. Ordered by date, then Plan order. Weeks before the Plan start are empty.
    /// </summary>
    public static WeekLayout Layout(
        Plan plan, IReadOnlyList<WeekSchedule> schedules, IReadOnlyList<Break> breaks, DateOnly weekDate)
    {
        var monday = WeekCalendar.MondayOf(weekDate);
        var breakDays = WeekCalendar.DaysOf(monday).Where(d => BreakCalendar.IsBreakDay(breaks, d)).ToList();
        if (!HasStarted(plan, monday)) return new(monday, [], breakDays);

        var moves = MovesOf(schedules, monday);
        var placed = plan.Sessions
            .Select(session => Place(plan, session, monday, moves, breakDays))
            .OfType<ScheduledSession>()
            .OrderBy(p => p.Date)
            .ToList();
        return new(monday, placed, breakDays);
    }

    /// <summary>
    /// Where one Session sits in the week starting on <paramref name="monday"/>, or null when the Plan hasn't started
    /// or the Session isn't scheduled that week (a benchmark Session outside its interval weeks).
    /// </summary>
    public static ScheduledSession? PlaceSession(
        Plan plan, IReadOnlyList<WeekSchedule> schedules, IReadOnlyList<Break> breaks, Session session, DateOnly monday)
    {
        if (!HasStarted(plan, monday)) return null;
        var breakDays = WeekCalendar.DaysOf(monday).Where(d => BreakCalendar.IsBreakDay(breaks, d)).ToList();
        return Place(plan, session, monday, MovesOf(schedules, monday), breakDays);
    }

    private static bool HasStarted(Plan plan, DateOnly monday) =>
        WeekRules.PlanWeekNumber(plan, WeekCalendar.SundayOf(monday)) is not null;

    private static IReadOnlyList<SessionMove> MovesOf(IReadOnlyList<WeekSchedule> schedules, DateOnly monday) =>
        schedules.FirstOrDefault(s => s.WeekStart == monday)?.Moves ?? [];

    private static ScheduledSession? Place(
        Plan plan, Session session, DateOnly monday, IReadOnlyList<SessionMove> moves, IReadOnlyList<DateOnly> breakDays)
    {
        if (!BenchmarkWeeks.IsScheduledInWeek(plan, session, monday)) return null;
        var move = moves.FirstOrDefault(m => m.SessionId == session.Id && WeekCalendar.InWeek(monday, m.Date));
        var date = move?.Date ?? WeekCalendar.DefaultDate(monday, session.DefaultDay);
        return new(session, date, move is not null, breakDays.Contains(date));
    }

    /// <summary>
    /// Missed sessions in the week containing <paramref name="weekDate"/>: scheduled before <paramref name="today"/>,
    /// not on a Break day or before the Plan start, and with no Workout from that Session in the same week.
    /// A Custom workout never clears one.
    /// </summary>
    public static IReadOnlyList<MissedSession> MissedInWeek(
        Plan plan, IReadOnlyList<WeekSchedule> schedules, IReadOnlyList<Break> breaks,
        IReadOnlyList<Workout> workouts, DateOnly weekDate, DateOnly today) =>
        MissedInWeek(plan, schedules, breaks, ByWeek(workouts), weekDate, today);

    /// <summary>Every Missed session from the Plan start up to and including today's week, oldest first.</summary>
    public static IReadOnlyList<MissedSession> Missed(
        Plan plan, IReadOnlyList<WeekSchedule> schedules, IReadOnlyList<Break> breaks,
        IReadOnlyList<Workout> workouts, DateOnly today)
    {
        var byWeek = ByWeek(workouts);
        var result = new List<MissedSession>();
        var thisMonday = WeekCalendar.MondayOf(today);
        for (var monday = WeekCalendar.MondayOf(plan.StartDate); monday <= thisMonday; monday = WeekCalendar.AddWeeks(monday, 1))
            result.AddRange(MissedInWeek(plan, schedules, breaks, byWeek, monday, today));
        return result;
    }

    private static ILookup<DateOnly, Workout> ByWeek(IReadOnlyList<Workout> workouts) =>
        workouts.ToLookup(w => WeekCalendar.MondayOf(w.Date));

    private static IReadOnlyList<MissedSession> MissedInWeek(
        Plan plan, IReadOnlyList<WeekSchedule> schedules, IReadOnlyList<Break> breaks,
        ILookup<DateOnly, Workout> workoutsByWeek, DateOnly weekDate, DateOnly today)
    {
        var layout = Layout(plan, schedules, breaks, weekDate);
        var doneThisWeek = workoutsByWeek[layout.Monday].Select(w => w.SessionId).OfType<string>().ToHashSet();
        return layout.Sessions
            .Where(s => s.Date < today && s.Date >= plan.StartDate && !s.IsBreakDay)
            .Where(s => !doneThisWeek.Contains(s.SessionId))
            .Select(s => new MissedSession(s.SessionId, s.Session.Name, s.Date))
            .ToList();
    }
}
