using Microsoft.JSInterop;
using Ui.Browser;

namespace Web.Browser;

/// <summary>Typed wrapper over <c>wwwroot/js/browser-capabilities.js</c>. Every failure returns false rather than throwing into the UI.</summary>
public sealed class BrowserCapabilities(IJSRuntime js) : IBrowserCapabilities, IAsyncDisposable
{
    public const string ModulePath = "./js/browser-capabilities.js";

    private IJSObjectReference? _module;
    private DotNetObjectReference<BrowserCapabilities>? _selfRef;
    private bool _listenerRegistered;

    public event Action? InstallAvailabilityChanged;

    public Task<bool> CopyTextAsync(string text) => TryAsync<bool>("copyText", text);

    public Task<bool> DownloadFileAsync(string fileName, string content, string contentType = "application/json") =>
        TryAsync<bool>("downloadFile", fileName, content, contentType);

    public Task<bool> CanShareAsync() => TryAsync<bool>("canShare");

    public Task<bool> ShareAsync(string title, string text, string? fileName = null, string contentType = "text/plain") =>
        TryAsync<bool>("share", title, text, fileName, contentType);

    public Task<bool> IsStandaloneAsync() => TryAsync<bool>("isStandalone");

    public async Task<bool> CanInstallAsync()
    {
        await EnsureListenerAsync();
        return await TryAsync<bool>("canInstall");
    }

    public Task<bool> PromptInstallAsync() => TryAsync<bool>("promptInstall");

    [JSInvokable]
    public void OnInstallAvailabilityChanged() => InstallAvailabilityChanged?.Invoke();

    public async ValueTask DisposeAsync()
    {
        _selfRef?.Dispose();
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

    private async Task EnsureListenerAsync()
    {
        if (_listenerRegistered)
        {
            return;
        }

        _selfRef ??= DotNetObjectReference.Create(this);
        _listenerRegistered = true;
        await TryAsync<bool>("registerInstallListener", _selfRef);
    }

    private async Task<T> TryAsync<T>(string function, params object?[] args)
    {
        try
        {
            _module ??= await js.InvokeAsync<IJSObjectReference>("import", ModulePath);
            return await _module.InvokeAsync<T>(function, args);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException)
        {
            return default!;
        }
    }
}
