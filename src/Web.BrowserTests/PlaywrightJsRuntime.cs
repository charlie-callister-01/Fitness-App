using Microsoft.JSInterop;
using Microsoft.Playwright;

namespace Web.BrowserTests;

/// <summary>
/// Stands in for Blazor's <see cref="IJSRuntime"/> so the production <c>IndexedDbInterop</c> (and so
/// <c>IndexedDbDataStore</c>) runs unchanged, with every call executed by the real module in the browser.
/// Only the <c>import</c> + module-function shape that <c>IndexedDbInterop</c> uses is supported.
/// </summary>
public sealed class PlaywrightJsRuntime(IPage page) : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        if (identifier != "import" || args is not [string path] || typeof(TValue) != typeof(IJSObjectReference))
            throw new NotSupportedException($"Only import of a module is supported, not '{identifier}'.");
        return new((TValue)(object)new Module(page, new Uri(new Uri(page.Url), path).ToString()));
    }

    private sealed class Module(IPage page, string url) : IJSObjectReference
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            await BrowserSession.CallModuleAsync<TValue>(page, url, identifier, args ?? []);


        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
