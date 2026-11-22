using Core.Ids;
using Core.Model;
using Core.Rules.PlanEditing;
using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;
using Ui.Saving;

namespace Ui.Pages;

public sealed partial class Plans : IDisposable
{
    private PageContext Page = default!;
    private GuardedSave _save = default!;
    private bool _newOpen;
    private string _newName = "";
    private string? _copyFromId;
    private string? _newMessage;

    [Inject] private AppData Data { get; set; } = default!;

    [Inject] private IClock Clock { get; set; } = default!;

    [Inject] private IIdGenerator Ids { get; set; } = default!;

    [Inject] private SaveFailureNotice Notice { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    /// <summary>The active Plan first, then the others by name.</summary>
    private IReadOnlyList<Plan> PlanList =>
        [.. Data.Snapshot.Plans.OrderByDescending(p => p.IsActive).ThenBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase)];

    protected override void OnInitialized()
    {
        Page = new PageContext(Data, Clock);
        _save = new GuardedSave(Data, Notice);
        _save.Started += OnSaveStarted;
        Data.Changed += OnDataChanged;
    }

    public void Dispose()
    {
        _save.Started -= OnSaveStarted;
        Data.Changed -= OnDataChanged;
    }

    private void OnDataChanged() => _ = InvokeAsync(StateHasChanged);

    private void OnSaveStarted() => StateHasChanged();

    private static string Summary(Plan plan)
    {
        var sessions = $"{plan.Sessions.Count} {(plan.Sessions.Count == 1 ? "Session" : "Sessions")}";
        var phases = $"{plan.Phases.Count} {(plan.Phases.Count == 1 ? "Phase" : "Phases")}";
        return PlanProgress.TotalWeeks(plan) is { } weeks ? $"{sessions} · {phases} · {weeks} {(weeks == 1 ? "week" : "weeks")}" : $"{sessions} · {phases}";
    }

    private void OpenNew()
    {
        _newName = "";
        _newMessage = null;
        _copyFromId = ActivePlan.Of(Data.Snapshot.Plans)?.Id ?? PlanList.FirstOrDefault()?.Id;
        _newOpen = true;
    }

    private void CloseNew() => _newOpen = false;

    /// <summary>The previous Plan stops being active in the same save, so there is never none or two.</summary>
    private async Task MakeActiveAsync(string planId)
    {
        var changed = PlanLifecycleRules.MakeActive(Data.Snapshot.Plans, planId);
        if (changed.Count > 0)
        {
            await _save.CommitAsync([.. changed.Select(plan => Change.Put(plan))]);
        }
    }

    private Task FromScratchAsync() => CreateAsync(PlanLifecycleRules.FromScratch(_newName, Page.Today, Ids));

    private Task CopyAsync()
    {
        if (Data.Snapshot.Plans.FirstOrDefault(p => p.Id == _copyFromId) is not { } source)
        {
            _newMessage = "Pick the Plan to copy.";
            return Task.CompletedTask;
        }

        return CreateAsync(PlanLifecycleRules.Copy(source, _newName, Ids));
    }

    private async Task CreateAsync(PlanEditResult<Plan> result)
    {
        if (!result.TryGet(out var created, out var reason))
        {
            _newMessage = reason;
            return;
        }

        _newMessage = null;
        var saved = await _save.CommitAsync(Change.Put(created));
        if (saved.IsSaved)
        {
            _newOpen = false;
            Navigation.NavigateTo($"plans/{created.Id}");
        }
        else if (saved.Status == SaveStatus.Failed)
        {
            _newMessage = Notice.Message;
        }
    }
}
