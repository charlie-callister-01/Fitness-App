using Core.Model;
using Core.Rules.Outcomes;
using Core.Rules.Weeks;

namespace Core.Rules.Tracks;

/// <summary>
/// A daily Track's progress up to today: current Streak of done days, best-ever Streak,
/// total done days since the Plan started, and whether today has a done Workout.
/// </summary>
public sealed record DailyTrackStatus(
    int Current,
    int BestRun,
    int DaysDone,
    bool TodayDone);

/// <summary>Day-done threshold, daily Streak, best run and total done days for a daily Track. "Today" is always passed in.</summary>
public static class DailyTrackRules
{
    /// <summary>Whether <paramref name="date"/> counts as done for <paramref name="track"/>.</summary>
    public static bool IsDayDone(Plan plan, Track track, IReadOnlyList<Workout> workouts, DateOnly date)
    {
        if (track.Rhythm is not TrackRhythm.Daily daily || date < plan.StartDate)
            return false;

        var sessionIds = SessionIdsOf(plan, track);
        return workouts.Any(w => w.Date == date && Qualifies(w, sessionIds, daily.DayDonePercent));
    }

    /// <summary>Whether <paramref name="workout"/> meets <paramref name="dayDonePercent"/> of its planned rows.</summary>
    public static bool IsWorkoutDone(Workout workout, int dayDonePercent)
    {
        var planned = workout.Planned.Count;
        if (planned == 0) return false;
        return DoneRowCount(workout) * 100 >= dayDonePercent * planned;
    }

    /// <summary>Integer percentage (0 to 100) of planned rows done in <paramref name="workout"/>; 0 when no planned rows exist.</summary>
    public static int DonePercent(Workout workout)
    {
        var planned = workout.Planned.Count;
        return planned == 0 ? 0 : DoneRowCount(workout) * 100 / planned;
    }

    /// <summary>
    /// Current Streak, best run, total done days and today's done state for <paramref name="track"/> over
    /// days from the Plan's start date through <paramref name="today"/>. Returns zeroes for a non-daily Track.
    /// </summary>
    public static DailyTrackStatus Status(
        Plan plan,
        Track track,
        IReadOnlyList<Workout> workouts,
        IReadOnlyList<Break> breaks,
        DateOnly today)
    {
        if (track.Rhythm is not TrackRhythm.Daily daily || today < plan.StartDate)
            return new DailyTrackStatus(0, 0, 0, false);

        var sessionIds = SessionIdsOf(plan, track);
        var doneDays = workouts
            .Where(w => w.Date >= plan.StartDate
                        && w.Date <= today
                        && Qualifies(w, sessionIds, daily.DayDonePercent))
            .Select(w => w.Date)
            .ToHashSet();

        var current = 0;
        var bestRun = 0;
        for (var day = plan.StartDate; day <= today; day = day.AddDays(1))
        {
            if (BreakCalendar.IsBreakDay(breaks, day)) continue;

            if (doneDays.Contains(day))
            {
                current++;
                bestRun = Math.Max(bestRun, current);
            }
            else if (day < today)
            {
                current = 0;
            }
        }

        return new DailyTrackStatus(current, bestRun, doneDays.Count, doneDays.Contains(today));
    }

    private static HashSet<string> SessionIdsOf(Plan plan, Track track) =>
        TrackRules.SessionsOf(plan, track).Select(s => s.Id).ToHashSet();

    private static bool Qualifies(Workout workout, HashSet<string> sessionIds, int dayDonePercent) =>
        workout.SessionId is { } sid
        && sessionIds.Contains(sid)
        && IsWorkoutDone(workout, dayDonePercent);

    private static int DoneRowCount(Workout workout) =>
        WorkoutOutcomes.Of(workout).Rows.Count(r => r.Kind == RowOutcomeKind.Done);
}
