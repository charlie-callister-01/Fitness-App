using Ui.Saving;

namespace Ui.Accessibility;

/// <summary>
/// The one way a screen tells a screen-reader user that something happened (Badge unlocked, a PR, rest over, an export, Backup or
/// import result). Screens hand it a message; the shell's <see cref="LiveRegion"/> reads it out politely. A status line that also shows on
/// screen goes through <see cref="StatusLine"/>, so the shown and spoken words are one string. A failed save is not announced here: the
/// <see cref="SaveFailureBanner"/> stays assertive (<c>role="alert"</c>).
/// </summary>
public sealed class Announcer
{
    private readonly HashSet<string> _announcedKeys = [];

    /// <summary>The last message announced, or null before the first.</summary>
    public string? Message { get; private set; }

    /// <summary>Raised with the message for every announcement, including one that repeats the last message.</summary>
    public event Action<string>? Announced;

    /// <summary>Reads <paramref name="message"/> out politely; a blank message is ignored.</summary>
    public void Announce(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Message = message;
        Announced?.Invoke(message);
    }

    /// <summary>
    /// Reads <paramref name="message"/> out the first time <paramref name="key"/> is seen and never again for that key, so an event
    /// shown by a component that can be created again (a Badge unlock on the finish summary) is announced once.
    /// </summary>
    public void AnnounceOnce(string key, string message)
    {
        if (_announcedKeys.Add(key))
        {
            Announce(message);
        }
    }
}
