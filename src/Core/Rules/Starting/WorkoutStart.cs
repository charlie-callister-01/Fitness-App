using Core.Ids;
using Core.Model;
using Core.Rules.Phases;
using Core.Rules.Schedule;
using Core.Rules.Tracks;
using Core.Rules.Weeks;
using Core.Time;

namespace Core.Rules.Starting;

/// <summary>Starting a Workout from a Session, and finding the one in progress. "Today" is always passed in.</summary>
public static class WorkoutStart
{
    /// <summary>
    /// A new in-progress Workout for <paramref name="session"/> on <paramref name="date"/>: a fresh ID, StartedAt now, no
    /// EndedAt and no entries. <see cref="Workout.Planned"/> is a copy of the Session's rows as in force that week (the
    /// covering Phase's Override when it has one, else the Plan default), so later Plan edits never change it.
    /// </summary>
    public static Workout Create(Plan plan, Session session, DateOnly date, IIdGenerator ids, IClock clock) =>
        new(ids.NewId(), date, session.Id, false, clock.UtcNow, null,
            [.. PhaseRules.ExerciseRows(plan, session.Id, date)], [], null);

    /// <summary>
    /// A new Workout logged afterwards for <paramref name="date"/>: the flag set, no start or end time (so no duration), and
    /// <see cref="Workout.Planned"/> as for <see cref="Create"/>. With no <paramref name="session"/> it is a Custom workout with an empty plan.
    /// </summary>
    public static Workout CreatePastDated(Plan? plan, Session? session, DateOnly date, IIdGenerator ids) =>
        new(ids.NewId(), date, session?.Id, true, null, null,
            plan is null || session is null ? [] : [.. PhaseRules.ExerciseRows(plan, session.Id, date)], [], null);

    /// <summary>The Workout with no end time, or null. There is at most one; if data ever holds more, the first is returned.</summary>
    public static Workout? InProgress(IEnumerable<Workout> workouts) => workouts.FirstOrDefault(w => w.EndedAt is null);

    /// <summary>
    /// The Session Start offers for <paramref name="track"/>: among this week's scheduled Sessions with no Workout from
    /// them this week, the one due today or, failing that, the next one due later this week; when only earlier (Missed)
    /// ones remain, the earliest of those. Null when there is no active Plan, no weekly Track, or nothing left to start.
    /// </summary>
    public static ScheduledSession? NextToStart(
        Plan? plan, Track? track, IReadOnlyList<WeekSchedule> schedules, IReadOnlyList<Break> breaks,
        IReadOnlyList<Workout> workouts, DateOnly today)
    {
        if (plan is not { IsActive: true } || track is not { Rhythm: TrackRhythm.Weekly }) return null;

        var layout = WeekScheduleRules.Layout(plan, track, schedules, breaks, today);
        var doneThisWeek = workouts
            .Where(w => WeekCalendar.InWeek(layout.Monday, w.Date))
            .Select(w => w.SessionId).OfType<string>().ToHashSet();
        var undone = layout.Sessions.Where(s => !doneThisWeek.Contains(s.SessionId)).ToList();

        return undone.FirstOrDefault(s => s.Date >= today) ?? undone.FirstOrDefault();
    }
}
