namespace Web.Storage;

/// <summary>The slice of <see cref="IndexedDbInterop"/> the data store uses; exists so the mapping can be tested with a fake.</summary>
public interface IIndexedDbInterop
{
    Task<IndexedDbResult> OpenAsync(string databaseName);
    Task<IndexedDbResult<LoadedData>> LoadAllAsync();
    Task<IndexedDbResult> ApplyAsync(IReadOnlyList<IndexedDbOperation> ops, IReadOnlyDictionary<string, string?>? meta = null);
    Task<IndexedDbResult> ReplaceAllAsync(
        IReadOnlyDictionary<string, IReadOnlyList<RawRecord>> records, IReadOnlyDictionary<string, string?>? meta = null);
}
