using Core.Model;
using Core.Rules.Finishing;
using Core.Storage;

namespace Ui.Saving;

/// <summary>
/// What an edit that can be refused came to: committed (<see cref="Commit"/> is the commit's result), refused (<see cref="Refusal"/> is the
/// plain reason, nothing was committed), or neither because the Workout is gone (<see cref="Commit"/> is null).
/// </summary>
public sealed record WriteOutcome(CommitResult? Commit, string? Refusal)
{
    public bool IsCommitted => Commit is CommitResult.Committed;
}

/// <summary>
/// Saves changes to one Workout. Each change is applied to the snapshot's latest copy of the Workout (never a copy held since an
/// earlier render, which other changes may have replaced), committed at once, and the outcome reported to the save-failure notice.
/// </summary>
public sealed class WorkoutWriter(AppData data, SaveFailureNotice notice)
{
    /// <summary>The commit's result, or null when the Workout is no longer there (the notice then says so).</summary>
    public async Task<CommitResult?> ChangeAsync(string workoutId, Func<Workout, Workout> change) =>
        (await EditAsync(workoutId, w => EditResult.Changed(change(w)))).Commit;

    /// <summary>
    /// Applies an edit that may be refused. The rule runs once, against the latest copy: a refusal commits nothing and comes back in
    /// the outcome for the caller to show (it is not a save failure, so the notice is left alone).
    /// </summary>
    public async Task<WriteOutcome> EditAsync(string workoutId, Func<Workout, EditResult> edit)
    {
        if (data.Snapshot.Workouts.FirstOrDefault(w => w.Id == workoutId) is not { } latest)
        {
            notice.ReportMessage("That Workout is no longer open, so nothing was saved.");
            return new WriteOutcome(null, null);
        }

        var result = edit(latest);
        if (result.Workout is not { } changed)
        {
            return new WriteOutcome(null, result.Refusal);
        }

        var commit = await data.CommitAsync(Change.Put(changed));
        notice.Report(commit);
        return new WriteOutcome(commit, null);
    }
}
