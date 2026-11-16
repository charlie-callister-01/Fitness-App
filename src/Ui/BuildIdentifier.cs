namespace Ui;

/// <summary>Turns a raw version string into the short identifier shown in the app.</summary>
public static class BuildIdentifier
{
    public const string Fallback = "dev";

    private const int ShortLength = 7;

    /// <summary>
    /// Drops any "+metadata" suffix the SDK appends to the informational version and keeps the
    /// first 7 characters (a short commit id). Null or blank input gives <see cref="Fallback"/>.
    /// </summary>
    public static string Shorten(string? version)
    {
        var core = version?.Split('+')[0];
        if (string.IsNullOrWhiteSpace(core))
        {
            return Fallback;
        }

        return core.Length <= ShortLength ? core : core[..ShortLength];
    }
}
