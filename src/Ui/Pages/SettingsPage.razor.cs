using System.Globalization;
using Core.Exchange;
using Core.Model;
using Core.Rules.Editing;
using Core.Rules.Formatting;
using Core.Rules.Units;
using Core.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;
using Ui.Browser;
using Ui.Saving;
using Ui.Theming;

namespace Ui.Pages;

/// <summary>
/// Settings (design S-11): Look (Theme, Mode, live primary-button preview), Units and training, Streak rules, and Data
/// (last backed up + Back up action, persistent storage status + request, storage usage/quota, and PWA install status).
/// </summary>
public sealed partial class SettingsPage : IDisposable
{
    private PageContext Page = default!;
    private GuardedSave _save = default!;

    private string _weightIncrementText = "";
    private string? _weightIncrementError;
    private string _restTimerText = "";
    private string? _restTimerError;
    private string _goalWeightText = "";
    private string? _goalWeightError;
    private string _streakMinText = "";
    private string? _streakMinError;

    private string? _backupError;
    private bool _isStandalone;
    private bool _canInstall;

    [Inject] private AppData Data { get; set; } = default!;
    [Inject] private IClock Clock { get; set; } = default!;
    [Inject] private SaveFailureNotice Notice { get; set; } = default!;
    [Inject] private ThemeService Themes { get; set; } = default!;
    [Inject] private IBrowserCapabilities Browser { get; set; } = default!;
    [Inject] private IBuildInfo BuildInfo { get; set; } = default!;

    private Settings Current => Data.Snapshot.Settings;

    private bool WriteDisabled => !Page.CanWrite || _save.IsBusy;

    private string WeightUnitLabel => new UnitConverter(Current).WeightLabel;

    private string LastBackupText => Data.LastBackupAt is { } at
        ? DisplayFormat.WeekdayDay(DateOnly.FromDateTime(at.ToLocalTime().DateTime))
        : "Never";

    private string PersistenceStatusText => Data.Health.Persistence switch
    {
        PersistenceStatus.Granted => "Granted (the browser promised not to clear your data)",
        PersistenceStatus.NotGranted => "Not granted — the browser may clear data if storage gets tight. Request persistence or back up regularly.",
        _ => "Not supported in this browser",
    };

    private string? StorageEstimateText => Data.Health.Estimate is { } estimate
        ? $"{FormatBytes(estimate.UsageBytes)} used of {FormatBytes(estimate.QuotaBytes)}"
        : null;

    private string InstallStatusText => _isStandalone
        ? "Installed"
        : _canInstall
            ? "Ready to install"
            : "Not installed";

    protected override void OnInitialized()
    {
        Page = new PageContext(Data, Clock);
        _save = new GuardedSave(Data, Notice);
        SyncDraftsFromSnapshot();
        Data.Changed += OnDataChanged;
        Themes.Changed += OnThemeChanged;
        Browser.InstallAvailabilityChanged += OnInstallAvailabilityChanged;
    }

    protected override async Task OnInitializedAsync()
    {
        await RefreshDataStatusAsync();
    }

    public void Dispose()
    {
        Data.Changed -= OnDataChanged;
        Themes.Changed -= OnThemeChanged;
        Browser.InstallAvailabilityChanged -= OnInstallAvailabilityChanged;
    }

    private void OnDataChanged() => _ = InvokeAsync(() =>
    {
        SyncDraftsFromSnapshot();
        StateHasChanged();
    });

    private void OnThemeChanged() => _ = InvokeAsync(StateHasChanged);

    private void OnInstallAvailabilityChanged() => _ = InvokeAsync(async () =>
    {
        _isStandalone = await Browser.IsStandaloneAsync();
        _canInstall = await Browser.CanInstallAsync();
        StateHasChanged();
    });

    private async Task RefreshDataStatusAsync()
    {
        await Data.RefreshEstimateAsync();
        _isStandalone = await Browser.IsStandaloneAsync();
        _canInstall = await Browser.CanInstallAsync();
    }

    private void SyncDraftsFromSnapshot()
    {
        var settings = Current;
        _weightIncrementText = settings.WeightIncrementKg.ToString("0.##", CultureInfo.InvariantCulture);
        _restTimerText = settings.RestTimerSeconds.ToString(CultureInfo.InvariantCulture);
        _goalWeightText = SettingsRules.FormatGoalWeight(settings);
        _streakMinText = settings.StreakMinWorkoutsPerWeek.ToString(CultureInfo.InvariantCulture);
    }

    private Task SetWeightUnitAsync(WeightUnit unit) =>
        CommitSettingsAsync(Current with { DisplayWeightUnit = unit });

    private Task SetDistanceUnitAsync(DistanceUnit unit) =>
        CommitSettingsAsync(Current with { DisplayDistanceUnit = unit });

    private async Task OnWeightIncrementChangedAsync(ChangeEventArgs e)
    {
        _weightIncrementText = e.Value?.ToString() ?? "";
        var result = SettingsRules.TrySetWeightIncrement(Current, _weightIncrementText);
        _weightIncrementError = result.Error;
        if (result.Settings is { } updated)
        {
            await CommitSettingsAsync(updated);
        }
    }

    private async Task OnRestTimerChangedAsync(ChangeEventArgs e)
    {
        _restTimerText = e.Value?.ToString() ?? "";
        var result = SettingsRules.TrySetRestTimer(Current, _restTimerText);
        _restTimerError = result.Error;
        if (result.Settings is { } updated)
        {
            await CommitSettingsAsync(updated);
        }
    }

    private async Task OnGoalWeightChangedAsync(ChangeEventArgs e)
    {
        _goalWeightText = e.Value?.ToString() ?? "";
        var result = SettingsRules.TrySetGoalWeight(Current, _goalWeightText);
        _goalWeightError = result.Error;
        if (result.Settings is { } updated)
        {
            await CommitSettingsAsync(updated);
        }
    }

    private async Task OnStreakMinChangedAsync(ChangeEventArgs e)
    {
        _streakMinText = e.Value?.ToString() ?? "";
        var result = SettingsRules.TrySetStreakMinWorkouts(Current, _streakMinText);
        _streakMinError = result.Error;
        if (result.Settings is { } updated)
        {
            await CommitSettingsAsync(updated);
        }
    }

    private Task OnStreakCountsCustomChangedAsync(ChangeEventArgs e)
    {
        var countsCustom = e.Value is bool value && value;
        return CommitSettingsAsync(Current with { StreakCountsCustomWorkouts = countsCustom });
    }

    private Task CommitSettingsAsync(Settings updated) =>
        _save.CommitAsync(Change.Put(updated));

    private async Task BackUpAsync()
    {
        if (WriteDisabled)
        {
            return;
        }

        _backupError = null;
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
        }
    }

    private async Task RequestPersistenceAsync()
    {
        await Data.RequestPersistenceAsync();
        await Data.RefreshEstimateAsync();
    }

    private async Task InstallAppAsync()
    {
        await Browser.PromptInstallAsync();
        _isStandalone = await Browser.IsStandaloneAsync();
        _canInstall = await Browser.CanInstallAsync();
    }

    private static string FormatBytes(long bytes)
    {
        const double Megabyte = 1_000_000.0;
        const double Kilobyte = 1_000.0;
        if (bytes >= Megabyte)
        {
            return $"{(bytes / Megabyte).ToString("0.0", CultureInfo.InvariantCulture)} MB";
        }

        if (bytes >= Kilobyte)
        {
            return $"{(bytes / Kilobyte).ToString("0", CultureInfo.InvariantCulture)} KB";
        }

        return $"{bytes} B";
    }
}
