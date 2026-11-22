using Bunit;
using Core.Model;
using Core.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Ui.Pages;
using Ui.Theming;

namespace Ui.Tests.Pages;

public sealed class MoreTests : BunitContext
{
    private sealed class FixedBuildInfo(string identifier) : IBuildInfo
    {
        public string Identifier => identifier;
    }

    private sealed class RecordingInterop : IThemeInterop
    {
        public List<(Theme, Mode)> Applied { get; } = [];

        public Task ApplyAsync(Theme theme, Mode mode)
        {
            Applied.Add((theme, mode));
            return Task.CompletedTask;
        }
    }

    private readonly RecordingInterop _interop = new();
    private AppData _data = null!;

    private async Task<IRenderedComponent<More>> RenderMoreAsync()
    {
        _data = await AppData.OpenAsync(new InMemoryDataStore());
        Services.AddSingleton<IBuildInfo>(new FixedBuildInfo("build-1234"));
        Services.AddSingleton(_data);
        Services.AddSingleton(new ThemeService(_data, _interop, new Ui.Saving.SaveFailureNotice()));
        return Render<More>();
    }

    [Fact]
    public async Task More_links_to_the_routed_Weight_screen()
    {
        var cut = await RenderMoreAsync();

        Assert.Equal("weight", cut.FindAll("nav a").Single(a => a.TextContent.Trim() == "Weight").GetAttribute("href"));
        Assert.Equal("/weight", ((RouteAttribute)Attribute.GetCustomAttribute(typeof(Weight), typeof(RouteAttribute))!).Template);
    }

    // Export and Import is a real page now (see ExportImportTests), so it is no longer a placeholder.
    [Fact]
    public async Task More_LinksToExportImport_WhichHasItsOwnRoute()
    {
        var cut = await RenderMoreAsync();

        Assert.Equal("backup", cut.FindAll("nav a").Single(a => a.TextContent.Trim() == "Export and Import").GetAttribute("href"));
        Assert.Equal("/backup", ((RouteAttribute)Attribute.GetCustomAttribute(typeof(ExportImport), typeof(RouteAttribute))!).Template);
    }

    // Settings is a real page now (see SettingsTests), so it is no longer in the placeholder list above.
    [Fact]
    public async Task More_LinksToSettings_WhichHasItsOwnRoute()
    {
        var cut = await RenderMoreAsync();

        Assert.Equal("settings", cut.FindAll("nav a").Single(a => a.TextContent.Trim() == "Settings").GetAttribute("href"));
        Assert.Equal("/settings", ((RouteAttribute)Attribute.GetCustomAttribute(typeof(SettingsPage), typeof(RouteAttribute))!).Template);
    }

    // Weekly review is a real page now (see WeeklyReviewTests), so it is no longer in the placeholder list above.
    [Fact]
    public async Task More_LinksToWeeklyReview_WhichHasItsOwnRoute()
    {
        var cut = await RenderMoreAsync();

        Assert.Equal("weekly-review", cut.FindAll("nav a").Single(a => a.TextContent.Trim() == "Weekly review").GetAttribute("href"));
        Assert.Equal("/weekly-review", ((RouteAttribute)Attribute.GetCustomAttribute(typeof(WeeklyReview), typeof(RouteAttribute))!).Template);
    }

    // Plans is a real page now (see PlansTests), so it is no longer in the placeholder list above.
    [Fact]
    public async Task More_LinksToPlans_WhichHasItsOwnRoute()
    {
        var cut = await RenderMoreAsync();

        Assert.Equal("plans", cut.FindAll("nav a").Single(a => a.TextContent.Trim() == "Plans").GetAttribute("href"));
        Assert.Contains(Attribute.GetCustomAttributes(typeof(Plans), typeof(RouteAttribute)), a => ((RouteAttribute)a).Template == "/plans");
    }

    [Fact]
    public async Task More_LinksToTheExerciseLibraryPage()
    {
        var cut = await RenderMoreAsync();

        var link = cut.FindAll("nav a").Single(a => a.TextContent.Trim() == "Exercise library");

        Assert.Equal("exercises", link.GetAttribute("href"));
        var route = ((RouteAttribute)Attribute.GetCustomAttribute(typeof(Exercises), typeof(RouteAttribute))!).Template;
        Assert.Equal("/exercises", route);
    }

    [Fact]
    public async Task More_ShowsTheBuildIdentifier()
    {
        var cut = await RenderMoreAsync();

        Assert.Contains("build-1234", cut.Find(".build-id").TextContent);
    }

    [Fact]
    public async Task ChoosingAThemeInAppearance_UpdatesTheApp()
    {
        var cut = await RenderMoreAsync();

        await cut.InvokeAsync(() => cut.FindAll("button").Single(b => b.TextContent == "Clean").Click());

        Assert.Equal(Theme.Clean, _interop.Applied[^1].Item1);
        Assert.Equal(Theme.Clean, _data.Snapshot.Settings.Theme);
    }

#if DEBUG
    [Fact]
    public async Task TheGalleryIsFoldedInAsACollapsedTemporarySection_WithNoSecondSetOfPickers()
    {
        var cut = await RenderMoreAsync();

        var details = cut.Find("details");
        Assert.Null(details.GetAttribute("open"));
        Assert.Contains("Components (temporary)", details.QuerySelector("summary")!.TextContent);
        Assert.Equal(1, cut.FindAll("button").Count(b => b.TextContent == "Clean"));
        Assert.Contains("On a Break", cut.Markup);
    }
#else
    [Fact]
    public async Task TheGalleryIsNotInAReleaseBuild()
    {
        var cut = await RenderMoreAsync();

        Assert.Empty(cut.FindAll("details"));
        Assert.DoesNotContain("On a Break", cut.Markup);
    }
#endif
}
