using Core.Model;
using Core.Rules.Finishing;
using Core.Storage;

namespace Ui.Saving;

/// <summary>
/// What an edit that can be refused came to: <see cref="Save"/> is how the guarded save ended, and <see cref="Refusal"/> is the plain
/// reason when the edit's rule refused it (nothing was committed, and it is not a save failure).
/// </summary>
public sealed record WriteOutcome(SaveResult Save, string? Refusal)
{
    public bool IsCommitted => Save.IsSaved;
}

/// <summary>
/// Saves changes to one Workout. Each change is applied to the snapshot's latest copy of the Workout (never a copy held since an
/// earlier render, which other changes may have replaced) and committed through the <see cref="GuardedSave"/>.
/// </summary>
public sealed class WorkoutWriter(AppData data, GuardedSave save)
{
    /// <summary>How the save ended; <see cref="SaveStatus.Gone"/> when the Workout is no longer there (the notice then says so).</summary>
    public async Task<SaveResult> ChangeAsync(string workoutId, Func<Workout, Workout> change) =>
        (await EditAsync(workoutId, w => EditResult.Changed(change(w)))).Save;

    /// <summary>
    /// Applies an edit that may be refused. The rule runs once, against the latest copy: a refusal commits nothing and comes back in
    /// the outcome for the caller to show (it is not a save failure, so the notice is left alone).
    /// </summary>
    public async Task<WriteOutcome> EditAsync(string workoutId, Func<Workout, EditResult> edit)
    {
        string? refusal = null;
        var result = await save.RunAsync(async () =>
        {
            if (data.Snapshot.Workouts.FirstOrDefault(w => w.Id == workoutId) is not { } latest)
            {
                return SaveWork.Gone("That Workout is no longer open, so nothing was saved.");
            }

            var edited = edit(latest);
            if (edited.Workout is not { } changed)
            {
                refusal = edited.Refusal;
                return SaveWork.None;
            }

            return SaveWork.Committed(await data.CommitAsync(Change.Put(changed)));
        });
        return new WriteOutcome(result, refusal);
    }
}
