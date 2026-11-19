using Core.Ids;
using Core.Model;
using Core.Rules.Finishing;
using Core.Storage;
using Core.Time;
using Ui.Saving;

namespace Ui.Log;

public enum FinishPhase
{
    /// <summary>Nothing is open: the log, or (once the Workout has an end time) the summary.</summary>
    None,
    PartialPrompt,
    PainCheck,
}

/// <summary>How the Pain check was answered. Skipped records nothing, so the Pain check stays "not answered".</summary>
public abstract record PainAnswer
{
    public sealed record No : PainAnswer;

    public sealed record Skipped : PainAnswer;

    public sealed record Yes(IReadOnlyList<BodyArea> Areas, PainSeverity? Severity, string? ExerciseId, string? Side, string? Note) : PainAnswer;
}

/// <summary>
/// The finish flow of the log: the Partial prompt, the finish write and the Pain check. The end time is committed before the Pain check
/// opens, so a killed tab leaves a finished Workout with an unanswered Pain check, never an open one; the summary is then what the
/// page shows for any Workout with an end time, and it never asks the Pain check again. Every write goes through
/// <see cref="WorkoutWriter"/>, and a failed one reports to the banner and leaves the flow where it was.
/// </summary>
public sealed class FinishFlow(AppData data, SaveFailureNotice notice, IClock clock, IIdGenerator ids)
{
    private readonly WorkoutWriter _writer = new(data, notice);

    public FinishPhase Phase { get; private set; }

    /// <summary>Plain wording for why Finish did nothing (no confirmed Set), shown on the log.</summary>
    public string? Message { get; private set; }

    /// <summary>The Pain check's plain message when Yes was saved without an area and a severity.</summary>
    public string? PainMessage { get; private set; }

    public bool Saving { get; private set; }

    /// <summary>What the Partial prompt says: how many planned Exercises are done, and how many there are.</summary>
    public (int Done, int Planned) PartialCounts { get; private set; }

    public event Action? Changed;

    /// <summary>The end time was committed (the Workout is no longer open).</summary>
    public event Action? Finished;

    /// <summary>Starts over for another Workout.</summary>
    public void Reset()
    {
        Phase = FinishPhase.None;
        Message = null;
        PainMessage = null;
        Saving = false;
    }

    /// <summary>Finish was tapped: a refusal message, the Partial prompt, or the finish write.</summary>
    public async Task RequestAsync(string workoutId, bool canWrite)
    {
        if (Saving || !canWrite || Find(workoutId) is not { EndedAt: null } workout)
        {
            return;
        }

        Message = null;
        var check = FinishCheck.Of(workout);
        if (check.Refusal is not null)
        {
            Message = check.Refusal;
            Changed?.Invoke();
        }
        else if (check.WouldBePartial)
        {
            PartialCounts = (check.DoneCount, check.PlannedCount);
            Phase = FinishPhase.PartialPrompt;
            Changed?.Invoke();
        }
        else
        {
            await FinishAsync(workoutId, partial: false);
        }
    }

    public void KeepGoing()
    {
        Phase = FinishPhase.None;
        Changed?.Invoke();
    }

    public async Task ConfirmPartialAsync(string workoutId, bool canWrite)
    {
        if (Saving || !canWrite || Phase != FinishPhase.PartialPrompt)
        {
            return;
        }

        await FinishAsync(workoutId, partial: true);
    }

    /// <summary>Records the Pain check answer. A failed save keeps the Pain check open; Skip records nothing.</summary>
    public async Task AnswerAsync(string workoutId, PainAnswer answer, bool canWrite)
    {
        if (Saving || !canWrite || Phase != FinishPhase.PainCheck || Find(workoutId) is not { } workout)
        {
            return;
        }

        PainMessage = null;
        Func<Workout, Workout>? change = null;
        switch (answer)
        {
            case PainAnswer.No:
                change = w => WorkoutFinishing.WithPainNone(w, ids);
                break;
            case PainAnswer.Yes yes:
                var note = PainNote.Compose(yes.Side, yes.Note);
                var result = WorkoutFinishing.WithPainReported(workout, yes.Areas, yes.Severity, yes.ExerciseId, note, ids);
                if (result.IsRefused)
                {
                    PainMessage = result.Refusal;
                    Changed?.Invoke();
                    return;
                }

                change = w => WorkoutFinishing.WithPainReported(w, yes.Areas, yes.Severity, yes.ExerciseId, note, ids).Workout ?? w;
                break;
        }

        if (change is null || await SaveAsync(workoutId, change) is CommitResult.Committed)
        {
            Phase = FinishPhase.None;
            Changed?.Invoke();
        }
    }

    private async Task FinishAsync(string workoutId, bool partial)
    {
        var committed = await SaveAsync(workoutId, w => WorkoutFinishing.Finish(w, partial, clock.UtcNow).Workout ?? w);
        if (committed is CommitResult.Committed)
        {
            Phase = FinishPhase.PainCheck;
            Changed?.Invoke();
            Finished?.Invoke();
        }
        else
        {
            Phase = FinishPhase.None;
            Changed?.Invoke();
        }
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
