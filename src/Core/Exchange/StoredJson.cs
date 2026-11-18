using Core.Storage;

namespace Core.Exchange;

/// <summary>Record comparison for Imports: two records are the same when their stored JSON (the exchange format's own text) is the same.</summary>
internal static class StoredJson
{
    public static bool Same<T>(T a, T b)
    {
        var descriptor = RecordDescriptors.For<T>();
        return descriptor.ToStored(a).Json == descriptor.ToStored(b).Json;
    }
}
