using Microsoft.JSInterop;

namespace Web.Storage;

/// <summary>Typed wrapper over <c>wwwroot/js/storage-environment.js</c>. No rules; the module never throws.</summary>
public sealed class StorageEnvironmentInterop(IJSRuntime js) : IStorageEnvironmentInterop, IAsyncDisposable
{
    public const string ModulePath = "./js/storage-environment.js";

    private IJSObjectReference? _module;

    public Task<string> RequestPersistenceAsync() => CallAsync<string>("requestPersistence");

    public Task<string> EstimateAsync() => CallAsync<string>("estimate");

    public Task<bool> TryTakeWriterLockAsync() => CallAsync<bool>("tryTakeWriterLock");

    public Task<bool> ReadClearedDataMarkerAsync() => CallAsync<bool>("readClearedDataMarker");

    public async Task WriteClearedDataMarkerAsync() => await CallAsync<bool>("writeClearedDataMarker");

    public async ValueTask DisposeAsync()
    {
        if (_module is not null) await _module.DisposeAsync();
    }

    private async Task<T> CallAsync<T>(string function)
    {
        _module ??= await js.InvokeAsync<IJSObjectReference>("import", ModulePath);
        return await _module.InvokeAsync<T>(function);
    }
}
