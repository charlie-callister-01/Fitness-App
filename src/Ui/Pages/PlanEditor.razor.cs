using Core.Ids;
using Core.Model;
using Core.Rules.PlanEditing;
using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Ui.Accessibility;
using Ui.Saving;

namespace Ui.Pages;

public sealed partial class PlanEditor : IDisposable
{
    private static readonly string[] Tabs = ["Settings", "Phases", "Sessions"];

    private PageContext Page = default!;
    private GuardedSave _save = default!;
    private PlanEditState? _state;
    private string? _openedFor;
    private IDisposable? _leaveGuard;
    private string _tab = Tabs[0];
    private string? _leaveTarget;
    private bool _allowLeave;
    private bool _justSaved;
    private string? _trackMessage;
    private bool _addTrackOpen;
    private string _newTrackName = "";
    private TrackCadence _newTrackCadence = TrackCadence.Weekly;
    private string? _addTrackMessage;

    [Inject] private AppData Data { get; set; } = default!;

    [Inject] private IClock Clock { get; set; } = default!;

    [Inject] private IIdGenerator Ids { get; set; } = default!;

    [Inject] private SaveFailureNotice Notice { get; set; } = default!;
    [Inject] private Announcer Announcer { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Parameter]
    public string PlanId { get; set; } = "";

    private bool Disabled => !Page.CanWrite || _save.IsBusy;

    protected override void OnInitialized()
    {
        Page = new PageContext(Data, Clock);
        _save = new GuardedSave(Data, Notice);
        _save.Started += OnSaveStarted;
        _leaveGuard = Navigation.RegisterLocationChangingHandler(OnLocationChanging);
        Data.Changed += OnDataChanged;
    }

    protected override void OnParametersSet()
    {
        if (_openedFor == PlanId)
        {
            return;
        }

        _openedFor = PlanId;
        _state = Data.Snapshot.Plans.FirstOrDefault(p => p.Id == PlanId) is { } plan ? new PlanEditState(plan) : null;
        _tab = Tabs[0];
        _trackMessage = null;
        _addTrackOpen = false;
    }

    public void Dispose()
    {
        _leaveGuard?.Dispose();
        _save.Started -= OnSaveStarted;
        Data.Changed -= OnDataChanged;
    }

    private void OnDataChanged() => _ = InvokeAsync(StateHasChanged);

    private void OnSaveStarted() => StateHasChanged();

    private void ShowTab(string tab) => _tab = tab;

    private void SelectTrack(string trackId)
    {
        _state?.SelectTrack(trackId);
        _trackMessage = null;
    }

    private void OpenAddTrackSheet()
    {
        _newTrackName = "";
        _newTrackCadence = TrackCadence.Weekly;
        _addTrackMessage = null;
        _addTrackOpen = true;
    }

    private void CloseAddTrackSheet() => _addTrackOpen = false;

    private void ApplyAddTrack()
    {
        if (_state is null)
        {
            return;
        }

        if (!_state.AddTrack(_newTrackName, _newTrackCadence, Ids).TryGet(out _, out var reason))
        {
            _addTrackMessage = reason;
            return;
        }

        _addTrackOpen = false;
        _addTrackMessage = null;
        _trackMessage = null;
        _justSaved = false;
    }

    private void MoveTrackUp()
    {
        if (_state is null)
        {
            return;
        }

        if (!_state.MoveSelectedTrackUp().TryGet(out _, out var reason))
        {
            _trackMessage = reason;
            return;
        }

        _trackMessage = null;
        _justSaved = false;
    }

    private void MoveTrackDown()
    {
        if (_state is null)
        {
            return;
        }

        if (!_state.MoveSelectedTrackDown().TryGet(out _, out var reason))
        {
            _trackMessage = reason;
            return;
        }

        _trackMessage = null;
        _justSaved = false;
    }

    private void OnEdited() => _justSaved = false;

    private async Task SaveAsync()
    {
        if (_state is not { Snapshot.Composed: { } plan } state)
        {
            return;
        }

        // The Plan and the Benchmark flags ticked on its Sessions go in one commit; it either all lands or none of it does.
        var changes = PlanSaveRules.Changes(plan, state.Saved, Data.Snapshot.Exercises, state.BenchmarkOverrides);
        var saved = changes.Count == 0 || (await _save.CommitAsync(changes)).IsSaved;
        if (saved)
        {
            state.MarkSaved(plan);
            _trackMessage = null;
            _justSaved = true;
            Announcer.Announce("Plan saved.");
        }
    }

    // Leaving with unsaved changes asks first: the sheet holds the place the owner was going to.
    private ValueTask OnLocationChanging(LocationChangingContext context)
    {
        if (_allowLeave || _state is not { IsDirty: true })
        {
            return ValueTask.CompletedTask;
        }

        context.PreventNavigation();
        _leaveTarget = context.TargetLocation;
        StateHasChanged();
        return ValueTask.CompletedTask;
    }

    private void KeepEditing() => _leaveTarget = null;

    private void Discard()
    {
        var target = _leaveTarget;
        _leaveTarget = null;
        _allowLeave = true;
        if (target is not null)
        {
            Navigation.NavigateTo(target);
        }
    }
}
