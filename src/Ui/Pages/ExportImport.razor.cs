using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;

namespace Ui.Pages;

/// <summary>
/// Export, Import, Backup, and Prompt pack shell (design S-10). Honours <c>?tab=</c> and <c>?preset=</c> query parameters.
/// </summary>
public sealed partial class ExportImport : IDisposable
{
    private static readonly string[] Tabs = ["Export", "Import", "Prompts"];

    private PageContext Page = default!;
    private string _tab = "Export";
    private string? _preset;

    [Inject] private AppData Data { get; set; } = default!;
    [Inject] private IClock Clock { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized()
    {
        Page = new PageContext(Data, Clock);
        ReadQuery();
        Data.Changed += OnDataChanged;
    }

    public void Dispose() => Data.Changed -= OnDataChanged;

    private void OnDataChanged() => _ = InvokeAsync(StateHasChanged);

    private void ReadQuery()
    {
        _tab = TabQuery.Pick(Navigation.Uri, Tabs, "Export");
        _preset = QueryValue(Navigation.Uri, "preset");
    }

    private static string? QueryValue(string uri, string key) =>
        new Uri(uri).Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2 && string.Equals(p[0], key, StringComparison.OrdinalIgnoreCase))
            .Select(p => Uri.UnescapeDataString(p[1]))
            .FirstOrDefault();
}
