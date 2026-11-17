using Core.Storage;

namespace Core.Exchange;

/// <summary>
/// Stamps the Backup date once the host knows a Backup was delivered. <see cref="Exporter"/> cannot do it, because only the host knows
/// the file was saved.
/// </summary>
public static class BackupRecorder
{
    /// <summary>Records <paramref name="now"/> as the last Backup when <paramref name="result"/> is a Backup; does nothing for any other Export.</summary>
    public static Task<StoreResult<Unit>> RecordAsync(AppData appData, ExportResult result, DateTimeOffset now) =>
        result.CountsAsBackup
            ? appData.RecordBackupAsync(now)
            : Task.FromResult<StoreResult<Unit>>(new StoreResult<Unit>.Success(default));
}
