namespace Core.Storage;

/// <summary>Whether the browser promised not to evict the data.</summary>
public enum PersistenceStatus
{
    Granted,
    NotGranted,
    Unsupported,
}

/// <summary>How much storage is used and how much the browser allows, in bytes.</summary>
public readonly record struct StorageEstimate(long UsageBytes, long QuotaBytes);

/// <summary>
/// The facts only the browser knows. No browser types here: the Web host implements this over the browser; tests use
/// <see cref="InMemoryStorageEnvironment"/>. No member throws for an expected failure (a browser that lacks a feature answers
/// "unsupported", "unknown" or "not obtained").
/// </summary>
public interface IStorageEnvironment
{
    /// <summary>Asks the browser to keep the data (persistent storage). Called at every launch until the answer is <see cref="PersistenceStatus.Granted"/>.</summary>
    Task<PersistenceStatus> RequestPersistenceAsync();

    /// <summary>Usage and quota, or null when the browser cannot say.</summary>
    Task<StorageEstimate?> EstimateAsync();

    /// <summary>Tries to take the exclusive writer lock for this window's lifetime. False means another window holds it.</summary>
    Task<bool> TryTakeWriterLockAsync();

    /// <summary>True if the cleared-data marker (kept outside the database, so a database wipe can be detected) is present.</summary>
    Task<bool> ReadClearedDataMarkerAsync();

    /// <summary>Writes the marker. Written once the first save has succeeded.</summary>
    Task WriteClearedDataMarkerAsync();
}
