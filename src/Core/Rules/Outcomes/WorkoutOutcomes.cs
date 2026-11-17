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

/// <summary>The outcome of one planned row. <see cref="ReplacementExerciseId"/> is set only for a swap; <see cref="SkipReason"/> only for a skip.</summary>
public sealed record RowOutcome(
    SessionExercise Row,
    RowOutcomeKind Kind,
    string? SkipReason,
    string? ReplacementExerciseId);

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
            var index = remaining.FindIndex(e => Matches(e, row));
            if (index < 0)
            {
                rows.Add(new RowOutcome(row, RowOutcomeKind.Unrecorded, null, null));
                continue;
            }

            var entry = remaining[index];
            remaining.RemoveAt(index);
            rows.Add(entry.Status switch
            {
                EntryStatus.Skipped => new RowOutcome(row, RowOutcomeKind.Skipped, entry.SkipReason, null),
                EntryStatus.Swapped => new RowOutcome(row, RowOutcomeKind.Swapped, null, entry.ExerciseId),
                _ => new RowOutcome(row, RowOutcomeKind.Done, null, null),
            });
        }

        var isPartial = rows.Any(r => r.Kind is RowOutcomeKind.Skipped or RowOutcomeKind.Unrecorded);
        return new WorkoutOutcome(rows, remaining, isPartial);
    }

    // A swapped entry names the planned original; every other entry names the planned Exercise itself.
    private static bool Matches(WorkoutEntry entry, SessionExercise row) =>
        entry.Status == EntryStatus.Swapped
            ? entry.SwappedFromExerciseId == row.ExerciseId
            : entry.ExerciseId == row.ExerciseId;
}
