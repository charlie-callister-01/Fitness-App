using Core.Exchange;
using Core.Rules.Formatting;
using Core.Rules.Insight;
using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;
using Ui.Browser;
using Ui.Pages;
using Ui.Saving;

namespace Ui.Components;

/// <summary>
/// The Export tab on <c>/backup</c> (design S-10, F-06): date range, include toggles, Health data switch,
/// Markdown/JSON format, live summary, Copy/Download/Share delivery, and Export everything (Backup).
/// </summary>
public sealed partial class ExportTab : IDisposable
{
    private PageContext Page = default!;
    private RangeChoice _range = default!;
    private bool _includeWorkouts = true;
    private bool _includePlan = true;
    private bool _includeExercises = true;
    private bool _includeGuides;
    private bool _includeHealthData;
    private ExportFormat _format = ExportFormat.Markdown;
    private ExportResult _result = default!;
    private bool _canShare;
    private string? _exportStatus;
    private string? _exportError;
    private bool _backingUp;
    private string? _backupStatus;
    private string? _backupError;

    [Parameter] public string? Preset { get; set; }

    [Inject] private AppData Data { get; set; } = default!;
    [Inject] private IClock Clock { get; set; } = default!;
    [Inject] private IBuildInfo BuildInfo { get; set; } = default!;
    [Inject] private IBrowserCapabilities Browser { get; set; } = default!;
    [Inject] private SaveFailureNotice Notice { get; set; } = default!;

    private DateOnly? PlanStart => Page.ActivePlan?.StartDate;

    private bool WriteDisabled => !Page.CanWrite || _backingUp;

    private string LastBackupText => Data.LastBackupAt is { } at
        ? DisplayFormat.WeekdayDay(DateOnly.FromDateTime(at.ToLocalTime().DateTime))
        : "Never";

    private string SummaryText => string.Join(" · ", _result.Summary);

    private string ContentType => _format == ExportFormat.Json ? "application/json" : "text/markdown";

    protected override void OnInitialized()
    {
        Page = new PageContext(Data, Clock);
        if (string.Equals(Preset, "llm", StringComparison.OrdinalIgnoreCase))
        {
            _range = new RangeChoice(RangePreset.Custom, new DateRange(Page.Today.AddDays(-6), Page.Today));
            _includeWorkouts = true;
            _includePlan = true;
            _includeExercises = true;
            _includeGuides = false;
            _includeHealthData = false;
            _format = ExportFormat.Markdown;
        }
        else
        {
            _range = new RangeChoice(
                RangePreset.Last12Weeks,
                RangeResolver.Resolve(RangePreset.Last12Weeks, Page.Today, PlanStart).Range!);
        }

        Data.Changed += OnDataChanged;
        Rebuild();
    }

    protected override async Task OnInitializedAsync()
    {
        _canShare = await Browser.CanShareAsync();
    }

    public void Dispose() => Data.Changed -= OnDataChanged;

    private void OnDataChanged() => _ = InvokeAsync(() =>
    {
        Rebuild();
        StateHasChanged();
    });

    private void OnRangeChanged(RangeChoice choice)
    {
        _range = choice;
        Rebuild();
    }

    private void ToggleWorkouts(bool value)
    {
        _includeWorkouts = value;
        Rebuild();
    }

    private void TogglePlan(bool value)
    {
        _includePlan = value;
        Rebuild();
    }

    private void ToggleExercises(bool value)
    {
        _includeExercises = value;
        Rebuild();
    }

    private void ToggleGuides(bool value)
    {
        _includeGuides = value;
        Rebuild();
    }

    private void ToggleHealthData(bool value)
    {
        _includeHealthData = value;
        Rebuild();
    }

    private void SetFormat(ExportFormat format)
    {
        _format = format;
        Rebuild();
    }

    private void Rebuild()
    {
        _exportStatus = null;
        _exportError = null;
        var request = new ExportRequest(
            _range.Range,
            Workouts: _includeWorkouts,
            Plan: _includePlan,
            WeighIns: _includeHealthData,
            PainChecks: _includeHealthData,
            Guides: _includeGuides,
            Exercises: _includeExercises,
            Format: _format);
        _result = Exporter.Export(Data.Snapshot, request, BuildInfo.Identifier, Clock.UtcNow);
    }

    private async Task CopyAsync()
    {
        _exportStatus = null;
        _exportError = null;
        if (await Browser.CopyTextAsync(_result.Text))
        {
            _exportStatus = _includeHealthData ? "Copied." : "Copied. Health data left out.";
        }
        else
        {
            _exportError = "Could not copy to the clipboard.";
        }
    }

    private async Task DownloadAsync()
    {
        _exportStatus = null;
        _exportError = null;
        if (await Browser.DownloadFileAsync(_result.FileName, _result.Text, ContentType))
        {
            _exportStatus = $"Downloaded {_result.FileName}.";
        }
        else
        {
            _exportError = "Could not start the download.";
        }
    }

    private async Task ShareAsync()
    {
        _exportStatus = null;
        _exportError = null;
        if (await Browser.ShareAsync("Fitness export", _result.Text, _result.FileName, ContentType))
        {
            _exportStatus = "Shared.";
        }
        else
        {
            _exportError = "Could not open the share sheet.";
        }
    }

    private async Task BackUpAsync()
    {
        if (WriteDisabled)
        {
            return;
        }

        _backingUp = true;
        _backupStatus = null;
        _backupError = null;
        try
        {
            var now = Clock.UtcNow;
            var exported = Exporter.Export(Data.Snapshot, ExportRequest.Backup, BuildInfo.Identifier, now);
            if (!await Browser.DownloadFileAsync(exported.FileName, exported.Text, "application/json"))
            {
                _backupError = "Could not start the Backup download.";
                return;
            }

            var recorded = await BackupRecorder.RecordAsync(Data, exported, now);
            if (recorded is StoreResult<Unit>.Failure failure)
            {
                Notice.Report(new CommitResult.StoreFailed(failure.Error));
                return;
            }

            _backupStatus = $"Backup downloaded ({exported.FileName}).";
        }
        finally
        {
            _backingUp = false;
        }
    }

    private static bool IsChecked(ChangeEventArgs e) => e.Value is bool value && value;
}
