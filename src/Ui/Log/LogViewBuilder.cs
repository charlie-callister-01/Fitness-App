using Core.Model;
using Core.Rules.Outcomes;
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
public sealed class LogViewBuilder(AppData data, LogEditState state)
{
    private DataSnapshot Snapshot => data.Snapshot;

    private UnitConverter Units => new(Snapshot.Settings);

    /// <summary>Where each place in the log stands: complete, skipped, swapped, added; and where the pager goes next.</summary>
    public LogStanding Standing { get; } = new(data);

    private Exercise? ExerciseOf(string id) => Standing.ExerciseOf(id);

    public LogView Build(Workout workout)
    {
        var planned = LogSlots.Of(workout);
        var current = state.Current;
        var exercise = planned.Count == 0 ? null : ExerciseViewOf(workout, planned[current]);
        var session = Snapshot.Plans.SelectMany(p => p.Sessions).FirstOrDefault(s => s.Id == workout.SessionId);
        return new LogView(
            session?.Name ?? "Workout",
            Standing.ProgressOf(workout, planned, PrCount(workout)),
            exercise,
            PrimaryOf(workout, exercise),
            Standing.ChipsOf(workout, planned),
            current < planned.Count - 1 ? Standing.NameOf(planned[current + 1].Row) : null);
    }

    /// <summary>The Exercises of the finished Workout, for the Pain check's "During which Exercise?".</summary>
    public IReadOnlyList<(string Id, string Name)> PainExercises(Workout? finished) =>
        finished is null ? [] : [.. finished.Entries.Select(e => e.ExerciseId).Distinct().Select(id => (id, ExerciseOf(id)?.Name ?? "Exercise"))];

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

        return $"PR! {SetText.Result(exercise.LogKind, best, Units)} beats {SetText.Short(exercise.LogKind, previous.Set, Units)}";
    }

    // ---- Progress and the primary button

    /// <summary>
    /// Confirm Set while a row waits. Then Next to the following Exercise; from the last Exercise, Next back to the first one still
    /// to log (so Finish is never offered over unlogged Exercises), and Finish only when nothing is left. Exercises that cannot be
    /// logged (free text, a deleted Exercise) never hold Finish back.
    /// </summary>
    private PrimaryButton PrimaryOf(Workout workout, ExerciseView? exercise)
    {
        var planned = LogSlots.Of(workout);
        if (exercise?.Rows?.CurrentRow is not null)
        {
            return new("Confirm Set", PrimaryAction.ConfirmSet);
        }

        if (state.Current < planned.Count - 1)
        {
            return new($"Next: {Standing.NameOf(planned[state.Current + 1].Row)}", PrimaryAction.Next, state.Current + 1);
        }

        var unlogged = planned.ToList().FindIndex(s => Standing.IsLoggable(s.Row) && !Standing.IsHandled(workout, s));
        return unlogged < 0
            ? new("Finish", PrimaryAction.Finish)
            : new($"Next: {Standing.NameOf(planned[unlogged].Row)}", PrimaryAction.Next, unlogged);
    }

    // ---- One Exercise

    private ExerciseView ExerciseViewOf(Workout workout, LogSlot slot)
    {
        var view = CoreViewOf(workout, slot.Row);
        var (swap, added) = Standing.MarksOf(slot);
        return view with { Swap = swap, Added = added, Picker = ExerciseOf(slot.Row.ExerciseId) is { } e ? Standing.PickerFor(workout, e) : null };
    }

    private ExerciseView CoreViewOf(Workout workout, SessionExercise planned)
    {
        if (ExerciseOf(planned.ExerciseId) is not { } exercise)
        {
            return new(new(planned.ExerciseId, "Exercise not found", LogKind.Text, null, null, false), null, null,
                "This Exercise is no longer in your library, so it cannot be shown here.", null);
        }

        var info = new ExerciseInfo(exercise.Id, exercise.Name, exercise.LogKind, PlannedText(planned), planned.Cue, exercise.IsBenchmark);
        if (LogStanding.SkipOf(workout, planned) is { } skip)
        {
            return new(info, null, null, null, null, skip);
        }

        if (RowSpec.For(exercise.LogKind) is not { } spec)
        {
            return new(info, null, null, "A text Exercise cannot be logged here yet, because a Set cannot hold text. Use Next to move on.", null);
        }

        var history = ExercisePerformance.Snapshot(exercise, FinishedHistory(workout), PlanOf(workout), Units);
        var rows = RowsOf(workout, exercise, planned, spec, history);
        var rpe = WorkoutEdits.FindDoneEntry(workout, exercise.Id)?.Rpe;
        return new(info, HeaderOf(exercise.LogKind, history), rows, null, rpe);
    }

    private ExerciseRows RowsOf(Workout workout, Exercise exercise, SessionExercise planned, RowSpec spec, ExerciseSnapshot history)
    {
        var confirmed = LogStanding.Confirmed(workout, exercise.Id);
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
                ? RowPrefill.FromStored(exercise.LogKind, stored, Units) ?? RowPrefillValues.Empty()
                : RowPrefill.For(exercise.LogKind, i + 1, history, planned, Units);
            var edit = state.EditOf(key);
            var rowState = isDone ? SetRowState.Done : i == current ? SetRowState.Current : SetRowState.Upcoming;

            // An added Set starts as "same as the one above".
            var (first, second) = !isDone && i >= planned.Sets && rows.Count > 0
                ? (rows[^1].Face.First, rows[^1].Face.Second)
                : (prefill.First, prefill.Second);
            var face = new RowFace(
                edit?.First ?? first, edit?.Second ?? second,
                spec.FirstHint(Units) ?? (spec.Shape == RowShape.Wide ? prefill.RepsHint : null),
                spec.Shape == RowShape.Pair ? prefill.RepsHint : null);
            var marks = new RowMarks(
                Pending: !isDone && !(edit?.Typed ?? false), Stored: stored is not null,
                IsMax: stored?.IsMax ?? edit?.Max ?? false, IsPr: isDone && prRow == i);
            rows.Add(new RowView(i, rowState, face, marks, state.Error?.Row == key));
        }

        var step = rows.Any(r => r.State == SetRowState.Current)
            ? spec.StepFor(Units, PlanOf(workout)?.WeightIncrementKg ?? Snapshot.Settings.WeightIncrementKg)
            : null;
        int? maxTarget = editingRow ?? (confirmed < rowCount ? confirmed : confirmed > 0 ? confirmed - 1 : null);
        int? removable = extra > 0 && editingRow is null && rowCount > confirmed && rowCount > 1 ? rowCount - 1 : null;
        return new ExerciseRows(spec, rows, new RowTools(step, maxTarget, removable));
    }

    private Plan? PlanOf(Workout workout) =>
        Snapshot.Plans.FirstOrDefault(p => p.Sessions.Any(s => s.Id == workout.SessionId))
            ?? Snapshot.Plans.FirstOrDefault(p => p.IsActive);

    /// <summary>Every other finished Workout on or before this one's date: Last, the Suggested target and PRs never come from the open one, nor from a day after it (a Workout logged afterwards is judged as of its date).</summary>
    private IEnumerable<Workout> FinishedHistory(Workout workout) =>
        Snapshot.Workouts.Where(w => w.Id != workout.Id && w.EndedAt is not null && w.Date <= workout.Date);

    private HeaderText HeaderOf(LogKind kind, ExerciseSnapshot history)
    {
        var last = history.Last is { } l && (l.BestSet ?? l.Sets.LastOrDefault()) is { } lastSet ? SetText.Result(kind, lastSet, Units) : null;
        var best = history.Best is { } b ? SetText.Result(kind, b.Set, Units) : null;
        var target = history.Target is { } t ? SetText.Target(kind, t, Units) : null;
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
        LogSlots.Of(workout).Select(s => s.Row.ExerciseId).Distinct()
            .Select(ExerciseOf).OfType<Exercise>()
            .Count(e => RowSpec.IsLoggable(e.LogKind) && PrOf(workout, e).Feedback.IsPr);
}
