using Core.Ids;
using Core.Model;
using Core.Rules.Outcomes;

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
    /// unchanged. Added Exercises with no Sets are always removed. With <paramref name="partial"/>,
    /// every planned row with no entry (or only an empty one, an unlogged swap included) first becomes a skipped entry for the
    /// planned Exercise with no reason (an RPE on an empty entry is dropped).
    /// </summary>
    public static EditResult Finish(Workout workout, bool partial, DateTimeOffset endedAt)
    {
        if (workout.EndedAt is not null)
        {
            return EditResult.Changed(workout);
        }

        if (!workout.Entries.Any(WorkoutOutcomes.HasSets))
        {
            return EditResult.Refused(NoSetRefusal);
        }

        var finished = FinishRows(workout, partial);
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

    // Added Exercises with no Sets are always dropped. With partial, every planned row with no entry, or only an empty one (no Sets:
    // a Done entry, an RPE-only entry or an unlogged swap), becomes a skipped entry for the planned Exercise with no reason.
    private static Workout FinishRows(Workout workout, bool partial)
    {
        var outcome = WorkoutOutcomes.Of(workout);
        var dropped = outcome.Extras.Where(IsEmpty).ToHashSet(ReferenceEqualityComparer.Instance);
        var emptyRows = partial
            ? outcome.Rows.Where(r => r.Entry is { } e && IsEmpty(e)).ToDictionary(r => (object)r.Entry!, r => r.Row.ExerciseId, ReferenceEqualityComparer.Instance)
            : [];

        var entries = new List<WorkoutEntry>();
        foreach (var entry in workout.Entries)
        {
            if (!dropped.Contains(entry))
            {
                entries.Add(emptyRows.TryGetValue(entry, out var plannedId) ? Skipped(plannedId) : entry);
            }
        }

        if (partial)
        {
            entries.AddRange(outcome.Rows.Where(r => r.Kind == RowOutcomeKind.Unrecorded).Select(r => Skipped(r.Row.ExerciseId)));
        }

        return workout with { Entries = entries };
    }

    private static WorkoutEntry Skipped(string exerciseId) =>
        new(exerciseId, EntryStatus.Skipped, null, null, null, []);

    private static bool IsEmpty(WorkoutEntry entry) => entry.Status != EntryStatus.Skipped && !WorkoutOutcomes.HasSets(entry);
}
