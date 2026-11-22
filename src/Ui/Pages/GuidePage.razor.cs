using Core.Model;
using Core.Rules.Guides;
using Core.Rules.LogKinds;
using Core.Rules.Performance;
using Core.Rules.Units;
using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Ui.Saving;

namespace Ui.Pages;

public sealed partial class GuidePage : IAsyncDisposable
{
    private const string VideoModule = "./_content/Ui/Pages/GuidePage.razor.js";

    private PageContext Page = default!;
    private GuardedSave _save = default!;
    private IDisposable? _leaveGuard;
    private IJSObjectReference? _module;
    private bool _editing;
    private GuideDraft _draft = GuideDraft.From(null);
    private GuideDraft _baseline = GuideDraft.From(null);
    private string? _message;
    private bool _confirmDiscard;
    private string? _pendingTarget;
    private bool _notesOpen;
    private string _notes = "";
    private string _notesBaseline = "";
    private bool _videoOffline;
    private string? _landedFor;

    [Inject] private AppData Data { get; set; } = default!;

    [Inject] private IClock Clock { get; set; } = default!;

    [Inject] private SaveFailureNotice Notice { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter]
    public string ExerciseId { get; set; } = "";

    private Exercise? Exercise => Data.Snapshot.Exercises.FirstOrDefault(e => e.Id == ExerciseId);

    private Guide? StoredGuide => Data.Snapshot.Guides.FirstOrDefault(g => g.ExerciseId == ExerciseId);

    private bool HasUnsavedEdits => (_editing && _draft != _baseline) || (_notesOpen && _notes != _notesBaseline);

    private UnitConverter Converter => new(Data.Snapshot.Settings);

    private GuideHistory History =>
        GuideHistoryView.Build(Exercise!, Data.Snapshot.Workouts, Page.ActivePlan, Page.Today);

    /// <summary>"Lower · 3 × 8 · Best 22 kg × 8": where it sits in the Plan and its Best, when there is something to say.</summary>
    private string HeaderLine
    {
        get
        {
            var history = History;
            var parts = new List<string>();
            if (history.Place?.Placements.FirstOrDefault() is { } first)
            {
                parts.Add($"{first.Session.Name} · {PlannedText(first.Row)}");
            }

            if (history.Best is { } best)
            {
                parts.Add($"Best {SetText.Result(Exercise!.LogKind, best.Set, Converter)}");
            }

            return string.Join(" · ", parts);
        }
    }

    protected override void OnInitialized()
    {
        Page = new PageContext(Data, Clock);
        _save = new GuardedSave(Data, Notice);
        _save.Started += Refresh;
        Data.Changed += Refresh;
        _leaveGuard = Navigation.RegisterLocationChangingHandler(OnLeaving);
    }

    protected override void OnParametersSet()
    {
        if (_landedFor == ExerciseId)
        {
            return;
        }

        _landedFor = ExerciseId;
        _editing = false;
        _notesOpen = false;
        _confirmDiscard = false;
        _videoOffline = false;
        _message = null;
    }

    public async ValueTask DisposeAsync()
    {
        _save.Started -= Refresh;
        Data.Changed -= Refresh;
        _leaveGuard?.Dispose();
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }

    private void Refresh() => _ = InvokeAsync(StateHasChanged);

    // Leaving the editor (the back arrow, a link, the browser's Back) with unsaved text asks first; the navigation waits for the answer.
    private ValueTask OnLeaving(LocationChangingContext context)
    {
        if (HasUnsavedEdits)
        {
            context.PreventNavigation();
            _pendingTarget = context.TargetLocation;
            _confirmDiscard = true;
            Refresh();
        }

        return ValueTask.CompletedTask;
    }

    private void StartEdit()
    {
        _baseline = GuideDraft.From(StoredGuide);
        _draft = _baseline;
        _message = null;
        _editing = true;
    }

    private void CancelEdit()
    {
        if (HasUnsavedEdits)
        {
            _pendingTarget = null;
            _confirmDiscard = true;
            return;
        }

        _editing = false;
        _message = null;
    }

    private void KeepEditing()
    {
        _confirmDiscard = false;
        _pendingTarget = null;
    }

    private void Discard()
    {
        _confirmDiscard = false;
        _editing = false;
        _notesOpen = false;
        _message = null;
        if (_pendingTarget is { } target)
        {
            _pendingTarget = null;
            Navigation.NavigateTo(target);
        }
    }

    private async Task SaveSectionsAsync()
    {
        var edit = GuideEdits.WithSections(StoredGuide, ExerciseId, _draft.ToSections());
        if (edit is GuideEditResult.Invalid invalid)
        {
            _message = invalid.Message;
            return;
        }

        _message = null;
        if (edit is GuideEditResult.Valid valid && (await _save.CommitAsync(Change.Put(valid.Guide))).IsSaved)
        {
            _editing = false;
        }
    }

    private void StartNotes()
    {
        _notesBaseline = StoredGuide?.PersonalNotes ?? "";
        _notes = _notesBaseline;
        _notesOpen = true;
    }

    private void CloseNotes()
    {
        if (_notes != _notesBaseline)
        {
            _pendingTarget = null;
            _confirmDiscard = true;
            return;
        }

        _notesOpen = false;
    }

    private async Task SaveNotesAsync()
    {
        var result = await _save.CommitAsync(Change.Put(GuideEdits.WithNotes(StoredGuide, ExerciseId, _notes)));
        if (result.IsSaved)
        {
            _notesOpen = false;
        }
    }

    private async Task WatchDemoAsync(string url)
    {
        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>("import", VideoModule);
            _videoOffline = !await _module.InvokeAsync<bool>("openVideo", url);
        }
        catch (JSException)
        {
            _videoOffline = true;
        }
    }

    private static string PlannedText(SessionExercise row) => row switch
    {
        { Reps: { } reps, RepsMax: { } max } => $"{row.Sets} × {reps}–{max}",
        { Reps: { } reps } => $"{row.Sets} × {reps}",
        _ => $"{row.Sets} sets",
    };

    private static string PlacementText(GuidePlacement placement) =>
        placement.Row.Cue is { Length: > 0 } cue
            ? $"{placement.Session.Name}: {PlannedText(placement.Row)} · {cue}"
            : $"{placement.Session.Name}: {PlannedText(placement.Row)}";

    private string ResultText(LastResult result) =>
        result.BestSet is { } best ? SetText.Result(Exercise!.LogKind, best, Converter) : $"{result.Sets.Count} sets";
}
