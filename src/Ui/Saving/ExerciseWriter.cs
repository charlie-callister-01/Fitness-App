using Core.Model;
using Core.Storage;

namespace Ui.Saving;

/// <summary>
/// Saves a change to one library Exercise (the Exercise menu's benchmark flag). As for <see cref="WorkoutWriter"/>, the change is applied to
/// the snapshot's latest copy and committed through the <see cref="GuardedSave"/>.
/// </summary>
public sealed class ExerciseWriter(AppData data, GuardedSave save)
{
    /// <summary>How the save ended; <see cref="SaveStatus.Gone"/> when the Exercise is no longer in the library (the notice then says so).</summary>
    public Task<SaveResult> ChangeAsync(string exerciseId, Func<Exercise, Exercise> change) =>
        save.RunAsync(async () =>
            data.Snapshot.Exercises.FirstOrDefault(e => e.Id == exerciseId) is { } latest
                ? SaveWork.Committed(await data.CommitAsync(Change.Put(change(latest))))
                : SaveWork.Gone("That Exercise is no longer in your library, so nothing was saved."));
}
