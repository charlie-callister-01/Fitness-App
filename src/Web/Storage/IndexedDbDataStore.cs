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

    private static readonly Dictionary<RecordKind, string> ModuleKinds = new()
    {
        [RecordKind.Exercise] = "exercises",
        [RecordKind.Settings] = "settings",
        [RecordKind.Plan] = "plans",
        [RecordKind.WeighIn] = "weighIns",
        [RecordKind.Break] = "breaks",
        [RecordKind.Badge] = "badges",
        [RecordKind.Guide] = "guides",
        [RecordKind.Workout] = "workouts",
        [RecordKind.WeekSchedule] = "weekSchedules",
    };

    private bool _opened;

    public async Task<StoreResult<StoredData>> LoadAllAsync()
    {
        var (result, openFailure) = await CallAsync(interop.LoadAllAsync);
        if (openFailure is not null)
        {
            return new StoreResult<StoredData>.Failure(openFailure);
        }

        if (result!.Failure is { } failure)
        {
            return new StoreResult<StoredData>.Failure(MapFailure(failure));
        }

        var loaded = result.Value!;
        var records = new List<StoredRecord>();
        foreach (var (kind, moduleKind) in ModuleKinds)
        {
            if (loaded.Records.TryGetValue(moduleKind, out var raw))
            {
                records.AddRange(raw.Select(rawRecord => new StoredRecord(kind, rawRecord.Key, rawRecord.Json)));
            }
        }
        return new StoreResult<StoredData>.Success(new StoredData(records, ReadMetadata(loaded.Meta)));
    }

    public async Task<StoreResult<Unit>> ApplyAsync(IReadOnlyList<StoreChange> changes, StoreMetadata? metadata = null)
    {
        var ops = changes.Select(change => change switch
        {
            StoreChange.Put put => IndexedDbOperation.Put(ModuleKinds[put.Record.Kind], put.Record.Key, put.Record.Json),
            StoreChange.Delete delete => IndexedDbOperation.Delete(ModuleKinds[delete.Kind], delete.Key),
            _ => throw new ArgumentOutOfRangeException(nameof(changes), change, "Unknown change type"),
        }).ToList();
        return await RunAsync(() => interop.ApplyAsync(ops, metadata is null ? null : WriteMetadata(metadata)));
    }

    public Task<StoreResult<Unit>> ReplaceAllAsync(IReadOnlyList<StoredRecord> records, StoreMetadata? metadata = null)
    {
        var grouped = new Dictionary<string, IReadOnlyList<RawRecord>>();
        foreach (var (kind, moduleKind) in ModuleKinds)
        {
            grouped[moduleKind] = records.Where(record => record.Kind == kind).Select(record => new RawRecord(record.Key, record.Json)).ToList();
        }

        return RunAsync(() => interop.ReplaceAllAsync(grouped, metadata is null ? null : WriteMetadata(metadata)));
    }

    public Task<StoreResult<Unit>> WriteMetadataAsync(StoreMetadata metadata) =>
        RunAsync(() => interop.ApplyAsync([], WriteMetadata(metadata)));

    /// <summary>
    /// A null value tells the module to delete the key, so "never written" round-trips. The layout version is not written from here:
    /// the module owns it and stamps it when it creates or upgrades the database.
    /// </summary>
    internal static IReadOnlyDictionary<string, string?> WriteMetadata(StoreMetadata metadata) => new Dictionary<string, string?>
    {
        [LastBackupAtKey] = metadata.LastBackupAt?.ToString("o", CultureInfo.InvariantCulture),
        [RecordFormatVersionKey] = metadata.RecordFormatVersion?.ToString(CultureInfo.InvariantCulture),
        [InitialisedKey] = metadata.Initialised ? "true" : null,
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
        if (_opened)
        {
            return null;
        }

        var result = await interop.OpenAsync(databaseName);
        if (result.Failure is { } failure)
        {
            return MapFailure(failure);
        }

        _opened = true;
        return null;
    }

    /// <summary>
    /// Opens if needed and runs the call. If the module says its connection was closed (another tab upgraded the database, or the browser
    /// closed it), opens again and retries once; if that cannot open, the open failure comes back instead.
    /// </summary>
    private async Task<(TResult? Result, StoreFailure? OpenFailure)> CallAsync<TResult>(Func<Task<TResult>> call)
        where TResult : IndexedDbResult
    {
        if (await EnsureOpenAsync() is { } openFailure)
        {
            return (null, openFailure);
        }

        var result = await call();
        if (result.Failure is not { Kind: IndexedDbFailureKind.NotOpen })
        {
            return (result, null);
        }

        _opened = false;
        if (await EnsureOpenAsync() is { } reopenFailure)
        {
            return (null, reopenFailure);
        }

        return (await call(), null);
    }

    private async Task<StoreResult<Unit>> RunAsync(Func<Task<IndexedDbResult>> call)
    {
        var (result, openFailure) = await CallAsync(call);
        if (openFailure is not null)
        {
            return new StoreResult<Unit>.Failure(openFailure);
        }

        return result!.Failure is { } failure
            ? new StoreResult<Unit>.Failure(MapFailure(failure))
            : new StoreResult<Unit>.Success(default);
    }
}
