namespace Ui.Preferences;

/// <summary>
/// Which Nudges the owner dismissed on this device. Not part of the exported data. A Nudge key already includes its week, so a new week
/// brings the Nudge back with no cleanup. A blocked or empty store reads as not dismissed.
/// </summary>
public sealed class NudgeDismissals(IDevicePreferences preferences)
{
    private const string Prefix = "nudge-dismissed:";

    /// <summary>The given keys that are dismissed.</summary>
    public async Task<IReadOnlySet<string>> LoadAsync(IEnumerable<string> keys)
    {
        var dismissed = new HashSet<string>();
        foreach (var key in keys)
        {
            if (await preferences.GetAsync(Prefix + key) == "1")
            {
                dismissed.Add(key);
            }
        }

        return dismissed;
    }

    public Task DismissAsync(string key) => preferences.SetAsync(Prefix + key, "1");
}
