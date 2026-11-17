namespace Core.Storage;

/// <summary>
/// An <see cref="IDataStore"/> that behaves like IndexedDB: put replaces by key, delete of a missing key is fine,
/// batches and replace-all are all-or-nothing, and callers never share mutable state with it.
/// </summary>
public sealed class InMemoryDataStore : IDataStore
{
    private Dictionary<(RecordKind Kind, string Key), string> _records = [];
    private StoreMetadata _metadata = StoreMetadata.Empty;

    public Task<StoreResult<StoredData>> LoadAllAsync()
    {
        var records = _records.Select(pair => new StoredRecord(pair.Key.Kind, pair.Key.Key, pair.Value)).ToList();
        return Task.FromResult<StoreResult<StoredData>>(new StoreResult<StoredData>.Success(new StoredData(records, _metadata)));
    }

    public Task<StoreResult<Unit>> ApplyAsync(IReadOnlyList<StoreChange> changes)
    {
        // Work on a copy and swap it in at the end, so a batch is all-or-nothing and later edits to the caller's list change nothing.
        var next = new Dictionary<(RecordKind, string), string>(_records);
        foreach (var change in changes)
        {
            switch (change)
            {
                case StoreChange.Put put:
                    next[(put.Record.Kind, put.Record.Key)] = put.Record.Json;
                    break;
                case StoreChange.Delete delete:
                    next.Remove((delete.Kind, delete.Key));
                    break;
            }
        }

        _records = next;
        return Done();
    }

    public Task<StoreResult<Unit>> ReplaceAllAsync(IReadOnlyList<StoredRecord> records, StoreMetadata? metadata = null)
    {
        var next = new Dictionary<(RecordKind, string), string>();
        foreach (var record in records)
        {
            next[(record.Kind, record.Key)] = record.Json;
        }

        _records = next;
        if (metadata is not null)
        {
            _metadata = metadata;
        }

        return Done();
    }

    public Task<StoreResult<Unit>> WriteMetadataAsync(StoreMetadata metadata)
    {
        _metadata = metadata;
        return Done();
    }

    private static Task<StoreResult<Unit>> Done() => Task.FromResult<StoreResult<Unit>>(new StoreResult<Unit>.Success(default));
}
