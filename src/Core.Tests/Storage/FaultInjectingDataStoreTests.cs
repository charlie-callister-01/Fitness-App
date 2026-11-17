using Core.Storage;

namespace Core.Tests.Storage;

public class FaultInjectingDataStoreTests
{
    private static readonly StoreChange SomePut = new StoreChange.Put(new StoredRecord(RecordKind.Plan, "plan_1", "{}"));

    [Theory]
    [InlineData(StoreFailureKind.StorageFull)]
    [InlineData(StoreFailureKind.WriterLockHeld)]
    public async Task Failing_writes_report_the_kind_and_still_allow_reads(StoreFailureKind kind)
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        store.FailAllWrites(kind);

        var write = await store.ApplyAsync([SomePut]);
        var read = await store.LoadAllAsync();

        Assert.Equal(kind, Assert.IsType<StoreResult<Unit>.Failure>(write).Error.Kind);
        Assert.Empty(Assert.IsType<StoreResult<StoredData>.Success>(read).Value.Records);
    }

    [Fact]
    public async Task Unavailable_storage_fails_reads_and_writes()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        store.FailEverything(StoreFailureKind.StorageUnavailable);

        Assert.IsType<StoreResult<StoredData>.Failure>(await store.LoadAllAsync());
        Assert.IsType<StoreResult<Unit>.Failure>(await store.WriteMetadataAsync(StoreMetadata.Empty));
        Assert.IsType<StoreResult<Unit>.Failure>(await store.ReplaceAllAsync([]));
    }

    [Fact]
    public async Task A_failed_last_operation_fails_one_batch_only()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        store.FailLastOperationOfNextBatch(StoreFailureKind.StorageFull);

        Assert.IsType<StoreResult<Unit>.Failure>(await store.ApplyAsync([SomePut]));
        Assert.IsType<StoreResult<Unit>.Success>(await store.ApplyAsync([SomePut]));
    }

    [Fact]
    public async Task Recover_clears_every_fault()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        store.FailEverything(StoreFailureKind.StorageUnavailable);

        store.Recover();

        Assert.IsType<StoreResult<Unit>.Success>(await store.ApplyAsync([SomePut]));
    }
}
