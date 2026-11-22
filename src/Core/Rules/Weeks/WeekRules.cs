using Core.Model;
using Core.Rules.Schedule;
using Core.Rules.Tracks;

namespace Core.Rules.Weeks;

public enum ConsistencyKind
{
    OnTrack,
    Behind,
    TargetOutOfReach,
    TargetHit,
}

/// <summary>How the current week is going, with a one-line reason for the dashboard.</summary>
public sealed record ConsistencyMessage(ConsistencyKind Kind, string Message);

/// <summary>Current Streak, best-ever run of Streak weeks and the best week's counted Workouts.</summary>
public sealed record StreakResult(int Current, int BestRun, int BestWeekCount);

/// <summary>Weeks, counted Workouts, Streak and consistency, all derived from history. "Today" is always passed in.</summary>
public static class WeekRules
{
    /// <summary>Counted Workouts for <paramref name="track"/> in the week starting on <paramref name="monday"/>.</summary>
    public static int CountedWorkouts(Plan plan, Track track, IReadOnlyList<Workout> workouts, DateOnly monday) =>
        workouts.Count(w => WeekCalendar.InWeek(monday, w.Date) && IsCounted(plan, track, w));

    /// <summary>Counted Workouts per week (keyed by the week's Monday) for <paramref name="track"/> across all history. Weeks with none are absent.</summary>
    public static IReadOnlyDictionary<DateOnly, int> CountedWorkoutsByWeek(Plan plan, Track track, IReadOnlyList<Workout> workouts) =>
        workouts
            .Where(w => IsCounted(plan, track, w))
            .GroupBy(w => WeekCalendar.MondayOf(w.Date))
            .ToDictionary(g => g.Key, g => g.Count());

    /// <summary>
    /// The Streak for <paramref name="track"/> over the Plan's weeks up to and including today's: the current run of Streak weeks,
    /// the best run ever, and the most counted Workouts in any one week (Break weeks included for that last figure).
    /// Returns zeroes for a non-weekly Track.
    /// </summary>
    public static StreakResult Streak(Plan plan, Track track, IReadOnlyList<Workout> workouts, IReadOnlyList<Break> breaks, DateOnly today)
    {
        if (track.Rhythm is not TrackRhythm.Weekly weekly) return new StreakResult(0, 0, 0);

        var thisMonday = WeekCalendar.MondayOf(today);
        var countsByWeek = CountedWorkoutsByWeek(plan, track, workouts);
        var weeks = PlanMondays(plan, thisMonday)
            .Select(monday => (Monday: monday, Count: countsByWeek.GetValueOrDefault(monday)))
            .ToList();

        var bestWeekCount = weeks.Select(w => w.Count).DefaultIfEmpty(0).Max();
        var streakWeeks = weeks.Where(w => !BreakCalendar.TouchesWeek(breaks, w.Monday));
        var (current, bestRun) = Runs(streakWeeks, weekly.StreakMinWorkoutsPerWeek, thisMonday);
        return new StreakResult(current, bestRun, bestWeekCount);
    }

    /// <summary>
    /// Runs of consecutive weeks reaching the minimum, over weeks already stripped of Break weeks (a Break week neither
    /// adds nor breaks a run). A week below the minimum ends the run, except the current week: it is still in progress.
    /// </summary>
    private static (int Current, int Best) Runs(IEnumerable<(DateOnly Monday, int Count)> weeks, int minimum, DateOnly thisMonday)
    {
        var current = 0;
        var best = 0;
        foreach (var (monday, count) in weeks)
        {
            if (count >= minimum)
            {
                current++;
                best = Math.Max(best, current);
            }
            else if (monday < thisMonday)
            {
                current = 0;
            }
        }

        return (current, best);
    }

    private static IEnumerable<DateOnly> PlanMondays(Plan plan, DateOnly lastMonday)
    {
        for (var monday = WeekCalendar.MondayOf(plan.StartDate); monday <= lastMonday; monday = WeekCalendar.AddWeeks(monday, 1))
            yield return monday;
    }

    public static ConsistencyMessage Consistency(Plan plan, Track track, IReadOnlyList<Workout> workouts, IReadOnlyList<Break> breaks, DateOnly today)
    {
        if (track.Rhythm is not TrackRhythm.Weekly weekly)
            return new(ConsistencyKind.TargetHit, "No weekly target for this Track.");

        var done = CountedWorkouts(plan, track, workouts, WeekCalendar.MondayOf(today));
        var target = weekly.WeeklyTarget;
        var remaining = Math.Max(0, target - done);
        if (remaining == 0)
            return new(ConsistencyKind.TargetHit, $"Target hit: {done} of {target} done this week.");

        var daysLeft = 0;
        for (var day = today; day <= WeekCalendar.SundayOf(today); day = day.AddDays(1))
            if (!BreakCalendar.IsBreakDay(breaks, day)) daysLeft++;

        var days = daysLeft == 1 ? "day" : "days";
        if (remaining > daysLeft)
            return new(ConsistencyKind.TargetOutOfReach, $"Target out of reach this week: {remaining} to go, {daysLeft} available {days} left.");
        if (remaining == daysLeft)
            return new(ConsistencyKind.Behind, $"Behind: {remaining} to go and {daysLeft} available {days} left, so no spare day.");
        return new(ConsistencyKind.OnTrack, $"On track: {remaining} to go with {daysLeft} available {days} left.");
    }

    /// <summary>The Plan week number (1 for the week containing the start date), or null before the Plan starts.</summary>
    public static int? PlanWeekNumber(Plan plan, DateOnly date)
    {
        if (date < plan.StartDate) return null;
        return (WeekCalendar.MondayOf(date).DayNumber - WeekCalendar.MondayOf(plan.StartDate).DayNumber) / 7 + 1;
    }

    private static bool IsCounted(Plan plan, Track track, Workout w)
    {
        if (track.Rhythm is not TrackRhythm.Weekly weekly) return false;
        if (!w.Entries.Any(e => e.Status is EntryStatus.Done or EntryStatus.Swapped)) return false;
        var isMain = plan.Tracks.Count > 0 && track.Id == TrackRules.Main(plan).Id;
        if (w.SessionId is null)
            return isMain && weekly.StreakCountsCustomWorkouts;

        var session = plan.Sessions.FirstOrDefault(s => s.Id == w.SessionId);
        return session is null
            ? isMain
            : session.TrackId == track.Id && !BenchmarkWeeks.IsBenchmark(session);
    }
}
