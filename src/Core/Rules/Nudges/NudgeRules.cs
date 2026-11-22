using System.Globalization;
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

/// <summary>An in-app message with a stable kind and the data it refers to: the Session for a missed session, the body area for repeated pain, or the Track for per-Track weekly Nudges.</summary>
public sealed record Nudge(NudgeKind Kind, string Message, string? SessionId = null, BodyArea? Area = null, string? TrackId = null);

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

        var weeklyTracks = plan.Tracks.Where(t => t.Rhythm is TrackRhythm.Weekly).ToList();
        var nameTrack = weeklyTracks.Count > 1;

        var monday = WeekCalendar.MondayOf(today);
        var nudges = new List<Nudge>();
        nudges.AddRange(RepeatedPain(workouts, monday));
        nudges.AddRange(Missed(plan, weeklyTracks, nameTrack, schedules, breaks, workouts, today));

        if (today.DayNumber - monday.DayNumber >= OneShortFromDayIndex)
        {
            foreach (var track in weeklyTracks)
            {
                var weekly = (TrackRhythm.Weekly)track.Rhythm;
                if (WeekRules.CountedWorkouts(plan, track, workouts, monday) != weekly.WeeklyTarget - 1) continue;
                var message = nameTrack
                    ? $"One workout short of your {track.Name} target of {weekly.WeeklyTarget} this week."
                    : $"One workout short of your target of {weekly.WeeklyTarget} this week.";
                nudges.Add(new(NudgeKind.OneShortOfTarget, message, TrackId: track.Id));
            }
        }

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
                $"{g.Key} pain reported twice this week. Consider an easier variation.", Area: g.Key));
    }

    /// <summary>The distinct body areas a Workout's Pain check reported, each tagged with the Workout. None when no pain was reported.</summary>
    private static IEnumerable<(string Id, BodyArea Area)> ReportedAreas(Workout workout)
    {
        if (workout.PainCheck is not { Status: PainStatus.Reported } pain) yield break;
        foreach (var area in pain.Areas.Distinct()) yield return (workout.Id, area);
    }

    private static IEnumerable<Nudge> Missed(
        Plan plan, IReadOnlyList<Track> weeklyTracks, bool nameTrack,
        IReadOnlyList<WeekSchedule> schedules, IReadOnlyList<Break> breaks,
        IReadOnlyList<Workout> workouts, DateOnly today) =>
        weeklyTracks
            .SelectMany(track => WeekScheduleRules.MissedInWeek(plan, track, schedules, breaks, workouts, today, today)
                .Select(m => (Track: track, Missed: m)))
            .OrderBy(x => x.Missed.Date)
            .Select(x =>
            {
                var message = nameTrack
                    ? $"You missed {x.Missed.SessionName} ({x.Track.Name}) on {x.Missed.Date.ToString("dddd", CultureInfo.InvariantCulture)}. Make it up this week?"
                    : $"You missed {x.Missed.SessionName} on {x.Missed.Date.ToString("dddd", CultureInfo.InvariantCulture)}. Make it up this week?";
                return new Nudge(NudgeKind.MissedSession, message, x.Missed.SessionId, TrackId: x.Track.Id);
            });
}
