using Core.Rules.Finishing;
using Core.Storage;
using Ui.Saving;

namespace Ui.Log;

/// <summary>
/// Re-dating the open Workout as Logged afterwards, and undoing it (design 8.6). The date rule is Core's
/// (<see cref="WorkoutFinishing.LoggedAfterwards"/>); this holds the date sheet's open state and refusal message. Every write goes
/// through the shared <see cref="WorkoutSavingFlow.WriteAsync"/>: a failed one reports to the banner and leaves the sheet open and the
/// Workout as it was.
/// </summary>
public sealed class LoggedAfterwardsFlow(AppData data, SaveFailureNotice notice) : WorkoutSavingFlow(data, notice)
{
    public bool Open { get; private set; }

    /// <summary>Plain wording for why the date was refused (a day after today), shown in the sheet.</summary>
    public string? Message { get; private set; }

    /// <summary>The Workout was re-dated (the rest countdown has no meaning any more).</summary>
    public event Action? Redated;

    public void Reset()
    {
        Open = false;
        Message = null;
        Saving = false;
    }

    public void Begin()
    {
        Open = true;
        Message = null;
        RaiseChanged();
    }

    public void Cancel()
    {
        Open = false;
        Message = null;
        RaiseChanged();
    }

    /// <summary>Re-dates to <paramref name="date"/>. A future date is refused with the message; the sheet then stays open.</summary>
    public async Task SaveAsync(string workoutId, DateOnly date, DateOnly today, bool canWrite)
    {
        if (!CanAct(canWrite) || FindOpen(workoutId) is null)
        {
            return;
        }

        var outcome = await WriteAsync(workoutId, w => WorkoutFinishing.LoggedAfterwards(w, date, today));
        Message = outcome.Refusal;
        if (outcome.IsCommitted)
        {
            Open = false;
            Redated?.Invoke();
        }

        RaiseChanged();
    }

    /// <summary>Takes the flag off and puts today's date back (the start time is not restored).</summary>
    public async Task UndoAsync(string workoutId, DateOnly today, bool canWrite)
    {
        if (!CanAct(canWrite) || FindOpen(workoutId) is null)
        {
            return;
        }

        await WriteAsync(workoutId, w => EditResult.Changed(WorkoutFinishing.UndoLoggedAfterwards(w, today)));
        RaiseChanged();
    }
}
