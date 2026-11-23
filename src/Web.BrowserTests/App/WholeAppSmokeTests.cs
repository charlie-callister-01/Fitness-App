using Core.Model;
using Microsoft.Playwright;

namespace Web.BrowserTests.App;

/// <summary>The published app runs from the fake origin: Home opens empty and with the fixture Backup restored, in every Theme and Mode.</summary>
[Collection(AppCollection.Name)]
public sealed class WholeAppSmokeTests(AppFixture app)
{
    public static TheoryData<Theme, Mode, bool> ThemeModeData()
    {
        var data = new TheoryData<Theme, Mode, bool>();
        foreach (var (theme, mode) in AppScreens.Appearances)
        {
            data.Add(theme, mode, false);
            data.Add(theme, mode, true);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ThemeModeData))]
    public async Task Home_opens_in_each_Theme_and_Mode_empty_and_with_the_fixture(Theme theme, Mode mode, bool withFixture)
    {
        await using var page = await app.OpenAsync("/", new AppPageOptions { Theme = theme, Mode = mode, WithFixture = withFixture });

        var html = page.Page.Locator("html");
        await Assertions.Expect(html).ToHaveAttributeAsync("data-theme", theme.ToString().ToLowerInvariant());
        await Assertions.Expect(html).ToHaveAttributeAsync("data-mode", mode.ToString().ToLowerInvariant());

        var main = page.Page.Locator("#main-content");
        if (withFixture)
        {
            await Assertions.Expect(main.GetByRole(AriaRole.Region, new() { Name = FixtureBackup.DailyTrackName, Exact = true })).ToBeVisibleAsync();
            await Assertions.Expect(main.GetByRole(AriaRole.Region, new() { Name = "Today", Exact = true })).Not.ToContainTextAsync("No active Plan");
        }
        else
        {
            await Assertions.Expect(main.GetByRole(AriaRole.Heading, new() { Name = "Welcome to the Keep" })).ToBeVisibleAsync();
        }

        await Assertions.Expect(page.Page.Locator("#blazor-error-ui")).ToBeHiddenAsync();
        Assert.Empty(page.Errors);
    }

    [Fact]
    public async Task The_page_helper_sets_the_viewport_text_scale_and_reduced_motion_and_uses_its_own_database()
    {
        await using var page = await app.OpenAsync("/", new AppPageOptions { Width = 900, Height = 700, TextScale = 2, ReducedMotion = true });

        Assert.Equal(900, await page.Page.EvaluateAsync<int>("window.innerWidth"));
        Assert.Equal("32px", await page.Page.EvaluateAsync<string>("getComputedStyle(document.documentElement).fontSize"));
        Assert.True(await page.Page.EvaluateAsync<bool>("matchMedia('(prefers-reduced-motion: reduce)').matches"));
        Assert.Contains(page.DatabaseName, await page.Page.EvaluateAsync<string[]>("(async () => (await indexedDB.databases()).map(d => d.name))()"));
    }

    [Fact]
    public async Task The_page_helper_navigates_inside_the_app_and_waits_for_the_new_screen()
    {
        await using var page = await app.OpenAsync("/", new AppPageOptions { WithFixture = true });

        await page.GotoAsync("/plans");

        await Assertions.Expect(page.Page.Locator("#main-content h1")).ToHaveTextAsync("Plans");
        await Assertions.Expect(page.Page.Locator("#main-content")).ToContainTextAsync(FixtureBackup.PlanName);
    }
}
