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
    /// <summary>The entry holding the Sets done for an Exercise, or null before the first one.</summary>
    public static WorkoutEntry? FindDoneEntry(Workout workout, string exerciseId) =>
        workout.Entries.FirstOrDefault(e => e.ExerciseId == exerciseId && e.Status == EntryStatus.Done);

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

    private static Workout Update(Workout workout, string exerciseId, Func<WorkoutEntry, WorkoutEntry> change)
    {
        var entries = workout.Entries.ToList();
        var index = entries.FindIndex(e => e.ExerciseId == exerciseId && e.Status == EntryStatus.Done);
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
