using System.Globalization;
using Core.Model;
using Core.Rules.Outcomes;
using Core.Rules.Performance;
using Core.Rules.Schedule;
using Core.Rules.Weeks;

namespace Core.Rules.Insight;

/// <summary>How one day of the Plan calendar looks. Every state is told apart without colour (fill, dashes, hatching).</summary>
public enum CalendarDayState
{
    /// <summary>Nothing planned and nothing done (a rest day, or a day before the Plan starts).</summary>
    Empty,

    /// <summary>Today or later with no Workout and no Break: drawn empty.</summary>
    Future,

    Done,
    Partial,
    Missed,
    BreakDay,
}

/// <summary>One cell of the heatmap. <see cref="WorkoutId"/> is the Workout a tap opens (null when the day has none).</summary>
public sealed record CalendarDay(
    DateOnly Date,
    CalendarDayState State,
    SessionType? SessionType,
    int PrCount,
    bool IsToday,
    string Label,
    string? WorkoutId)
{
    public bool HasPr => PrCount > 0;
}

/// <summary>One week column, identified by its Monday and numbered from 1 at the Plan's first week. Days run Monday to Sunday.</summary>
public sealed record CalendarWeek(DateOnly Monday, int Number, IReadOnlyList<CalendarDay> Days);

/// <summary>A Phase on the strip above the weeks: its name and how many weeks it spans.</summary>
public sealed record CalendarPhase(string Name, int StartWeek, int EndWeek)
{
    public int Weeks => EndWeek - StartWeek + 1;
}

/// <summary>The whole Plan from its first week to race day as weeks by days, plus the Phase strip.</summary>
public sealed record CalendarView(IReadOnlyList<CalendarWeek> Weeks, IReadOnlyList<CalendarPhase> Phases, bool HasHistory);

/// <summary>
/// The Calendar tab's view (main Track). Built from the existing week, Break, Missed and Partial rules and takes plain inputs,
/// so per-Track rows can be added later. The grid runs from the Plan's first week to the Goal date (or the last Phase's end, or
/// this week when neither exists) and always includes today's week.
/// </summary>
public static class CalendarViewBuilder
{
    public static CalendarView Build(
        Plan plan, IReadOnlyList<WeekSchedule> schedules, IReadOnlyList<Break> breaks,
        IReadOnlyList<Workout> workouts, IReadOnlyList<Exercise> exercises, DateOnly today)
    {
        var finished = workouts.Where(w => w.EndedAt is not null).ToList();
        var missed = WeekScheduleRules.Missed(plan, schedules, breaks, workouts, today)
            .ToDictionary(m => m.Date, m => m);
        var prs = PrCounts(exercises, finished);
        var firstMonday = WeekCalendar.MondayOf(plan.StartDate);
        var lastMonday = LastMonday(plan, firstMonday, today);

        var weeks = new List<CalendarWeek>();
        for (var monday = firstMonday; monday <= lastMonday; monday = WeekCalendar.AddWeeks(monday, 1))
        {
            var days = WeekCalendar.DaysOf(monday)
                .Select(d => Day(plan, breaks, finished, missed, prs, d, today))
                .ToList();
            weeks.Add(new CalendarWeek(monday, WeekRules.PlanWeekNumber(plan, monday)!.Value, days));
        }

        var phases = plan.Phases
            .OrderBy(p => p.StartWeek)
            .Select(p => new CalendarPhase(p.Focus, p.StartWeek, p.EndWeek))
            .ToList();
        return new CalendarView(weeks, phases, finished.Count > 0);
    }

    private static DateOnly LastMonday(Plan plan, DateOnly firstMonday, DateOnly today)
    {
        var last = WeekCalendar.MondayOf(today);
        if (plan.GoalDate is { } goal && WeekCalendar.MondayOf(goal) > last) last = WeekCalendar.MondayOf(goal);
        if (plan.Phases.Count > 0)
        {
            var phaseEnd = WeekCalendar.AddWeeks(firstMonday, plan.Phases.Max(p => p.EndWeek) - 1);
            if (phaseEnd > last) last = phaseEnd;
        }

        return last < firstMonday ? firstMonday : last;
    }

    private static Dictionary<string, int> PrCounts(IReadOnlyList<Exercise> exercises, IReadOnlyList<Workout> finished) =>
        exercises
            .SelectMany(e => ExercisePerformance.Prs(e, finished))
            .GroupBy(p => p.WorkoutId)
            .ToDictionary(g => g.Key, g => g.Count());

    private static CalendarDay Day(
        Plan plan, IReadOnlyList<Break> breaks, IReadOnlyList<Workout> finished,
        Dictionary<DateOnly, MissedSession> missed, Dictionary<string, int> prs, DateOnly date, DateOnly today)
    {
        var isToday = date == today;
        var onDay = finished.Where(w => w.Date == date).ToList();
        var prCount = onDay.Sum(w => prs.GetValueOrDefault(w.Id));

        if (onDay.Count > 0)
        {
            var partial = onDay.All(w => WorkoutOutcomes.Of(w).IsPartial);
            var shown = onDay.FirstOrDefault(w => w.SessionId is not null) ?? onDay[0];
            var session = plan.Sessions.FirstOrDefault(s => s.Id == shown.SessionId);
            var state = partial ? CalendarDayState.Partial : CalendarDayState.Done;
            return new CalendarDay(date, state, session?.Type, prCount, isToday,
                Label(date, session?.Name ?? "Custom workout", state, prCount, isToday), shown.Id);
        }

        if (BreakCalendar.IsBreakDay(breaks, date))
            return new CalendarDay(date, CalendarDayState.BreakDay, null, 0, isToday, Label(date, null, CalendarDayState.BreakDay, 0, isToday), null);

        if (missed.TryGetValue(date, out var miss))
        {
            var type = plan.Sessions.FirstOrDefault(s => s.Id == miss.SessionId)?.Type;
            return new CalendarDay(date, CalendarDayState.Missed, type, 0, isToday, Label(date, miss.SessionName, CalendarDayState.Missed, 0, isToday), null);
        }

        return date >= today
            ? new CalendarDay(date, CalendarDayState.Future, null, 0, isToday, Label(date, null, CalendarDayState.Future, 0, isToday), null)
            : new CalendarDay(date, CalendarDayState.Empty, null, 0, isToday, Label(date, null, CalendarDayState.Empty, 0, isToday), null);
    }

    /// <summary>"Wed 18 Nov: Session C, done, 1 PR".</summary>
    private static string Label(DateOnly date, string? name, CalendarDayState state, int prs, bool isToday)
    {
        var word = state switch
        {
            CalendarDayState.Done => "done",
            CalendarDayState.Partial => "partial",
            CalendarDayState.Missed => "missed",
            CalendarDayState.BreakDay => "Break",
            CalendarDayState.Future => "upcoming",
            _ => "nothing planned",
        };
        var text = InsightFormat.WeekdayDay(date) + ": ";
        text += name is null ? word : $"{name}, {word}";
        if (prs > 0) text += prs == 1 ? ", 1 PR" : $", {prs} PRs";
        return isToday ? text + ", today" : text;
    }
}
