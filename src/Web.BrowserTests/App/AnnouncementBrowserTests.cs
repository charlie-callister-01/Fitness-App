using Microsoft.Playwright;

namespace Web.BrowserTests.App;

/// <summary>
/// Announcements (B11): one polite live region in the shell, present on every screen from first render, that reads out a PR, the end
/// of the rest timer, a Badge unlocked and an Import result. Failed saves stay assertive in their own banner.
/// </summary>
[Collection(AppCollection.Name)]
public sealed class AnnouncementBrowserTests(AppFixture app)
{
    private const string LowerSessionName = "A: Lower";

    private static ILocator Region(AppPage page) => page.Page.GetByTestId("live-region");

    [Fact]
    public async Task The_one_polite_region_is_on_every_screen_visually_hidden_and_nothing_is_assertive()
    {
        await using var page = await app.OpenAsync("/");

        foreach (var route in new[] { "/", "/week", "/progress", "/trophies", "/more", "/settings", "/backup", "/plans", "/exercises" })
        {
            await page.GotoAsync(route);

            await Assertions.Expect(Region(page)).ToHaveCountAsync(1);
            await Assertions.Expect(Region(page)).ToHaveAttributeAsync("aria-live", "polite");
            var box = await Region(page).BoundingBoxAsync();
            Assert.NotNull(box);
            Assert.True(box.Width <= 1 && box.Height <= 1, $"The live region shows on {route}: {box.Width} x {box.Height}");
            Assert.Equal(0, await page.Page.Locator("[aria-live='assertive']").CountAsync());
        }
    }

    [Fact]
    public async Task An_Import_result_is_announced()
    {
        await using var page = await app.OpenAsync("/backup?tab=import&kind=restore");

        await page.Page.Locator("#import-paste").FillAsync(FixtureBackup.Json);
        await page.Page.GetByRole(AriaRole.Button, new() { Name = "Check", Exact = true }).ClickAsync();
        var apply = page.Page.GetByTestId("import-preview").GetByRole(AriaRole.Button, new() { Name = "Apply", Exact = true });
        await Assertions.Expect(apply).ToBeEnabledAsync(new() { Timeout = 10_000 });
        await apply.ClickAsync();

        await Assertions.Expect(Region(page)).ToContainTextAsync("Import applied.");
    }

    [Fact]
    public async Task A_PR_the_end_of_the_rest_and_a_Badge_unlocked_are_announced()
    {
        await using var page = await app.OpenAsync("/", new AppPageOptions { WithFixture = true });
        var log = page.Page;
        await log.Clock.InstallAsync();

        // The fixture leaves a Mobility flow open; finish it so A: Lower can start.
        await page.GotoAsync($"/log/{FixtureBackup.OpenWorkoutId}");
        await FinishAsync(log);
        await page.GotoAsync("/");
        await StartLowerAsync(log);

        await log.GetByRole(AriaRole.Button, new() { Name = "Workout menu" }).ClickAsync();
        await log.GetByRole(AriaRole.Group, new() { Name = "Rest timer" }).GetByRole(AriaRole.Button, new() { Name = "60 s", Exact = true }).ClickAsync();
        await log.Keyboard.PressAsync("Escape");

        // 40 kg beats the fixture's best of 24 kg and reaches the "40 kg goblet squat" Badge.
        await log.GetByLabel("Weight, Set 1").FillAsync("40");
        await log.GetByLabel("Reps, Set 1").FillAsync("8");
        await log.Locator(".fa-confirmbar .fa-btn--primary").ClickAsync();
        await Assertions.Expect(Region(page)).ToContainTextAsync("PR! 40 kg");
        await Assertions.Expect(log.Locator(".fa-confirmbar__rest")).ToHaveAttributeAsync("aria-live", "off");

        // The countdown says nothing while it runs, then "Rest over" once at zero.
        await log.Clock.FastForwardAsync("00:30");
        await Assertions.Expect(log.Locator(".fa-confirmbar__rest")).ToHaveTextAsync("Rest 0:30");
        await Assertions.Expect(Region(page)).ToContainTextAsync("PR!");
        await log.Clock.FastForwardAsync("00:31");
        await Assertions.Expect(log.Locator(".fa-confirmbar__rest")).ToHaveCountAsync(0);
        await Assertions.Expect(Region(page)).ToHaveTextAsync("Rest over");

        await FinishAsync(log);
        await Assertions.Expect(Region(page)).ToHaveTextAsync("Badge unlocked: 40 kg goblet squat");
        Assert.Empty(page.Errors);
    }

    // Workout menu, Finish Workout, Finish as Partial when asked, and No to the Pain check.
    private static async Task FinishAsync(IPage log)
    {
        await log.GetByRole(AriaRole.Button, new() { Name = "Workout menu" }).ClickAsync();
        await log.GetByRole(AriaRole.Button, new() { Name = "Finish Workout" }).ClickAsync();
        var partial = log.GetByRole(AriaRole.Dialog, new() { Name = "Finish as Partial?" });
        var pain = log.GetByRole(AriaRole.Dialog, new() { Name = "Any pain today?" });
        await partial.Or(pain).First.WaitForAsync();
        if (await partial.IsVisibleAsync())
        {
            await partial.GetByRole(AriaRole.Button, new() { Name = "Finish", Exact = true }).ClickAsync();
        }

        await pain.GetByRole(AriaRole.Button, new() { Name = "No", Exact = true }).ClickAsync();
        await log.GetByRole(AriaRole.Link, new() { Name = "Done", Exact = true }).WaitForAsync();
    }

    // Starts the Plan's "A: Lower" Session: from Today when it is today's, otherwise through Do a different Session.
    private static async Task StartLowerAsync(IPage home)
    {
        var today = home.GetByRole(AriaRole.Region, new() { Name = "Today", Exact = true });
        if (await today.GetByText(LowerSessionName).CountAsync() > 0)
        {
            await today.GetByRole(AriaRole.Button, new() { Name = "Start" }).First.ClickAsync();
        }
        else
        {
            await home.GetByRole(AriaRole.Button, new() { Name = "Do a different Session" }).ClickAsync();
            await home.GetByRole(AriaRole.Dialog).Locator(".fa-sessionrow", new() { HasText = LowerSessionName }).ClickAsync();
        }

        await Assertions.Expect(home.Locator("#main-content h1")).ToHaveTextAsync(LowerSessionName);
    }
}
