using Core.Model;
using Core.Serialisation;
using Core.Storage;
using Core.Tests.Serialisation;

namespace Core.Tests.Storage;

public class AppDataReplaceAllTests
{
    private static Exercise Rdl() => TestData.GobletSquat() with { Id = "rdl", Name = "RDL" };

    private static async Task<StoredData> Stored(IDataStore store) =>
        Assert.IsType<StoreResult<StoredData>.Success>(await store.LoadAllAsync()).Value;

    private static async Task<InMemoryDataStore> StoreHolding(ExchangeDocument document)
    {
        var store = new InMemoryDataStore();
        Assert.IsType<StoreResult<Unit>.Success>(
            await store.ReplaceAllAsync(StoredRecords.FromDocument(document), new StoreMetadata(null, null, null, true)));
        return store;
    }

    [Fact]
    public async Task Replace_all_swaps_the_whole_dataset_and_removes_what_is_not_in_the_document()
    {
        var store = await StoreHolding(TestData.Minimal() with { WeighIns = [new WeighIn("wi-1", new DateOnly(2026, 11, 1), 80)] });
        var app = await AppData.OpenAsync(store);
        var changes = 0;
        app.Changed += () => changes++;
        var restored = TestData.Minimal() with { Exercises = [Rdl()] };

        var result = await app.ReplaceAllAsync(restored);

        Assert.IsType<CommitResult.Committed>(result);
        Assert.Equal(["rdl"], app.Snapshot.Exercises.Select(e => e.Id));
        Assert.Empty(app.Snapshot.WeighIns);
        Assert.Equal(1, changes);
        var stored = await Stored(store);
        Assert.Equal(["rdl"], stored.Records.Where(r => r.Kind == RecordKind.Exercise).Select(r => r.Key));
        Assert.DoesNotContain(stored.Records, r => r.Kind == RecordKind.WeighIn);
        Assert.Equal(ExchangeFormat.CurrentVersion, stored.Metadata.RecordFormatVersion);
        Assert.Equal(["rdl"], (await AppData.OpenAsync(store)).Snapshot.Exercises.Select(e => e.Id));
    }

    [Fact]
    public async Task Replace_all_into_a_first_run_store_marks_it_initialised()
    {
        var store = new InMemoryDataStore();
        var app = await AppData.OpenAsync(store);

        await app.ReplaceAllAsync(TestData.Minimal());

        Assert.True((await Stored(store)).Metadata.Initialised);
        Assert.Equal(StartupState.Ready, app.Startup.State);
    }

    [Fact]
    public async Task Replace_all_keeps_the_last_backup_time()
    {
        var store = await StoreHolding(TestData.Minimal());
        var app = await AppData.OpenAsync(store);
        var at = new DateTimeOffset(2026, 11, 17, 8, 0, 0, TimeSpan.Zero);
        await app.RecordBackupAsync(at);

        await app.ReplaceAllAsync(TestData.Minimal() with { Exercises = [Rdl()] });

        Assert.Equal(at, app.LastBackupAt);
        Assert.Equal(at, (await Stored(store)).Metadata.LastBackupAt);
    }

    [Fact]
    public async Task A_failed_replace_all_leaves_the_old_data_and_snapshot_and_raises_nothing()
    {
        var inner = await StoreHolding(TestData.Minimal());
        var store = new FaultInjectingDataStore(inner);
        var app = await AppData.OpenAsync(store);
        var before = await Stored(inner);
        var before_snapshot = app.Snapshot;
        var changes = 0;
        app.Changed += () => changes++;
        store.FailAllWrites(StoreFailureKind.StorageFull);

        var result = await app.ReplaceAllAsync(TestData.Minimal() with { Exercises = [Rdl()] });

        Assert.Equal(StoreFailureKind.StorageFull, Assert.IsType<CommitResult.StoreFailed>(result).Failure.Kind);
        Assert.Same(before_snapshot, app.Snapshot);
        Assert.Equal(0, changes);
        Assert.Equal(before.Records, (await Stored(inner)).Records);
    }

    [Fact]
    public async Task An_invalid_document_is_rejected_without_any_change()
    {
        var store = await StoreHolding(TestData.Minimal());
        var app = await AppData.OpenAsync(store);
        var before = await Stored(store);
        var invalid = TestData.Minimal() with { Exercises = [TestData.GobletSquat(), TestData.GobletSquat()] };

        var result = await app.ReplaceAllAsync(invalid);

        Assert.NotEmpty(Assert.IsType<CommitResult.Rejected>(result).Issues);
        Assert.Equal(before.Records, (await Stored(store)).Records);
        Assert.Equal(["goblet-squat"], app.Snapshot.Exercises.Select(e => e.Id));
    }

    [Fact]
    public async Task Replace_all_can_rescue_data_that_could_not_be_read()
    {
        var store = await StoreHolding(TestData.Minimal());
        await store.WriteMetadataAsync(new StoreMetadata(null, null, ExchangeFormat.CurrentVersion + 1, true));
        var app = await AppData.OpenAsync(store);
        Assert.Equal(StartupState.CannotRead, app.Startup.State);

        var result = await app.ReplaceAllAsync(TestData.Minimal() with { Exercises = [Rdl()] });

        Assert.IsType<CommitResult.Committed>(result);
        Assert.Equal(StartupState.Ready, app.Startup.State);
        Assert.IsType<CommitResult.Committed>(await app.CommitAsync(Change.Put(Rdl() with { Name = "RDL 2" })));
    }

    [Fact]
    public async Task Replace_all_stamps_the_backup_time_in_the_same_store_write()
    {
        var inner = await StoreHolding(TestData.Minimal());
        var app = await AppData.OpenAsync(inner);
        var at = new DateTimeOffset(2026, 11, 10, 8, 0, 0, TimeSpan.Zero);

        await app.ReplaceAllAsync(TestData.Minimal() with { Exercises = [Rdl()] }, at);

        Assert.Equal(at, app.LastBackupAt);
        Assert.Equal(at, (await Stored(inner)).Metadata.LastBackupAt);
        Assert.Equal(at, (await AppData.OpenAsync(inner)).LastBackupAt);
    }

    [Fact]
    public async Task A_failed_replace_all_does_not_stamp_the_backup_time()
    {
        var old = new DateTimeOffset(2026, 11, 1, 8, 0, 0, TimeSpan.Zero);
        var inner = await StoreHolding(TestData.Minimal());
        var store = new FaultInjectingDataStore(inner);
        var app = await AppData.OpenAsync(store);
        await app.RecordBackupAsync(old);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        await app.ReplaceAllAsync(TestData.Minimal() with { Exercises = [Rdl()] }, old.AddDays(9));

        Assert.Equal(old, app.LastBackupAt);
        Assert.Equal(old, (await Stored(inner)).Metadata.LastBackupAt);
    }

    [Fact]
    public async Task A_rejected_replace_all_does_not_stamp_the_backup_time()
    {
        var app = await AppData.OpenAsync(await StoreHolding(TestData.Minimal()));
        var invalid = TestData.Minimal() with { Exercises = [TestData.GobletSquat(), TestData.GobletSquat()] };

        await app.ReplaceAllAsync(invalid, new DateTimeOffset(2026, 11, 10, 8, 0, 0, TimeSpan.Zero));

        Assert.Null(app.LastBackupAt);
    }
}
