using Core.Ids;
using Core.Model;
using Core.Rules.LogKinds;
using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Ui.Log;
using Ui.Preferences;
using Ui.Saving;

namespace Ui.Pages;

/// <summary>
/// S-02, the Workout log, at /log/{WorkoutId}. What is on screen comes from <see cref="LogViewBuilder"/>; what is not saved lives in
/// <see cref="LogEditState"/>; every write is <see cref="LogActions"/> or one of the two flows; the rest countdown, screen lock and
/// subscriptions are <see cref="LogSession"/>. This class only wires events to them.
/// </summary>
public sealed partial class WorkoutLog : ComponentBase, IDisposable
{
    /// <summary>A Workout started longer ago than this counts as resumed even before its first Set.</summary>
    private static readonly TimeSpan ResumedAfter = TimeSpan.FromMinutes(1);

    private string? _landedFor;
    private LogViewBuilder _builder = default!;
    private LogSession _session = default!;
    private LogActions _actions = default!;
    private FinishFlow _finish = default!;
    private LoggedAfterwardsFlow _afterwards = default!;

    [Inject] private AppData Data { get; set; } = default!;

    [Inject] private IClock Clock { get; set; } = default!;

    [Inject] private IIdGenerator Ids { get; set; } = default!;

    [Inject] private SaveFailureNotice Notice { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] private ISecondTicker Ticker { get; set; } = default!;

    [Inject] private IWakeLock WakeLock { get; set; } = default!;

    [Inject] private RestTimerSetting RestSetting { get; set; } = default!;

    [Inject] private InAppHistory History { get; set; } = default!;

    [Parameter]
    public string WorkoutId { get; set; } = "";

    private LogEditState State { get; } = new();

    private Workout? OpenWorkout =>
        Data.Snapshot.Workouts.FirstOrDefault(w => w.Id == WorkoutId) is { EndedAt: null } open ? open : null;

    /// <summary>A Workout with an end time: its summary is shown here, whether it just finished or the URL was reopened later.</summary>
    private Workout? FinishedWorkout =>
        Data.Snapshot.Workouts.FirstOrDefault(w => w.Id == WorkoutId) is { EndedAt: not null } finished ? finished : null;

    private DateOnly Today => DateOnly.FromDateTime(Clock.UtcNow.ToLocalTime().DateTime);

    private bool CanWrite => Data.Health.CanWrite;

    /// <summary>Plain wording for why this window cannot save, or null when it can.</summary>
    private string? ReadOnlyMessage => ReadOnlyWording.For(Data, "log");

    protected override void OnInitialized()
    {
        _builder = new LogViewBuilder(Data, State);
        _finish = new FinishFlow(Data, Notice, Clock, Ids);
        _afterwards = new LoggedAfterwardsFlow(Data, Notice);
        _session = new LogSession(Data, Clock, Ticker, WakeLock, RestSetting, _finish, _afterwards, OnDataChanged);
        _actions = new LogActions(Data, Notice, Ids, State, _builder, _session);
    }

    protected override Task OnAfterRenderAsync(bool firstRender) => _session.SyncScreenLockAsync(OpenWorkout);

    public void Dispose() => _session.Dispose();

    private void OnDataChanged() => _ = InvokeAsync(StateHasChanged);

    protected override void OnParametersSet()
    {
        if (_landedFor == WorkoutId)
        {
            return;
        }

        _landedFor = WorkoutId;
        _actions.WorkoutId = WorkoutId;
        _finish.Reset();
        _afterwards.Reset();
        if (OpenWorkout is { } workout)
        {
            State.Reset(_builder.Standing.FirstIncomplete(workout), IsResumed(workout));
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
        State.ClampTo(LogStanding.SlotCount(workout));
        return _builder.Build(workout);
    }

    private void Step(int direction)
    {
        if (OpenWorkout is { } workout
            && ViewOf(workout).Exercise is { Rows: { Tools.Step: { } step, CurrentRow: { } current } rows } exercise)
        {
            State.Step(exercise.Info.ExerciseId, rows.Items, current, step, direction);
        }
    }

    private Task ConfirmAsync() =>
        OpenWorkout is { } workout && ViewOf(workout).Exercise is { } exercise ? _actions.ConfirmSetAsync(exercise) : Task.CompletedTask;

    private Task PrimaryAsync()
    {
        if (OpenWorkout is not { } workout)
        {
            return Task.CompletedTask;
        }

        switch (ViewOf(workout).Primary)
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

    private Task FinishAsync() => _finish.RequestAsync(WorkoutId, CanWrite);

    private Task AnswerPainAsync(PainAnswer answer) => _finish.AnswerAsync(WorkoutId, answer, CanWrite);

    private Task ConfirmPartialAsync() => _finish.ConfirmPartialAsync(WorkoutId, CanWrite);

    /// <summary>The Workout menu's Logged afterwards item: the date sheet, or the undo when the Workout already is one.</summary>
    private Task LoggedAfterwardsAsync()
    {
        if (OpenWorkout is not { } workout)
        {
            return Task.CompletedTask;
        }

        if (!workout.LoggedAfterwards)
        {
            _afterwards.Begin();
            return Task.CompletedTask;
        }

        return _afterwards.UndoAsync(WorkoutId, Today, CanWrite);
    }

    private Task SaveDateAsync(DateOnly date) => _afterwards.SaveAsync(WorkoutId, date, Today, CanWrite);

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
