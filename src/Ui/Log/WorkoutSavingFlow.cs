using Core.Model;
using Core.Rules.Finishing;
using Core.Storage;
using Ui.Saving;

namespace Ui.Log;

/// <summary>
/// The plumbing the log's multi-step flows share: the <see cref="GuardedSave"/>, the <see cref="Saving"/> state and <see cref="Changed"/>
/// event, the can-act guard and the one way to save. A flow keeps only its own phase state and rules; every write goes through
/// <see cref="WriteAsync"/>, so a rule's refusal is produced inside the single merge against the latest Workout.
/// </summary>
public abstract class WorkoutSavingFlow
{
    private readonly AppData _data;
    private readonly GuardedSave _save;
    private readonly WorkoutWriter _writer;

    protected WorkoutSavingFlow(AppData data, SaveFailureNotice notice)
    {
        _data = data;
        _save = new GuardedSave(data, notice);
        _writer = new WorkoutWriter(data, _save);
        _save.Started += RaiseChanged;
    }

    public bool Saving => _save.IsBusy;

    public event Action? Changed;

    /// <summary>Whether a tap should do anything: no save is running and this window can write.</summary>
    protected bool CanAct() => _save.CanAct;

    /// <summary>This window can write.</summary>
    protected bool CanWrite => _data.Health.CanWrite;

    /// <summary>The open Workout's latest stored copy, or null when it is gone or already has an end time.</summary>
    protected Workout? FindOpen(string workoutId) =>
        _data.Snapshot.Workouts.FirstOrDefault(w => w.Id == workoutId) is { EndedAt: null } open ? open : null;

    protected void RaiseChanged() => Changed?.Invoke();

    /// <summary>Saves the edit (see <see cref="WorkoutWriter.EditAsync"/>) under the guard; <see cref="Saving"/> is true while it runs.</summary>
    protected Task<WriteOutcome> WriteAsync(string workoutId, Func<Workout, EditResult> edit) => _writer.EditAsync(workoutId, edit);
}
