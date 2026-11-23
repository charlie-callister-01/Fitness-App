using Core.Model;

namespace Web.BrowserTests.App;

/// <summary>How <see cref="AppFixture.OpenAsync"/> opens the app. The defaults are a 360 px phone, Keep dark, normal text and motion, no data.</summary>
public sealed record AppPageOptions
{
    public int Width { get; init; } = 360;

    public int Height { get; init; } = 780;

    /// <summary>Chosen on the Settings screen, as the owner would.</summary>
    public Theme Theme { get; init; } = Theme.Keep;

    /// <summary>Chosen on the Settings screen. <see cref="Mode.System"/> follows the context's colour scheme, which is dark.</summary>
    public Mode Mode { get; init; } = Mode.Dark;

    /// <summary>The root font size as a multiple of the browser default (2 is 200%).</summary>
    public double TextScale { get; init; } = 1;

    public bool ReducedMotion { get; init; }

    /// <summary>Restores the committed fixture Backup through the Import tab before anything else.</summary>
    public bool WithFixture { get; init; }
}
