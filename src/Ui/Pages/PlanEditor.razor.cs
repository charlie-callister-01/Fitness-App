using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
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

    [Inject] private AppData Data { get; set; } = default!;

    [Inject] private IClock Clock { get; set; } = default!;

    [Inject] private SaveFailureNotice Notice { get; set; } = default!;

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

    private void OnEdited() => _justSaved = false;

    private async Task SaveAsync()
    {
        if (_state?.Compose() is not { } plan)
        {
            return;
        }

        var result = await _save.CommitAsync(Change.Put(plan));
        if (result.IsSaved)
        {
            _state.MarkSaved(plan);
            _justSaved = true;
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
