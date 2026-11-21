using Core.Ids;
using Core.Model;
using Core.Rules.Phases;
using Core.Time;

namespace Core.Rules.Starting;

/// <summary>Starting an in-progress Custom workout, and resolving its planned rows.</summary>
public static class CustomWorkoutStart
{
    /// <summary>
    /// A new in-progress Custom workout for <paramref name="plan"/> on <paramref name="date"/>: a fresh ID, StartedAt now,
    /// no EndedAt, SessionId null, LoggedAfterwards false, no entries and no Pain check.
    /// <see cref="Workout.Planned"/> is set to a copy of <paramref name="rows"/>.
    /// </summary>
    public static Workout Create(Plan plan, IReadOnlyList<SessionExercise> rows, DateOnly date, IIdGenerator ids, IClock clock) =>
        new(ids.NewId(), date, null, false, clock.UtcNow, null, [.. rows], [], null);

    /// <summary>
    /// Builds the rows for Exercises picked from the library: for each id in <paramref name="exerciseIds"/> (order kept,
    /// duplicates dropped keeping the first), the row of that Exercise in the active Plan (search the Plan's Sessions
    /// using <see cref="PhaseRules.ExerciseRows(Plan, Phase?, string)"/> so the current Phase's rows apply; first match wins)
    /// with its Sets, Reps, RepsMax and Cue kept; if the Exercise is in no Session, a plain row SessionExercise(id, 3, null, null, null).
    /// </summary>
    public static IReadOnlyList<SessionExercise> RowsForExercises(Plan plan, IReadOnlyList<string> exerciseIds, DateOnly today)
    {
        var distinctIds = exerciseIds.Distinct().ToList();
        var currentPhase = PhaseRules.CurrentPhase(plan, today);
        var sessionRows = plan.Sessions
            .Select(s => PhaseRules.ExerciseRows(plan, currentPhase, s.Id))
            .ToList();

        var result = new List<SessionExercise>(distinctIds.Count);
        foreach (var id in distinctIds)
        {
            var match = sessionRows.SelectMany(r => r).FirstOrDefault(r => r.ExerciseId == id);
            result.Add(match ?? new SessionExercise(id, 3, null, null, null));
        }

        return result;
    }

    /// <summary>
    /// A copy of <paramref name="session"/>'s rows as in force that week (<see cref="PhaseRules.ExerciseRows(Plan, string, DateOnly)"/>).
    /// </summary>
    public static IReadOnlyList<SessionExercise> RowsForMakeUp(Plan plan, Session session, DateOnly today) =>
        [.. PhaseRules.ExerciseRows(plan, session.Id, today)];
}
