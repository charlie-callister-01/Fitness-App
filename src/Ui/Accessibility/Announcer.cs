namespace Ui.Saving;

/// <summary>
/// The one way a screen tells a screen-reader user that something happened (Badge unlocked, a PR, rest over, an export, Backup or
/// import result). Screens hand it a message; the shell's <see cref="LiveRegion"/> reads it out politely. Visible status text stays
/// on the screen as it is; only the announcement comes through here. A failed save is not announced here: the
/// <see cref="SaveFailureBanner"/> stays assertive (<c>role="alert"</c>).
/// </summary>
public sealed class Announcer
{
    /// <summary>The last message announced, or null before the first.</summary>
    public string? Message { get; private set; }

    /// <summary>Raised for every announcement, including one that repeats the last message.</summary>
    public event Action? Announced;

    /// <summary>Reads <paramref name="message"/> out politely; a blank message is ignored.</summary>
    public void Announce(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Message = message;
        Announced?.Invoke();
    }
}
