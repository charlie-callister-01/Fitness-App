using Core.Serialisation;
using Core.Storage;

namespace Core.Exchange;

/// <summary>The automatic Backup handed to the host, which saves it somewhere (in the browser, a file download).</summary>
public sealed record BackupFile(string Text, string FileName, DateTimeOffset At);

/// <summary>Where the pre-Import Backup goes. The host decides how; true means it was handed over safely.</summary>
public interface IBackupSink
{
    Task<bool> DeliverAsync(BackupFile file);
}

/// <summary>How an Import ended. Anything but <see cref="Applied"/> left the data as it was.</summary>
public abstract record ApplyOutcome
{
    private ApplyOutcome() { }

    public sealed record Applied(IReadOnlyList<string> Summary) : ApplyOutcome;

    public sealed record Rejected(IReadOnlyList<FormatIssue> Issues) : ApplyOutcome;

    public sealed record StoreFailed(StoreFailure Failure) : ApplyOutcome;

    public sealed record Blocked(StartupState State) : ApplyOutcome;

    /// <summary>The data changed since the preview was built; look at a fresh preview.</summary>
    public sealed record PreviewOutdated : ApplyOutcome;

    /// <summary>The Backup could not be saved, so the Import was stopped.</summary>
    public sealed record BackupNotSaved : ApplyOutcome;

    /// <summary>The preview still has a blocker or an open choice.</summary>
    public sealed record NotReady : ApplyOutcome;
}

/// <summary>
/// The one write of an Import. Refuses a preview that is not ready or out of date, delivers a Backup of the current data, stamps it,
/// then commits the preview's changes (Restore replaces everything, the other kinds are one atomic batch).
/// </summary>
public static class ImportApplier
{
    private const string BackupBuild = "Fitness App (automatic Backup before Import)";

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
            ? await appData.ReplaceAllAsync(replacement, preview.BackupStamp)
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
    private static bool HasReadableData(AppData appData)
    {
        if (appData.Startup.State is StartupState.CannotRead or StartupState.StorageFailed)
        {
            return false;
        }

        var s = appData.Snapshot;
        return s.Exercises.Count + s.WeighIns.Count + s.Breaks.Count + s.Badges.Count + s.Guides.Count
            + s.Plans.Count + s.Workouts.Count + s.WeekSchedules.Count > 0;
    }

    /// <summary>Delivers and stamps the Backup; returns the outcome that stops the Import, or null to go on.</summary>
    private static async Task<ApplyOutcome?> BackUpAsync(AppData appData, IBackupSink backupSink, DateTimeOffset now)
    {
        var backup = Exporter.Export(appData.Snapshot, ExportRequest.Backup, BackupBuild, now);
        var fileName = $"fitness-backup-before-import-{DateOnly.FromDateTime(now.DateTime):yyyy-MM-dd}.json";
        if (!await backupSink.DeliverAsync(new BackupFile(backup.Text, fileName, now)))
        {
            return new ApplyOutcome.BackupNotSaved();
        }

        return await BackupRecorder.RecordAsync(appData, backup, now) is StoreResult<Unit>.Failure failure
            ? new ApplyOutcome.StoreFailed(failure.Error)
            : null;
    }
}
