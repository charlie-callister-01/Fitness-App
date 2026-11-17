using Core.Model;
using Core.Serialisation;
using Core.Storage;
using Core.Tests.Serialisation;

namespace Core.Tests.Storage;

/// <summary>
/// The format has only version 1, so an older version is synthetic: the app is opened as if its current version were 2 over data
/// recorded as version 1 (which the real reader still reads).
/// </summary>
public class AppDataMigrationTests
{
    private const int SyntheticCurrent = ExchangeFormat.CurrentVersion + 1;

    private static async Task<InMemoryDataStore> StoreHolding(ExchangeDocument document, StoreMetadata metadata)
    {
        var store = new InMemoryDataStore();
        Assert.IsType<StoreResult<Unit>.Success>(await store.ReplaceAllAsync(StoredRecords.FromDocument(document), metadata));
        return store;
    }

    private static async Task<StoredData> Stored(IDataStore store) =>
        Assert.IsType<StoreResult<StoredData>.Success>(await store.LoadAllAsync()).Value;

    private static StoreMetadata OlderMetadata() => new(null, 7, ExchangeFormat.CurrentVersion, true);

    [Fact]
    public async Task Older_stored_data_is_kept_and_the_new_version_is_recorded_at_startup()
    {
        var store = await StoreHolding(TestData.Minimal(), OlderMetadata());

        var app = await AppData.OpenAsync(store, SyntheticCurrent);

        Assert.Equal(StartupState.Ready, app.Startup.State);
        Assert.Equal(["goblet-squat"], app.Snapshot.Exercises.Select(e => e.Id));
        var stored = await Stored(store);
        Assert.Equal(SyntheticCurrent, stored.Metadata.RecordFormatVersion);
        Assert.Equal(["goblet-squat"], stored.Records.Where(r => r.Kind == RecordKind.Exercise).Select(r => r.Key));
    }

    [Fact]
    public async Task The_layout_version_is_not_changed_by_a_format_migration()
    {
        var store = await StoreHolding(TestData.Minimal(), OlderMetadata());

        await AppData.OpenAsync(store, SyntheticCurrent);

        Assert.Equal(7, (await Stored(store)).Metadata.LayoutVersion);
    }

    [Fact]
    public async Task Data_already_in_the_current_version_is_not_rewritten()
    {
        var current = OlderMetadata() with { RecordFormatVersion = ExchangeFormat.CurrentVersion };
        var store = new FaultInjectingDataStore(await StoreHolding(TestData.Minimal(), current));
        store.FailAllWrites(StoreFailureKind.Unexpected);

        var app = await AppData.OpenAsync(store);

        Assert.Equal(StartupState.Ready, app.Startup.State);
    }

    [Fact]
    public async Task A_migration_that_fails_changes_nothing_and_starts_with_a_clear_state()
    {
        var inner = await StoreHolding(TestData.Minimal(), OlderMetadata());
        var before = await Stored(inner);
        var store = new FaultInjectingDataStore(inner);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        var app = await AppData.OpenAsync(store, SyntheticCurrent);

        Assert.Equal(StartupState.CannotRead, app.Startup.State);
        Assert.Empty(app.Snapshot.Exercises);
        Assert.IsType<CommitResult.Blocked>(await app.CommitAsync(Change.Put(TestData.GobletSquat())));
        var after = await Stored(inner);
        Assert.Equal(before.Records, after.Records);
        Assert.Equal(before.Metadata, after.Metadata);
    }

    [Fact]
    public async Task A_migration_is_not_run_when_a_record_had_to_be_set_aside_so_nothing_is_lost()
    {
        var inner = await StoreHolding(TestData.Minimal(), OlderMetadata());
        Assert.IsType<StoreResult<Unit>.Success>(await inner.ApplyAsync(
            [new StoreChange.Put(new StoredRecord(RecordKind.Exercise, "bad", """{"id":"bad","name":"Bad","logKind":"nonsense"}"""))]));
        var before = await Stored(inner);

        var app = await AppData.OpenAsync(inner, SyntheticCurrent);

        Assert.Equal(StartupState.CannotRead, app.Startup.State);
        var after = await Stored(inner);
        Assert.Equal(before.Records, after.Records);
        Assert.Equal(before.Metadata, after.Metadata);
    }

    [Fact]
    public async Task Data_from_a_newer_version_than_the_app_still_cannot_be_read_and_nothing_is_written()
    {
        var store = await StoreHolding(TestData.Minimal(), OlderMetadata() with { RecordFormatVersion = SyntheticCurrent + 1 });
        var before = await Stored(store);

        var app = await AppData.OpenAsync(store, SyntheticCurrent);

        Assert.Equal(StartupState.CannotRead, app.Startup.State);
        Assert.Equal(before.Metadata, (await Stored(store)).Metadata);
    }

    [Fact]
    public async Task Every_save_records_the_current_format_version()
    {
        var store = await StoreHolding(TestData.Minimal(), new StoreMetadata(null, null, null, true));
        var app = await AppData.OpenAsync(store);

        await app.CommitAsync(Change.Put(TestData.GobletSquat() with { Name = "Renamed" }));

        Assert.Equal(ExchangeFormat.CurrentVersion, (await Stored(store)).Metadata.RecordFormatVersion);
    }

    [Fact]
    public async Task A_save_after_a_migration_writes_in_the_new_version()
    {
        var store = await StoreHolding(TestData.Minimal(), OlderMetadata());
        var app = await AppData.OpenAsync(store, SyntheticCurrent);

        var result = await app.CommitAsync(Change.Put(TestData.GobletSquat() with { Name = "Renamed" }));

        Assert.IsType<CommitResult.Committed>(result);
        Assert.Equal(SyntheticCurrent, (await Stored(store)).Metadata.RecordFormatVersion);
    }
}
