using Core.Ids;
using Core.Model;

namespace Core.Rules.Finishing;

/// <summary>
/// The pure rules for finishing a Workout: the Pain check answer, Finish (with the Partial choice) and re-dating a Workout as
/// logged afterwards. Each returns a new Workout; the ones that can be refused return an <see cref="EditResult"/>.
/// </summary>
public static class WorkoutFinishing
{
    public const string NoSetRefusal = "Confirm at least one Set before finishing.";
    public const string FutureDateRefusal = "A Workout cannot be dated in the future.";
    public const string PainDetailsRefusal = "Choose at least one body area and a severity.";

    /// <summary>Answers the Pain check as "none": no areas, severity, Exercise or note.</summary>
    public static Workout WithPainNone(Workout workout, IIdGenerator ids) =>
        workout with { PainCheck = new PainCheck(PainId(workout, ids), PainStatus.None, [], null, null, null) };

    /// <summary>Answers the Pain check as reported. Refused without at least one area and a severity.</summary>
    public static EditResult WithPainReported(
        Workout workout, IReadOnlyList<BodyArea> areas, PainSeverity? severity, string? exerciseId, string? note, IIdGenerator ids) =>
        areas.Count == 0 || severity is null
            ? EditResult.Refused(PainDetailsRefusal)
            : EditResult.Changed(workout with
            {
                PainCheck = new PainCheck(PainId(workout, ids), PainStatus.Reported, areas, severity, note, exerciseId),
            });

    /// <summary>Returns the Pain check to unanswered (which is not the same as "none").</summary>
    public static Workout WithPainCleared(Workout workout) => workout with { PainCheck = null };

    /// <summary>
    /// Stamps <paramref name="endedAt"/>. A Workout with no confirmed Set is refused. An already finished Workout is returned
    /// unchanged. With <paramref name="partial"/>, every planned row with no entry (or only an empty Done entry) first becomes a
    /// skipped entry with no reason (an RPE on an empty entry is dropped), and added Exercises with no Sets are removed.
    /// </summary>
    public static EditResult Finish(Workout workout, bool partial, DateTimeOffset endedAt)
    {
        if (workout.EndedAt is not null)
        {
            return EditResult.Changed(workout);
        }

        if (!workout.Entries.Any(e => e.Sets.Count > 0))
        {
            return EditResult.Refused(NoSetRefusal);
        }

        var finished = partial ? RecordUnfinishedRows(workout) : workout;
        return EditResult.Changed(finished with { EndedAt = endedAt });
    }

    /// <summary>Re-dates the Workout as logged afterwards: the flag set, <paramref name="date"/> (today or earlier) and no start time.</summary>
    public static EditResult LoggedAfterwards(Workout workout, DateOnly date, DateOnly today) =>
        date > today
            ? EditResult.Refused(FutureDateRefusal)
            : EditResult.Changed(workout with { LoggedAfterwards = true, Date = date, StartedAt = null });

    /// <summary>Undoes <see cref="LoggedAfterwards"/>: the flag cleared and the date back to <paramref name="today"/>.</summary>
    public static Workout UndoLoggedAfterwards(Workout workout, DateOnly today) =>
        workout with { LoggedAfterwards = false, Date = today };

    private static string PainId(Workout workout, IIdGenerator ids) => workout.PainCheck?.Id ?? ids.NewId();

    private static Workout RecordUnfinishedRows(Workout workout)
    {
        var entries = workout.Entries.ToList();
        var matched = new HashSet<int>();
        var added = new List<WorkoutEntry>();

        foreach (var row in workout.Planned)
        {
            var index = Enumerable.Range(0, entries.Count).FirstOrDefault(i => !matched.Contains(i) && Matches(entries[i], row), -1);
            if (index < 0)
            {
                added.Add(Skipped(row.ExerciseId));
                continue;
            }

            matched.Add(index);
            if (IsEmptyDone(entries[index]))
            {
                entries[index] = Skipped(row.ExerciseId);
            }
        }

        var kept = entries.Where((e, i) => matched.Contains(i) || !IsEmptyDone(e));
        return workout with { Entries = [.. kept, .. added] };
    }

    private static WorkoutEntry Skipped(string exerciseId) =>
        new(exerciseId, EntryStatus.Skipped, null, null, null, []);

    private static bool IsEmptyDone(WorkoutEntry entry) => entry.Status == EntryStatus.Done && entry.Sets.Count == 0;

    // A swapped entry names the planned original; every other entry names the planned Exercise itself.
    private static bool Matches(WorkoutEntry entry, SessionExercise row) =>
        entry.Status == EntryStatus.Swapped
            ? entry.SwappedFromExerciseId == row.ExerciseId
            : entry.ExerciseId == row.ExerciseId;
}
