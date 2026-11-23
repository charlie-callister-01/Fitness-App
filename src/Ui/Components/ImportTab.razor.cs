using System.Text;
using Core.Exchange;
using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Ui.Accessibility;
using Ui.Browser;
using Ui.Pages;
using Ui.Saving;

namespace Ui.Components;

/// <summary>
/// The Import tab on <c>/backup</c> (design S-10, C-16, F-05, F-10): paste box + Check first, file picker second,
/// kind selection, validation errors, live preview with Exercise and Guide decisions, and atomic Apply with automatic pre-import Backup.
/// </summary>
public sealed partial class ImportTab : IDisposable
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;

    private static readonly (ImportKind Kind, string Label)[] KindOptions =
    [
        (ImportKind.Restore, "Backup"),
        (ImportKind.Plan, "Plan"),
        (ImportKind.Merge, "Merge"),
        (ImportKind.Guides, "Guides"),
    ];

    private PageContext Page = default!;
    private string _inputText = string.Empty;
    private ImportKind _kind = ImportKind.Plan;
    private bool _kindSelected;
    private bool _kindExplicit;
    private Inspected? _inspected;
    private ImportDecisions _decisions = ImportDecisions.None;
    private Core.Exchange.ImportPreview? _preview;
    private List<string> _errors = [];
    private List<string> _notices = [];
    private IReadOnlyList<string>? _appliedSummary;
    private bool _applying;

    [Parameter] public ImportKind? InitialKind { get; set; }

    [Inject] private AppData Data { get; set; } = default!;
    [Inject] private IClock Clock { get; set; } = default!;
    [Inject] private IBrowserCapabilities Browser { get; set; } = default!;
    [Inject] private SaveFailureNotice Notice { get; set; } = default!;
    [Inject] private Announcer Announcer { get; set; } = default!;

    protected override void OnInitialized()
    {
        Page = new PageContext(Data, Clock);
        if (InitialKind is { } kind)
        {
            _kind = kind;
            _kindSelected = true;
            _kindExplicit = true;
        }

        Data.Changed += OnDataChanged;
    }

    public void Dispose() => Data.Changed -= OnDataChanged;

    private void OnDataChanged() => _ = InvokeAsync(StateHasChanged);

    private void OnTextInput(ChangeEventArgs e)
    {
        _inputText = e.Value?.ToString() ?? string.Empty;
    }

    private void SelectKind(ImportKind kind)
    {
        _kind = kind;
        _kindSelected = true;
        _kindExplicit = true;
        if (_inspected is not null)
        {
            _decisions = ImportDecisions.None;
            RebuildPreview();
        }
    }

    private void Check()
    {
        _errors = [];
        _notices = [];
        _appliedSummary = null;

        if (string.IsNullOrWhiteSpace(_inputText))
        {
            _inspected = null;
            _preview = null;
            _errors = ["Paste JSON or choose a file first."];
            return;
        }

        var result = ImportInput.Inspect(_inputText);
        if (!result.IsAccepted)
        {
            _inspected = null;
            _preview = null;
            _errors = [.. result.Rejections];
            return;
        }

        _inspected = result.Inspected!;
        _notices = [.. _inspected.Notices, .. _inspected.Notes];
        if (!_kindExplicit)
        {
            _kind = _inspected.SuggestedKind;
            _kindSelected = true;
        }

        _decisions = ImportDecisions.None;
        RebuildPreview();
    }

    private async Task OnFileSelectedAsync(InputFileChangeEventArgs e)
    {
        _errors = [];
        _appliedSummary = null;
        try
        {
            await using var stream = e.File.OpenReadStream(MaxUploadBytes);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            _inputText = await reader.ReadToEndAsync();
            Check();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            _inspected = null;
            _preview = null;
            _errors = [$"Could not read the selected file: {ex.Message}"];
        }
    }

    private void OnDecisionsChanged(ImportDecisions decisions)
    {
        _decisions = decisions;
        RebuildPreview();
    }

    private void RebuildPreview()
    {
        if (_inspected is null)
        {
            _preview = null;
            return;
        }

        _preview = ImportPreviewer.Preview(Data, _inspected, _kind, _decisions, Clock.UtcNow);
    }

    private void CancelPreview()
    {
        _preview = null;
        _inspected = null;
        _notices = [];
        _errors = [];
    }

    private async Task ApplyAsync()
    {
        if (_preview is null || _applying || !Page.CanWrite)
        {
            return;
        }

        _applying = true;
        _errors = [];
        try
        {
            var sink = new BrowserBackupSink(Browser);
            var outcome = await ImportApplier.ApplyAsync(Data, _preview, sink, Clock.UtcNow);
            switch (outcome)
            {
                case ApplyOutcome.Applied applied:
                    _appliedSummary = applied.Summary;
                    Announcer.Announce(string.Join(" ", ["Import applied.", .. applied.Summary]));
                    _preview = null;
                    _inspected = null;
                    _notices = [];
                    break;
                case ApplyOutcome.BackupNotSaved:
                    _errors = ["Could not download the automatic Backup. Nothing was changed."];
                    break;
                case ApplyOutcome.PreviewOutdated:
                    _errors = ["Your data changed since this preview was built. Check again to see an updated preview."];
                    break;
                case ApplyOutcome.NotReady:
                    _errors = ["Resolve all questions and blockers before applying."];
                    break;
                case ApplyOutcome.Blocked:
                    _errors = ["Cannot apply because storage could not be read. Only a Restore can write."];
                    break;
                case ApplyOutcome.Rejected rejected:
                    _errors = rejected.Issues.Select(i => $"{i.Path}: {i.Message}").ToList();
                    break;
                case ApplyOutcome.StoreFailed failed:
                    Notice.Report(new CommitResult.StoreFailed(failed.Failure));
                    _errors = ["Could not save the Import."];
                    break;
            }
        }
        finally
        {
            _applying = false;
        }
    }

    private sealed class BrowserBackupSink(IBrowserCapabilities browser) : IBackupSink
    {
        public Task<bool> DeliverAsync(BackupFile file) =>
            browser.DownloadFileAsync(file.FileName, file.Text, "application/json");
    }
}
