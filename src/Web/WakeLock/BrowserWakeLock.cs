using Microsoft.JSInterop;
using Ui.Log;

namespace Web.WakeLock;

/// <summary>Typed wrapper over <c>wwwroot/js/wake-lock.js</c>. Every failure (no support, a refusal, a closing page) is swallowed: the screen may sleep, nothing else.</summary>
public sealed class BrowserWakeLock(IJSRuntime js) : IWakeLock, IAsyncDisposable
{
    public const string ModulePath = "./js/wake-lock.js";

    private IJSObjectReference? _module;

    public async Task<bool> IsSupportedAsync() => await TryAsync<bool>("isSupported");

    public async Task AcquireAsync() => await TryAsync<bool>("acquire");

    public async Task ReleaseAsync() => await TryAsync<bool>("release");

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }

    private async Task<T> TryAsync<T>(string function)
    {
        try
        {
            _module ??= await js.InvokeAsync<IJSObjectReference>("import", ModulePath);
            return await _module.InvokeAsync<T>(function);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException)
        {
            return default!;
        }
    }
}
