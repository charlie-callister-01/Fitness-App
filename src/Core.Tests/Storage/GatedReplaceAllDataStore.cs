using Core.Storage;

namespace Core.Tests.Storage;

/// <summary>Test helper: holds <see cref="IDataStore.ReplaceAllAsync"/> open until released, so another call can be queued behind it.</summary>
public sealed class GatedReplaceAllDataStore(IDataStore inner) : IDataStore
{
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _reached = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes once a replace-all is waiting at the gate.</summary>
    public Task ReplaceAllReached => _reached.Task;

    public void Release() => _release.TrySetResult();

    public Task<StoreResult<StoredData>> LoadAllAsync() => inner.LoadAllAsync();

    public Task<StoreResult<Unit>> ApplyAsync(IReadOnlyList<StoreChange> changes, StoreMetadata? metadata = null) =>
        inner.ApplyAsync(changes, metadata);

    public async Task<StoreResult<Unit>> ReplaceAllAsync(IReadOnlyList<StoredRecord> records, StoreMetadata? metadata = null)
    {
        _reached.TrySetResult();
        await _release.Task;
        return await inner.ReplaceAllAsync(records, metadata);
    }

    public Task<StoreResult<Unit>> WriteMetadataAsync(StoreMetadata metadata) => inner.WriteMetadataAsync(metadata);
}
