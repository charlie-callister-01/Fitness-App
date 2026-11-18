using Core.Ids;
using Core.Model;
using Core.Rules.Editing;
using Core.Rules.LogKinds;
using Core.Rules.Parsing;
using Core.Rules.Units;
using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Ui.Log;
using Ui.Saving;

namespace Ui.Pages;

/// <summary>
/// S-02, the Workout log, at /log/{WorkoutId}. The page owns all saving: every change is merged into the snapshot's latest copy of the
/// Workout by <see cref="WorkoutWriter"/> and committed at once, and a failed commit is reported to <see cref="SaveFailureNotice"/>
/// while the typed text stays. What is on screen comes from <see cref="LogViewBuilder"/>; what is not saved lives in
/// <see cref="LogEditState"/>. This class only wires events to them.
/// </summary>
public sealed partial class WorkoutLog : ComponentBase, IDisposable
{
    /// <summary>A Workout started longer ago than this counts as resumed even before its first Set.</summary>
    private static readonly TimeSpan ResumedAfter = TimeSpan.FromMinutes(1);

    private string? _landedFor;
    private bool _busy;

    [Inject] private AppData Data { get; set; } = default!;

    [Inject] private IClock Clock { get; set; } = default!;

    [Inject] private IIdGenerator Ids { get; set; } = default!;

    [Inject] private SaveFailureNotice Notice { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] private InAppHistory History { get; set; } = default!;

    [Parameter]
    public string WorkoutId { get; set; } = "";

    private LogEditState State { get; } = new();

    private LogViewBuilder Builder => new(Data.Snapshot, State);

    private WorkoutWriter Writer => new(Data, Notice);

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
        if (OpenWorkout is { } workout)
        {
            State.Reset(Builder.FirstIncomplete(workout), IsResumed(workout));
        }
        else
        {
            State.Reset(0, false);
        }
    }

    private bool IsResumed(Workout workout) =>
        workout.Entries.Any(e => e.Sets.Count > 0)
        || (workout.StartedAt is { } started && Clock.UtcNow - started >= ResumedAfter);

    private LogView ViewOf(Workout workout)
    {
        State.ClampTo(workout.Planned.Count);
        return Builder.Build(workout);
    }

    // ---- Steppers, Max set and RPE

    private void Step(int direction)
    {
        if (OpenWorkout is { } workout
            && ViewOf(workout).Exercise is { Rows: { Tools.Step: { } step, CurrentRow: { } current } rows } exercise)
        {
            State.Step(exercise.Info.ExerciseId, rows.Items, current, step, direction);
        }
    }

    /// <summary>A Set that exists is flagged in the stored Workout at once; a row still to be confirmed keeps the flag until it is.</summary>
    private async Task ToggleMaxAsync(ExerciseView exercise)
    {
        if (_busy || exercise.Rows is not { Tools.MaxTarget: { } index } rows)
        {
            return;
        }

        var id = exercise.Info.ExerciseId;
        var marks = rows.Items[index].Marks;
        if (marks.Stored)
        {
            await RunExclusive(() => Writer.ChangeAsync(WorkoutId, w => WorkoutEdits.WithMax(w, id, index, !marks.IsMax)));
        }
        else
        {
            State.MarkMax(new RowKey(id, index), !marks.IsMax, marks.Pending);
        }
    }

    /// <summary>Sets or clears the Exercise's RPE, creating its entry when no Set has been confirmed yet. Never blocks Confirm.</summary>
    private Task SetRpeAsync(string exerciseId, int? rpe) =>
        RunExclusive(() => Writer.ChangeAsync(WorkoutId, w => WorkoutEdits.WithRpe(w, exerciseId, rpe)));

    // ---- The primary action

    private Task PrimaryAsync()
    {
        if (OpenWorkout is not { } workout)
        {
            return Task.CompletedTask;
        }

        var primary = ViewOf(workout).Primary;
        switch (primary)
        {
            case { Action: PrimaryAction.ConfirmSet }:
                return ConfirmAsync();
            case { Action: PrimaryAction.Next, JumpTo: { } index }:
                State.Show(index);
                return Task.CompletedTask;
            default:
                return FinishAsync();
        }
    }

    private async Task ConfirmAsync()
    {
        if (!CanAct
            || OpenWorkout is not { } workout
            || ViewOf(workout).Exercise is not { Rows.CurrentRow: { } row } exercise)
        {
            return;
        }

        var info = exercise.Info;
        var key = new RowKey(info.ExerciseId, row.Index);
        var parser = new InputParser(new UnitConverter(Data.Snapshot.Settings));
        switch (RowInput.Check(info.Kind, row.Face.First, row.Face.Second, parser))
        {
            case RowCheck.Invalid invalid:
                State.Fail(key, invalid.Error, invalid.Field);
                break;
            case RowCheck.Valid valid:
                var isMax = State.EditOf(key)?.Max ?? false;
                await RunExclusive(async () =>
                {
                    var result = await Writer.ChangeAsync(
                        WorkoutId, w => WorkoutEdits.WithSet(w, info.ExerciseId, row.Index, valid.Set, isMax, Ids));
                    if (result is CommitResult.Committed)
                    {
                        State.Confirmed(key);
                        State.Toast = OpenWorkout is { } saved ? Builder.ToastFor(saved, info.ExerciseId, row.Index) : null;
                    }
                });
                break;
        }
    }

    // TEMPORARY (B1): Finish only stamps EndedAt and returns Home, so a Workout is never stranded in progress. B2 replaces this wholesale
    // with the full finish flow (Partial prompt, Pain check, summary). Once committed the Workout is no longer open, so a second tap is a
    // no-op; _busy covers a second tap while the first save is still in flight. Either way EndedAt is stamped once.
    private Task FinishAsync() =>
        RunExclusive(async () =>
        {
            var result = await Writer.ChangeAsync(WorkoutId, w => w with { EndedAt = Clock.UtcNow });
            if (result is CommitResult.Committed)
            {
                Navigation.NavigateTo("");
            }
        });

    private bool CanAct => !_busy && CanWrite;

    /// <summary>Runs a save unless another is in flight or this window cannot write, so two taps never save twice.</summary>
    private async Task RunExclusive(Func<Task> save)
    {
        if (!CanAct)
        {
            return;
        }

        _busy = true;
        try
        {
            await save();
        }
        finally
        {
            _busy = false;
        }
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
