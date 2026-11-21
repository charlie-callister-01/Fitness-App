using Core.Model;

namespace Core.Rules.Outcomes;

/// <summary>What happened to one planned row of a Workout. An <see cref="Unrecorded"/> row counts as skipped with no reason.</summary>
public enum RowOutcomeKind
{
    Done,
    Skipped,
    Swapped,
    Unrecorded,
}

/// <summary>The outcome of one planned row. <see cref="Entry"/> is the entry that matched it (null for Unrecorded). <see cref="ReplacementExerciseId"/> is set only for a swap; <see cref="SkipReason"/> only for a skip.</summary>
public sealed record RowOutcome(
    SessionExercise Row,
    RowOutcomeKind Kind,
    string? SkipReason,
    string? ReplacementExerciseId,
    WorkoutEntry? Entry);

/// <summary>
/// Every planned row's outcome (in planned order), the entries that matched no planned row (extras),
/// and whether the Workout is a Partial workout (any planned row skipped or unrecorded). A Partial workout still counts.
/// </summary>
public sealed record WorkoutOutcome(
    IReadOnlyList<RowOutcome> Rows,
    IReadOnlyList<WorkoutEntry> Extras,
    bool IsPartial);

/// <summary>The one place that answers per-row outcomes, extras and Partial workout for a Workout.</summary>
public static class WorkoutOutcomes
{
    public static WorkoutOutcome Of(Workout workout)
    {
        var remaining = workout.Entries.ToList();
        var rows = new List<RowOutcome>(workout.Planned.Count);

        foreach (var row in workout.Planned)
        {
            var index = remaining.FindIndex(e => StandsFor(e, row.ExerciseId));
            if (index < 0)
            {
                rows.Add(new RowOutcome(row, RowOutcomeKind.Unrecorded, null, null, null));
                continue;
            }

            var entry = remaining[index];
            remaining.RemoveAt(index);
            rows.Add(entry.Status switch
            {
                EntryStatus.Skipped => new RowOutcome(row, RowOutcomeKind.Skipped, entry.SkipReason, null, entry),
                EntryStatus.Swapped => new RowOutcome(row, RowOutcomeKind.Swapped, null, entry.ExerciseId, entry),
                _ => new RowOutcome(row, RowOutcomeKind.Done, null, null, entry),
            });
        }

        var isPartial = rows.Any(r => r.Kind is RowOutcomeKind.Skipped or RowOutcomeKind.Unrecorded);
        return new WorkoutOutcome(rows, remaining, isPartial);
    }

    /// <summary>The one "has Sets" rule: whether the entry holds at least one confirmed Set. A skip, an RPE-only entry and an unlogged swap do not.</summary>
    public static bool HasSets(WorkoutEntry entry) => entry.Sets.Count > 0;

    /// <summary>Whether the entry stands for the planned Exercise: a swapped entry names the planned original, every other entry the Exercise itself.</summary>
    public static bool StandsFor(WorkoutEntry entry, string plannedExerciseId) =>
        entry.Status == EntryStatus.Swapped
            ? entry.SwappedFromExerciseId == plannedExerciseId
            : entry.ExerciseId == plannedExerciseId;
}
