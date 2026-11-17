namespace Web.Storage;

/// <summary>The module's estimate answer, in bytes.</summary>
public sealed record StorageUsage(long Usage, long Quota);

/// <summary>The slice of <c>wwwroot/js/storage-environment.js</c> the environment uses; exists so the mapping can be tested with a fake.</summary>
public interface IStorageEnvironmentInterop
{
    /// <summary>"granted", "notGranted" or "unsupported".</summary>
    Task<string> RequestPersistenceAsync();

    /// <summary>Usage and quota, or null when the browser cannot say.</summary>
    Task<StorageUsage?> EstimateAsync();

    /// <summary>"obtained", "heldElsewhere", "unsupported" or "unknown".</summary>
    Task<string> TryTakeWriterLockAsync();

    Task<bool> ReadClearedDataMarkerAsync();

    Task WriteClearedDataMarkerAsync();
}
