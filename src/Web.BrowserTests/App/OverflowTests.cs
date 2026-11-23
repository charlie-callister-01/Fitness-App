using Microsoft.Playwright;

namespace Web.BrowserTests.App;

/// <summary>
/// No screen or tab (<see cref="AppScreens"/>) scrolls sideways on a 360 px phone, empty or with the fixture, at normal text and with the
/// root text at 200%.
/// </summary>
[Collection(AppCollection.Name)]
public sealed class OverflowTests(AppFixture app)
{
    // Waits until the screen has stopped changing (data loads after the h1 renders), then lists what sticks out to the right.
    private const string MeasureScript = """
        async () => {
          await new Promise(resolve => {
            let timer = setTimeout(done, 300);
            const observer = new MutationObserver(() => { clearTimeout(timer); timer = setTimeout(done, 300); });
            const giveUp = setTimeout(done, 3000);
            observer.observe(document.body, { childList: true, subtree: true, characterData: true, attributes: true });
            function done() { observer.disconnect(); clearTimeout(timer); clearTimeout(giveUp); resolve(); }
          });
          const width = document.documentElement.clientWidth;
          if (document.documentElement.scrollWidth <= width) return '';
          const culprits = [...document.querySelectorAll('body *')]
            .filter(e => e.getBoundingClientRect().right > width + 0.5)
            .filter(e => !e.parentElement || e.parentElement.getBoundingClientRect().right <= width + 0.5)
            .slice(0, 5)
            .map(e => e.tagName.toLowerCase() + (e.className && typeof e.className === 'string' ? '.' + e.className.trim().split(/\s+/).join('.') : '') + ' "' + (e.textContent || '').trim().slice(0, 40) + '"');
          return document.documentElement.scrollWidth + ' > ' + width + ': ' + culprits.join('; ');
        }
        """;

    public static TheoryData<bool, double> Cases() => new()
    {
        { false, 1 },
        { false, 2 },
        { true, 1 },
        { true, 2 },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task No_screen_scrolls_sideways_at_360_px(bool withFixture, double textScale)
    {
        await using var page = await app.OpenAsync("/", new AppPageOptions { WithFixture = withFixture, TextScale = textScale });

        var overflowing = new List<string>();
        foreach (var screen in AppScreens.All.Where(s => s.RunsWith(withFixture)))
        {
            try
            {
                await AppScreens.ShowAsync(page, screen);
            }
            catch (TimeoutException)
            {
                overflowing.Add(screen.Name + " did not render");
                continue;
            }

            var result = await page.Page.EvaluateAsync<string>(MeasureScript);
            if (result.Length > 0)
            {
                overflowing.Add(screen.Name + " " + result);
            }
        }

        Assert.True(overflowing.Count == 0, string.Join(Environment.NewLine, overflowing));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task The_picker_and_a_Workout_log_in_progress_do_not_scroll_sideways_at_360_px(double textScale)
    {
        // The fixture leaves its Morning mobility Workout open; it is finished first so Start begins a new Workout.
        await using var page = await app.OpenAsync($"/log/{FixtureBackup.OpenWorkoutId}", new AppPageOptions { WithFixture = true, TextScale = textScale });
        var main = page.Page.Locator("#main-content");
        await main.GetByRole(AriaRole.Button, new() { Name = "Finish", Exact = true }).ClickAsync();
        await page.Page.GetByRole(AriaRole.Button, new() { Name = "No", Exact = true }).ClickAsync();
        await Assertions.Expect(main.Locator("h1")).ToHaveTextAsync("Workout done");
        await page.GotoAsync("/custom");

        await main.GetByRole(AriaRole.Button, new() { Name = "Add Exercise" }).ClickAsync();
        await page.Page.GetByRole(AriaRole.Button, new() { Name = "Goblet squat" }).ClickAsync();
        await main.GetByRole(AriaRole.Button, new() { Name = "Add Exercise" }).ClickAsync();
        Assert.Equal("", await page.Page.EvaluateAsync<string>(MeasureScript));
        await page.Page.GetByRole(AriaRole.Button, new() { Name = "Row erg 1000 m" }).ClickAsync();

        await main.GetByRole(AriaRole.Button, new() { Name = "Start", Exact = true }).ClickAsync();
        await Assertions.Expect(main.Locator(".fa-exercise__name").First).ToHaveTextAsync("Goblet squat");
        await Assertions.Expect(main.GetByRole(AriaRole.Textbox).First).ToBeVisibleAsync();

        Assert.Equal("", await page.Page.EvaluateAsync<string>(MeasureScript));
    }

    [Fact]
    public async Task Ellipsised_tile_names_keep_the_full_name_in_the_accessible_name()
    {
        await using var page = await app.OpenAsync("/trophies", new AppPageOptions { WithFixture = true, TextScale = 2 });
        await Assertions.Expect(page.Page.Locator(".fa-tile").First).ToBeVisibleAsync();

        // Each visibly cut name (text without its aria-hidden marker) paired with the accessible name its tile carries.
        var cut = await page.Page.EvaluateAsync<string[][]>("""
            () => [...document.querySelectorAll('.fa-tile__n')]
              .filter(n => n.scrollWidth > n.clientWidth)
              .map(n => {
                const copy = n.cloneNode(true);
                copy.querySelectorAll('[aria-hidden="true"]').forEach(h => h.remove());
                return [copy.textContent.trim(), n.closest('[aria-label]')?.getAttribute('aria-label') ?? ''];
              })
            """);

        Assert.NotEmpty(cut);
        Assert.All(cut, pair => Assert.Contains(pair[0], pair[1], StringComparison.Ordinal));
    }
}
