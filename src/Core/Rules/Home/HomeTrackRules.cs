using Core.Model;
using Core.Rules.Outcomes;
using Core.Rules.PlanEditing;
using Core.Rules.Schedule;
using Core.Rules.Tracks;
using Core.Rules.Weeks;
using Core.Storage;

namespace Core.Rules.Home;

/// <summary>Where a daily Track stands today.</summary>
public enum DailyTrackState
{
    NotStarted,
    InProgress,
    Done,
}

/// <summary>
/// Home's card for one daily Track: today's Session, its state, the done percentage reached today,
/// the daily Streak and whether today is a Break day. A daily Session is never Missed.
/// </summary>
public sealed record DailyTrackCard(
    string TrackId,
    string TrackName,
    Session? Session,
    DailyTrackState State,
    int DonePercent,
    int Streak,
    bool OnBreak,
    Workout? InProgressWorkout);

/// <summary>One Session of an extra weekly Track in this week, with its state.</summary>
public sealed record ExtraWeekSession(Session Session, DateOnly Date, bool IsToday, TileState State);

/// <summary>Home's week line for one extra (non-main) weekly Track: its Sessions this week, progress against target, and Streak.</summary>
public sealed record ExtraWeekLine(
    string TrackId,
    string TrackName,
    IReadOnlyList<ExtraWeekSession> Sessions,
    int Done,
    int Target,
    int Streak,
    IReadOnlyList<string> MissedSessionIds);

/// <summary>Home data for the Tracks after the main Track. Pure rules, no clock.</summary>
public static class HomeTrackRules
{
    public static IReadOnlyList<DailyTrackCard> DailyCards(DataSnapshot snapshot, DateOnly today)
    {
        var plan = ActivePlan.Of(snapshot.Plans);
        if (plan is null) return [];

        return [.. plan.Tracks
            .Where(t => t.Rhythm is TrackRhythm.Daily)
            .Select(t => DailyCard(plan, t, snapshot, today))];
    }

    public static IReadOnlyList<ExtraWeekLine> ExtraWeekLines(DataSnapshot snapshot, DateOnly today)
    {
        var plan = ActivePlan.Of(snapshot.Plans);
        if (plan is null) return [];

        return [.. plan.Tracks
            .Skip(1)
            .Where(t => t.Rhythm is TrackRhythm.Weekly)
            .Select(t => ExtraLine(plan, t, snapshot, today))];
    }

    /// <summary>Whether <paramref name="workout"/> was made from a Session of a Track other than the main Track.</summary>
    public static bool IsExtraTrackWorkout(Plan plan, Workout workout)
    {
        if (workout.SessionId is not { } sessionId) return false;
        var session = plan.Sessions.FirstOrDefault(s => s.Id == sessionId);
        return session is not null && session.TrackId != TrackRules.Main(plan).Id;
    }

    private static DailyTrackCard DailyCard(Plan plan, Track track, DataSnapshot snapshot, DateOnly today)
    {
        var daily = (TrackRhythm.Daily)track.Rhythm;
        var sessions = TrackRules.SessionsOf(plan, track);
        var ids = sessions.Select(s => s.Id).ToHashSet();
        var todays = snapshot.Workouts
            .Where(w => w.Date == today && w.SessionId is { } id && ids.Contains(id))
            .ToList();

        var open = todays.FirstOrDefault(w => w.EndedAt is null);
        var percent = todays.Count == 0 ? 0 : todays.Max(DailyTrackRules.DonePercent);
        var done = todays.Any(w => w.EndedAt is not null && DailyTrackRules.IsWorkoutDone(w, daily.DayDonePercent));
        var state = done ? DailyTrackState.Done : open is not null ? DailyTrackState.InProgress : DailyTrackState.NotStarted;
        var status = DailyTrackRules.Status(plan, track, snapshot.Workouts, snapshot.Breaks, today);

        return new DailyTrackCard(
            track.Id,
            track.Name,
            sessions.FirstOrDefault(),
            state,
            percent,
            status.Current,
            BreakCalendar.IsBreakDay(snapshot.Breaks, today),
            done ? null : open);
    }

    private static ExtraWeekLine ExtraLine(Plan plan, Track track, DataSnapshot snapshot, DateOnly today)
    {
        var monday = WeekCalendar.MondayOf(today);
        var layout = WeekScheduleRules.Layout(plan, track, snapshot.WeekSchedules, snapshot.Breaks, today);
        var missedIds = WeekScheduleRules
            .MissedInWeek(plan, track, snapshot.WeekSchedules, snapshot.Breaks, snapshot.Workouts, today, today)
            .OrderBy(m => m.Date)
            .Select(m => m.SessionId)
            .ToList();
        var thisWeek = snapshot.Workouts.Where(w => WeekCalendar.InWeek(monday, w.Date)).ToList();

        var sessions = layout.Sessions
            .Select(s => new ExtraWeekSession(s.Session, s.Date, s.Date == today, StateOf(s, thisWeek, missedIds, today)))
            .ToList();
        var target = track.Rhythm is TrackRhythm.Weekly weekly ? weekly.WeeklyTarget : 0;
        var streak = WeekRules.Streak(plan, track, snapshot.Workouts, snapshot.Breaks, today).Current;

        return new ExtraWeekLine(
            track.Id,
            track.Name,
            sessions,
            WeekRules.CountedWorkouts(plan, track, snapshot.Workouts, monday),
            target,
            streak,
            missedIds);
    }

    private static TileState StateOf(ScheduledSession scheduled, List<Workout> thisWeek, List<string> missedIds, DateOnly today)
    {
        var finished = thisWeek
            .Where(w => w.SessionId == scheduled.SessionId && w.EndedAt is not null)
            .Select(WorkoutOutcomes.Of)
            .ToList();
        if (finished.Count > 0)
            return finished.Any(o => !o.IsPartial) ? TileState.Done : TileState.Partial;
        if (scheduled.IsBreakDay) return TileState.BreakDay;
        if (missedIds.Contains(scheduled.SessionId)) return TileState.Missed;
        return scheduled.Date >= today ? TileState.Upcoming : TileState.Rest;
    }
}
