using Core.Model;
using Core.Rules.Editing;
using Core.Rules.LogKinds;
using Core.Rules.Parsing;
using Core.Rules.Performance;
using Core.Rules.Units;
using Core.Storage;
using Ui.Components;

namespace Ui.Log;

/// <summary>
/// Works out what the log screen shows for an open Workout from the stored data and the <see cref="LogEditState"/>: every row's
/// text and state, the header, PR markers and counts, the pager and the one primary button. It only reads; the page owns
/// saving and the state's transitions.
/// </summary>
public sealed class LogViewBuilder(DataSnapshot snapshot, LogEditState state)
{
    private readonly UnitConverter _units = new(snapshot.Settings);

    public Exercise? ExerciseOf(string id) => snapshot.Exercises.FirstOrDefault(e => e.Id == id);

    /// <summary>The first Exercise still to log, else the last one: where a reopened Workout lands.</summary>
    public int FirstIncomplete(Workout workout)
    {
        var index = workout.Planned.ToList().FindIndex(p => !IsHandled(workout, p));
        return index < 0 ? Math.Max(0, workout.Planned.Count - 1) : index;
    }

    public LogView Build(Workout workout)
    {
        var planned = workout.Planned;
        var current = state.Current;
        var exercise = planned.Count == 0 ? null : ExerciseViewOf(workout, planned[current]);
        var session = snapshot.Plans.SelectMany(p => p.Sessions).FirstOrDefault(s => s.Id == workout.SessionId);
        return new LogView(
            session?.Name ?? "Workout",
            new LogProgress(planned.Count, planned.Count(p => IsComplete(workout, p)), PrCount(workout), planned.Count(p => SkipOf(workout, p) is not null)),
            exercise,
            PrimaryOf(workout, exercise),
            [.. planned.Select(p => new ExercisePager.Chip(NameOf(p), IsComplete(workout, p), SkipOf(workout, p) is not null))],
            current < planned.Count - 1 ? NameOf(planned[current + 1]) : null);
    }

    /// <summary>The toast for the Set just confirmed, or null when it is not a PR.</summary>
    public string? ToastFor(Workout workout, string exerciseId, int row)
    {
        if (ExerciseOf(exerciseId) is not { } exercise)
        {
            return null;
        }

        var (prRow, feedback) = PrOf(workout, exercise);
        if (prRow != row || feedback.WorkoutBest is not { } best || feedback.PreviousBest is not { } previous)
        {
            return null;
        }

        return $"PR! {SetText.Result(exercise.LogKind, best, _units)} beats {SetText.Short(exercise.LogKind, previous.Set, _units)}";
    }

    // ---- Progress and the primary button

    private string NameOf(SessionExercise planned) => ExerciseOf(planned.ExerciseId)?.Name ?? "Exercise";

    private bool IsLoggable(SessionExercise planned) => ExerciseOf(planned.ExerciseId) is { } e && RowSpec.IsLoggable(e.LogKind);

    private static int Confirmed(Workout workout, string exerciseId) => WorkoutEdits.ConfirmedSets(workout, exerciseId).Count;

    /// <summary>The skip recorded for a planned Exercise, or null when it is not skipped.</summary>
    private static SkipInfo? SkipOf(Workout workout, SessionExercise planned) =>
        workout.Entries.FirstOrDefault(e => e.Status == EntryStatus.Skipped && e.ExerciseId == planned.ExerciseId) is { } entry
            ? new SkipInfo(entry.SkipReason)
            : null;

    /// <summary>Complete or skipped: nothing more to do for this Exercise.</summary>
    private bool IsHandled(Workout workout, SessionExercise planned) => IsComplete(workout, planned) || SkipOf(workout, planned) is not null;

    /// <summary>
    /// Where the log goes after an Exercise is skipped: the next Exercise (wrapping round) that is not complete or skipped, else the last
    /// one, whose primary button is then Finish.
    /// </summary>
    public int AfterSkip(Workout workout, int from)
    {
        var planned = workout.Planned;
        for (var step = 1; step < planned.Count; step++)
        {
            var index = (from + step) % planned.Count;
            if (!IsHandled(workout, planned[index]))
            {
                return index;
            }
        }

        return Math.Max(0, planned.Count - 1);
    }

    private bool IsComplete(Workout workout, SessionExercise planned) =>
        IsLoggable(planned) && Confirmed(workout, planned.ExerciseId) >= planned.Sets;

    /// <summary>
    /// Confirm Set while a row waits. Then Next to the following Exercise; from the last Exercise, Next back to the first one still
    /// to log (so Finish is never offered over unlogged Exercises), and Finish only when nothing is left. Exercises that cannot be
    /// logged (free text, a deleted Exercise) never hold Finish back.
    /// </summary>
    private PrimaryButton PrimaryOf(Workout workout, ExerciseView? exercise)
    {
        var planned = workout.Planned;
        if (exercise?.Rows?.CurrentRow is not null)
        {
            return new("Confirm Set", PrimaryAction.ConfirmSet);
        }

        if (state.Current < planned.Count - 1)
        {
            return new($"Next: {NameOf(planned[state.Current + 1])}", PrimaryAction.Next, state.Current + 1);
        }

        var unlogged = planned.ToList().FindIndex(p => IsLoggable(p) && !IsHandled(workout, p));
        return unlogged < 0
            ? new("Finish", PrimaryAction.Finish)
            : new($"Next: {NameOf(planned[unlogged])}", PrimaryAction.Next, unlogged);
    }

    // ---- One Exercise

    private ExerciseView ExerciseViewOf(Workout workout, SessionExercise planned)
    {
        if (ExerciseOf(planned.ExerciseId) is not { } exercise)
        {
            return new(new(planned.ExerciseId, "Exercise not found", LogKind.Text, null, null, false), null, null,
                "This Exercise is no longer in your library, so it cannot be shown here.", null);
        }

        var info = new ExerciseInfo(exercise.Id, exercise.Name, exercise.LogKind, PlannedText(planned), planned.Cue, exercise.IsBenchmark);
        if (SkipOf(workout, planned) is { } skip)
        {
            return new(info, null, null, null, null, skip);
        }

        if (RowSpec.For(exercise.LogKind) is not { } spec)
        {
            return new(info, null, null, "A text Exercise cannot be logged here yet, because a Set cannot hold text. Use Next to move on.", null);
        }

        var history = ExercisePerformance.Snapshot(exercise, FinishedHistory(workout), PlanOf(workout), _units);
        var rows = RowsOf(workout, exercise, planned, spec, history);
        var rpe = WorkoutEdits.FindDoneEntry(workout, exercise.Id)?.Rpe;
        return new(info, HeaderOf(exercise.LogKind, history), rows, null, rpe);
    }

    private ExerciseRows RowsOf(Workout workout, Exercise exercise, SessionExercise planned, RowSpec spec, ExerciseSnapshot history)
    {
        var confirmed = Confirmed(workout, exercise.Id);
        var extra = state.ExtraRows(exercise.Id);
        var rowCount = Math.Max(planned.Sets + extra, confirmed);
        var editingRow = state.Editing is { } editing && editing.ExerciseId == exercise.Id ? editing.Row : (int?)null;
        var current = editingRow ?? confirmed;
        var storedSets = WorkoutEdits.ConfirmedSets(workout, exercise.Id);
        var prRow = PrOf(workout, exercise).Row;
        var rows = new List<RowView>();
        for (var i = 0; i < rowCount; i++)
        {
            var key = new RowKey(exercise.Id, i);
            var stored = storedSets.ElementAtOrDefault(i);
            var isDone = i < confirmed && editingRow != i;
            var prefill = isDone && stored is not null
                ? RowPrefill.FromStored(exercise.LogKind, stored, _units) ?? RowPrefillValues.Empty()
                : RowPrefill.For(exercise.LogKind, i + 1, history, planned, _units);
            var edit = state.EditOf(key);
            var rowState = isDone ? SetRowState.Done : i == current ? SetRowState.Current : SetRowState.Upcoming;

            // An added Set starts as "same as the one above".
            var (first, second) = !isDone && i >= planned.Sets && rows.Count > 0
                ? (rows[^1].Face.First, rows[^1].Face.Second)
                : (prefill.First, prefill.Second);
            var face = new RowFace(
                edit?.First ?? first, edit?.Second ?? second,
                spec.FirstHint(_units) ?? (spec.Shape == RowShape.Wide ? prefill.RepsHint : null),
                spec.Shape == RowShape.Pair ? prefill.RepsHint : null);
            var marks = new RowMarks(
                Pending: !isDone && !(edit?.Typed ?? false), Stored: stored is not null,
                IsMax: stored?.IsMax ?? edit?.Max ?? false, IsPr: isDone && prRow == i);
            rows.Add(new RowView(i, rowState, face, marks, state.Error?.Row == key));
        }

        var step = rows.Any(r => r.State == SetRowState.Current)
            ? spec.StepFor(_units, PlanOf(workout)?.WeightIncrementKg ?? snapshot.Settings.WeightIncrementKg)
            : null;
        int? maxTarget = editingRow ?? (confirmed < rowCount ? confirmed : confirmed > 0 ? confirmed - 1 : null);
        int? removable = extra > 0 && editingRow is null && rowCount > confirmed && rowCount > 1 ? rowCount - 1 : null;
        return new ExerciseRows(spec, rows, new RowTools(step, maxTarget, removable));
    }

    private Plan? PlanOf(Workout workout) =>
        snapshot.Plans.FirstOrDefault(p => p.Sessions.Any(s => s.Id == workout.SessionId))
            ?? snapshot.Plans.FirstOrDefault(p => p.IsActive);

    /// <summary>Every other finished Workout: Last, the Suggested target and PRs never come from the open one.</summary>
    private IEnumerable<Workout> FinishedHistory(Workout workout) =>
        snapshot.Workouts.Where(w => w.Id != workout.Id && w.EndedAt is not null);

    private HeaderText HeaderOf(LogKind kind, ExerciseSnapshot history)
    {
        var last = history.Last is { } l && (l.BestSet ?? l.Sets.LastOrDefault()) is { } lastSet ? SetText.Result(kind, lastSet, _units) : null;
        var best = history.Best is { } b ? SetText.Result(kind, b.Set, _units) : null;
        var target = history.Target is { } t ? SetText.Target(kind, t, _units) : null;
        return new HeaderText(last, best, target, target is null ? null : history.Target?.Reason);
    }

    private static string? PlannedText(SessionExercise planned) => planned switch
    {
        { Reps: { } low, RepsMax: { } high } when high > low => $"{planned.Sets} × {low}-{high}",
        { Reps: { } reps } => $"{planned.Sets} × {reps}",
        _ => null,
    };

    // ---- PR feedback (C-03, 8.3): derived from the Sets as they are now, never stored.

    /// <summary>
    /// The rule's answer for one Exercise. The Set being reopened is not confirmed any more, so it is left out until it is
    /// confirmed again.
    /// </summary>
    private (int? Row, SetPrFeedback Feedback) PrOf(Workout workout, Exercise exercise)
    {
        var sets = WorkoutEdits.ConfirmedSets(workout, exercise.Id);
        var reopened = state.Editing is { } editing && editing.ExerciseId == exercise.Id ? editing.Row : -1;
        var feedback = SetPrFeedbackRule.For(exercise, FinishedHistory(workout), sets.Where((_, i) => i != reopened));
        var row = feedback.IsPr && feedback.WorkoutBest is { } best ? sets.ToList().FindIndex(s => s.Id == best.Id) : -1;
        return (row < 0 ? null : row, feedback);
    }

    private int PrCount(Workout workout) =>
        workout.Planned.Select(p => p.ExerciseId).Distinct()
            .Select(ExerciseOf).OfType<Exercise>()
            .Count(e => RowSpec.IsLoggable(e.LogKind) && PrOf(workout, e).Feedback.IsPr);
}
