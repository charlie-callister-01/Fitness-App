using Core.Storage;
using Web.Storage;

namespace Web.Tests.Storage;

public class IndexedDbDataStoreTests
{
    private sealed class FakeInterop : IIndexedDbInterop
    {
        public IndexedDbFailure? OpenFailure { get; set; }
        public IndexedDbFailure? CallFailure { get; set; }
        public int OpenCalls { get; private set; }
        /// <summary>The next this-many calls (after open) report that the module lost its connection.</summary>
        public int NotOpenResponses { get; set; }
        private IndexedDbFailure? NextFailure() =>
            NotOpenResponses-- > 0 ? new IndexedDbFailure(IndexedDbFailureKind.NotOpen, "closed") : CallFailure;
        public LoadedData Loaded { get; set; } = new(new Dictionary<string, IReadOnlyList<RawRecord>>(), new Dictionary<string, string>());
        public IReadOnlyList<IndexedDbOperation>? AppliedOps { get; private set; }
        public IReadOnlyDictionary<string, string?>? AppliedMeta { get; private set; }
        public IReadOnlyDictionary<string, IReadOnlyList<RawRecord>>? Replaced { get; private set; }
        public IReadOnlyDictionary<string, string?>? ReplacedMeta { get; private set; }

        public Task<IndexedDbResult> OpenAsync(string databaseName)
        {
            OpenCalls++;
            return Task.FromResult(new IndexedDbResult(OpenFailure));
        }

        public Task<IndexedDbResult<LoadedData>> LoadAllAsync() => Task.FromResult(new IndexedDbResult<LoadedData>(Loaded, NextFailure()));

        public Task<IndexedDbResult> ApplyAsync(IReadOnlyList<IndexedDbOperation> ops, IReadOnlyDictionary<string, string?>? meta = null)
        {
            AppliedOps = ops;
            AppliedMeta = meta;
            return Task.FromResult(new IndexedDbResult(NextFailure()));
        }

        public Task<IndexedDbResult> ReplaceAllAsync(IReadOnlyDictionary<string, IReadOnlyList<RawRecord>> records, IReadOnlyDictionary<string, string?>? meta = null)
        {
            Replaced = records;
            ReplacedMeta = meta;
            return Task.FromResult(new IndexedDbResult(NextFailure()));
        }
    }

    private static readonly (RecordKind Kind, string Module)[] KindNames =
    [
        (RecordKind.Exercise, "exercises"), (RecordKind.Settings, "settings"), (RecordKind.Plan, "plans"),
        (RecordKind.WeighIn, "weighIns"), (RecordKind.Break, "breaks"), (RecordKind.Badge, "badges"),
        (RecordKind.Guide, "guides"), (RecordKind.Workout, "workouts"), (RecordKind.WeekSchedule, "weekSchedules"),
    ];

    [Fact]
    public async Task Apply_maps_every_kind_to_its_module_name()
    {
        var fake = new FakeInterop();
        var store = new IndexedDbDataStore(fake);
        var changes = KindNames.SelectMany(kind => new StoreChange[]
        {
            new StoreChange.Put(new StoredRecord(kind.Kind, "k", "{}")),
            new StoreChange.Delete(kind.Kind, "gone"),
        }).ToList();

        var result = await store.ApplyAsync(changes);

        Assert.IsType<StoreResult<Unit>.Success>(result);
        Assert.Equal(KindNames.SelectMany(kind => new[] { ("put", kind.Module, "k"), ("delete", kind.Module, "gone") }),
            fake.AppliedOps!.Select(o => (o.Op, o.Kind, o.Key)));
    }

    [Fact]
    public async Task Load_maps_records_back_to_core_kinds_and_reads_metadata()
    {
        var at = new DateTimeOffset(2026, 11, 1, 8, 30, 0, TimeSpan.Zero);
        var fake = new FakeInterop
        {
            Loaded = new(
                new Dictionary<string, IReadOnlyList<RawRecord>>
                {
                    ["weekSchedules"] = [new RawRecord("2026-11-16", "{a}")],
                    ["workouts"] = [new RawRecord("w1", "{b}")],
                },
                new Dictionary<string, string>
                {
                    ["lastBackupAt"] = at.ToString("o"), ["layoutVersion"] = "1", ["recordFormatVersion"] = "1", ["initialised"] = "true",
                }),
        };

        var result = await new IndexedDbDataStore(fake).LoadAllAsync();

        var data = Assert.IsType<StoreResult<StoredData>.Success>(result).Value;
        Assert.Contains(new StoredRecord(RecordKind.WeekSchedule, "2026-11-16", "{a}"), data.Records);
        Assert.Contains(new StoredRecord(RecordKind.Workout, "w1", "{b}"), data.Records);
        Assert.Equal(new StoreMetadata(at, 1, 1, true), data.Metadata);
    }

    [Fact]
    public async Task Load_of_empty_metadata_is_the_empty_metadata()
    {
        var result = await new IndexedDbDataStore(new FakeInterop()).LoadAllAsync();

        Assert.Equal(StoreMetadata.Empty, Assert.IsType<StoreResult<StoredData>.Success>(result).Value.Metadata);
    }

    [Fact]
    public async Task ReplaceAll_writes_metadata_with_the_records_when_supplied()
    {
        var fake = new FakeInterop();
        var at = new DateTimeOffset(2026, 11, 1, 8, 30, 0, TimeSpan.Zero);

        await new IndexedDbDataStore(fake).ReplaceAllAsync(
            [new StoredRecord(RecordKind.Plan, "p1", "{}")], new StoreMetadata(at, 1, 1, true));

        Assert.Single(fake.Replaced!["plans"]);
        Assert.Empty(fake.Replaced["workouts"]);
        Assert.Equal(at.ToString("o"), fake.ReplacedMeta!["lastBackupAt"]);
        Assert.Equal("true", fake.ReplacedMeta["initialised"]);
    }

    [Fact]
    public async Task ReplaceAll_without_metadata_leaves_metadata_alone()
    {
        var fake = new FakeInterop();

        await new IndexedDbDataStore(fake).ReplaceAllAsync([]);

        Assert.Null(fake.ReplacedMeta);
    }

    [Fact]
    public async Task WriteMetadata_sends_unset_values_as_null_so_they_are_deleted()
    {
        var fake = new FakeInterop();

        await new IndexedDbDataStore(fake).WriteMetadataAsync(StoreMetadata.Empty);

        Assert.Empty(fake.AppliedOps!);
        Assert.All(fake.AppliedMeta!.Values, Assert.Null);
        Assert.Equal(3, fake.AppliedMeta.Count);
        Assert.DoesNotContain("layoutVersion", fake.AppliedMeta.Keys);
    }

    [Theory]
    [InlineData(IndexedDbFailureKind.Quota, StoreFailureKind.StorageFull, "d")]
    [InlineData(IndexedDbFailureKind.Unavailable, StoreFailureKind.StorageUnavailable, "d")]
    [InlineData(IndexedDbFailureKind.Blocked, StoreFailureKind.UpgradeBlocked, "d")]
    [InlineData(IndexedDbFailureKind.NotOpen, StoreFailureKind.StorageUnavailable, "d")]
    [InlineData(IndexedDbFailureKind.Unexpected, StoreFailureKind.Unexpected, "d")]
    public async Task Module_failures_map_to_store_failure_kinds(IndexedDbFailureKind module, StoreFailureKind expected, string detail)
    {
        var store = new IndexedDbDataStore(new FakeInterop { CallFailure = new IndexedDbFailure(module, "d") });

        var apply = await store.ApplyAsync([]);
        var load = await store.LoadAllAsync();

        Assert.Equal(new StoreFailure(expected, detail), Assert.IsType<StoreResult<Unit>.Failure>(apply).Error);
        Assert.Equal(new StoreFailure(expected, detail), Assert.IsType<StoreResult<StoredData>.Failure>(load).Error);
    }

    [Fact]
    public async Task A_connection_the_module_lost_is_reopened_and_the_call_retried_once()
    {
        var fake = new FakeInterop();
        var store = new IndexedDbDataStore(fake);
        await store.ApplyAsync([]);
        fake.NotOpenResponses = 1;

        var apply = await store.ApplyAsync([new StoreChange.Put(new StoredRecord(RecordKind.Plan, "p", "{}"))]);
        fake.NotOpenResponses = 1;
        var load = await store.LoadAllAsync();

        Assert.IsType<StoreResult<Unit>.Success>(apply);
        Assert.IsType<StoreResult<StoredData>.Success>(load);
        Assert.Equal(3, fake.OpenCalls);
        Assert.Single(fake.AppliedOps!);
    }

    [Fact]
    public async Task A_lost_connection_that_cannot_be_reopened_returns_the_open_failure()
    {
        var fake = new FakeInterop();
        var store = new IndexedDbDataStore(fake);
        await store.ApplyAsync([]);
        fake.NotOpenResponses = 1;
        fake.OpenFailure = new IndexedDbFailure(IndexedDbFailureKind.Blocked, "upgrade waiting");

        var result = await store.ApplyAsync([]);

        Assert.Equal(StoreFailureKind.UpgradeBlocked, Assert.IsType<StoreResult<Unit>.Failure>(result).Error.Kind);
    }

    [Fact]
    public async Task A_connection_that_stays_closed_after_reopening_fails_instead_of_looping()
    {
        var fake = new FakeInterop();
        var store = new IndexedDbDataStore(fake);
        await store.ApplyAsync([]);
        fake.NotOpenResponses = 5;

        var result = await store.ApplyAsync([]);

        Assert.Equal(StoreFailureKind.StorageUnavailable, Assert.IsType<StoreResult<Unit>.Failure>(result).Error.Kind);
        Assert.Equal(2, fake.OpenCalls);
    }

    [Fact]
    public async Task Open_failure_is_returned_and_retried_next_call()
    {
        var fake = new FakeInterop { OpenFailure = new IndexedDbFailure(IndexedDbFailureKind.Unavailable, "private") };
        var store = new IndexedDbDataStore(fake);

        var first = await store.ApplyAsync([]);
        fake.OpenFailure = null;
        var second = await store.ApplyAsync([]);
        await store.ApplyAsync([]);

        Assert.Equal(StoreFailureKind.StorageUnavailable, Assert.IsType<StoreResult<Unit>.Failure>(first).Error.Kind);
        Assert.IsType<StoreResult<Unit>.Success>(second);
        Assert.Equal(2, fake.OpenCalls);
    }
}
