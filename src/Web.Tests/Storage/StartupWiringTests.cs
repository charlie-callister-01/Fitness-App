using Core.Storage;
using Web.Storage;

namespace Web.Tests.Storage;

public class StartupWiringTests
{
    private sealed class FailingOpenInterop(IndexedDbFailure failure) : IIndexedDbInterop
    {
        public Task<IndexedDbResult> OpenAsync(string databaseName) => Task.FromResult(new IndexedDbResult(failure));
        public Task<IndexedDbResult<LoadedData>> LoadAllAsync() => throw new InvalidOperationException("never opened");
        public Task<IndexedDbResult> ApplyAsync(IReadOnlyList<IndexedDbOperation> ops, IReadOnlyDictionary<string, string?>? meta = null) =>
            throw new InvalidOperationException("never opened");
        public Task<IndexedDbResult> ReplaceAllAsync(
            IReadOnlyDictionary<string, IReadOnlyList<RawRecord>> records, IReadOnlyDictionary<string, string?>? meta = null) =>
            throw new InvalidOperationException("never opened");
    }

    [Fact]
    public async Task Unavailable_indexeddb_falls_back_to_memory_with_health_unavailable()
    {
        var holder = new AppDataHolder(
            new IndexedDbDataStore(new FailingOpenInterop(new IndexedDbFailure(IndexedDbFailureKind.Unavailable, "private browsing"))),
            new InMemoryStorageEnvironment());

        await holder.OpenAsync();

        Assert.False(holder.AppData.Health.DatabaseAvailable);
        Assert.Equal(StartupState.FirstRun, holder.AppData.Startup.State);
    }

    [Fact]
    public async Task A_blocked_open_is_reported_so_the_ui_can_say_close_other_windows()
    {
        var holder = new AppDataHolder(
            new IndexedDbDataStore(new FailingOpenInterop(new IndexedDbFailure(IndexedDbFailureKind.Blocked, "upgrade waiting"))),
            new InMemoryStorageEnvironment());

        await holder.OpenAsync();

        Assert.Equal(StartupState.StorageFailed, holder.AppData.Startup.State);
        Assert.True(holder.AppData.Startup.Failure!.IsBlockedByAnotherWindow);
        Assert.False(holder.AppData.Health.CanWrite);
    }

    [Fact]
    public async Task Other_failures_are_not_reported_as_blocked()
    {
        var holder = new AppDataHolder(
            new IndexedDbDataStore(new FailingOpenInterop(new IndexedDbFailure(IndexedDbFailureKind.Unexpected, "boom"))),
            new InMemoryStorageEnvironment());

        await holder.OpenAsync();

        Assert.False(holder.AppData.Startup.Failure!.IsBlockedByAnotherWindow);
    }

    [Fact]
    public void Reading_the_data_before_opening_is_a_wiring_error()
    {
        var holder = new AppDataHolder(new InMemoryDataStore(), new InMemoryStorageEnvironment());

        Assert.Throws<InvalidOperationException>(() => holder.AppData);
    }
}
