using Core.Model;
using Microsoft.JSInterop;
using Ui.Theming;

namespace Web.Theming;

/// <summary>Typed wrapper over <c>wwwroot/js/theme.js</c>, which index.html loads before first paint (it exposes <c>window.FitnessTheme</c>). No rules.</summary>
public sealed class ThemeInterop(IJSRuntime js) : IThemeInterop
{
    public async Task ApplyAsync(Theme theme, Mode mode) =>
        await js.InvokeVoidAsync("FitnessTheme.apply", theme.ToString().ToLowerInvariant(), mode.ToString().ToLowerInvariant());
}
