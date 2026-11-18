using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Ui.Components;

namespace Ui.Tests.Shell;

public sealed class TabBarTests : BunitContext
{
    private IRenderedComponent<TabBar> RenderAt(string relativePath)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(relativePath);
        return Render<TabBar>();
    }

    private static string CurrentLabel(IRenderedComponent<TabBar> cut) =>
        Assert.Single(cut.FindAll("a[aria-current='page']")).TextContent.Trim();

    [Fact]
    public void TabBar_ShowsTheFiveDestinationsInOrderInALabelledNavigation()
    {
        var cut = RenderAt("");

        Assert.Equal("Main", cut.Find("nav").GetAttribute("aria-label"));
        Assert.Equal(["Home", "Week", "Progress", "Trophies", "More"],
            cut.FindAll("nav a").Select(a => a.TextContent.Trim()));
        Assert.All(cut.FindAll("nav a"), a => Assert.NotNull(a.QuerySelector("svg")));
    }

    [Fact]
    public void TabBar_LinksAreRelativeSoTheyWorkUnderASubPath()
    {
        var cut = RenderAt("");

        Assert.All(cut.FindAll("nav a"), a => Assert.False(a.GetAttribute("href")!.StartsWith('/')));
    }

    [Theory]
    [InlineData("", "Home")]
    [InlineData("week", "Week")]
    [InlineData("progress", "Progress")]
    [InlineData("progress/benchmarks", "Progress")]
    [InlineData("trophies", "Trophies")]
    [InlineData("more", "More")]
    [InlineData("more/library/guide-1", "More")]
    [InlineData("gallery", "More")]
    [InlineData("appearance-stub", "More")]
    public void TabBar_MarksTheTabOwningTheCurrentRoute(string path, string expected)
    {
        Assert.Equal(expected, CurrentLabel(RenderAt(path)));
    }

    [Fact]
    public void TabBar_MarksNoTabOnAnUnknownRoute()
    {
        Assert.Empty(RenderAt("nowhere").FindAll("a[aria-current='page']"));
    }

    [Fact]
    public void TabBar_FollowsNavigationWithoutAReload()
    {
        var cut = RenderAt("");

        cut.InvokeAsync(() => Services.GetRequiredService<NavigationManager>().NavigateTo("week"));

        Assert.Equal("Week", CurrentLabel(cut));
    }

    [Fact]
    public void TabBar_DoesNotConfuseAShortPrefixWithAnotherRoute()
    {
        Assert.Equal("Home", CurrentLabel(RenderAt("")));
        Assert.Empty(RenderAt("weekend").FindAll("a[aria-current='page']"));
    }
}
