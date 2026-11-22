using Core.Ids;
using Core.Model;
using Core.Rules.Library;
using Core.Rules.Starting;
using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;
using Ui.Components;
using Ui.Saving;

namespace Ui.Pages;

public sealed partial class CustomWorkoutDraft : IDisposable
{
    private List<string> _draft = [];
    private string? _initialisedFor;
    private bool _initialised;
    private bool _pickerOpen;
    private PageContext Page = default!;
    private GuardedSave _save = default!;

    [Inject] private AppData Data { get; set; } = default!;

    [Inject] private IClock Clock { get; set; } = default!;

    [Inject] private IIdGenerator Ids { get; set; } = default!;

    [Inject] private SaveFailureNotice Notice { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    /// <summary>Set by the Make up route: the Session whose Exercises pre-fill the draft.</summary>
    [Parameter]
    public string? SessionId { get; set; }


    private Session? MakeUpSession =>
        SessionId is null ? null : Page.ActivePlan?.Sessions.FirstOrDefault(s => s.Id == SessionId);

    private bool UnknownSession => SessionId is not null && MakeUpSession is null;

    private string Title => MakeUpSession is { } session ? $"Make up {session.Name}" : "Custom workout";




    private PickerRequest PickerRequest
    {
        get
        {
            var snapshot = Data.Snapshot;
            return PickerRequest.ForCustomWorkout(
                Page.ActivePlan, snapshot.WeekSchedules, snapshot.Breaks, snapshot.Workouts, snapshot.Exercises, Page.Today, _draft);
        }
    }

    protected override void OnInitialized()
    {
        Page = new PageContext(Data, Clock);
        _save = new GuardedSave(Data, Notice);
        Data.Changed += OnDataChanged;
    }

    protected override void OnParametersSet()
    {
        if (_initialised && _initialisedFor == SessionId)
        {
            return;
        }

        _initialised = true;
        _initialisedFor = SessionId;
        _draft = MakeUpSession is { } session && Page.ActivePlan is { } plan
            ? [.. CustomWorkoutStart.RowsForMakeUp(plan, session, Page.Today).Select(r => r.ExerciseId).Distinct()]
            : [];
    }

    public void Dispose() => Data.Changed -= OnDataChanged;

    private void OnDataChanged() => _ = InvokeAsync(StateHasChanged);

    private string NameOf(string exerciseId) =>
        Data.Snapshot.Exercises.FirstOrDefault(e => e.Id == exerciseId)?.Name ?? "Exercise";

    private void Remove(string exerciseId) => _draft.Remove(exerciseId);

    private void OpenPicker() => _pickerOpen = true;

    private void ClosePicker() => _pickerOpen = false;

    private void Add(string exerciseId)
    {
        if (!_draft.Contains(exerciseId))
        {
            _draft.Add(exerciseId);
        }

        _pickerOpen = false;
    }

    private IReadOnlyList<SessionExercise> RowsToStart(Plan plan)
    {
        if (MakeUpSession is not { } session)
        {
            return CustomWorkoutStart.RowsForExercises(plan, _draft, Page.Today);
        }

        var sessionRows = CustomWorkoutStart.RowsForMakeUp(plan, session, Page.Today);
        return
        [
            .. _draft.Select(id =>
                sessionRows.FirstOrDefault(r => r.ExerciseId == id)
                ?? CustomWorkoutStart.RowsForExercises(plan, [id], Page.Today)[0]),
        ];
    }

    private async Task StartAsync()
    {
        if (_draft.Count == 0 || Page.ActivePlan is not { } plan)
        {
            return;
        }

        // A second tap while the first is saving is refused by the guard, so it cannot start another Workout.
        StartOutcome? outcome = null;
        await _save.RunAsync(async () =>
        {
            outcome = await new WorkoutStarter(Data, Ids, Clock).StartCustomAsync(plan, RowsToStart(plan), Page.Today);
            return outcome.Commit is { } commit ? SaveWork.Committed(commit) : SaveWork.None;
        });
        if (outcome?.Workout is { } workout)
        {
            Navigation.NavigateTo($"log/{workout.Id}");
        }
    }
}
