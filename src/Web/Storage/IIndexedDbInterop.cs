namespace Web.Storage;

/// <summary>The slice of <see cref="IndexedDbInterop"/> the data store uses; exists so the mapping can be tested with a fake.</summary>
public interface IIndexedDbInterop
{
    Task<IndexedDbResult<object?>> OpenAsync(string databaseName);
    Task<IndexedDbResult<LoadedData>> LoadAllAsync();
    Task<IndexedDbResult<object?>> ApplyAsync(IReadOnlyList<IndexedDbOperation> ops, IReadOnlyDictionary<string, string?>? meta = null);
    Task<IndexedDbResult<object?>> ReplaceAllAsync(
        IReadOnlyDictionary<string, IReadOnlyList<RawRecord>> records, IReadOnlyDictionary<string, string?>? meta = null);
}
