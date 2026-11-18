using System.Globalization;
using Core.Ids;
using Core.Model;
using Core.Rules.LogKinds;
using Core.Rules.Parsing;
using Core.Rules.Performance;
using Core.Rules.Units;
using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Ui.Components;
using Ui.Saving;

namespace Ui.Pages;

/// <summary>
/// S-02, the Workout log, at /log/{WorkoutId}. The page owns all saving: every change is merged into the snapshot's latest copy of the
/// Workout and committed at once, and a failed commit is reported to <see cref="SaveFailureNotice"/> while the typed text stays. It holds
/// only transient state (the Exercise showing, typed text not yet confirmed, the row whose check failed).
/// </summary>
public sealed partial class WorkoutLog : ComponentBase, IDisposable
{
    /// <summary>A Workout started longer ago than this counts as resumed even before its first Set.</summary>
    private static readonly TimeSpan ResumedAfter = TimeSpan.FromMinutes(1);

    [Inject] private AppData Data { get; set; } = default!;

    [Inject] private IClock Clock { get; set; } = default!;

    [Inject] private IIdGenerator Ids { get; set; } = default!;

    [Inject] private SaveFailureNotice Notice { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] private InAppHistory History { get; set; } = default!;

    [Parameter]
    public string WorkoutId { get; set; } = "";

    /// <summary>What the owner typed in a row and has not confirmed. A null field still shows the prefill.</summary>
    /// <param name="Carried">The values were only carried down by a stepper, so the row still looks pending.</param>
    /// <param name="Max">Marked as a Max set before it is confirmed; a stored Set carries its own flag.</param>
    private sealed record Draft(string? First, string? Second, bool Carried = false, bool Max = false);

    private readonly Dictionary<(string ExerciseId, int Row), Draft> _drafts = [];

    /// <summary>Rows added with Add Set beyond the planned count, per Exercise. Transient: a confirmed extra Set is stored and needs no counter.</summary>
    private readonly Dictionary<string, int> _extraRows = [];
    private string? _landedFor;
    private int _current;
    private bool _resumed;
    private int? _editingRow;
    private (string ExerciseId, int Row)? _errorRow;
    private string? _errorMessage;
    private int _invalidField;
    private int _focusRow = -1;
    private int _focusField;
    private int _focusSequence;
    private bool _busy;
    private string? _toast;

    private Workout? OpenWorkout =>
        Data.Snapshot.Workouts.FirstOrDefault(w => w.Id == WorkoutId) is { EndedAt: null } open ? open : null;

    private bool CanWrite => Data.Health.CanWrite;

    /// <summary>Plain wording for why this window cannot save, or null when it can.</summary>
    private string? ReadOnlyMessage =>
        CanWrite ? null
        : Data.Health.AnotherWindowOpen ? "Another window of the app is open and is the one that saves. This log is read-only here."
        : Data.Startup.State is StartupState.CannotRead or StartupState.StorageFailed
            ? "Your saved data could not be read, so nothing can be shown or saved here. Your data is untouched."
            : "This window cannot save right now, so the log is read-only.";

    protected override void OnInitialized() => Data.Changed += OnDataChanged;

    public void Dispose() => Data.Changed -= OnDataChanged;

    private void OnDataChanged() => _ = InvokeAsync(StateHasChanged);

    protected override void OnParametersSet()
    {
        if (_landedFor == WorkoutId)
        {
            return;
        }

        _landedFor = WorkoutId;
        _drafts.Clear();
        _extraRows.Clear();
        _editingRow = null;
        _errorRow = null;
        if (OpenWorkout is { } workout)
        {
            _current = FirstIncomplete(workout);
            _resumed = workout.Entries.Any(e => e.Sets.Count > 0)
                || (workout.StartedAt is { } started && Clock.UtcNow - started >= ResumedAfter);
        }
    }

    // ---- What is on screen

    private sealed record RowView(
        int Index, SetRowState State, string First, string Second, bool Pending, string? SecondHint, bool HasError,
        bool IsMax = false, bool Stored = false, bool IsPr = false);

    /// <param name="Step">The steppers' step while a row is waiting to be confirmed; null hides them.</param>
    /// <param name="MaxTarget">The row the Max set link acts on: the current row, else the last confirmed one.</param>
    /// <param name="RemovableRow">The last row when it is an extra, unconfirmed one that Remove would take away.</param>
    private sealed record ExerciseView(
        string ExerciseId, string Name, LogKind Kind, string? PlannedText, string? Cue,
        IReadOnlyList<RowView> Rows, string UnavailableMessage, bool Complete,
        StepSize? Step = null, int? MaxTarget = null, int? RemovableRow = null, HeaderText? Header = null, int? Rpe = null)
    {
        public string? StepAmount => Step?.Amount.ToString("0.##", CultureInfo.InvariantCulture);
    }

    /// <summary>Last, Best and the Suggested target as text in the owner's units; a part is null when there is none.</summary>
    private sealed record HeaderText(string? Last, string? Best, string? Target, string? Reason);

    private enum PrimaryAction
    {
        ConfirmSet,
        Next,
        Finish,
    }

    private sealed record LogView(
        string SessionName, int ExerciseCount, int DoneCount, int PrCount, ExerciseView? Exercise, string PrimaryLabel, PrimaryAction Action,
        IReadOnlyList<ExercisePager.Chip> Chips, string? UpNext)
    {
        public bool AllDone => ExerciseCount > 0 && DoneCount == ExerciseCount;

        public bool PrimaryNeedsWrite => Action != PrimaryAction.Next;
    }

    /// <summary>The kinds this page can log so far; others show a note and can be passed with Next.</summary>
    private static bool IsLoggable(LogKind kind) => kind is LogKind.Load or LogKind.BodyweightLoad;

    private UnitConverter Units => new(Data.Snapshot.Settings);

    private Exercise? ExerciseOf(string id) => Data.Snapshot.Exercises.FirstOrDefault(e => e.Id == id);

    private static int Confirmed(Workout workout, string exerciseId) =>
        workout.Entries.FirstOrDefault(e => e.ExerciseId == exerciseId && e.Status == EntryStatus.Done)?.Sets.Count ?? 0;

    private bool IsComplete(Workout workout, SessionExercise planned) =>
        ExerciseOf(planned.ExerciseId) is { } exercise && IsLoggable(exercise.LogKind)
        && Confirmed(workout, planned.ExerciseId) >= planned.Sets;

    private int FirstIncomplete(Workout workout)
    {
        var index = workout.Planned.ToList().FindIndex(p => !IsComplete(workout, p));
        return index < 0 ? Math.Max(0, workout.Planned.Count - 1) : index;
    }

    private LogView ViewOf(Workout workout)
    {
        var rows = workout.Planned;
        _current = Math.Clamp(_current, 0, Math.Max(0, rows.Count - 1));
        var exercise = rows.Count == 0 ? null : ExerciseViewOf(workout, rows[_current]);
        var (label, action) = Primary(rows, exercise);
        var session = Data.Snapshot.Plans.SelectMany(p => p.Sessions).FirstOrDefault(s => s.Id == workout.SessionId);
        var chips = rows.Select(p => new ExercisePager.Chip(NameOf(p), IsComplete(workout, p))).ToList();
        var upNext = _current < rows.Count - 1 ? NameOf(rows[_current + 1]) : null;
        return new LogView(
            session?.Name ?? "Workout", rows.Count, rows.Count(p => IsComplete(workout, p)), PrCount(workout), exercise, label, action, chips, upNext);
    }

    private string NameOf(SessionExercise planned) => ExerciseOf(planned.ExerciseId)?.Name ?? "Exercise";

    private (string Label, PrimaryAction Action) Primary(IReadOnlyList<SessionExercise> rows, ExerciseView? exercise)
    {
        if (exercise is not null && exercise.Rows.Any(r => r.State == SetRowState.Current))
        {
            return ("Confirm Set", PrimaryAction.ConfirmSet);
        }

        return _current < rows.Count - 1
            ? ($"Next: {NameOf(rows[_current + 1])}", PrimaryAction.Next)
            : ("Finish", PrimaryAction.Finish);
    }

    private ExerciseView ExerciseViewOf(Workout workout, SessionExercise planned)
    {
        var exercise = ExerciseOf(planned.ExerciseId);
        if (exercise is null)
        {
            return new(planned.ExerciseId, "Exercise not found", LogKind.Text, null, null, [],
                "This Exercise is no longer in your library, so it cannot be shown here.", false);
        }

        var plannedText = PlannedText(planned);
        if (!IsLoggable(exercise.LogKind))
        {
            return new(exercise.Id, exercise.Name, exercise.LogKind, plannedText, planned.Cue, [],
                "This kind of Exercise cannot be logged here yet. Use Next to move on.", false);
        }

        var confirmed = Confirmed(workout, exercise.Id);
        var extra = _extraRows.GetValueOrDefault(exercise.Id);
        var rowCount = Math.Max(planned.Sets + extra, confirmed);
        var current = _editingRow ?? confirmed;
        var snapshot = SnapshotOf(workout, exercise);
        var pr = PrOf(workout, exercise, _editingRow);
        var rows = new List<RowView>();
        for (var i = 0; i < rowCount; i++)
        {
            var stored = workout.Entries.FirstOrDefault(e => e.ExerciseId == exercise.Id)?.Sets.ElementAtOrDefault(i);
            var isDone = i < confirmed && _editingRow != i;
            var prefill = isDone && stored is not null
                ? RowPrefill.FromStored(exercise.LogKind, stored, Units) ?? RowPrefillValues.Empty()
                : RowPrefill.For(exercise.LogKind, i + 1, snapshot, planned, Units);
            var draft = _drafts.GetValueOrDefault((exercise.Id, i));
            var state = isDone ? SetRowState.Done : i == current ? SetRowState.Current : SetRowState.Upcoming;

            // An added Set starts as "same as the one above".
            var (first, second) = !isDone && i >= planned.Sets && rows.Count > 0
                ? (rows[^1].First, rows[^1].Second)
                : (prefill.First, prefill.Second);
            rows.Add(new RowView(
                i, state, draft?.First ?? first, draft?.Second ?? second,
                !isDone && (draft is null || draft.Carried), prefill.RepsHint, _errorRow == (exercise.Id, i),
                stored?.IsMax ?? draft?.Max ?? false, stored is not null, isDone && pr.Row == i));
        }

        var complete = confirmed >= planned.Sets && _editingRow is null;
        var hasCurrent = rows.Any(r => r.State == SetRowState.Current);
        var step = hasCurrent
            ? StepRules.For(exercise.LogKind, Units, PlanOf(workout)?.WeightIncrementKg ?? Data.Snapshot.Settings.WeightIncrementKg,
                Data.Snapshot.Settings.DisplayWeightUnit)
            : null;
        int? maxTarget = _editingRow ?? (confirmed < rowCount ? confirmed : confirmed > 0 ? confirmed - 1 : null);
        int? removable = extra > 0 && _editingRow is null && rowCount > confirmed && rowCount > 1 ? rowCount - 1 : null;
        var rpe = workout.Entries.FirstOrDefault(e => e.ExerciseId == exercise.Id && e.Status == EntryStatus.Done)?.Rpe;
        return new(exercise.Id, exercise.Name, exercise.LogKind, plannedText, planned.Cue, rows, "", complete,
            step, maxTarget, removable, HeaderOf(exercise.LogKind, snapshot), rpe);
    }

    private Plan? PlanOf(Workout workout) =>
        Data.Snapshot.Plans.FirstOrDefault(p => p.Sessions.Any(s => s.Id == workout.SessionId))
            ?? Data.Snapshot.Plans.FirstOrDefault(p => p.IsActive);

    /// <summary>Last and the Suggested target come from finished history only, never from this Workout.</summary>
    private ExerciseSnapshot SnapshotOf(Workout workout, Exercise exercise)
    {
        var plan = PlanOf(workout);
        var history = Data.Snapshot.Workouts.Where(w => w.Id != workout.Id && w.EndedAt is not null);
        return ExercisePerformance.Snapshot(exercise, history, plan);
    }

    private HeaderText HeaderOf(LogKind kind, ExerciseSnapshot snapshot)
    {
        var unit = Data.Snapshot.Settings.DisplayWeightUnit;
        var last = snapshot.Last is { } l && (l.BestSet ?? l.Sets.LastOrDefault()) is { } lastSet
            ? SetText.Result(kind, lastSet, Units, unit)
            : null;
        var best = snapshot.Best is { } b ? SetText.Result(kind, b.Set, Units, unit) : null;
        var target = snapshot.Target is { } t ? SetText.Target(kind, t, Units, unit) : null;
        return new HeaderText(last, best, target, target is null ? null : snapshot.Target?.Reason);
    }

    private static string? PlannedText(SessionExercise planned) => planned switch
    {
        { Reps: { } low, RepsMax: { } high } when high > low => $"{planned.Sets} × {low}-{high}",
        { Reps: { } reps } => $"{planned.Sets} × {reps}",
        _ => null,
    };

    // ---- PR feedback (C-03, 8.3): derived from the Sets as they are now, never stored.

    /// <summary>
    /// The rule's answer for one Exercise. The history is every other finished Workout (never the open one); the Set being reopened is
    /// not confirmed any more, so it is left out until it is confirmed again.
    /// </summary>
    private (int? Row, SetPrFeedback Feedback) PrOf(Workout workout, Exercise exercise, int? reopenedRow)
    {
        var history = Data.Snapshot.Workouts.Where(w => w.Id != workout.Id && w.EndedAt is not null);
        var sets = workout.Entries.FirstOrDefault(e => e.ExerciseId == exercise.Id && e.Status == EntryStatus.Done)?.Sets ?? [];
        var confirmed = sets.Where((_, i) => i != reopenedRow).ToList();
        var feedback = SetPrFeedbackRule.For(exercise, history, confirmed);
        var row = feedback.IsPr && feedback.WorkoutBest is { } best ? sets.ToList().FindIndex(s => s.Id == best.Id) : -1;
        return (row < 0 ? null : row, feedback);
    }

    private int PrCount(Workout workout) =>
        workout.Planned.Select(p => p.ExerciseId).Distinct()
            .Select(ExerciseOf).OfType<Exercise>()
            .Count(e => IsLoggable(e.LogKind) && PrOf(workout, e, e.Id == ShownExerciseId(workout) ? _editingRow : null).Feedback.IsPr);

    private string? ShownExerciseId(Workout workout) => workout.Planned.ElementAtOrDefault(_current)?.ExerciseId;

    /// <summary>The toast for the Set just confirmed, or null when it is not a PR.</summary>
    private string? ToastFor(Workout workout, Exercise exercise, int row)
    {
        var (prRow, feedback) = PrOf(workout, exercise, null);
        if (prRow != row || feedback.WorkoutBest is not { } best || feedback.PreviousBest is not { } previous)
        {
            return null;
        }

        var unit = Data.Snapshot.Settings.DisplayWeightUnit;
        return $"PR! {SetText.Result(exercise.LogKind, best, Units, unit)} beats {SetText.Short(exercise.LogKind, previous.Set, Units)}";
    }

    // ---- Typing

    private void EditFirst(ExerciseView exercise, int row, string text)
    {
        var shown = exercise.Rows[row];
        var key = (exercise.ExerciseId, row);
        var existing = _drafts.GetValueOrDefault(key);
        _drafts[key] = new Draft(text, existing?.Second ?? shown.Second, Max: existing?.Max ?? false);
    }

    private void EditSecond(ExerciseView exercise, int row, string text)
    {
        var shown = exercise.Rows[row];
        var key = (exercise.ExerciseId, row);
        var existing = _drafts.GetValueOrDefault(key);
        _drafts[key] = new Draft(existing?.First ?? shown.First, text, Max: existing?.Max ?? false);
    }

    // ---- Steppers, Add Set and Max set

    private void Step(int direction)
    {
        if (OpenWorkout is not { } workout
            || ViewOf(workout).Exercise is not { Step: { } step } exercise
            || exercise.Rows.FirstOrDefault(r => r.State == SetRowState.Current) is not { } current)
        {
            return;
        }

        var useSecond = step.Field == StepField.Second;
        var typed = (useSecond ? current.Second : current.First).Replace(',', '.');
        var start = double.TryParse(typed, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : 0;
        var stepped = Math.Max(0, Math.Round(start + direction * step.Amount, 2))
            .ToString("0.##", CultureInfo.InvariantCulture);

        // The new value goes to this row and down to the later rows not yet confirmed, which stay muted.
        foreach (var row in exercise.Rows.Where(r => r.Index >= current.Index && r.State != SetRowState.Done))
        {
            var key = (exercise.ExerciseId, row.Index);
            var existing = _drafts.GetValueOrDefault(key) ?? new Draft(null, null);
            var carried = row.Index != current.Index && (existing.Carried || !_drafts.ContainsKey(key));
            _drafts[key] = useSecond
                ? existing with { First = existing.First ?? row.First, Second = stepped, Carried = carried }
                : existing with { First = stepped, Second = existing.Second ?? row.Second, Carried = carried };
        }

        _errorRow = null;
    }

    private void AddSet(ExerciseView exercise) =>
        _extraRows[exercise.ExerciseId] = _extraRows.GetValueOrDefault(exercise.ExerciseId) + 1;

    private void RemoveSet(ExerciseView exercise)
    {
        if (exercise.RemovableRow is not { } row)
        {
            return;
        }

        _extraRows[exercise.ExerciseId] = Math.Max(0, _extraRows.GetValueOrDefault(exercise.ExerciseId) - 1);
        _drafts.Remove((exercise.ExerciseId, row));
        if (_errorRow == (exercise.ExerciseId, row))
        {
            _errorRow = null;
        }
    }

    /// <summary>A Set that exists is flagged in the stored Workout at once; a row still to be confirmed keeps the flag until it is.</summary>
    private async Task ToggleMaxAsync(ExerciseView exercise)
    {
        if (_busy || exercise.MaxTarget is not { } index)
        {
            return;
        }

        var row = exercise.Rows[index];
        if (!row.Stored)
        {
            var key = (exercise.ExerciseId, index);
            var existing = _drafts.GetValueOrDefault(key) ?? new Draft(null, null, Carried: row.Pending);
            _drafts[key] = existing with { Max = !row.IsMax };
            return;
        }

        if (!CanWrite)
        {
            return;
        }

        _busy = true;
        try
        {
            await CommitAsync(latest => WithMax(latest, exercise.ExerciseId, index, !row.IsMax));
        }
        finally
        {
            _busy = false;
        }
    }

    private static Workout WithMax(Workout workout, string exerciseId, int row, bool isMax)
    {
        var entries = workout.Entries.ToList();
        var index = entries.FindIndex(e => e.ExerciseId == exerciseId && e.Status == EntryStatus.Done);
        if (index < 0 || row >= entries[index].Sets.Count)
        {
            return workout;
        }

        var sets = entries[index].Sets.ToList();
        sets[row] = sets[row] with { IsMax = isMax };
        entries[index] = entries[index] with { Sets = sets };
        return workout with { Entries = entries };
    }

    private void Reopen(ExerciseView exercise, int row)
    {
        var shown = exercise.Rows[row];
        _editingRow = row;
        _toast = null;
        _drafts[(exercise.ExerciseId, row)] = new Draft(shown.First, shown.Second);
        _errorRow = null;
        RequestFocus(row, 1);
    }

    private void RequestFocus(int row, int field)
    {
        _focusRow = row;
        _focusField = field;
        _focusSequence++;
    }

    // ---- Actions

    private Task PrimaryAsync() => OpenWorkout is { } workout ? ViewOf(workout).Action switch
    {
        PrimaryAction.ConfirmSet => ConfirmAsync(),
        PrimaryAction.Next => GoNext(),
        _ => FinishAsync(),
    } : Task.CompletedTask;

    private Task GoNext() => JumpTo(_current + 1);

    /// <summary>Shows another Exercise. Nothing is lost: confirmed Sets are saved and typed-but-unconfirmed text stays in the drafts.</summary>
    private Task JumpTo(int index)
    {
        _current = index;
        _toast = null;
        _editingRow = null;
        _errorRow = null;
        return Task.CompletedTask;
    }

    private async Task ConfirmAsync()
    {
        if (_busy || !CanWrite || OpenWorkout is not { } workout)
        {
            return;
        }

        var view = ViewOf(workout);
        if (view.Exercise is not { } exercise || exercise.Rows.FirstOrDefault(r => r.State == SetRowState.Current) is not { } row)
        {
            return;
        }

        var parser = new InputParser(Units);
        var text = RowInput.Join(exercise.Kind, row.First, row.Second);
        if (parser.Parse(text, exercise.Kind) is ParseResult.Failure failure)
        {
            _errorRow = (exercise.ExerciseId, row.Index);
            _errorMessage = failure.Error;
            // The first field is the culprit when it fails even with a valid second field.
            _invalidField = parser.Parse(RowInput.Join(exercise.Kind, row.First, "1"), exercise.Kind) is ParseResult.Failure ? 1 : 2;
            RequestFocus(row.Index, _invalidField);
            return;
        }

        var parsed = ((ParseResult.Success)parser.Parse(text, exercise.Kind)).Set;
        _busy = true;
        try
        {
            var isMax = _drafts.GetValueOrDefault((exercise.ExerciseId, row.Index))?.Max ?? false;
            var result = await CommitAsync(latest => WithSet(latest, exercise.ExerciseId, row.Index, parsed, isMax));
            if (result is CommitResult.Committed)
            {
                _drafts.Remove((exercise.ExerciseId, row.Index));
                _editingRow = null;
                _errorRow = null;
                _toast = OpenWorkout is { } saved && ExerciseOf(exercise.ExerciseId) is { } shown
                    ? ToastFor(saved, shown, row.Index)
                    : null;
            }
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>Sets or clears the Exercise's RPE on its entry, creating the entry when no Set has been confirmed yet. Never blocks Confirm.</summary>
    private async Task SetRpeAsync(string exerciseId, int? rpe)
    {
        if (_busy || !CanWrite)
        {
            return;
        }

        _busy = true;
        try
        {
            await CommitAsync(latest => WithRpe(latest, exerciseId, rpe));
        }
        finally
        {
            _busy = false;
        }
    }

    private static Workout WithRpe(Workout workout, string exerciseId, int? rpe)
    {
        var entries = workout.Entries.ToList();
        var index = entries.FindIndex(e => e.ExerciseId == exerciseId && e.Status == EntryStatus.Done);
        if (index < 0)
        {
            entries.Add(new WorkoutEntry(exerciseId, EntryStatus.Done, null, null, rpe, []));
        }
        else
        {
            entries[index] = entries[index] with { Rpe = rpe };
        }

        return workout with { Entries = entries };
    }

    // TEMPORARY (B1): Finish only stamps EndedAt and returns Home, so a Workout is never stranded in progress. B2 replaces this wholesale
    // with the full finish flow (Partial prompt, Pain check, summary). Once committed the Workout is no longer open, so a second tap is a
    // no-op; _busy covers a second tap while the first save is still in flight. Either way EndedAt is stamped once.
    private async Task FinishAsync()
    {
        if (_busy || !CanWrite)
        {
            return;
        }

        _busy = true;
        try
        {
            var result = await CommitAsync(latest => latest with { EndedAt = Clock.UtcNow });
            if (result is CommitResult.Committed)
            {
                Navigation.NavigateTo("");
            }
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>
    /// Applies <paramref name="change"/> to the snapshot's latest copy of the Workout (never a copy held since an earlier render, which
    /// other changes may have replaced) and commits it, reporting the outcome to the save-failure notice.
    /// </summary>
    private async Task<CommitResult?> CommitAsync(Func<Workout, Workout> change)
    {
        if (Data.Snapshot.Workouts.FirstOrDefault(w => w.Id == WorkoutId) is not { } latest)
        {
            return null;
        }

        var result = await Data.CommitAsync(Change.Put(change(latest)));
        Notice.Report(result);
        return result;
    }

    /// <summary>The Set goes into its Exercise's entry, created on the first Set; a reopened row keeps its ID and Max flag.</summary>
    private Workout WithSet(Workout workout, string exerciseId, int row, ParsedSet parsed, bool isMax)
    {
        var entries = workout.Entries.ToList();
        var index = entries.FindIndex(e => e.ExerciseId == exerciseId && e.Status == EntryStatus.Done);
        if (index < 0)
        {
            entries.Add(new WorkoutEntry(exerciseId, EntryStatus.Done, null, null, null, [parsed.ToWorkoutSet(Ids.NewId(), isMax)]));
            return workout with { Entries = entries };
        }

        var sets = entries[index].Sets.ToList();
        if (row < sets.Count)
        {
            sets[row] = parsed.ToWorkoutSet(sets[row].Id, sets[row].IsMax);
        }
        else
        {
            sets.Add(parsed.ToWorkoutSet(Ids.NewId(), isMax));
        }

        entries[index] = entries[index] with { Sets = sets };
        return workout with { Entries = entries };
    }

    // After a deep link there is nothing in the app to go back to, so go Home rather than out of the app.
    private async Task GoBack()
    {
        if (History.HasEarlierPage)
        {
            await JS.InvokeVoidAsync("history.back");
        }
        else
        {
            Navigation.NavigateTo("", replace: true);
        }
    }
}
