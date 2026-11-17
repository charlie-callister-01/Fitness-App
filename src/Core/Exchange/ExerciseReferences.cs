using Core.Model;

namespace Core.Exchange;

/// <summary>
/// The one walk over every place a document names an Exercise (Plan rows, Phase goals, overrides, Workout planned rows, entries,
/// swapped-from and Pain checks, Guides) and a Badge metric. Paths match the A1 validator's, so findings point at the same field.
/// </summary>
internal static class ExerciseReferences
{
    /// <summary>
    /// Returns the document with each Exercise ID replaced by <paramref name="exercise"/>(path, id) and each Badge metric by <paramref name="metric"/>.
    /// Only the Exercise-referencing collections are rebuilt; everything else is carried over as is.
    /// </summary>
    public static ExchangeDocument Rewrite(ExchangeDocument d, Func<string, string, string> exercise, Func<string, string> metric) => d with
    {
        Badges = d.Badges?.Select(b => b with { Metric = metric(b.Metric) }).ToList(),
        Guides = d.Guides?.Select((g, i) => g with { ExerciseId = exercise($"guides[{i}].exerciseId", g.ExerciseId) }).ToList(),
        Plans = d.Plans?.Select((p, i) => Plan(p, $"plans[{i}]", exercise)).ToList(),
        Workouts = d.Workouts?.Select((w, i) => Workout(w, $"workouts[{i}]", exercise)).ToList(),
    };

    /// <summary>Every Exercise ID the document names, with the path it sits at, in document order.</summary>
    public static IReadOnlyList<(string Path, string Id)> Paths(ExchangeDocument d)
    {
        var found = new List<(string Path, string Id)>();
        Rewrite(d, (path, id) => { found.Add((path, id)); return id; }, metric => metric);
        return found;
    }

    private static Plan Plan(Plan plan, string path, Func<string, string, string> exercise) => plan with
    {
        Sessions = plan.Sessions.Select((s, i) => s with { Exercises = Rows(s.Exercises, $"{path}.sessions[{i}].exercises", exercise) }).ToList(),
        Phases = plan.Phases.Select((ph, i) => Phase(ph, $"{path}.phases[{i}]", exercise)).ToList(),
    };

    private static Phase Phase(Phase phase, string path, Func<string, string, string> exercise) => phase with
    {
        Goals = phase.Goals.Select((g, i) => g with { ExerciseId = exercise($"{path}.goals[{i}].exerciseId", g.ExerciseId) }).ToList(),
        SessionOverrides = phase.SessionOverrides
            .Select((o, i) => o with { Exercises = Rows(o.Exercises, $"{path}.sessionOverrides[{i}].exercises", exercise) }).ToList(),
    };

    private static Workout Workout(Workout w, string path, Func<string, string, string> exercise) => w with
    {
        Planned = Rows(w.Planned, $"{path}.planned", exercise),
        Entries = w.Entries.Select((e, i) => Entry(e, $"{path}.entries[{i}]", exercise)).ToList(),
        PainCheck = w.PainCheck is { ExerciseId: { } id } pain ? pain with { ExerciseId = exercise($"{path}.painCheck.exerciseId", id) } : w.PainCheck,
    };

    private static WorkoutEntry Entry(WorkoutEntry e, string path, Func<string, string, string> exercise) => e with
    {
        ExerciseId = exercise($"{path}.exerciseId", e.ExerciseId),
        SwappedFromExerciseId = e.SwappedFromExerciseId is { } from ? exercise($"{path}.swappedFromExerciseId", from) : null,
    };

    private static List<SessionExercise> Rows(IReadOnlyList<SessionExercise> rows, string path, Func<string, string, string> exercise) =>
        rows.Select((r, i) => r with { ExerciseId = exercise($"{path}[{i}].exerciseId", r.ExerciseId) }).ToList();
}
