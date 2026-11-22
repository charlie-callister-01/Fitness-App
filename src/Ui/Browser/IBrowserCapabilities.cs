namespace Ui.Browser;

/// <summary>
/// The narrow browser-owned capabilities that Ui needs and tests fake: clipboard copy, file download, Web Share, standalone PWA
/// detection, and install prompting. No rules here; every failure returns false rather than throwing.
/// </summary>
public interface IBrowserCapabilities
{
    /// <summary>Raised when <c>beforeinstallprompt</c> or <c>appinstalled</c> changes install prompt availability.</summary>
    event Action? InstallAvailabilityChanged;

    Task<bool> CopyTextAsync(string text);

    Task<bool> DownloadFileAsync(string fileName, string content, string contentType = "application/json");

    Task<bool> CanShareAsync();

    Task<bool> ShareAsync(string title, string text, string? fileName = null, string contentType = "text/plain");

    Task<bool> IsStandaloneAsync();

    Task<bool> CanInstallAsync();

    Task<bool> PromptInstallAsync();
}
