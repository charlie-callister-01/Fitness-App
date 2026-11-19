using Core.Model;
using Core.Rules.Finishing;
using Core.Storage;
using Ui.Saving;

namespace Ui.Log;

/// <summary>
/// Re-dating the open Workout as Logged afterwards, and undoing it (design 8.6). The date rule is Core's
/// (<see cref="WorkoutFinishing.LoggedAfterwards"/>); this holds the date sheet's open state and refusal message. Every write goes
/// through <see cref="WorkoutWriter"/>: a failed one reports to the banner and leaves the sheet open and the Workout as it was.
/// </summary>
public sealed class LoggedAfterwardsFlow(AppData data, SaveFailureNotice notice)
{
    private readonly WorkoutWriter _writer = new(data, notice);

    public bool Open { get; private set; }

    /// <summary>Plain wording for why the date was refused (a day after today), shown in the sheet.</summary>
    public string? Message { get; private set; }

    public bool Saving { get; private set; }

    public event Action? Changed;

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
        Changed?.Invoke();
    }

    public void Cancel()
    {
        Open = false;
        Message = null;
        Changed?.Invoke();
    }

    /// <summary>Re-dates to <paramref name="date"/>. A future date is refused with the message; the sheet then stays open.</summary>
    public async Task SaveAsync(string workoutId, DateOnly date, DateOnly today, bool canWrite)
    {
        if (Saving || !canWrite || Find(workoutId) is not { EndedAt: null } workout)
        {
            return;
        }

        var check = WorkoutFinishing.LoggedAfterwards(workout, date, today);
        Message = check.Refusal;
        if (check.IsRefused)
        {
            Changed?.Invoke();
            return;
        }

        if (await SaveAsync(workoutId, w => WorkoutFinishing.LoggedAfterwards(w, date, today).Workout ?? w) is CommitResult.Committed)
        {
            Open = false;
            Redated?.Invoke();
        }

        Changed?.Invoke();
    }

    /// <summary>Takes the flag off and puts today's date back (the start time is not restored).</summary>
    public async Task UndoAsync(string workoutId, DateOnly today, bool canWrite)
    {
        if (Saving || !canWrite || Find(workoutId) is not { EndedAt: null })
        {
            return;
        }

        await SaveAsync(workoutId, w => WorkoutFinishing.UndoLoggedAfterwards(w, today));
        Changed?.Invoke();
    }

    private async Task<CommitResult?> SaveAsync(string workoutId, Func<Workout, Workout> change)
    {
        Saving = true;
        Changed?.Invoke();
        try
        {
            return await _writer.ChangeAsync(workoutId, change);
        }
        finally
        {
            Saving = false;
        }
    }

    private Workout? Find(string workoutId) => data.Snapshot.Workouts.FirstOrDefault(w => w.Id == workoutId);
}
