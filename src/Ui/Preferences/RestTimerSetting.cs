namespace Ui.Preferences;

/// <summary>
/// The rest timer length the owner chose (Off, 60, 90 or 120 s), remembered on this device and not part of the exported data. Off is the
/// default, and anything stored that is not one of the choices reads as Off. Saving never throws into the UI.
/// </summary>
public sealed class RestTimerSetting(IDevicePreferences preferences)
{
    private const string Key = "rest-timer-seconds";

    /// <summary>The choices in order; 0 is Off.</summary>
    public static IReadOnlyList<int> Choices { get; } = [0, 60, 90, 120];

    private bool _loaded;

    /// <summary>The chosen length in seconds; 0 means Off.</summary>
    public int Seconds { get; private set; }

    /// <summary>Raised after <see cref="Seconds"/> changes.</summary>
    public event Action? Changed;

    /// <summary>Reads the remembered choice, once.</summary>
    public async Task LoadAsync()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;
        var stored = await preferences.GetAsync(Key);
        Seconds = int.TryParse(stored, out var seconds) && Choices.Contains(seconds) ? seconds : 0;
        Changed?.Invoke();
    }

    public async Task SetAsync(int seconds)
    {
        if (!Choices.Contains(seconds))
        {
            return;
        }

        _loaded = true;
        Seconds = seconds;
        Changed?.Invoke();
        await preferences.SetAsync(Key, seconds.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
