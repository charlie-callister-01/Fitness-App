using Core.Exchange;
using Core.Model;
using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;
using Ui.Browser;
using Ui.Saving;
using Ui.Pages;

namespace Ui.Components;

/// <summary>
/// The Prompts tab on <c>/backup</c> (design S-10): three ready-made prompt cards (Analyse my training, Write me a Plan, Write Guides).
/// </summary>
public sealed partial class PromptsTab : IDisposable
{
    private enum PromptCard
    {
        Analyse,
        Plan,
        Guides,
    }

    private PageContext Page = default!;
    private IReadOnlyList<Exercise> _exercises = [];
    private HashSet<string> _selectedGuideExercises = [];
    private PromptCard? _statusCard;
    private string? _statusMessage;
    private PromptCard? _errorCard;
    private string? _errorMessage;

    [Inject] private AppData Data { get; set; } = default!;
    [Inject] private IClock Clock { get; set; } = default!;
    [Inject] private IBuildInfo BuildInfo { get; set; } = default!;
    [Inject] private IBrowserCapabilities Browser { get; set; } = default!;
    [Inject] private Announcer Announcer { get; set; } = default!;

    protected override void OnInitialized()
    {
        Page = new PageContext(Data, Clock);
        RefreshExercises(resetSelection: true);
        Data.Changed += OnDataChanged;
    }

    public void Dispose() => Data.Changed -= OnDataChanged;

    private void OnDataChanged() => _ = InvokeAsync(() =>
    {
        RefreshExercises(resetSelection: false);
        StateHasChanged();
    });

    private void RefreshExercises(bool resetSelection)
    {
        _exercises = Data.Snapshot.Exercises
            .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (resetSelection)
        {
            _selectedGuideExercises = _exercises
                .Where(e => !HasGuide(e.Id))
                .Select(e => e.Id)
                .ToHashSet();
        }
        else
        {
            var valid = _exercises.Select(e => e.Id).ToHashSet();
            _selectedGuideExercises.IntersectWith(valid);
        }
    }

    private bool HasGuide(string exerciseId) =>
        PromptPack.HasGuideContent(Data.Snapshot, exerciseId);

    private void ToggleGuideExercise(string exerciseId, bool selected)
    {
        if (selected)
        {
            _selectedGuideExercises.Add(exerciseId);
        }
        else
        {
            _selectedGuideExercises.Remove(exerciseId);
        }

        ClearFeedback();
    }

    private Task CopyAnalyseAsync()
    {
        var request = ExportRequest.Default(Page.Today);
        var export = Exporter.Export(Data.Snapshot, request, BuildInfo.Identifier, Clock.UtcNow);
        var text = PromptPack.Analyse(Data.Snapshot, export, request.Range);
        return CopyCardAsync(PromptCard.Analyse, text);
    }

    private Task CopyPlanAsync() =>
        CopyCardAsync(PromptCard.Plan, PromptPack.WritePlan(Data.Snapshot));

    private Task CopyGuidesAsync() =>
        CopyCardAsync(PromptCard.Guides, PromptPack.WriteGuides(Data.Snapshot, _selectedGuideExercises));

    private async Task CopyCardAsync(PromptCard card, string text)
    {
        ClearFeedback();

        if (await Browser.CopyTextAsync(text))
        {
            _statusCard = card;
            _statusMessage = "Copied.";
            Announcer.Announce(_statusMessage);
        }
        else
        {
            _errorCard = card;
            _errorMessage = "Could not copy to the clipboard.";
        }
    }

    private void ClearFeedback()
    {
        _statusCard = null;
        _statusMessage = null;
        _errorCard = null;
        _errorMessage = null;
    }

    private static bool IsChecked(ChangeEventArgs args) => args.Value is true;
}
