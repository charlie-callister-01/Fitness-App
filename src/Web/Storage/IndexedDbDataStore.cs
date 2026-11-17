using System.Globalization;
using Core.Storage;

namespace Web.Storage;

/// <summary>
/// <see cref="IDataStore"/> over the IndexedDB module. Maps Core's kinds, failures and metadata to the module's
/// string surface; contains no storage rules. Opens the database lazily on first use.
/// </summary>
public sealed class IndexedDbDataStore(IIndexedDbInterop interop, string databaseName = IndexedDbDataStore.DefaultDatabaseName) : IDataStore
{
    public const string DefaultDatabaseName = "fitness-app";

    internal const string LastBackupAtKey = "lastBackupAt";
    internal const string LayoutVersionKey = "layoutVersion";
    internal const string RecordFormatVersionKey = "recordFormatVersion";
    internal const string InitialisedKey = "initialised";

    private static readonly Dictionary<RecordKind, string> ModuleKinds =
        Enum.GetValues<RecordKind>().ToDictionary(kind => kind, kind => kind.WireName());

    private bool _opened;

    public async Task<StoreResult<StoredData>> LoadAllAsync()
    {
        var (result, openFailure) = await CallAsync(interop.LoadAllAsync);
        if (openFailure is not null) return new StoreResult<StoredData>.Failure(openFailure);
        if (result!.Failure is { } failure) return new StoreResult<StoredData>.Failure(MapFailure(failure));

        var loaded = result.Value!;
        var records = new List<Core.Storage.StoredRecord>();
        foreach (var (kind, moduleKind) in ModuleKinds)
        {
            if (!loaded.Records.TryGetValue(moduleKind, out var raw)) continue;
            records.AddRange(raw.Select(r => new Core.Storage.StoredRecord(kind, r.Key, r.Json)));
        }
        return new StoreResult<StoredData>.Success(new StoredData(records, ReadMetadata(loaded.Meta)));
    }

    public async Task<StoreResult<Unit>> ApplyAsync(IReadOnlyList<StoreChange> changes, StoreMetadata? metadata = null)
    {
        var ops = changes.Select(c => c switch
        {
            StoreChange.Put p => IndexedDbOperation.Put(ModuleKinds[p.Record.Kind], p.Record.Key, p.Record.Json),
            StoreChange.Delete d => IndexedDbOperation.Delete(ModuleKinds[d.Kind], d.Key),
            _ => throw new ArgumentOutOfRangeException(nameof(changes), c, "Unknown change type"),
        }).ToList();
        return await RunAsync(() => interop.ApplyAsync(ops, metadata is null ? null : WriteMetadata(metadata)));
    }

    public Task<StoreResult<Unit>> ReplaceAllAsync(IReadOnlyList<Core.Storage.StoredRecord> records, StoreMetadata? metadata = null)
    {
        var grouped = ModuleKinds.ToDictionary(
            k => k.Value,
            k => (IReadOnlyList<RawRecord>)records.Where(r => r.Kind == k.Key).Select(r => new RawRecord(r.Key, r.Json)).ToList());
        return RunAsync(() => interop.ReplaceAllAsync(grouped, metadata is null ? null : WriteMetadata(metadata)));
    }

    public Task<StoreResult<Unit>> WriteMetadataAsync(StoreMetadata metadata) =>
        RunAsync(() => interop.ApplyAsync([], WriteMetadata(metadata)));

    /// <summary>
    /// A null value tells the module to delete the key, so "never written" round-trips. The layout version is not written from here:
    /// the module owns it and stamps it when it creates or upgrades the database.
    /// </summary>
    internal static IReadOnlyDictionary<string, string?> WriteMetadata(StoreMetadata m) => new Dictionary<string, string?>
    {
        [LastBackupAtKey] = m.LastBackupAt?.ToString("o", CultureInfo.InvariantCulture),
        [RecordFormatVersionKey] = m.RecordFormatVersion?.ToString(CultureInfo.InvariantCulture),
        [InitialisedKey] = m.Initialised ? "true" : null,
    };

    internal static StoreMetadata ReadMetadata(IReadOnlyDictionary<string, string> meta) => new(
        meta.TryGetValue(LastBackupAtKey, out var at) && DateTimeOffset.TryParse(at, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed) ? parsed : null,
        ReadInt(meta, LayoutVersionKey),
        ReadInt(meta, RecordFormatVersionKey),
        meta.TryGetValue(InitialisedKey, out var init) && init == "true");

    private static int? ReadInt(IReadOnlyDictionary<string, string> meta, string key) =>
        meta.TryGetValue(key, out var text) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : null;

    internal static StoreFailure MapFailure(IndexedDbFailure failure) => failure.Kind switch
    {
        IndexedDbFailureKind.Quota => new StoreFailure(StoreFailureKind.StorageFull, failure.Detail),
        IndexedDbFailureKind.Unavailable or IndexedDbFailureKind.NotOpen => new StoreFailure(StoreFailureKind.StorageUnavailable, failure.Detail),
        IndexedDbFailureKind.Blocked => new StoreFailure(StoreFailureKind.UpgradeBlocked, failure.Detail),
        _ => new StoreFailure(StoreFailureKind.Unexpected, failure.Detail),
    };

    private async Task<StoreFailure?> EnsureOpenAsync()
    {
        if (_opened) return null;
        var result = await interop.OpenAsync(databaseName);
        if (result.Failure is { } failure) return MapFailure(failure);
        _opened = true;
        return null;
    }

    /// <summary>
    /// Opens if needed and runs the call. If the module says its connection was closed (another tab upgraded the database, or the browser
    /// closed it), opens again and retries once; if that cannot open, the open failure comes back instead.
    /// </summary>
    private async Task<(IndexedDbResult<T>? Result, StoreFailure? OpenFailure)> CallAsync<T>(Func<Task<IndexedDbResult<T>>> call)
    {
        if (await EnsureOpenAsync() is { } openFailure) return (null, openFailure);
        var result = await call();
        if (result.Failure is not { Kind: IndexedDbFailureKind.NotOpen }) return (result, null);

        _opened = false;
        if (await EnsureOpenAsync() is { } reopenFailure) return (null, reopenFailure);
        return (await call(), null);
    }

    private async Task<StoreResult<Unit>> RunAsync(Func<Task<IndexedDbResult<object?>>> call)
    {
        var (result, openFailure) = await CallAsync(call);
        if (openFailure is not null) return new StoreResult<Unit>.Failure(openFailure);
        return result!.Failure is { } failure
            ? new StoreResult<Unit>.Failure(MapFailure(failure))
            : new StoreResult<Unit>.Success(default);
    }
}
