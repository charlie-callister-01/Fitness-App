using Core.Model;

namespace Ui.Theming;

/// <summary>The words on the Theme and Mode choices, shared by every screen that offers them (Settings and the Workout menu).</summary>
public static class AppearanceLabels
{
    public static IReadOnlyDictionary<Theme, string> Themes { get; } = new Dictionary<Theme, string>
    {
        [Theme.Keep] = "Keep",
        [Theme.Clean] = "Clean",
    };

    public static IReadOnlyDictionary<Mode, string> Modes { get; } = new Dictionary<Mode, string>
    {
        [Mode.System] = "Match phone",
        [Mode.Dark] = "Dark",
        [Mode.Light] = "Light",
    };
}
