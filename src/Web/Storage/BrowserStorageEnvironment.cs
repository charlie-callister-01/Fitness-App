using System.Text.Json;
using Core.Storage;

namespace Web.Storage;

/// <summary>
/// <see cref="IStorageEnvironment"/> over the browser. Maps the module's plain answers to Core's types and turns any failure to
/// ask (the module missing, a JS error) into the safe answer: persistence unsupported, estimate unknown, writer lock not obtained,
/// marker absent. It never throws. A browser without Web Locks is treated as having only one window: the module grants the lock.
/// </summary>
public sealed class BrowserStorageEnvironment(IStorageEnvironmentInterop interop) : IStorageEnvironment
{
    public async Task<PersistenceStatus> RequestPersistenceAsync() =>
        await TryAsync(interop.RequestPersistenceAsync, "") switch
        {
            "granted" => PersistenceStatus.Granted,
            "notGranted" => PersistenceStatus.NotGranted,
            _ => PersistenceStatus.Unsupported,
        };

    public async Task<StorageEstimate?> EstimateAsync()
    {
        var json = await TryAsync(interop.EstimateAsync, "");
        if (json.Length == 0) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            return new StorageEstimate(root.GetProperty("usage").GetInt64(), root.GetProperty("quota").GetInt64());
        }
        catch (Exception e) when (e is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            return null;
        }
    }

    public Task<bool> TryTakeWriterLockAsync() => TryAsync(interop.TryTakeWriterLockAsync, false);

    public Task<bool> ReadClearedDataMarkerAsync() => TryAsync(interop.ReadClearedDataMarkerAsync, false);

    public async Task WriteClearedDataMarkerAsync() =>
        await TryAsync(async () => { await interop.WriteClearedDataMarkerAsync(); return true; }, false);

    private static async Task<T> TryAsync<T>(Func<Task<T>> call, T fallback)
    {
        try
        {
            return await call();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return fallback;
        }
    }
}
