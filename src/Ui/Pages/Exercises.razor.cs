using Core.Model;
using Core.Rules.Library;
using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;
using Ui.Components;

namespace Ui.Pages;

public sealed partial class Exercises : IDisposable
{
    private string _search = "";
    private string? _tag;
    private bool _adding;
    private string? _offer;
    private PageContext Page = default!;

    [Inject] private AppData Data { get; set; } = default!;

    [Inject] private IClock Clock { get; set; } = default!;

    private LibraryView View => LibraryViewBuilder.Build(Page.ActivePlan, Data.Snapshot.Exercises, Data.Snapshot.Guides, _search, _tag);

    protected override void OnInitialized()
    {
        Page = new PageContext(Data, Clock);
        Data.Changed += OnDataChanged;
    }

    public void Dispose() => Data.Changed -= OnDataChanged;

    private void OnDataChanged() => _ = InvokeAsync(StateHasChanged);

    private bool IsTag(string tag) => string.Equals(_tag, tag, StringComparison.OrdinalIgnoreCase);

    /// <summary>Choosing the tag that is already chosen clears it, so a chip toggles.</summary>
    private void PickTag(string? tag) => _tag = tag is null || IsTag(tag) ? null : tag;

    private void StartAdding(string? offer)
    {
        _offer = offer;
        _adding = true;
    }

    private void StopAdding() => _adding = false;

    private static string GuideHref(string exerciseId) => $"exercises/{exerciseId}/guide";

    private static string MetaOf(Exercise exercise)
    {
        var tags = string.Join(" · ", LibraryViewBuilder.TagWords(exercise));
        return tags.Length == 0 ? LogKindLabels.For(exercise.LogKind) : $"{LogKindLabels.For(exercise.LogKind)} · {tags}";
    }
}
