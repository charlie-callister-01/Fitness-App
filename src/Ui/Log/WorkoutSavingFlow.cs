using Core.Model;
using Core.Rules.Finishing;
using Core.Storage;
using Ui.Saving;

namespace Ui.Log;

/// <summary>
/// The plumbing the log's multi-step flows share: the writer, the <see cref="Saving"/> flag and <see cref="Changed"/> event, the can-act
/// guard and the one way to save. A flow keeps only its own phase state and rules; every write goes through
/// <see cref="WriteAsync"/>, so a rule's refusal is produced inside the single merge against the latest Workout.
/// </summary>
public abstract class WorkoutSavingFlow(AppData data, SaveFailureNotice notice)
{
    private readonly WorkoutWriter _writer = new(data, notice);

    public bool Saving { get; protected set; }

    public event Action? Changed;

    /// <summary>Whether a tap should do anything: no save is running and this window can write.</summary>
    protected bool CanAct(bool canWrite) => !Saving && canWrite;

    /// <summary>The open Workout's latest stored copy, or null when it is gone or already has an end time.</summary>
    protected Workout? FindOpen(string workoutId) =>
        data.Snapshot.Workouts.FirstOrDefault(w => w.Id == workoutId) is { EndedAt: null } open ? open : null;

    protected void RaiseChanged() => Changed?.Invoke();

    /// <summary>Saves the edit (see <see cref="WorkoutWriter.EditAsync"/>) with <see cref="Saving"/> set around it.</summary>
    protected async Task<WriteOutcome> WriteAsync(string workoutId, Func<Workout, EditResult> edit)
    {
        Saving = true;
        RaiseChanged();
        try
        {
            return await _writer.EditAsync(workoutId, edit);
        }
        finally
        {
            Saving = false;
        }
    }
}
