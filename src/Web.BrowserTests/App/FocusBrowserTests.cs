using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace Web.BrowserTests.App;

/// <summary>
/// Focus in the real app (spec B11, Focus): every screen state has exactly one h1, which takes focus after in-app navigation with no ring
/// on that programmatic focus; and the shared Sheet starts focus inside, keeps Tab inside, closes on Escape and hands focus back.
/// </summary>
[Collection(AppCollection.Name)]
public sealed partial class FocusBrowserTests(AppFixture app)
{
    // Every route, including the empty, unknown-id and not-found states; with the fixture the ids below exist.
    private static readonly string[] Routes =
    [
        "/", "/week", "/progress", "/trophies", "/more", "/plans", $"/plans/{FixturePlanId}", "/plans/no-such-plan", "/exercises",
        "/exercises/goblet-squat/guide", "/exercises/no-such-exercise/guide", "/custom", "/custom/makeup/no-such-session", "/settings",
        "/backup", "/weekly-review", "/weight", "/log/w-2026-11-09", "/log/no-such-workout", "/no-such-page",
    ];

    private const string FixturePlanId = "test-plan";
    private const string FixtureOpenWorkoutId = "w-2026-11-17";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Every_screen_state_has_one_h1_that_takes_focus_after_navigation_without_a_ring(bool withFixture)
    {
        await using var page = await app.OpenAsync("/more", new AppPageOptions { WithFixture = withFixture });

        var wrong = new List<string>();
        foreach (var route in Routes)
        {
            // From a different screen each time, so every route is a real navigation.
            await page.GotoAsync(route == "/more" ? "/settings" : "/more");
            await page.GotoAsync(route);

            // Let a screen that loads its data settle before reading it.
            await page.Page.WaitForTimeoutAsync(300);
            var state = await page.Page.EvaluateAsync<string>(
                "() => { const h = document.querySelectorAll('h1'); const f = h.length === 1 && document.activeElement === h[0]; " +
                "return `${h.length} h1, focused ${f}, outline ${h.length ? getComputedStyle(h[0]).outlineStyle : '-'}`; }");
            if (state != "1 h1, focused true, outline none")
            {
                wrong.Add($"{route}: {state}");
            }
        }

        Assert.Empty(wrong);
        Assert.Empty(page.Errors);
    }

    [Fact]
    public async Task ExercisePicker_keeps_focus_inside_closes_on_Escape_and_returns_focus()
    {
        await using var page = await app.OpenAsync("/custom", new AppPageOptions { WithFixture = true });
        var opener = page.Page.GetByRole(AriaRole.Button, new() { Name = "Add Exercise", Exact = true });

        await opener.ClickAsync();
        var sheet = page.Page.GetByRole(AriaRole.Dialog, new() { Name = "Add Exercise" });

        await AssertFocusStaysInsideAsync(page.Page, sheet);
        await page.Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(sheet).ToBeHiddenAsync();
        await Assertions.Expect(opener).ToBeFocusedAsync();
    }

    [Fact]
    public async Task DateRangePicker_keeps_focus_inside_closes_on_Escape_and_returns_focus()
    {
        await using var page = await app.OpenAsync("/progress", new AppPageOptions { WithFixture = true });
        var opener = page.Page.GetByRole(AriaRole.Button, new() { NameRegex = DateRangeField() });

        await opener.ClickAsync();
        var sheet = page.Page.GetByRole(AriaRole.Dialog, new() { Name = "Date range" });

        await AssertFocusStaysInsideAsync(page.Page, sheet);
        await page.Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(sheet).ToBeHiddenAsync();
        await Assertions.Expect(opener).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Confirm_sheet_keeps_focus_inside_closes_on_Escape_and_returns_focus()
    {
        await using var page = await app.OpenAsync("/exercises/goblet-squat/guide", new AppPageOptions { WithFixture = true });
        await page.Page.Locator("#main-content").GetByRole(AriaRole.Button, new() { Name = "Edit", Exact = true }).ClickAsync();
        await page.Page.Locator("#guide-cues").FillAsync("Elbows in");
        var opener = page.Page.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true });

        await opener.ClickAsync();
        var sheet = page.Page.GetByRole(AriaRole.Dialog, new() { Name = "Discard changes?" });

        await AssertFocusStaysInsideAsync(page.Page, sheet);
        await page.Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(sheet).ToBeHiddenAsync();
        await Assertions.Expect(opener).ToBeFocusedAsync();
        await Assertions.Expect(page.Page.Locator("#guide-cues")).ToHaveValueAsync("Elbows in");
    }

    [Fact]
    public async Task PainCheckSheet_keeps_focus_inside_and_Escape_skips_to_the_summary_heading()
    {
        // The fixture's open "Morning mobility" Workout has every Exercise done, so Finish goes straight to the Pain check.
        await using var page = await app.OpenAsync($"/log/{FixtureOpenWorkoutId}", new AppPageOptions { WithFixture = true });
        await page.Page.GetByRole(AriaRole.Button, new() { Name = "Workout menu", Exact = true }).ClickAsync();
        await page.Page.GetByRole(AriaRole.Button, new() { NameRegex = FinishWorkout() }).ClickAsync();
        var sheet = page.Page.GetByRole(AriaRole.Dialog, new() { Name = "Any pain today?" });

        await AssertFocusStaysInsideAsync(page.Page, sheet);
        await page.Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(sheet).ToBeHiddenAsync();

        // The control that led here (Finish) has gone with the log, so focus goes to the new screen's heading.
        var heading = page.Page.Locator("h1");
        await Assertions.Expect(heading).ToHaveTextAsync("Workout done");
        await Assertions.Expect(heading).ToBeFocusedAsync();

        // No sheet is left holding Tab: it moves on through the screen.
        await page.Page.Keyboard.PressAsync("Tab");
        await Assertions.Expect(heading).Not.ToBeFocusedAsync();
        Assert.False(await page.Page.EvaluateAsync<bool>("() => document.activeElement === document.body"));
        Assert.Empty(page.Errors);
    }

    // Focus starts inside; Tab and Shift+Tab, pressed more times than there are controls, never leave the sheet.
    private static async Task AssertFocusStaysInsideAsync(IPage page, ILocator sheet)
    {
        await Assertions.Expect(sheet).ToBeVisibleAsync();
        await page.WaitForFunctionAsync("d => d.contains(document.activeElement)", await sheet.ElementHandleAsync(), new() { Timeout = 5_000 });

        var controls = await sheet.EvaluateAsync<int>("d => d.querySelectorAll('button, a[href], input, select, textarea').length");
        foreach (var key in new[] { "Tab", "Shift+Tab" })
        {
            for (var i = 0; i < controls + 2; i++)
            {
                await page.Keyboard.PressAsync(key);
                await AssertFocusInsideAsync(sheet);
            }
        }
    }

    private static async Task AssertFocusInsideAsync(ILocator sheet) =>
        Assert.True(await sheet.EvaluateAsync<bool>("d => d.contains(document.activeElement)"), "Focus left the sheet.");

    [GeneratedRegex("^Date range: ")]
    private static partial Regex DateRangeField();

    [GeneratedRegex("^Finish Workout")]
    private static partial Regex FinishWorkout();
}
