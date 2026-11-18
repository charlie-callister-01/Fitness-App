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
        Services.AddSingleton(new ThemeService(_data, _interop));
        return Render<More>();
    }

    [Theory]
    [InlineData("Plans", "plans", typeof(Plans))]
    [InlineData("Exercise library", "exercises", typeof(Exercises))]
    [InlineData("Weight", "weight", typeof(Weight))]
    [InlineData("Weekly review", "weekly-review", typeof(WeeklyReview))]
    [InlineData("Export and Import", "backup", typeof(ExportImport))]
    [InlineData("Settings", "settings", typeof(SettingsPage))]
    public async Task More_LinksToEachSecondaryScreen_WhichIsARoutedPlaceholder(string label, string href, Type page)
    {
        var cut = await RenderMoreAsync();

        var link = cut.FindAll("nav a").Single(a => a.TextContent.Trim() == label);
        Assert.Equal(href, link.GetAttribute("href"));

        var route = ((RouteAttribute)Attribute.GetCustomAttribute(page, typeof(RouteAttribute))!).Template;
        Assert.Equal("/" + href, route);

        var placeholder = Render(b => { b.OpenComponent(0, page); b.CloseComponent(); });
        Assert.NotEmpty(placeholder.Find("h1").TextContent);
        Assert.NotNull(placeholder.Find(".empty"));
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
