namespace Ui.Preferences;

/// <summary>
/// The narrow seam to per-device settings that are not part of the owner's data (real implementation in Web over localStorage; a fake in
/// tests). Values are plain text. A blocked or cleared store simply reads as "never set"; writing never throws into the UI.
/// </summary>
public interface IDevicePreferences
{
    Task<string?> GetAsync(string key);

    Task SetAsync(string key, string value);
}
