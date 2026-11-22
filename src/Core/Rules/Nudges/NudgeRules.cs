using Core.Model;
using Core.Rules.Schedule;
using Core.Rules.Tracks;
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
        if (BreakCalendar.IsBreakDay(breaks, today)) return [];

        var monday = WeekCalendar.MondayOf(today);
        var nudges = new List<Nudge>();
        nudges.AddRange(RepeatedPain(workouts, monday));
        nudges.AddRange(Missed(plan, schedules, breaks, workouts, today));

        var main = TrackRules.Main(plan);
        var done = WeekRules.CountedWorkouts(plan, main, workouts, monday);
        if (main.Rhythm is TrackRhythm.Weekly weekly
            && today.DayNumber - monday.DayNumber >= OneShortFromDayIndex
            && done == weekly.WeeklyTarget - 1)
            nudges.Add(new(NudgeKind.OneShortOfTarget,
                $"One workout short of your target of {weekly.WeeklyTarget} this week."));

        if (lastBackup is null)
            nudges.Add(new(NudgeKind.NoRecentBackup, "No Backup yet. Make one so you don't lose your data."));
        else if (today.DayNumber - BackupDay(lastBackup.Value).DayNumber >= BackupNudgeDays)
            nudges.Add(new(NudgeKind.NoRecentBackup, $"No Backup in {BackupNudgeDays} days. Make one so you don't lose your data."));

        return nudges;
    }

    /// <summary>
    /// The calendar day a Backup was made on: its date in the offset it was stamped with (the owner's local clock at the time),
    /// never converted to UTC or to the device's current zone. The 14-day comparison is then whole calendar days against "today".
    /// </summary>
    private static DateOnly BackupDay(DateTimeOffset backup) => DateOnly.FromDateTime(backup.DateTime);

    private static IEnumerable<Nudge> RepeatedPain(IReadOnlyList<Workout> workouts, DateOnly monday)
    {
        return workouts
            .Where(w => WeekCalendar.InWeek(monday, w.Date))
            .SelectMany(ReportedAreas)
            .GroupBy(x => x.Area)
            .Where(g => g.Select(x => x.Id).Distinct().Count() >= 2)
            .OrderBy(g => g.Key)
            .Select(g => new Nudge(NudgeKind.RepeatedPain,
                $"{g.Key} pain reported twice this week. Consider easing off.", Area: g.Key));
    }

    /// <summary>The distinct body areas a Workout's Pain check reported, each tagged with the Workout. None when no pain was reported.</summary>
    private static IEnumerable<(string Id, BodyArea Area)> ReportedAreas(Workout workout)
    {
        if (workout.PainCheck is not { Status: PainStatus.Reported } pain) yield break;
        foreach (var area in pain.Areas.Distinct()) yield return (workout.Id, area);
    }

    private static IEnumerable<Nudge> Missed(
        Plan plan, IReadOnlyList<WeekSchedule> schedules, IReadOnlyList<Break> breaks,
        IReadOnlyList<Workout> workouts, DateOnly today) =>
        WeekScheduleRules.MissedInWeek(plan, TrackRules.Main(plan), schedules, breaks, workouts, today, today)
            .OrderBy(m => m.Date)
            .Select(m => new Nudge(
                NudgeKind.MissedSession, $"You missed {m.SessionName} on {m.Date:dddd}. Make it up this week?", m.SessionId));
}
