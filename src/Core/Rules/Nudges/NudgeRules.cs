using Core.Model;
using Core.Rules.Schedule;
using Core.Rules.Weeks;

namespace Core.Rules.Nudges;

public enum NudgeKind
{
    RepeatedPain,
    MissedSession,
    OneShortOfTarget,
    NoRecentBackup,
}

/// <summary>An in-app message with a stable kind and the data it refers to: the Session for a missed session, the body area for repeated pain.</summary>
public sealed record Nudge(NudgeKind Kind, string Message, string? SessionId = null, BodyArea? Area = null);

/// <summary>Nudges, computed from data plus an explicit "today". Nothing is stored.</summary>
public static class NudgeRules
{
    public const int BackupNudgeDays = 14;

    /// <summary>Days into the week (Monday = 0) from which the "one short of target" Nudge shows: Thursday (spec assumption 7).</summary>
    private const int OneShortFromDayIndex = 3;

    /// <summary>
    /// The Nudges for <paramref name="today"/>, most important first: repeated pain (by area), Missed sessions (by date),
    /// one short of target, no Backup. Empty on a Break day. A null <paramref name="lastBackup"/> means never backed up.
    /// </summary>
    public static IReadOnlyList<Nudge> Compute(
        Plan plan, IReadOnlyList<WeekSchedule> schedules, IReadOnlyList<Break> breaks,
        IReadOnlyList<Workout> workouts, DateTimeOffset? lastBackup, DateOnly today)
    {
        if (breaks.Any(b => today >= b.StartDate && today <= b.EndDate)) return [];

        var monday = WeekCalendar.MondayOf(today);
        var nudges = new List<Nudge>();
        nudges.AddRange(RepeatedPain(workouts, monday));
        nudges.AddRange(Missed(plan, schedules, breaks, workouts, today));

        var done = WeekRules.CountedWorkouts(plan, workouts, monday);
        if (today.DayNumber - monday.DayNumber >= OneShortFromDayIndex && done == plan.WeeklyTarget - 1)
            nudges.Add(new(NudgeKind.OneShortOfTarget,
                $"One workout short of your target of {plan.WeeklyTarget} this week."));

        if (lastBackup is null)
            nudges.Add(new(NudgeKind.NoRecentBackup, "No Backup yet. Make one so you don't lose your data."));
        else if (today.DayNumber - DateOnly.FromDateTime(lastBackup.Value.DateTime).DayNumber >= BackupNudgeDays)
            nudges.Add(new(NudgeKind.NoRecentBackup, $"No Backup in {BackupNudgeDays} days. Make one so you don't lose your data."));

        return nudges;
    }

    private static IEnumerable<Nudge> RepeatedPain(IReadOnlyList<Workout> workouts, DateOnly monday)
    {
        var sunday = monday.AddDays(6);
        return workouts
            .Where(w => w.Date >= monday && w.Date <= sunday && w.PainCheck is { Status: PainStatus.Reported })
            .SelectMany(w => w.PainCheck!.Areas.Distinct().Select(a => (w.Id, Area: a)))
            .GroupBy(x => x.Area)
            .Where(g => g.Select(x => x.Id).Distinct().Count() >= 2)
            .OrderBy(g => g.Key)
            .Select(g => new Nudge(NudgeKind.RepeatedPain,
                $"{g.Key} pain reported twice this week. Consider easing off.", Area: g.Key));
    }

    private static IEnumerable<Nudge> Missed(
        Plan plan, IReadOnlyList<WeekSchedule> schedules, IReadOnlyList<Break> breaks,
        IReadOnlyList<Workout> workouts, DateOnly today) =>
        WeekScheduleRules.MissedInWeek(plan, schedules, breaks, workouts, today, today)
            .OrderBy(m => m.Date)
            .Select(m =>
            {
                var name = plan.Sessions.First(s => s.Id == m.SessionId).Name;
                return new Nudge(NudgeKind.MissedSession, $"You missed {name} on {m.Date:dddd}. Make it up this week?", m.SessionId);
            });
}
