namespace Core.Storage;

/// <summary>One stored top-level record: its kind, its key (see <see cref="RecordKeys"/>) and its JSON text in the exchange format's serialisation. The store never interprets the JSON.</summary>
public sealed record StoredRecord(RecordKind Kind, string Key, string Json);

/// <summary>One step of an atomic batch: put (insert or replace by key) or delete (a missing key is not an error).</summary>
public abstract record StoreChange
{
    private StoreChange() { }

    public sealed record Put(StoredRecord Record) : StoreChange;

    public sealed record Delete(RecordKind Kind, string Key) : StoreChange;
}

/// <summary>Device bookkeeping. It is never part of the exchange format or a Backup. Null means "never written".</summary>
public sealed record StoreMetadata(
    DateTimeOffset? LastBackupAt,
    int? LayoutVersion,
    int? RecordFormatVersion,
    bool Initialised)
{
    public static StoreMetadata Empty { get; } = new(null, null, null, false);
}

/// <summary>Everything the store holds: the raw records of every kind plus the metadata.</summary>
public sealed record StoredData(IReadOnlyList<StoredRecord> Records, StoreMetadata Metadata);
