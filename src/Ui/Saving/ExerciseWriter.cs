using Core.Model;
using Core.Storage;

namespace Ui.Saving;

/// <summary>
/// Saves a change to one library Exercise (the Exercise menu's benchmark flag). As for <see cref="WorkoutWriter"/>, the change is applied to
/// the snapshot's latest copy, committed at once and the outcome reported to the save-failure notice.
/// </summary>
public sealed class ExerciseWriter(AppData data, SaveFailureNotice notice)
{
    /// <summary>The commit's result, or null when the Exercise is no longer in the library (the notice then says so).</summary>
    public async Task<CommitResult?> ChangeAsync(string exerciseId, Func<Exercise, Exercise> change)
    {
        if (data.Snapshot.Exercises.FirstOrDefault(e => e.Id == exerciseId) is not { } latest)
        {
            notice.ReportMessage("That Exercise is no longer in your library, so nothing was saved.");
            return null;
        }

        var result = await data.CommitAsync(Change.Put(change(latest)));
        notice.Report(result);
        return result;
    }
}
