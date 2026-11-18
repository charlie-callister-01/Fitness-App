namespace Core.Exchange;

/// <summary>Where the pre-Import Backup goes. The host decides how; true means it was handed over safely.</summary>
public interface IBackupSink
{
    Task<bool> DeliverAsync(BackupFile file);
}
