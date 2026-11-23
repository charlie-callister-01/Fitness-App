using Core.Exchange;
using Core.Rules.Formatting;
using Core.Storage;
using Core.Time;
using Ui.Accessibility;
using Ui.Browser;

namespace Ui.Saving;

/// <summary>Result of running the shared Backup download and timestamp-recording flow: the success message once it finished, else an error to show (or neither when it was busy or the store failure went to the notice).</summary>
public sealed record BackupOutcome(string? Message, string? Error);

/// <summary>Shared Backup download + timestamp recording with a busy guard and last-backup formatting; a finished Backup is announced politely.</summary>
public sealed class BackupFlow(
    AppData data,
    IClock clock,
    IBuildInfo buildInfo,
    IBrowserCapabilities browser,
    SaveFailureNotice notice,
    Announcer announcer)
{
    public bool IsBusy { get; private set; }

    /// <summary>What a finished Backup says, on screen and through the <see cref="Announcer"/>.</summary>
    private static string SuccessMessage(string fileName) => $"Backup downloaded ({fileName}).";

    public static string FormatLastBackup(DateTimeOffset? lastBackupAt) => lastBackupAt is { } at
        ? DisplayFormat.WeekdayDay(DateOnly.FromDateTime(at.ToLocalTime().DateTime))
        : "Never";

    public async Task<BackupOutcome> RunAsync()
    {
        if (IsBusy)
        {
            return new BackupOutcome(null, null);
        }

        IsBusy = true;
        try
        {
            var now = clock.UtcNow;
            var exported = Exporter.Export(data.Snapshot, ExportRequest.Backup, buildInfo.Identifier, now);
            if (!await browser.DownloadFileAsync(exported.FileName, exported.Text, "application/json"))
            {
                return new BackupOutcome(null, "Could not start the Backup download.");
            }

            var recorded = await BackupRecorder.RecordAsync(data, exported, now);
            if (recorded is StoreResult<Unit>.Failure failure)
            {
                notice.Report(new CommitResult.StoreFailed(failure.Error));
                return new BackupOutcome(null, null);
            }

            var message = SuccessMessage(exported.FileName);
            announcer.Announce(message);
            return new BackupOutcome(message, null);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
