namespace Core.Storage;

/// <summary>The browser-facing half of <see cref="AppData"/>: persistence, health, the writer lock, the cleared-data marker and the in-memory fallback.</summary>
public sealed partial class AppData
{
    private IStorageEnvironment _environment = new InMemoryStorageEnvironment();
    private PersistenceStatus _persistence;
    private StorageEstimate? _estimate;
    private bool _databaseAvailable = true;
    private bool _hasWriterLock = true;

    /// <summary>The current storage facts. Refreshed by <see cref="RefreshEstimateAsync"/>; the rest is fixed at open.</summary>
    public StorageHealth Health => new(
        _persistence,
        _estimate,
        _databaseAvailable,
        _hasWriterLock && Startup.State is not (StartupState.CannotRead or StartupState.StorageFailed),
        AnotherWindowOpen: !_hasWriterLock);

    /// <summary>
    /// Opens over <paramref name="store"/>: asks for persistent storage (a denial blocks nothing), tries to take the writer lock (without
    /// it this window is read-only), loads, and tells first run from browser-cleared data using the marker. If the store reports
    /// <see cref="StoreFailureKind.StorageUnavailable"/>, runs on an in-memory store so the owner can still log.
    /// </summary>
    internal static async Task<AppData> OpenAsync(IDataStore store, IStorageEnvironment environment, int currentVersion)
    {
        var persistence = await environment.RequestPersistenceAsync();
        var estimate = await environment.EstimateAsync();
        var hasLock = await environment.TryTakeWriterLockAsync();
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
                app = new AppData(store, Classify(startup, success.Value, marker), metadata, currentVersion);
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
        app._hasWriterLock = hasLock;
        return app;
    }

    /// <summary>Asks the environment again for usage and quota (for example when a health screen opens).</summary>
    public async Task RefreshEstimateAsync() => _estimate = await _environment.EstimateAsync();

    /// <summary>An empty store is a first run only if nothing says data once existed: the marker or the initialised metadata means the browser cleared it.</summary>
    private static StartupResult Classify(StartupResult read, StoredData data, bool markerPresent) =>
        data.Records.Count == 0 && read.State is StartupState.Ready or StartupState.FirstRun
            ? read with { State = markerPresent || data.Metadata.Initialised ? StartupState.BrowserCleared : StartupState.FirstRun }
            : read;

    private StoreFailure? WriterLockFailure() => _hasWriterLock ? null : new StoreFailure(StoreFailureKind.WriterLockHeld);

    /// <summary>With no database nothing is kept, so a marker would claim data that is not there.</summary>
    private async Task WriteMarkerAsync()
    {
        if (_databaseAvailable)
        {
            await _environment.WriteClearedDataMarkerAsync();
        }
    }
}
