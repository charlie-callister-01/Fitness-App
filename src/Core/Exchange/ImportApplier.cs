using Core.Storage;

namespace Core.Exchange;

/// <summary>
/// The one write of an Import. Refuses a preview that is not ready or out of date, delivers a Backup of the current data, stamps it,
/// then commits the preview's changes (Restore replaces everything, the other kinds are one atomic batch).
/// </summary>
public static class ImportApplier
{
    private const string BackupBuild = "Fitness App (automatic Backup before Import)";

    /// <param name="now">Stamps the Backup date for a Restore whose file has no <c>exportedAt</c>, and dates the automatic Backup.</param>
    public static async Task<ApplyOutcome> ApplyAsync(AppData appData, ImportPreview preview, IBackupSink backupSink, DateTimeOffset now)
    {
        if (!preview.IsReady)
        {
            return new ApplyOutcome.NotReady();
        }

        if (!ReferenceEquals(preview.Source, appData.Snapshot))
        {
            return new ApplyOutcome.PreviewOutdated();
        }

        if (HasReadableData(appData) && await BackUpAsync(appData, backupSink, now) is { } stopped)
        {
            return stopped;
        }

        var commit = preview.Replacement is { } replacement
            ? await appData.ReplaceAllAsync(replacement, preview.BackupStamp ?? now)
            : await appData.CommitAsync(preview.Changes);
        return commit switch
        {
            CommitResult.Committed => new ApplyOutcome.Applied(preview.Summary),
            CommitResult.Rejected rejected => new ApplyOutcome.Rejected(rejected.Issues),
            CommitResult.StoreFailed failed => new ApplyOutcome.StoreFailed(failed.Failure),
            CommitResult.Blocked blocked => new ApplyOutcome.Blocked(blocked.State),
            _ => throw new InvalidOperationException($"Unknown commit result {commit.GetType().Name}."),
        };
    }

    /// <summary>There is nothing worth saving when startup could not read the data, or no record exists.</summary>
    private static bool HasReadableData(AppData appData) =>
        appData.Startup.State is not (StartupState.CannotRead or StartupState.StorageFailed) && !appData.Snapshot.IsEmpty;

    /// <summary>Delivers and stamps the Backup; returns the outcome that stops the Import, or null to go on.</summary>
    private static async Task<ApplyOutcome?> BackUpAsync(AppData appData, IBackupSink backupSink, DateTimeOffset now)
    {
        var backup = Exporter.Export(appData.Snapshot, ExportRequest.Backup, BackupBuild, now);
        var file = new BackupFile(backup.Text, ExportSummary.BackupFileName(now, beforeImport: true), now);
        if (!await backupSink.DeliverAsync(file))
        {
            return new ApplyOutcome.BackupNotSaved();
        }

        return await BackupRecorder.RecordAsync(appData, backup, now) is StoreResult<Unit>.Failure failure
            ? new ApplyOutcome.StoreFailed(failure.Error)
            : null;
    }
}
