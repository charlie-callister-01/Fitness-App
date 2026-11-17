using Core.Storage;

namespace Core.Tests.Storage;

/// <summary>
/// Test helper: wraps a real store and makes it report an expected failure on demand. A failing write never reaches the
/// wrapped store, which is exactly what an atomic store looks like after a failed transaction.
/// </summary>
public sealed class FaultInjectingDataStore(IDataStore inner) : IDataStore
{
    private StoreFailureKind? _writeFailure;
    private StoreFailureKind? _allFailure;
    private StoreFailureKind? _nextBatchFailure;

    /// <summary>Every write reports <paramref name="kind"/> (for example storage full, or the writer lock held by another tab).</summary>
    public void FailAllWrites(StoreFailureKind kind) => _writeFailure = kind;

    /// <summary>Every operation, reads included, reports <paramref name="kind"/> (for example storage unavailable).</summary>
    public void FailEverything(StoreFailureKind kind) => _allFailure = kind;

    /// <summary>The next batch fails on its final change, so none of it is applied. Later batches succeed.</summary>
    public void FailLastOperationOfNextBatch(StoreFailureKind kind) => _nextBatchFailure = kind;

    public void Recover()
    {
        _writeFailure = null;
        _allFailure = null;
        _nextBatchFailure = null;
    }

    public Task<StoreResult<StoredData>> LoadAllAsync() =>
        _allFailure is { } kind ? Task.FromResult<StoreResult<StoredData>>(Fail<StoredData>(kind)) : inner.LoadAllAsync();

    public Task<StoreResult<Unit>> ApplyAsync(IReadOnlyList<StoreChange> changes)
    {
        if (CurrentWriteFailure() is { } kind)
        {
            return Task.FromResult<StoreResult<Unit>>(Fail<Unit>(kind));
        }

        if (_nextBatchFailure is { } batchKind)
        {
            _nextBatchFailure = null;
            return Task.FromResult<StoreResult<Unit>>(Fail<Unit>(batchKind));
        }

        return inner.ApplyAsync(changes);
    }

    public Task<StoreResult<Unit>> ReplaceAllAsync(IReadOnlyList<StoredRecord> records, StoreMetadata? metadata = null) =>
        CurrentWriteFailure() is { } kind ? Task.FromResult<StoreResult<Unit>>(Fail<Unit>(kind)) : inner.ReplaceAllAsync(records, metadata);

    public Task<StoreResult<Unit>> WriteMetadataAsync(StoreMetadata metadata) =>
        CurrentWriteFailure() is { } kind ? Task.FromResult<StoreResult<Unit>>(Fail<Unit>(kind)) : inner.WriteMetadataAsync(metadata);

    private StoreFailureKind? CurrentWriteFailure() => _allFailure ?? _writeFailure;

    private static StoreResult<T>.Failure Fail<T>(StoreFailureKind kind) => new(new StoreFailure(kind));
}
