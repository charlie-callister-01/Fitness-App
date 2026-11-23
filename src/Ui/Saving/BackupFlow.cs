using Core.Exchange;
using Core.Rules.Formatting;
using Core.Storage;
using Core.Time;
using Ui.Browser;

namespace Ui.Saving;

/// <summary>Result of running the shared Backup download and timestamp-recording flow.</summary>
public sealed record BackupOutcome(bool Succeeded, string? FileName, string? Error);

/// <summary>Shared Backup download + timestamp recording with a busy guard and last-backup formatting.</summary>
public sealed class BackupFlow(
    AppData data,
    IClock clock,
    IBuildInfo buildInfo,
    IBrowserCapabilities browser,
    SaveFailureNotice notice)
{
    public bool IsBusy { get; private set; }

    public static string FormatLastBackup(DateTimeOffset? lastBackupAt) => lastBackupAt is { } at
        ? DisplayFormat.WeekdayDay(DateOnly.FromDateTime(at.ToLocalTime().DateTime))
        : "Never";

    public async Task<BackupOutcome> RunAsync()
    {
        if (IsBusy)
        {
            return new BackupOutcome(false, null, null);
        }

        IsBusy = true;
        try
        {
            var now = clock.UtcNow;
            var exported = Exporter.Export(data.Snapshot, ExportRequest.Backup, buildInfo.Identifier, now);
            if (!await browser.DownloadFileAsync(exported.FileName, exported.Text, "application/json"))
            {
                return new BackupOutcome(false, null, "Could not start the Backup download.");
            }

            var recorded = await BackupRecorder.RecordAsync(data, exported, now);
            if (recorded is StoreResult<Unit>.Failure failure)
            {
                notice.Report(new CommitResult.StoreFailed(failure.Error));
                return new BackupOutcome(false, null, null);
            }

            return new BackupOutcome(true, exported.FileName, null);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
