using Core.Ids;
using Core.Model;
using Core.Rules.Parsing;

namespace Core.Rules.Editing;

/// <summary>
/// The changes the log makes to an open Workout, as pure functions returning a new Workout. Each works on the Exercise's
/// <see cref="EntryStatus.Done"/> entry (the only kind B1 writes), creating it when the first Set or RPE for the Exercise arrives.
/// </summary>
public static class WorkoutEdits
{
    /// <summary>The entry holding the Sets logged for an Exercise (done, or swapped in), or null before the first one.</summary>
    public static WorkoutEntry? FindDoneEntry(Workout workout, string exerciseId) =>
        workout.Entries.FirstOrDefault(e => e.ExerciseId == exerciseId && IsLogged(e));

    /// <summary>The confirmed Sets of an Exercise, in order; empty before the first one.</summary>
    public static IReadOnlyList<WorkoutSet> ConfirmedSets(Workout workout, string exerciseId) =>
        FindDoneEntry(workout, exerciseId)?.Sets ?? [];

    /// <summary>
    /// Stores <paramref name="parsed"/> as the Exercise's Set number <paramref name="row"/> (counting from 0). An existing row keeps its
    /// ID and Max flag; a row past the end appends a new Set with a fresh ID and <paramref name="isMax"/>.
    /// </summary>
    public static Workout WithSet(Workout workout, string exerciseId, int row, ParsedSet parsed, bool isMax, IIdGenerator ids) =>
        Update(workout, exerciseId, entry =>
        {
            var sets = entry.Sets.ToList();
            if (row < sets.Count)
            {
                sets[row] = parsed.ToWorkoutSet(sets[row].Id, sets[row].IsMax);
            }
            else
            {
                sets.Add(parsed.ToWorkoutSet(ids.NewId(), isMax));
            }

            return entry with { Sets = sets };
        });

    /// <summary>Flags or unflags a confirmed Set as a Max set; the Workout is returned unchanged when there is no such Set.</summary>
    public static Workout WithMax(Workout workout, string exerciseId, int row, bool isMax)
    {
        if (row < 0 || row >= ConfirmedSets(workout, exerciseId).Count)
        {
            return workout;
        }

        return Update(workout, exerciseId, entry =>
        {
            var sets = entry.Sets.ToList();
            sets[row] = sets[row] with { IsMax = isMax };
            return entry with { Sets = sets };
        });
    }

    /// <summary>Sets or clears (null) the Exercise's RPE. With no Set yet this creates an empty Done entry to hold it.</summary>
    public static Workout WithRpe(Workout workout, string exerciseId, int? rpe) =>
        Update(workout, exerciseId, entry => entry with { Rpe = rpe });

    /// <summary>
    /// Records the planned Exercise as skipped with an optional reason. Any confirmed Sets, RPE, earlier skip or swap for it are replaced.
    /// </summary>
    public static Workout Skip(Workout workout, string exerciseId, string? reason) =>
        ReplacePlannedEntry(workout, exerciseId, new WorkoutEntry(exerciseId, EntryStatus.Skipped, reason, null, null, []));

    /// <summary>Takes back a skip, returning the Exercise to not-yet-done. Unchanged when it is not skipped.</summary>
    public static Workout Unskip(Workout workout, string exerciseId) => RemovePlannedEntry(workout, exerciseId, EntryStatus.Skipped);

    /// <summary>
    /// Replaces the planned Exercise with <paramref name="newExerciseId"/> at the same position, recording the original; the planned rows
    /// are untouched. Any earlier entry for the row is replaced. Refused (the Workout is returned unchanged) when the row is not planned
    /// or the new Exercise is already in the Workout.
    /// </summary>
    public static Workout Swap(Workout workout, string plannedExerciseId, string newExerciseId)
    {
        if (workout.Planned.All(p => p.ExerciseId != plannedExerciseId) || IsInWorkout(workout, newExerciseId))
        {
            return workout;
        }

        return ReplacePlannedEntry(
            workout, plannedExerciseId, new WorkoutEntry(newExerciseId, EntryStatus.Swapped, null, plannedExerciseId, null, []));
    }

    /// <summary>Takes back a swap, restoring the original as not-yet-done. Unchanged when it is not swapped.</summary>
    public static Workout UndoSwap(Workout workout, string plannedExerciseId) =>
        RemovePlannedEntry(workout, plannedExerciseId, EntryStatus.Swapped);

    /// <summary>
    /// Adds an empty entry for <paramref name="newExerciseId"/> right after the entry of <paramref name="afterExerciseId"/> (at the end
    /// when that Exercise has no entry yet). It matches no planned row, so the outcomes rule reports it as an extra. Refused (unchanged)
    /// when the Exercise is already in the Workout.
    /// </summary>
    public static Workout AddExercise(Workout workout, string afterExerciseId, string newExerciseId)
    {
        if (IsInWorkout(workout, newExerciseId))
        {
            return workout;
        }

        var entries = workout.Entries.ToList();
        var after = entries.FindIndex(e => e.ExerciseId == afterExerciseId);
        entries.Insert(after < 0 ? entries.Count : after + 1, new WorkoutEntry(newExerciseId, EntryStatus.Done, null, null, null, []));
        return workout with { Entries = entries };
    }

    /// <summary>Removes an added Exercise that has no Sets. Unchanged for a planned Exercise or one with Sets.</summary>
    public static Workout RemoveAdded(Workout workout, string exerciseId)
    {
        var isAdded = workout.Planned.All(p => p.ExerciseId != exerciseId);
        var entries = workout.Entries.ToList();
        var index = entries.FindIndex(e => e.ExerciseId == exerciseId && e.Status == EntryStatus.Done && e.Sets.Count == 0);
        if (!isAdded || index < 0)
        {
            return workout;
        }

        entries.RemoveAt(index);
        return workout with { Entries = entries };
    }

    // Sets are logged into a Done entry, or into a swapped entry (the replacement Exercise is logged like any other).
    private static bool IsLogged(WorkoutEntry entry) => entry.Status is EntryStatus.Done or EntryStatus.Swapped;

    private static bool IsInWorkout(Workout workout, string exerciseId) =>
        workout.Planned.Any(p => p.ExerciseId == exerciseId) || workout.Entries.Any(e => e.ExerciseId == exerciseId);

    // The entry that stands for a planned row: the row's own entry, or the swapped entry naming it as the original.
    private static bool StandsFor(WorkoutEntry entry, string plannedExerciseId) =>
        entry.Status == EntryStatus.Swapped ? entry.SwappedFromExerciseId == plannedExerciseId : entry.ExerciseId == plannedExerciseId;

    private static Workout ReplacePlannedEntry(Workout workout, string plannedExerciseId, WorkoutEntry replacement)
    {
        var entries = workout.Entries.ToList();
        var index = entries.FindIndex(e => StandsFor(e, plannedExerciseId));
        if (index < 0)
        {
            entries.Add(replacement);
        }
        else
        {
            entries[index] = replacement;
        }

        return workout with { Entries = entries };
    }

    private static Workout RemovePlannedEntry(Workout workout, string plannedExerciseId, EntryStatus status)
    {
        var entries = workout.Entries.ToList();
        var removed = entries.RemoveAll(e => e.Status == status && StandsFor(e, plannedExerciseId));
        return removed == 0 ? workout : workout with { Entries = entries };
    }

    private static Workout Update(Workout workout, string exerciseId, Func<WorkoutEntry, WorkoutEntry> change)
    {
        var entries = workout.Entries.ToList();
        var index = entries.FindIndex(e => e.ExerciseId == exerciseId && IsLogged(e));
        if (index < 0)
        {
            entries.Add(change(new WorkoutEntry(exerciseId, EntryStatus.Done, null, null, null, [])));
        }
        else
        {
            entries[index] = change(entries[index]);
        }

        return workout with { Entries = entries };
    }
}
