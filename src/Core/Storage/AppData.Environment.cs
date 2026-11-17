using Core.Serialisation;

namespace Core.Storage;

/// <summary>What the browser said at open, held together because they are learned together and never change independently (except the estimate).</summary>
internal sealed record EnvironmentFacts(
    IStorageEnvironment Environment,
    PersistenceStatus Persistence,
    StorageEstimate? Estimate,
    bool DatabaseAvailable,
    bool HasWriterLock);

/// <summary>The browser-facing half of <see cref="AppData"/>: persistence, health, the writer lock, the cleared-data marker and the in-memory fallback.</summary>
public sealed partial class AppData
{
    private EnvironmentFacts _environment;

    /// <summary>The current storage facts. Refreshed by <see cref="RefreshEstimateAsync"/>; the rest is fixed at open.</summary>
    public StorageHealth Health => new(
        _environment.Persistence,
        _environment.Estimate,
        _environment.DatabaseAvailable,
        _environment.HasWriterLock && _state.Startup.State is not (StartupState.CannotRead or StartupState.StorageFailed),
        AnotherWindowOpen: !_environment.HasWriterLock);

    /// <summary>
    /// Opens over <paramref name="store"/>: asks for persistent storage (a denial blocks nothing), tries to take the writer lock (without
    /// it this window is read-only), loads, and tells first run from browser-cleared data using the marker. If the store reports
    /// <see cref="StoreFailureKind.StorageUnavailable"/>, runs on an in-memory store so the owner can still log. Without an
    /// <paramref name="environment"/> it behaves as a writable window with persistence granted and no marker (tests and tools; the app
    /// passes the real one).
    /// </summary>
    public static Task<AppData> OpenAsync(IDataStore store, IStorageEnvironment? environment = null) =>
        OpenAsync(store, environment ?? new InMemoryStorageEnvironment(), ExchangeFormat.CurrentVersion);

    /// <summary>
    /// Test seam: <paramref name="currentVersion"/> stands in for the app's record format version, so an older stored version can be
    /// exercised before a real format bump exists.
    /// </summary>
    internal static Task<AppData> OpenAsync(IDataStore store, int currentVersion) =>
        OpenAsync(store, new InMemoryStorageEnvironment(), currentVersion);

    private static async Task<AppData> OpenAsync(IDataStore store, IStorageEnvironment environment, int currentVersion)
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

        // With no database the marker says nothing about this (in-memory) store, so it is not consulted.
        var markerPresent = marker && databaseAvailable;
        var state = await loaded.Match(
            data => StartAsync(store, data, markerPresent, currentVersion),
            failure => Task.FromResult(new State(
                DataSnapshot.Empty, StoreMetadata.Empty, new StartupResult(StartupState.StorageFailed, [], failure))));

        return new AppData(store, state, new EnvironmentFacts(environment, persistence, estimate, databaseAvailable, hasLock), currentVersion);
    }

    private static async Task<State> StartAsync(IDataStore store, StoredData data, bool markerPresent, int currentVersion)
    {
        var read = new StartupReader(data, currentVersion).Read();
        var upgraded = await UpgradeIfOlderAsync(store, data, read, currentVersion);
        return new State(
            upgraded.Outcome.Snapshot, upgraded.Metadata, Classify(upgraded.Outcome.Startup, data, markerPresent));
    }

    /// <summary>Asks the environment again for usage and quota (for example when a health screen opens).</summary>
    public async Task RefreshEstimateAsync() =>
        _environment = _environment with { Estimate = await _environment.Environment.EstimateAsync() };

    /// <summary>
    /// The browser cleared the data only if the marker (outside the database) says something was saved, yet the database is empty and
    /// not marked initialised. The initialised flag lives inside the database, so if it is still set the database was not wiped: an
    /// empty store is then simply empty (<see cref="StartupState.Ready"/>).
    /// </summary>
    private static StartupResult Classify(StartupResult read, StoredData data, bool markerPresent)
    {
        var storeLooksWiped = data.Records.Count == 0 && markerPresent && !data.Metadata.Initialised;
        var readCleanly = read.State is StartupState.Ready or StartupState.FirstRun;
        return storeLooksWiped && readCleanly ? read with { State = StartupState.BrowserCleared } : read;
    }

    /// <summary>With no database nothing is kept, so a marker would claim data that is not there.</summary>
    private async Task WriteMarkerAsync()
    {
        if (_environment.DatabaseAvailable)
        {
            await _environment.Environment.WriteClearedDataMarkerAsync();
        }
    }
}
