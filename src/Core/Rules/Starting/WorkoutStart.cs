using Core.Ids;
using Core.Model;
using Core.Rules.Phases;
using Core.Rules.Schedule;
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

    /// <summary>The Workout with no end time, or null. There is at most one; if data ever holds more, the first is returned.</summary>
    public static Workout? InProgress(IEnumerable<Workout> workouts) => workouts.FirstOrDefault(w => w.EndedAt is null);

    /// <summary>
    /// The Session Start offers: among this week's scheduled Sessions with no Workout from them this week, the one due
    /// today or, failing that, the next one due later this week; when only earlier (Missed) ones remain, the earliest of
    /// those. Null when there is no active Plan or nothing is left to start.
    /// </summary>
    public static ScheduledSession? NextToStart(
        Plan? plan, IReadOnlyList<WeekSchedule> schedules, IReadOnlyList<Break> breaks,
        IReadOnlyList<Workout> workouts, DateOnly today)
    {
        if (plan is not { IsActive: true }) return null;

        var layout = WeekScheduleRules.Layout(plan, schedules, breaks, today);
        var doneThisWeek = workouts
            .Where(w => WeekCalendar.InWeek(layout.Monday, w.Date))
            .Select(w => w.SessionId).OfType<string>().ToHashSet();
        var undone = layout.Sessions.Where(s => !doneThisWeek.Contains(s.SessionId)).ToList();

        return undone.FirstOrDefault(s => s.Date >= today) ?? undone.FirstOrDefault();
    }
}
