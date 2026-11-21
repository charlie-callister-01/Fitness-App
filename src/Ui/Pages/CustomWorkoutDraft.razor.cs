using Core.Ids;
using Core.Model;
using Core.Rules.CustomWorkout;
using Core.Rules.Starting;
using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;
using Ui.Components;
using Ui.Saving;

namespace Ui.Pages;

public sealed partial class CustomWorkoutDraft : IDisposable
{
    private const int MaxLibraryWithoutSearch = 20;

    private List<string> _draft = [];
    private string? _initialisedFor;
    private bool _initialised;
    private bool _pickerOpen;
    private bool _saving;

    [Inject] private AppData Data { get; set; } = default!;

    [Inject] private IClock Clock { get; set; } = default!;

    [Inject] private IIdGenerator Ids { get; set; } = default!;

    [Inject] private SaveFailureNotice Notice { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    /// <summary>Set by the Make up route: the Session whose Exercises pre-fill the draft.</summary>
    [Parameter]
    public string? SessionId { get; set; }

    private Plan? ActivePlan => Data.Snapshot.Plans.FirstOrDefault(p => p.IsActive);

    private Session? MakeUpSession =>
        SessionId is null ? null : ActivePlan?.Sessions.FirstOrDefault(s => s.Id == SessionId);

    private bool UnknownSession => SessionId is not null && MakeUpSession is null;

    private string Title => MakeUpSession is { } session ? $"Make up {session.Name}" : "Custom workout";

    private DateOnly Today => DateOnly.FromDateTime(Clock.UtcNow.ToLocalTime().DateTime);

    private bool CanWrite => Data.Health.CanWrite;

    private string? ReadOnlyMessage =>
        CanWrite ? null
        : Data.Health.AnotherWindowOpen ? "Another window of the app is open and is the one that saves. This page is read-only here."
        : "This window cannot save right now, so nothing can be started from here.";

    private ExercisePickerChoices PickerChoices
    {
        get
        {
            var library = Data.Snapshot.Exercises;
            if (ActivePlan is not { } plan)
            {
                return new([], [.. library.Where(e => !_draft.Contains(e.Id))], library.Count > MaxLibraryWithoutSearch);
            }

            var ordered = CustomWorkoutOffer.Order(plan, Data.Snapshot.WeekSchedules, Data.Snapshot.Breaks, Data.Snapshot.Workouts, library, Today);
            return new([], [.. ordered.Where(e => !_draft.Contains(e.Id))], library.Count > MaxLibraryWithoutSearch);
        }
    }

    protected override void OnInitialized() => Data.Changed += OnDataChanged;

    protected override void OnParametersSet()
    {
        if (_initialised && _initialisedFor == SessionId)
        {
            return;
        }

        _initialised = true;
        _initialisedFor = SessionId;
        _draft = MakeUpSession is { } session && ActivePlan is { } plan
            ? [.. CustomWorkoutStart.RowsForMakeUp(plan, session, Today).Select(r => r.ExerciseId).Distinct()]
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
            return CustomWorkoutStart.RowsForExercises(plan, _draft, Today);
        }

        var sessionRows = CustomWorkoutStart.RowsForMakeUp(plan, session, Today);
        return
        [
            .. _draft.Select(id =>
                sessionRows.FirstOrDefault(r => r.ExerciseId == id)
                ?? CustomWorkoutStart.RowsForExercises(plan, [id], Today)[0]),
        ];
    }

    private async Task StartAsync()
    {
        // A second tap while the first is saving must not start another Workout.
        if (_saving || !CanWrite || _draft.Count == 0 || ActivePlan is not { } plan)
        {
            return;
        }

        _saving = true;
        try
        {
            var outcome = await new WorkoutStarter(Data, Ids, Clock).StartCustomAsync(plan, RowsToStart(plan), Today);
            if (outcome.Commit is { } commit)
            {
                Notice.Report(commit);
            }

            if (outcome.Workout is { } workout)
            {
                Navigation.NavigateTo($"log/{workout.Id}");
            }
        }
        finally
        {
            _saving = false;
        }
    }
}
