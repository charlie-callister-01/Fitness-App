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
        var sunday = WeekCalendar.SundayOf(monday);
        var breakDays = Enumerable.Range(0, 7).Select(monday.AddDays).Where(d => IsBreakDay(breaks, d)).ToList();
        if (WeekRules.PlanWeekNumber(plan, sunday) is null) return new(monday, [], breakDays);

        var moves = schedules.FirstOrDefault(s => s.WeekStart == monday)?.Moves ?? [];
        var placed = new List<ScheduledSession>();
        foreach (var session in plan.Sessions.Where(s => BenchmarkWeeks.IsScheduledInWeek(plan, s, monday)))
        {
            var move = moves.FirstOrDefault(m => m.SessionId == session.Id && m.Date >= monday && m.Date <= sunday);
            var date = move?.Date ?? DefaultDate(monday, session.DefaultDay);
            placed.Add(new(session.Id, date, move is not null, breakDays.Contains(date), BenchmarkWeeks.IsBenchmark(session)));
        }

        return new(monday, placed.OrderBy(p => p.Date).ToList(), breakDays);
    }

    /// <summary>
    /// Missed sessions in the week containing <paramref name="weekDate"/>: scheduled before <paramref name="today"/>,
    /// not on a Break day or before the Plan start, and with no Workout from that Session in the same week.
    /// A Custom workout never clears one.
    /// </summary>
    public static IReadOnlyList<MissedSession> MissedInWeek(
        Plan plan, IReadOnlyList<WeekSchedule> schedules, IReadOnlyList<Break> breaks,
        IReadOnlyList<Workout> workouts, DateOnly weekDate, DateOnly today)
    {
        var layout = Layout(plan, schedules, breaks, weekDate);
        var sunday = WeekCalendar.SundayOf(layout.Monday);
        return layout.Sessions
            .Where(s => s.Date < today && s.Date >= plan.StartDate && !s.IsBreakDay)
            .Where(s => !workouts.Any(w => w.SessionId == s.SessionId && w.Date >= layout.Monday && w.Date <= sunday))
            .Select(s => new MissedSession(s.SessionId, s.Date))
            .ToList();
    }

    /// <summary>Every Missed session from the Plan start up to and including today's week, oldest first.</summary>
    public static IReadOnlyList<MissedSession> Missed(
        Plan plan, IReadOnlyList<WeekSchedule> schedules, IReadOnlyList<Break> breaks,
        IReadOnlyList<Workout> workouts, DateOnly today)
    {
        var result = new List<MissedSession>();
        var thisMonday = WeekCalendar.MondayOf(today);
        for (var monday = WeekCalendar.MondayOf(plan.StartDate); monday <= thisMonday; monday = WeekCalendar.AddWeeks(monday, 1))
            result.AddRange(MissedInWeek(plan, schedules, breaks, workouts, monday, today));
        return result;
    }

    private static DateOnly DefaultDate(DateOnly monday, DayOfWeek day) =>
        monday.AddDays(((int)day + 6) % 7);

    private static bool IsBreakDay(IReadOnlyList<Break> breaks, DateOnly day) =>
        breaks.Any(b => day >= b.StartDate && day <= b.EndDate);
}
