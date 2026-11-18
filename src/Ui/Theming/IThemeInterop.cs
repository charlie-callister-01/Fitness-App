using Core.Model;

namespace Ui.Theming;

/// <summary>
/// The narrow seam to the browser for the look of the app (real implementation in Web, over <c>wwwroot/js/theme.js</c>; a fake in tests).
/// Applying sets the Theme and Mode attributes on the page, resolves System with the phone's colour scheme (and keeps following it),
/// sets the browser chrome colour and refreshes the local mirror the pre-paint script reads.
/// </summary>
public interface IThemeInterop
{
    Task ApplyAsync(Theme theme, Mode mode);
}
