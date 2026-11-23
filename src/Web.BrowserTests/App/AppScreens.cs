using Core.Model;
using Microsoft.Playwright;
using static Web.BrowserTests.App.FixtureBackup;

namespace Web.BrowserTests.App;

/// <summary>Whether a screen is checked with no data, with the fixture, or both.</summary>
public enum ScreenData { Both, EmptyOnly, FixtureOnly }

/// <summary>A screen state: its name, its route, for a tabbed screen the tab to show, and which data it is checked with.</summary>
public sealed record AppScreen(string Name, string Route, string? Tab = null, ScreenData Data = ScreenData.Both)
{
    public bool RunsWith(bool withFixture) => Data == ScreenData.Both || Data == (withFixture ? ScreenData.FixtureOnly : ScreenData.EmptyOnly);
}

/// <summary>
/// Every screen S-01 to S-13 with each of its tabs, shared by the axe, overflow and focus checks. Routes naming fixture records show their
/// not-found state when the app is empty. Two routes of one page are kept apart, so moving between neighbours is always a new screen.
/// </summary>
public static class AppScreens
{
    // S-12 First run is Home with no data (Welcome); the empty runs of S-01 cover it.
    public static readonly AppScreen[] All =
    [
        new("S-01 Home", "/"),
        new("S-02 Workout log, Swapped", $"/log/{SwappedWorkoutId}"),
        new("S-02 Make up a Session", $"/custom/makeup/{SessionId}"),
        new("S-06 Week", "/week"),
        new("S-09 Weekly review", "/weekly-review"),
        new("S-07 Progress", "/progress", "Progress"),
        new("S-07 Benchmarks", "/progress", "Benchmarks"),
        new("S-07 Calendar", "/progress", "Calendar"),
        new("S-13 Badges", "/trophies", "Badges"),
        new("S-13 Bests", "/trophies", "Bests"),
        new("S-13 Trophies calendar", "/trophies", "Calendar"),
        new("S-11 More", "/more"),
        new("S-11 Settings", "/settings"),
        new("S-10 Export", "/backup", "Export"),
        new("S-10 Import", "/backup", "Import"),
        new("S-10 Prompts", "/backup", "Prompts"),
        new("S-08 Weight", "/weight"),
        new("S-05 Plans", "/plans"),

        // With no data the Plan does not exist, so the editor shows its not-found state and has no tabs.
        new("S-05 Plan editor, no such Plan", $"/plans/{PlanId}", Data: ScreenData.EmptyOnly),
        new("S-05 Plan editor Settings", $"/plans/{PlanId}", "Settings", ScreenData.FixtureOnly),
        new("S-05 Plan editor Phases", $"/plans/{PlanId}", "Phases", ScreenData.FixtureOnly),
        new("S-05 Plan editor Sessions", $"/plans/{PlanId}", "Sessions", ScreenData.FixtureOnly),
        new("S-04 Exercise library", "/exercises"),
        new("S-03 Guide", $"/exercises/{GuideExerciseId}/guide"),
        new("S-02 Custom workout", "/custom"),
        new("S-02 Workout log", $"/log/{FinishedWorkoutId}"),
    ];

    /// <summary>Routes that only ever show a not-found or unknown-id state.</summary>
    public static readonly string[] NotFoundRoutes =
    [
        "/plans/no-such-plan", "/exercises/no-such-exercise/guide", "/custom/makeup/no-such-session", "/log/no-such-workout", "/no-such-page",
    ];

    /// <summary>The four Theme × Mode sets every appearance check runs in.</summary>
    public static readonly (Theme Theme, Mode Mode)[] Appearances =
        [(Theme.Keep, Mode.Dark), (Theme.Keep, Mode.Light), (Theme.Clean, Mode.Dark), (Theme.Clean, Mode.Light)];

    public static AppScreen Named(string name) => All.Single(s => s.Name == name);

    /// <summary>Moves to the screen (staying on its page when only the tab changes) and shows its tab.</summary>
    public static async Task ShowAsync(AppPage page, AppScreen screen)
    {
        if (new Uri(page.Page.Url).AbsolutePath != screen.Route)
        {
            await page.GotoAsync(screen.Route);
        }

        if (screen.Tab is { } tab)
        {
            var button = page.Page.Locator("#main-content").GetByRole(AriaRole.Group)
                .GetByRole(AriaRole.Button, new() { Name = tab, Exact = true });
            await button.ClickAsync();
            await Assertions.Expect(button).ToHaveAttributeAsync("aria-pressed", "true");
        }
    }
}
