using Microsoft.Playwright;

namespace Web.BrowserTests.App;

/// <summary>
/// Design §9 by element positions: from 900 px the TabBar is a 220 px left rail and Home shows its main and side columns side by side;
/// below 900 px the TabBar is the bottom bar and Home is one column. The Workout log keeps the rail on wide screens and stays one centred
/// column of at most 430 px.
/// </summary>
[Collection(AppCollection.Name)]
public sealed class WideLayoutTests(AppFixture app)
{
    private const int RailWidth = 220;
    private const int Height = 800;
    private static readonly string[] TabNames = ["Home", "Week", "Progress", "Trophies", "More"];

    [Fact]
    public async Task At_1280_px_the_TabBar_is_a_left_rail_with_the_same_items_stacked_icon_beside_word()
    {
        await using var page = await app.OpenAsync("/", new AppPageOptions { Width = 1280, Height = Height, WithFixture = true });

        var rail = await BoxAsync(Nav(page));
        Assert.Equal(0, rail.X, 1);
        Assert.Equal(RailWidth, rail.Width, 1);
        Assert.Equal(Height, rail.Height, 1);

        var tabs = await TabBoxesAsync(page);
        for (var i = 1; i < tabs.Count; i++)
        {
            Assert.Equal(tabs[0].X, tabs[i].X, 1);
            Assert.True(tabs[i].Y >= tabs[i - 1].Y + tabs[i - 1].Height, $"{TabNames[i]} sits below {TabNames[i - 1]}");
        }

        var home = Nav(page).GetByRole(AriaRole.Link, new() { Name = "Home", Exact = true });
        var icon = await BoxAsync(home.Locator("svg"));
        var word = await BoxAsync(home.Locator("span"));
        Assert.True(word.X >= icon.X + icon.Width, "The word sits beside the icon");
        Assert.True(word.Y < icon.Y + icon.Height && icon.Y < word.Y + word.Height, "The word and icon share a line");

        var main = await BoxAsync(page.Page.Locator("#main-content"));
        Assert.True(main.X >= RailWidth, "The content starts beside the rail");
    }

    [Theory]
    [InlineData(700)]
    [InlineData(360)]
    public async Task Below_900_px_the_TabBar_is_the_bottom_bar_with_the_same_items_in_a_row(int width)
    {
        await using var page = await app.OpenAsync("/", new AppPageOptions { Width = width, Height = Height, WithFixture = true });

        var bar = await BoxAsync(Nav(page));
        Assert.Equal(0, bar.X, 1);
        Assert.Equal(width, bar.Width, 1);
        Assert.Equal(Height, bar.Y + bar.Height, 1);

        var tabs = await TabBoxesAsync(page);
        for (var i = 1; i < tabs.Count; i++)
        {
            Assert.True(tabs[i].X >= tabs[i - 1].X + tabs[i - 1].Width - 1, $"{TabNames[i]} sits right of {TabNames[i - 1]}");
            Assert.Equal(tabs[0].Y, tabs[i].Y, 1);
        }
    }

    [Fact]
    public async Task At_1280_px_Home_shows_the_main_and_side_columns_side_by_side()
    {
        await using var page = await app.OpenAsync("/", new AppPageOptions { Width = 1280, Height = Height, WithFixture = true });

        var (main, side) = await HomeColumnsAsync(page);
        Assert.True(main.X >= RailWidth, "The main column starts beside the rail");
        Assert.True(side.X >= main.X + main.Width, "The side column sits right of the main column");
        Assert.Equal(main.Y, side.Y, 1);
        Assert.True(main.Width > side.Width, "The main column is the wide one");

        await AssertGroupingAsync(page);
    }

    [Theory]
    [InlineData(700)]
    [InlineData(360)]
    public async Task Below_900_px_Home_is_one_column_main_then_side(int width)
    {
        await using var page = await app.OpenAsync("/", new AppPageOptions { Width = width, Height = Height, WithFixture = true });

        var (main, side) = await HomeColumnsAsync(page);
        Assert.Equal(main.X, side.X, 1);
        Assert.True(side.Y >= main.Y + main.Height, "The side column follows the main column");

        await AssertGroupingAsync(page);
    }

    [Fact]
    public async Task At_1280_px_the_Workout_log_keeps_the_rail_and_is_one_centred_column_of_at_most_430_px()
    {
        await using var page = await app.OpenAsync("/", new AppPageOptions { Width = 1280, Height = Height, WithFixture = true });
        await StartTheDailyTrackAsync(page);

        var rail = await BoxAsync(Nav(page));
        Assert.Equal(RailWidth, rail.Width, 1);

        var log = await BoxAsync(page.Page.Locator("#main-content .log"));
        Assert.True(log.Width <= 430.5, $"The log column is {log.Width} px wide");
        var leftGap = log.X - RailWidth;
        var rightGap = 1280 - (log.X + log.Width);
        Assert.Equal(leftGap, rightGap, 1);

        var bar = await BoxAsync(page.Page.Locator("#main-content .fa-confirmbar"));
        Assert.True(bar.X >= RailWidth, "The ConfirmBar does not cover the rail");
        Assert.Equal(log.X, bar.X, 1);
        Assert.Equal(log.Width, bar.Width, 1);
    }

    [Fact]
    public async Task At_360_px_the_Workout_log_has_no_TabBar()
    {
        await using var page = await app.OpenAsync("/", new AppPageOptions { Width = 360, Height = Height, WithFixture = true });
        await StartTheDailyTrackAsync(page);

        await Assertions.Expect(Nav(page)).ToBeHiddenAsync();
        var log = await BoxAsync(page.Page.Locator("#main-content .log"));
        Assert.Equal(0, log.X, 1);
        Assert.Equal(360, log.Width, 1);
    }

    private static ILocator Nav(AppPage page) => page.Page.GetByRole(AriaRole.Navigation, new() { Name = "Main", Exact = true });

    private static async Task<List<LocatorBoundingBoxResult>> TabBoxesAsync(AppPage page)
    {
        var links = Nav(page).GetByRole(AriaRole.Link);
        Assert.Equal(TabNames, await links.AllInnerTextsAsync());
        var boxes = new List<LocatorBoundingBoxResult>();
        foreach (var name in TabNames)
        {
            boxes.Add(await BoxAsync(Nav(page).GetByRole(AriaRole.Link, new() { Name = name, Exact = true })));
        }

        return boxes;
    }

    private static async Task<(LocatorBoundingBoxResult Main, LocatorBoundingBoxResult Side)> HomeColumnsAsync(AppPage page) =>
        (await BoxAsync(page.Page.Locator(".fa-home__main")), await BoxAsync(page.Page.Locator(".fa-home__side")));

    // The mockup's grouping: the TodayCard, week and Nudges in the main column; Recent PRs and Weight beside them.
    private static async Task AssertGroupingAsync(AppPage page)
    {
        var main = page.Page.Locator(".fa-home__main");
        var side = page.Page.Locator(".fa-home__side");
        await Assertions.Expect(main.GetByRole(AriaRole.Region, new() { Name = "Today", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(main.Locator(".fa-weekstrip, .fa-weekline").First).ToBeVisibleAsync();
        await Assertions.Expect(side.GetByRole(AriaRole.Region, new() { Name = "Recent PRs", Exact = true })).ToBeVisibleAsync();
        await Assertions.Expect(side.GetByRole(AriaRole.Region, new() { Name = "Weight", Exact = true })).ToBeVisibleAsync();
    }

    private static async Task StartTheDailyTrackAsync(AppPage page)
    {
        var daily = page.Page.GetByRole(AriaRole.Region, new() { Name = FixtureBackup.DailyTrackName, Exact = true });
        await daily.GetByRole(AriaRole.Button).Filter(new() { HasTextRegex = new System.Text.RegularExpressions.Regex("^(Start|Continue)$") }).ClickAsync();
        await page.Page.WaitForURLAsync("**/log/**");
        await page.Page.Locator("#main-content .fa-confirmbar").WaitForAsync();
    }

    private static async Task<LocatorBoundingBoxResult> BoxAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.NotNull(box);
        return box;
    }
}
