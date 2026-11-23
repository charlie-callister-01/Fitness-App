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
    // Every screen's route and every not-found state; with the fixture the ids exist, empty they show their not-found states.
    private static readonly string[] Routes = [.. AppScreens.All.Select(s => s.Route).Distinct(), .. AppScreens.NotFoundRoutes];
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

            // Let a screen that loads its data settle: wait for its one h1 to hold focus, or give up and report what is there.
            try
            {
                await page.Page.WaitForFunctionAsync(
                    "() => { const h = document.querySelectorAll('h1'); return h.length === 1 && document.activeElement === h[0]; }",
                    null, new() { Timeout = 5_000 });
            }
            catch (TimeoutException)
            {
                // Reported below with the route.
            }
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
    public async Task Moving_between_two_routes_of_one_screen_focuses_its_h1()
    {
        await using var page = await app.OpenAsync("/custom", new AppPageOptions { WithFixture = true });

        await page.GotoAsync("/custom/makeup/no-such-session");

        var heading = page.Page.Locator("h1");
        await Assertions.Expect(heading).ToHaveTextAsync("Make up a Session");
        await Assertions.Expect(heading).ToBeFocusedAsync();

        // Two Guides render the same h1 element with new text.
        await page.GotoAsync($"/exercises/{FixtureBackup.GuideExerciseId}/guide");
        await page.GotoAsync("/exercises/plank/guide");
        await Assertions.Expect(heading).ToHaveTextAsync("Plank");
        await Assertions.Expect(heading).ToBeFocusedAsync();
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
        await using var page = await app.OpenAsync($"/exercises/{FixtureBackup.GuideExerciseId}/guide", new AppPageOptions { WithFixture = true });
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
        await using var page = await app.OpenAsync($"/log/{FixtureBackup.OpenWorkoutId}", new AppPageOptions { WithFixture = true });
        await page.WorkoutMenu.ClickAsync();
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
