using Core.Model;
using Core.Storage;

namespace Ui.Saving;

/// <summary>
/// Saves changes to one Workout. Each change is applied to the snapshot's latest copy of the Workout (never a copy held since an
/// earlier render, which other changes may have replaced), committed at once, and the outcome reported to the save-failure notice.
/// </summary>
public sealed class WorkoutWriter(AppData data, SaveFailureNotice notice)
{
    /// <summary>The commit's result, or null when the Workout is no longer there (the notice then says so).</summary>
    public async Task<CommitResult?> ChangeAsync(string workoutId, Func<Workout, Workout> change)
    {
        if (data.Snapshot.Workouts.FirstOrDefault(w => w.Id == workoutId) is not { } latest)
        {
            notice.ReportMessage("That Workout is no longer open, so nothing was saved.");
            return null;
        }

        var result = await data.CommitAsync(Change.Put(change(latest)));
        notice.Report(result);
        return result;
    }
}
