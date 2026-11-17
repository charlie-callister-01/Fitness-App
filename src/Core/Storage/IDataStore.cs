namespace Core.Storage;

/// <summary>
/// The storage seam: persistence only, no rules. Implementations (in memory, IndexedDB, later SQLite or a file) all pass the
/// same contract suite. No method throws for an expected failure; it returns <see cref="StoreResult{T}.Failure"/>.
/// </summary>
public interface IDataStore
{
    /// <summary>Loads every stored record plus the metadata. An empty store loads empty.</summary>
    Task<StoreResult<StoredData>> LoadAllAsync();

    /// <summary>
    /// Applies the changes in order as one atomic unit: all happen or none do. A single save is a batch of one. If <paramref name="metadata"/>
    /// is given it is written in the same unit, otherwise the metadata is left as it was.
    /// </summary>
    Task<StoreResult<Unit>> ApplyAsync(IReadOnlyList<StoreChange> changes, StoreMetadata? metadata = null);

    /// <summary>
    /// Atomically clears every kind and writes <paramref name="records"/>; if <paramref name="metadata"/> is given it is written
    /// in the same unit, otherwise the metadata is left as it was. On failure the previous data is untouched.
    /// </summary>
    Task<StoreResult<Unit>> ReplaceAllAsync(IReadOnlyList<StoredRecord> records, StoreMetadata? metadata = null);

    /// <summary>Overwrites the device metadata.</summary>
    Task<StoreResult<Unit>> WriteMetadataAsync(StoreMetadata metadata);
}
