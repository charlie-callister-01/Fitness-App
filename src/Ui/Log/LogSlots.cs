using Core.Model;
using Core.Rules.Outcomes;

namespace Ui.Log;

/// <summary>
/// One place in the log's pager: a planned row (<see cref="SwappedFromId"/> set when another Exercise stands in for it, in which case
/// <see cref="Row"/> is the stand-in with the planned Sets and reps kept) or an Exercise added during the Workout.
/// </summary>
public sealed record LogSlot(SessionExercise Row, string? SwappedFromId = null, bool Added = false)
{
    /// <summary>The planned Exercise this place belongs to: Skip, Swap and Undo act on it. For an added Exercise it is its own.</summary>
    public string PlannedId => SwappedFromId ?? Row.ExerciseId;
}

/// <summary>
/// The places the log shows for a Workout, in order: every planned row (swapped ones as their replacement) with the added Exercises
/// after the row whose entry they follow in the stored entries. "Added" is the outcomes rule's extras, so nothing extra is stored.
/// An Exercise added where no earlier row has an entry yet goes after the first row.
/// </summary>
public static class LogSlots
{
    public static IReadOnlyList<LogSlot> Of(Workout workout)
    {
        var outcome = WorkoutOutcomes.Of(workout);
        var anchors = AnchorsOf(workout, outcome);
        var slots = new List<LogSlot>();
        for (var i = 0; i < outcome.Rows.Count; i++)
        {
            var row = outcome.Rows[i];
            slots.Add(row is { Kind: RowOutcomeKind.Swapped, ReplacementExerciseId: { } replacement }
                ? new LogSlot(row.Row with { ExerciseId = replacement, Cue = null }, row.Row.ExerciseId)
                : new LogSlot(row.Row));
            slots.AddRange(AddedAfter(i, outcome.Extras, anchors));
        }

        if (outcome.Rows.Count == 0)
        {
            slots.AddRange(AddedAfter(-1, outcome.Extras, anchors));
        }

        return slots;
    }

    private static IEnumerable<LogSlot> AddedAfter(int row, IReadOnlyList<WorkoutEntry> extras, IReadOnlyDictionary<WorkoutEntry, int> anchors) =>
        extras.Where(e => anchors[e] == row).Select(e => new LogSlot(new SessionExercise(e.ExerciseId, 1, null, null, null), Added: true));

    // The planned row each extra follows: the row owning the nearest earlier entry that belongs to a planned row.
    private static Dictionary<WorkoutEntry, int> AnchorsOf(Workout workout, WorkoutOutcome outcome)
    {
        var rowOfEntry = new Dictionary<int, int>();
        var used = new HashSet<int>();
        for (var i = 0; i < outcome.Rows.Count; i++)
        {
            var at = Enumerable.Range(0, workout.Entries.Count)
                .FirstOrDefault(n => !used.Contains(n) && Stands(workout.Entries[n], outcome.Rows[i].Row), -1);
            if (at >= 0)
            {
                used.Add(at);
                rowOfEntry[at] = i;
            }
        }

        var anchors = new Dictionary<WorkoutEntry, int>(ReferenceEqualityComparer.Instance);
        foreach (var extra in outcome.Extras)
        {
            var position = IndexOfReference(workout.Entries, extra);
            var before = rowOfEntry.Where(p => p.Key < position).Select(p => (Entry: p.Key, Row: p.Value)).OrderByDescending(p => p.Entry).ToList();
            anchors[extra] = before.Count > 0 ? before[0].Row : Math.Min(0, outcome.Rows.Count - 1);
        }

        return anchors;
    }

    private static int IndexOfReference(IReadOnlyList<WorkoutEntry> entries, WorkoutEntry entry)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            if (ReferenceEquals(entries[i], entry))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool Stands(WorkoutEntry entry, SessionExercise row) =>
        entry.Status == EntryStatus.Swapped ? entry.SwappedFromExerciseId == row.ExerciseId : entry.ExerciseId == row.ExerciseId;
}
