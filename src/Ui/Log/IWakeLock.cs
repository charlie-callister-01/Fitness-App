namespace Ui.Log;

/// <summary>
/// Keeps the screen awake while the log is open. Every member is safe to call anywhere: a browser without the feature, a refusal or a
/// failure is silently a no-op, never an error. Reacquiring after the tab is hidden and shown again is the implementation's job.
/// </summary>
public interface IWakeLock
{
    /// <summary>Whether this browser can hold a screen lock at all.</summary>
    Task<bool> IsSupportedAsync();

    Task AcquireAsync();

    Task ReleaseAsync();
}
