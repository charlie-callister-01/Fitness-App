using Core.Storage;

namespace Ui.Saving;

/// <summary>The plain reason a window cannot save, worded once for every page that has to say so.</summary>
public static class ReadOnlyWording
{
    /// <summary>Why <paramref name="data"/> cannot be written, or null when it can.</summary>
    public static string? For(AppData data) =>
        data.Health.CanWrite ? null
        : data.Health.AnotherWindowOpen ? "Another window of the app is open and is the one that saves. This page is read-only here."
        : data.Startup.State is StartupState.CannotRead or StartupState.StorageFailed
            ? "Your saved data could not be read, so nothing can be shown or saved here. Your data is untouched."
            : "This window cannot save right now, so this page is read-only.";
}
