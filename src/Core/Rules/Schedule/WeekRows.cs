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

/// <summary>Where a daily Session stands today in the Week view. Never Missed.</summary>
public enum DailyRowState
{
    NotStarted,
    InProgress,
    Done,
}

/// <summary>One daily Session in the Week view ("Every day"), with its Track name and today's state. Never wears a Missed chip.</summary>
public sealed record DailyWeekRow(
    Session Session,
    string TrackId,
    string TrackName,
    DailyRowState State,
    Workout? InProgressWorkout,
    Workout? DoneWorkout)
{
    public string SessionId => Session.Id;

    public string Name => Session.Name;
}

/// <summary>One Session on its day in the Week view, with its chip and Track context.</summary>
public sealed record WeekRow(ScheduledSession Placed, WeekRowChip Chip, string TrackName = "", bool ShowTrackName = false)
{
    public string SessionId => Placed.SessionId;

    public string Name => Placed.Session.Name;

    public string TrackId => Placed.Session.TrackId;

    public DateOnly Date => Placed.Date;

    public bool IsMoved => Placed.IsMoved;

    public bool IsBreakDay => Placed.IsBreakDay;
}

/// <summary>What the Week view lists: daily Sessions ("Every day") separately above this week's day Sessions in day order with their chips.</summary>
public static class WeekRows
{
    public static IReadOnlyList<WeekRow> Build(
        Plan plan, IReadOnlyList<WeekSchedule> schedules, IReadOnlyList<Break> breaks, IReadOnlyList<Workout> workouts, DateOnly today)
    {
        var weeklyTracks = plan.Tracks.Where(t => t.Rhythm is TrackRhythm.Weekly).ToList();
        if (weeklyTracks.Count == 0) return [];

        var showTrackName = weeklyTracks.Count > 1;
        var monday = WeekCalendar.MondayOf(today);
        var done = workouts
            .Where(w => w.EndedAt is not null && WeekCalendar.InWeek(monday, w.Date))
            .Select(w => w.SessionId).OfType<string>().ToHashSet();

        var rows = new List<(WeekRow Row, int TrackIndex, int SessionIndex)>();
        for (var trackIndex = 0; trackIndex < weeklyTracks.Count; trackIndex++)
        {
            var track = weeklyTracks[trackIndex];
            var layout = WeekScheduleRules.Layout(plan, track, schedules, breaks, today);
            var missed = WeekScheduleRules.MissedInWeek(plan, track, schedules, breaks, workouts, today, today).Select(m => m.SessionId).ToHashSet();
            for (var sessionIndex = 0; sessionIndex < layout.Sessions.Count; sessionIndex++)
            {
                var placed = layout.Sessions[sessionIndex];
                rows.Add((new WeekRow(placed, ChipOf(placed, done, missed, today), track.Name, showTrackName), trackIndex, sessionIndex));
            }
        }

        return [.. rows.OrderBy(r => r.Row.Date).ThenBy(r => r.TrackIndex).ThenBy(r => r.SessionIndex).Select(r => r.Row)];
    }

    /// <summary>
    /// Every daily Session in <paramref name="plan"/> as one "Every day" row with its Track name and today's done state
    /// (<see cref="DailyRowState.Done"/>, <see cref="DailyRowState.InProgress"/>, <see cref="DailyRowState.NotStarted"/>).
    /// Never produces a Missed state even after days pass without a Workout.
    /// </summary>
    public static IReadOnlyList<DailyWeekRow> BuildDaily(
        Plan plan, IReadOnlyList<Workout> workouts, DateOnly today)
    {
        var rows = new List<DailyWeekRow>();
        foreach (var track in plan.Tracks)
        {
            if (track.Rhythm is not TrackRhythm.Daily daily) continue;

            foreach (var session in TrackRules.SessionsOf(plan, track))
            {
                var todays = workouts.Where(w => w.Date == today && w.SessionId == session.Id).ToList();
                var open = todays.FirstOrDefault(DailyTrackRules.IsOpen);
                var done = todays.FirstOrDefault(w => !DailyTrackRules.IsOpen(w) && DailyTrackRules.IsWorkoutDone(w, daily.DayDonePercent));
                var state = done is not null
                    ? DailyRowState.Done
                    : open is not null
                        ? DailyRowState.InProgress
                        : DailyRowState.NotStarted;
                rows.Add(new DailyWeekRow(session, track.Id, track.Name, state, done is null ? open : null, done));
            }
        }

        return rows;
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

