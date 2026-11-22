using Core.Model;
using Core.Rules.Tracks;
using Core.Rules.Weeks;

namespace Core.Rules.Schedule;

/// <summary>The chip a Week view row wears. A finished Workout wins over Missed, which wins over Today.</summary>
public enum WeekRowChip
{
    None,
    Done,
    Missed,
    Today,
}

/// <summary>One Session on its day in the Week view, with its chip.</summary>
public sealed record WeekRow(ScheduledSession Placed, WeekRowChip Chip)
{
    public string SessionId => Placed.SessionId;

    public string Name => Placed.Session.Name;

    public DateOnly Date => Placed.Date;

    public bool IsMoved => Placed.IsMoved;

    public bool IsBreakDay => Placed.IsBreakDay;
}

/// <summary>What the Week view lists: this week's Sessions in day order with their chips.</summary>
public static class WeekRows
{
    public static IReadOnlyList<WeekRow> Build(
        Plan plan, IReadOnlyList<WeekSchedule> schedules, IReadOnlyList<Break> breaks, IReadOnlyList<Workout> workouts, DateOnly today)
    {
        var main = TrackRules.Main(plan);
        var layout = WeekScheduleRules.Layout(plan, main, schedules, breaks, today);
        var missed = WeekScheduleRules.MissedInWeek(plan, main, schedules, breaks, workouts, today, today).Select(m => m.SessionId).ToHashSet();
        var done = workouts
            .Where(w => w.EndedAt is not null && WeekCalendar.InWeek(layout.Monday, w.Date))
            .Select(w => w.SessionId).OfType<string>().ToHashSet();
        return [.. layout.Sessions.Select(s => new WeekRow(s, ChipOf(s, done, missed, today)))];
    }

    /// <summary>Whether any Break touches the week containing <paramref name="today"/> (the only time a suggested week is worth offering).</summary>
    public static bool BreakTouchesWeek(IReadOnlyList<Break> breaks, DateOnly today) =>
        breaks.Any(b => b.EndDate >= WeekCalendar.MondayOf(today) && b.StartDate <= WeekCalendar.SundayOf(today));

    private static WeekRowChip ChipOf(ScheduledSession s, HashSet<string> done, HashSet<string> missed, DateOnly today) =>
        done.Contains(s.SessionId) ? WeekRowChip.Done
        : missed.Contains(s.SessionId) ? WeekRowChip.Missed
        : s.Date == today ? WeekRowChip.Today
        : WeekRowChip.None;
}
