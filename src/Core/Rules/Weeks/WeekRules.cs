using Core.Model;

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
    /// <summary>Counted Workouts in the week starting on <paramref name="monday"/>.</summary>
    public static int CountedWorkouts(Plan plan, IReadOnlyList<Workout> workouts, DateOnly monday)
    {
        var sunday = monday.AddDays(6);
        return workouts.Count(w => w.Date >= monday && w.Date <= sunday && IsCounted(plan, w));
    }

    public static StreakResult Streak(Plan plan, IReadOnlyList<Workout> workouts, IReadOnlyList<Break> breaks, DateOnly today)
    {
        var thisMonday = WeekCalendar.MondayOf(today);
        var run = 0;
        var best = 0;
        var bestWeek = 0;

        for (var monday = WeekCalendar.MondayOf(plan.StartDate); monday <= thisMonday; monday = WeekCalendar.AddWeeks(monday, 1))
        {
            var count = CountedWorkouts(plan, workouts, monday);
            bestWeek = Math.Max(bestWeek, count);
            if (HasBreakDay(breaks, monday)) continue;

            if (count >= plan.StreakMinWorkoutsPerWeek)
            {
                run++;
                best = Math.Max(best, run);
            }
            else if (monday < thisMonday)
            {
                run = 0;
            }
            // A current week below the minimum is still in progress: it neither adds nor breaks.
        }

        return new StreakResult(run, best, bestWeek);
    }

    public static ConsistencyMessage Consistency(Plan plan, IReadOnlyList<Workout> workouts, IReadOnlyList<Break> breaks, DateOnly today)
    {
        var done = CountedWorkouts(plan, workouts, WeekCalendar.MondayOf(today));
        var remaining = Math.Max(0, plan.WeeklyTarget - done);
        if (remaining == 0)
            return new(ConsistencyKind.TargetHit, $"Target hit: {done} of {plan.WeeklyTarget} done this week.");

        var daysLeft = 0;
        for (var day = today; day <= WeekCalendar.SundayOf(today); day = day.AddDays(1))
            if (!IsBreakDay(breaks, day)) daysLeft++;

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

    private static bool IsCounted(Plan plan, Workout w)
    {
        if (!w.Entries.Any(e => e.Status is EntryStatus.Done or EntryStatus.Swapped)) return false;
        if (w.SessionId is null) return plan.StreakCountsCustomWorkouts;
        var session = plan.Sessions.FirstOrDefault(s => s.Id == w.SessionId);
        return session is not { Type: SessionType.Test, BenchmarkIntervalWeeks: not null };
    }

    private static bool IsBreakDay(IReadOnlyList<Break> breaks, DateOnly day) =>
        breaks.Any(b => day >= b.StartDate && day <= b.EndDate);

    private static bool HasBreakDay(IReadOnlyList<Break> breaks, DateOnly monday) =>
        breaks.Any(b => b.StartDate <= monday.AddDays(6) && b.EndDate >= monday);
}
