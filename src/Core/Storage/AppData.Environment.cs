namespace Core.Storage;

/// <summary>The browser-facing half of <see cref="AppData"/>: persistence, health, the writer lock, the cleared-data marker and the in-memory fallback.</summary>
public sealed partial class AppData
{
    private IStorageEnvironment _environment = new InMemoryStorageEnvironment();
    private PersistenceStatus _persistence;
    private StorageEstimate? _estimate;
    private bool _databaseAvailable = true;
    private WriterLockResult _writerLock = WriterLockResult.Obtained;

    /// <summary>The current storage facts. Refreshed by <see cref="RefreshEstimateAsync"/>; the rest is fixed at open.</summary>
    public StorageHealth Health => new(
        _persistence,
        _estimate,
        _databaseAvailable,
        HasWriterLock && Startup.State is not (StartupState.CannotRead or StartupState.StorageFailed),
        _writerLock);

    /// <summary>
    /// Opens over <paramref name="store"/>: asks for persistent storage (a denial blocks nothing), tries to take the writer lock (without
    /// it this window is read-only), loads, and tells first run from browser-cleared data using the marker. If the store reports
    /// <see cref="StoreFailureKind.StorageUnavailable"/>, runs on an in-memory store so the owner can still log.
    /// </summary>
    internal static async Task<AppData> OpenAsync(IDataStore store, IStorageEnvironment environment, int currentVersion)
    {
        var persistence = await environment.RequestPersistenceAsync();
        var estimate = await environment.EstimateAsync();
        var writerLock = await environment.TryTakeWriterLockAsync();
        var marker = await environment.ReadClearedDataMarkerAsync();

        var databaseAvailable = true;
        var loaded = await store.LoadAllAsync();
        if (loaded is StoreResult<StoredData>.Failure { Error.Kind: StoreFailureKind.StorageUnavailable })
        {
            databaseAvailable = false;
            store = new InMemoryDataStore();
            loaded = await store.LoadAllAsync();
        }

        AppData app;
        switch (loaded)
        {
            case StoreResult<StoredData>.Success success:
                var (startup, metadata) = await UpgradeIfOlderAsync(store, success.Value, Read(success.Value, currentVersion), currentVersion);
                // With no database the marker says nothing about this (in-memory) store, so it is not consulted.
                app = new AppData(store, Classify(startup, success.Value, marker && databaseAvailable), metadata, currentVersion);
                break;
            case StoreResult<StoredData>.Failure failure:
                app = new AppData(
                    store, new StartupResult(StartupState.StorageFailed, DataSnapshot.Empty, [], failure.Error), StoreMetadata.Empty, currentVersion);
                break;
            default:
                throw new InvalidOperationException("Unreachable: StoreResult has two cases.");
        }

        app._environment = environment;
        app._persistence = persistence;
        app._estimate = estimate;
        app._databaseAvailable = databaseAvailable;
        app._writerLock = writerLock;
        return app;
    }

    /// <summary>Asks the environment again for usage and quota (for example when a health screen opens).</summary>
    public async Task RefreshEstimateAsync() => _estimate = await _environment.EstimateAsync();

    /// <summary>
    /// The browser cleared the data only if the marker (outside the database) says something was saved, yet the database is empty and
    /// not marked initialised. The initialised flag lives inside the database, so if it is still set the database was not wiped: an
    /// empty store is then simply empty (<see cref="StartupState.Ready"/>).
    /// </summary>
    private static StartupResult Classify(StartupResult read, StoredData data, bool markerPresent) =>
        data.Records.Count == 0 && markerPresent && !data.Metadata.Initialised && read.State is StartupState.Ready or StartupState.FirstRun
            ? read with { State = StartupState.BrowserCleared }
            : read;

    /// <summary>The writer-lock policy: only a lock that was obtained, or cannot exist in this browser, lets this window write.</summary>
    private bool HasWriterLock => _writerLock is WriterLockResult.Obtained or WriterLockResult.Unsupported;

    private StoreFailure? WriterLockFailure() => HasWriterLock ? null : new StoreFailure(StoreFailureKind.WriterLockHeld);

    /// <summary>With no database nothing is kept, so a marker would claim data that is not there.</summary>
    private async Task WriteMarkerAsync()
    {
        if (_databaseAvailable)
        {
            await _environment.WriteClearedDataMarkerAsync();
        }
    }
}
