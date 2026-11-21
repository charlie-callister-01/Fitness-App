using Microsoft.JSInterop;
using Ui.Preferences;

namespace Web.Preferences;

/// <summary>
/// Per-device settings over the browser's localStorage (no script file of its own: it calls localStorage directly). Blocked or cleared
/// storage reads as "never set" and a failed write is dropped, so a choice then lasts only until the page is closed. No rules.
/// </summary>
public sealed class LocalStorageDevicePreferences(IJSRuntime js) : IDevicePreferences
{
    private const string Prefix = "fitness-app.";

    public async Task<string?> GetAsync(string key)
    {
        try
        {
            return await js.InvokeAsync<string?>("localStorage.getItem", Prefix + key);
        }
        catch (Exception exception) when (exception is JSException or InvalidOperationException)
        {
            return null;
        }
    }

    public async Task SetAsync(string key, string value)
    {
        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", Prefix + key, value);
        }
        catch (Exception exception) when (exception is JSException or InvalidOperationException)
        {
            // Storage is blocked or full: the choice is kept for this session only.
        }
    }
}
