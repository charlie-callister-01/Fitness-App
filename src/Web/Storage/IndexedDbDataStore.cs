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
        if (await EnsureOpenAsync() is { } openFailure) return new StoreResult<StoredData>.Failure(openFailure);
        var result = await interop.LoadAllAsync();
        if (result.Failure is { } failure) return new StoreResult<StoredData>.Failure(MapFailure(failure));

        var loaded = result.Value!;
        var records = new List<Core.Storage.StoredRecord>();
        foreach (var (kind, moduleKind) in ModuleKinds)
        {
            if (!loaded.Records.TryGetValue(moduleKind, out var raw)) continue;
            records.AddRange(raw.Select(r => new Core.Storage.StoredRecord(kind, r.Key, r.Json)));
        }
        return new StoreResult<StoredData>.Success(new StoredData(records, ReadMetadata(loaded.Meta)));
    }

    public async Task<StoreResult<Unit>> ApplyAsync(IReadOnlyList<StoreChange> changes)
    {
        var ops = changes.Select(c => c switch
        {
            StoreChange.Put p => IndexedDbOperation.Put(ModuleKinds[p.Record.Kind], p.Record.Key, p.Record.Json),
            StoreChange.Delete d => IndexedDbOperation.Delete(ModuleKinds[d.Kind], d.Key),
            _ => throw new ArgumentOutOfRangeException(nameof(changes), c, "Unknown change type"),
        }).ToList();
        return await RunAsync(() => interop.ApplyAsync(ops));
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

    /// <summary>A null value tells the module to delete the key, so "never written" round-trips.</summary>
    internal static IReadOnlyDictionary<string, string?> WriteMetadata(StoreMetadata m) => new Dictionary<string, string?>
    {
        [LastBackupAtKey] = m.LastBackupAt?.ToString("o", CultureInfo.InvariantCulture),
        [LayoutVersionKey] = m.LayoutVersion?.ToString(CultureInfo.InvariantCulture),
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

    /// <summary>The module's "blocked" is not a closed Core kind: it becomes Unexpected, with "blocked" kept recognisable in the detail.</summary>
    internal static StoreFailure MapFailure(IndexedDbFailure failure) => failure.Kind switch
    {
        IndexedDbFailureKind.Quota => new StoreFailure(StoreFailureKind.StorageFull, failure.Detail),
        IndexedDbFailureKind.Unavailable => new StoreFailure(StoreFailureKind.StorageUnavailable, failure.Detail),
        IndexedDbFailureKind.Blocked => new StoreFailure(StoreFailureKind.Unexpected, $"blocked: {failure.Detail}"),
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

    private async Task<StoreResult<Unit>> RunAsync(Func<Task<IndexedDbResult<object?>>> call)
    {
        if (await EnsureOpenAsync() is { } openFailure) return new StoreResult<Unit>.Failure(openFailure);
        var result = await call();
        return result.Failure is { } failure
            ? new StoreResult<Unit>.Failure(MapFailure(failure))
            : new StoreResult<Unit>.Success(default);
    }
}
