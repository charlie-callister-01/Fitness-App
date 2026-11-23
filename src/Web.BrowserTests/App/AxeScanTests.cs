using Core.Model;
using Microsoft.Playwright;
using Xunit.Abstractions;

namespace Web.BrowserTests.App;

/// <summary>
/// axe scans (WCAG 2.0/2.1 A and AA) of every screen and tab in <see cref="AppScreens"/>, in all four Theme × Mode sets, empty and with the fixture,
/// plus the open state of each sheet that needs data to appear. Serious and critical violations fail; moderate and minor are written
/// to the test output for the refactor backlog. Suppressions live only in <see cref="AxeSuppressions"/>.
/// </summary>
[Collection(AppCollection.Name)]
public sealed class AxeScanTests(AppFixture app, ITestOutputHelper output)
{
    public static TheoryData<string, Theme, Mode, bool> ScreenData()
    {
        var data = new TheoryData<string, Theme, Mode, bool>();
        foreach (var screen in AppScreens.All)
        {
            foreach (var (theme, mode) in AppScreens.Appearances)
            {
                if (screen.RunsWith(withFixture: false)) data.Add(screen.Name, theme, mode, false);
                if (screen.RunsWith(withFixture: true)) data.Add(screen.Name, theme, mode, true);
            }
        }

        return data;
    }

    public static TheoryData<string, Theme, Mode> SheetData()
    {
        var data = new TheoryData<string, Theme, Mode>();
        foreach (var sheet in Sheets.Keys)
        {
            foreach (var (theme, mode) in AppScreens.Appearances)
            {
                data.Add(sheet, theme, mode);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ScreenData))]
    public async Task Screen_has_no_serious_or_critical_axe_violations(string screen, Theme theme, Mode mode, bool withFixture)
    {
        var appScreen = AppScreens.Named(screen);
        await using var page = await app.OpenAsync(appScreen.Route, Options(theme, mode, withFixture));
        await AppScreens.ShowAsync(page, appScreen);

        await AssertNoBlockingViolationsAsync(page.Page);
    }

    /// <summary>Sheets that only open with data: each opens from the fixture's Home, Week or a live Workout.</summary>
    private static readonly Dictionary<string, Func<AppPage, Task>> Sheets = new()
    {
        ["Do a different Session"] = async page =>
            await MainContent(page.Page).GetByRole(AriaRole.Button, new() { Name = "Do a different Session", Exact = true }).ClickAsync(),
        ["Log a past Workout"] = async page =>
            await MainContent(page.Page).GetByRole(AriaRole.Button, new() { Name = "Log a past Workout", Exact = true }).ClickAsync(),
        ["C-13 Pain check"] = async page =>
        {
            await StartMobilityAsync(page);
            await page.Page.GetByRole(AriaRole.Button, new() { Name = "Workout menu", Exact = true }).ClickAsync();
            await Dialog(page.Page).GetByRole(AriaRole.Button, new() { Name = "Finish", Exact = false }).First.ClickAsync();
            var partial = page.Page.GetByRole(AriaRole.Dialog, new() { Name = "Finish as Partial?" });
            await partial.Or(page.Page.GetByRole(AriaRole.Dialog, new() { Name = "Any pain today?" })).First.WaitForAsync();
            if (await partial.IsVisibleAsync())
            {
                await partial.GetByRole(AriaRole.Button).Filter(new() { HasText = "Finish" }).First.ClickAsync();
            }

            await page.Page.GetByRole(AriaRole.Dialog, new() { Name = "Any pain today?" }).WaitForAsync();
        },
        ["Exercise menu"] = async page =>
        {
            await StartMobilityAsync(page);
            await OpenExerciseMenuAsync(page.Page);
        },
        ["Guide sheet"] = async page =>
        {
            await StartMobilityAsync(page);
            await OpenExerciseMenuAsync(page.Page);
            await Dialog(page.Page).GetByRole(AriaRole.Button).Filter(new() { HasText = "View Guide" }).ClickAsync();
        },
        ["Skip sheet"] = async page =>
        {
            await StartMobilityAsync(page);
            await OpenExerciseMenuAsync(page.Page);
            await Dialog(page.Page).GetByRole(AriaRole.Button).Filter(new() { HasText = "Skip Exercise" }).ClickAsync();
        },
        ["C-14 Swap picker"] = async page =>
        {
            await StartMobilityAsync(page);
            await OpenExerciseMenuAsync(page.Page);
            await Dialog(page.Page).GetByRole(AriaRole.Button).Filter(new() { HasText = "Swap Exercise" }).ClickAsync();
        },
        ["Workout menu"] = async page =>
        {
            await StartMobilityAsync(page);
            await page.Page.GetByRole(AriaRole.Button, new() { Name = "Workout menu", Exact = true }).ClickAsync();
        },
        ["C-15 Date range"] = async page =>
        {
            await page.GotoAsync("/progress");
            await MainContent(page.Page).GetByRole(AriaRole.Button, new() { Name = "Date range: " }).ClickAsync();
        },
    };

    [Theory]
    [MemberData(nameof(SheetData))]
    public async Task Open_sheet_has_no_serious_or_critical_axe_violations(string sheet, Theme theme, Mode mode)
    {
        await using var page = await app.OpenAsync("/", Options(theme, mode, withFixture: true));
        await Sheets[sheet](page);
        await Assertions.Expect(page.Page.GetByRole(AriaRole.Dialog).Last).ToBeVisibleAsync();

        await AssertNoBlockingViolationsAsync(page.Page);
    }

    // Reduced motion, so a sheet or screen is scanned at rest rather than mid-transition.
    private static AppPageOptions Options(Theme theme, Mode mode, bool withFixture) =>
        new() { Theme = theme, Mode = mode, WithFixture = withFixture, ReducedMotion = true };

    private static ILocator MainContent(IPage page) => page.Locator("#main-content");

    private static ILocator Dialog(IPage page) => page.GetByRole(AriaRole.Dialog).Last;

    private static async Task StartMobilityAsync(AppPage page)
    {
        await MainContent(page.Page).GetByRole(AriaRole.Button, new() { Name = "Start Mobility flow", Exact = true }).ClickAsync();
        await page.Page.WaitForURLAsync("**/log/**");
        await page.Page.GetByRole(AriaRole.Button, new() { Name = "Workout menu", Exact = true }).WaitForAsync();
    }

    private static Task OpenExerciseMenuAsync(IPage page) =>
        page.GetByRole(AriaRole.Button, new() { Name = "Menu for " }).First.ClickAsync();

    private async Task AssertNoBlockingViolationsAsync(IPage page)
    {
        var findings = await Axe.ScanAsync(page);
        foreach (var finding in findings.Where(f => !f.IsBlocking))
        {
            output.WriteLine($"AXE-BACKLOG {finding}");
        }

        var blocking = findings.Where(f => f.IsBlocking && !AxeSuppressions.Covers(f)).ToList();
        Assert.True(blocking.Count == 0, $"{blocking.Count} serious or critical axe violation(s):\n{string.Join("\n", blocking)}");
    }
}
