using Core.Storage;

namespace Web.Storage;

/// <summary>
/// <see cref="IStorageEnvironment"/> over the browser. Maps the module's plain answers to Core's types and turns any failure to
/// ask (the module missing, a JS error) into the safe answer: persistence unsupported, estimate unknown, writer lock unknown
/// (which <c>AppData</c> keeps read-only), marker absent. It never throws. What a missing feature means for writing is Core's
/// policy, not decided here.
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

    public async Task<StorageEstimate?> EstimateAsync() =>
        await TryAsync(interop.EstimateAsync, null) is { } usage ? new StorageEstimate(usage.Usage, usage.Quota) : null;

    public async Task<WriterLockResult> TryTakeWriterLockAsync() =>
        await TryAsync(interop.TryTakeWriterLockAsync, "") switch
        {
            "obtained" => WriterLockResult.Obtained,
            "heldElsewhere" => WriterLockResult.HeldElsewhere,
            "unsupported" => WriterLockResult.Unsupported,
            _ => WriterLockResult.Unknown,
        };

    public Task<bool> ReadClearedDataMarkerAsync() => TryAsync(interop.ReadClearedDataMarkerAsync, false);

    public Task WriteClearedDataMarkerAsync() => TryAsync(interop.WriteClearedDataMarkerAsync);

    private static async Task<T> TryAsync<T>(Func<Task<T>> call, T fallback)
    {
        try
        {
            return await call();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return fallback;
        }
    }

    private static async Task TryAsync(Func<Task> call)
    {
        try
        {
            await call();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Nothing to do: a marker that could not be written is caught later by the initialised metadata.
        }
    }
}
