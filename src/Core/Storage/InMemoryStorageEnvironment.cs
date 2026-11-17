namespace Core.Storage;

/// <summary>
/// A scriptable <see cref="IStorageEnvironment"/> with no browser behind it. It is in <c>Core</c> (not the test project) because the
/// Web host and the browser test page need it too. The defaults are the happy path: persistence granted, lock free, no marker.
/// </summary>
public sealed class InMemoryStorageEnvironment : IStorageEnvironment
{
    /// <summary>The answer to the persistence request.</summary>
    public PersistenceStatus Persistence { get; set; } = PersistenceStatus.Granted;

    /// <summary>The answer to the estimate; null means the browser cannot say.</summary>
    public StorageEstimate? Estimate { get; set; } = new(1_000_000, 100_000_000);

    /// <summary>False simulates another window holding the writer lock.</summary>
    public bool WriterLockAvailable { get; set; } = true;

    /// <summary>The cleared-data marker.</summary>
    public bool MarkerPresent { get; set; }

    /// <summary>How many times persistence was requested.</summary>
    public int PersistenceRequests { get; private set; }

    public Task<PersistenceStatus> RequestPersistenceAsync()
    {
        PersistenceRequests++;
        return Task.FromResult(Persistence);
    }

    public Task<StorageEstimate?> EstimateAsync() => Task.FromResult(Estimate);

    public Task<bool> TryTakeWriterLockAsync() => Task.FromResult(WriterLockAvailable);

    public Task<bool> ReadClearedDataMarkerAsync() => Task.FromResult(MarkerPresent);

    public Task WriteClearedDataMarkerAsync()
    {
        MarkerPresent = true;
        return Task.CompletedTask;
    }
}
