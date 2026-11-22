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
/// page shows for any Workout with an end time, and it never asks the Pain check again. Every write goes through the shared
/// <see cref="WorkoutSavingFlow.WriteAsync"/>: a refusal comes back from it with its message, and a failed save reports to the banner and
/// leaves the flow where it was.
/// </summary>
public sealed class FinishFlow(AppData data, SaveFailureNotice notice, IClock clock, IIdGenerator ids) : WorkoutSavingFlow(data, notice)
{
    public FinishPhase Phase { get; private set; }

    /// <summary>Plain wording for why Finish did nothing (no confirmed Set), shown on the log.</summary>
    public string? Message { get; private set; }

    /// <summary>The Pain check's plain message when Yes was saved without an area and a severity.</summary>
    public string? PainMessage { get; private set; }

    /// <summary>What the Partial prompt says: how many planned Exercises are done, and how many there are.</summary>
    public (int Done, int Planned) PartialCounts { get; private set; }

    /// <summary>The end time was committed (the Workout is no longer open).</summary>
    public event Action? Finished;

    /// <summary>Starts over for another Workout.</summary>
    public void Reset()
    {
        Phase = FinishPhase.None;
        Message = null;
        PainMessage = null;
    }

    /// <summary>Finish was tapped: the Partial prompt when planned Exercises are not done, else the finish write (which refuses with no confirmed Set).</summary>
    public async Task RequestAsync(string workoutId)
    {
        if (!CanAct() || FindOpen(workoutId) is not { } workout)
        {
            return;
        }

        Message = null;
        var check = FinishCheck.Of(workout);
        if (check.WouldBePartial)
        {
            PartialCounts = (check.DoneCount, check.PlannedCount);
            Phase = FinishPhase.PartialPrompt;
            RaiseChanged();
        }
        else
        {
            await FinishAsync(workoutId, partial: false);
        }
    }

    public void KeepGoing()
    {
        Phase = FinishPhase.None;
        RaiseChanged();
    }

    public async Task ConfirmPartialAsync(string workoutId)
    {
        if (!CanAct() || Phase != FinishPhase.PartialPrompt)
        {
            return;
        }

        await FinishAsync(workoutId, partial: true);
    }

    /// <summary>Records the Pain check answer. A refused or failed save keeps the Pain check open; Skip records nothing, so it needs no write.</summary>
    public async Task AnswerAsync(string workoutId, PainAnswer answer)
    {
        if (Saving || Phase != FinishPhase.PainCheck || (answer is not PainAnswer.Skipped && !CanWrite))
        {
            return;
        }

        PainMessage = null;
        if (answer is not PainAnswer.Skipped)
        {
            var outcome = await WriteAsync(workoutId, w => answer is PainAnswer.Yes yes
                ? WorkoutFinishing.WithPainReported(w, yes.Areas, yes.Severity, yes.ExerciseId, PainNote.Compose(yes.Side, yes.Note), ids)
                : EditResult.Changed(WorkoutFinishing.WithPainNone(w, ids)));
            if (!outcome.IsCommitted)
            {
                PainMessage = outcome.Refusal;
                RaiseChanged();
                return;
            }
        }

        Phase = FinishPhase.None;
        RaiseChanged();
    }

    private async Task FinishAsync(string workoutId, bool partial)
    {
        var outcome = await WriteAsync(workoutId, w => WorkoutFinishing.Finish(w, partial, clock.UtcNow));
        Message = outcome.Refusal;
        if (outcome.IsCommitted)
        {
            Phase = FinishPhase.PainCheck;
            RaiseChanged();
            Finished?.Invoke();
        }
        else
        {
            Phase = FinishPhase.None;
            RaiseChanged();
        }
    }
}
