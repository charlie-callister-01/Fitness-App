namespace Core.Storage;

/// <summary>
/// One typed step of a commit: put (insert or replace by key) or delete a single record. Puts keep the position of the record they replace
/// and append new ones. Kinds and keys come from <see cref="RecordDescriptors"/>; the stored JSON is the exchange format's own, so there
/// is no second mapping.
/// </summary>
public abstract class Change
{
    private Change(RecordKind kind, string key)
    {
        Kind = kind;
        Key = key;
    }

    public RecordKind Kind { get; }

    public string Key { get; }

    /// <summary>
    /// Puts the record: inserts it, or replaces the one with the same key. <typeparamref name="T"/> must be one of the stored record types
    /// (Exercise, Settings, Plan, WeighIn, Break, Badge, Guide, Workout, WeekSchedule); any other type throws.
    /// </summary>
    public static Change Put<T>(T record)
    {
        var descriptor = RecordDescriptors.For<T>();
        return new PutChange<T>(descriptor, record);
    }

    /// <summary>Deletes the record with this key (see <see cref="RecordKeys"/>). A missing key is not an error. Settings cannot be deleted, only replaced.</summary>
    public static Change Delete(RecordKind kind, string key)
    {
        var descriptor = RecordDescriptors.For(kind);
        return descriptor.IsSingleton
            ? throw new ArgumentException($"A {kind} record cannot be deleted, only replaced.", nameof(kind))
            : new DeleteChange(descriptor, key);
    }

    internal abstract DataSnapshot ApplyTo(DataSnapshot snapshot);

    /// <summary>What to hand the store for this change.</summary>
    internal abstract StoreChange ToStoreChange();

    private sealed class PutChange<T>(RecordDescriptor<T> descriptor, T record) : Change(descriptor.Kind, descriptor.KeyOf(record))
    {
        internal override DataSnapshot ApplyTo(DataSnapshot snapshot) => descriptor.ApplyPut(snapshot, record);

        internal override StoreChange ToStoreChange() => new StoreChange.Put(descriptor.ToStored(record));
    }

    private sealed class DeleteChange(RecordDescriptor descriptor, string key) : Change(descriptor.Kind, key)
    {
        internal override DataSnapshot ApplyTo(DataSnapshot snapshot) => descriptor.ApplyDelete(snapshot, Key);

        internal override StoreChange ToStoreChange() => new StoreChange.Delete(Kind, Key);
    }
}
