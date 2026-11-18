using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Ui.Layouts;
using Ui.Pages;

namespace Ui.Tests.Shell;

public sealed class LayoutTests : BunitContext
{
    private sealed class PlainPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "page body");
    }

    [Layout(typeof(FullScreenLayout))]
    private sealed class FullScreenPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "page body");
    }

    [Layout(typeof(PushedPageLayout))]
    private sealed class PushedPage : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "page body");
    }

    public LayoutTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    // Mirrors App.razor: the router's RouteView with the main layout as the default.
    private IRenderedComponent<RouteView> RenderRoute(Type page) =>
        Render<RouteView>(p => p
            .Add(r => r.RouteData, new RouteData(page, new Dictionary<string, object?>()))
            .Add(r => r.DefaultLayout, typeof(MainLayout)));

    [Fact]
    public void ADefaultPage_GetsTheMainLayout_WithTabBarSkipLinkAndMainLandmark()
    {
        var cut = RenderRoute(typeof(PlainPage));

        Assert.NotNull(cut.Find("nav[aria-label='Main']"));
        Assert.Contains("page body", cut.Find("main").TextContent);
        Assert.Equal("Skip to content", cut.Find("a.skip-link").TextContent.Trim());
        Assert.Equal("main-content", cut.Find("main").Id);
    }

    [Fact]
    public void SkipLink_MovesFocusToTheMainLandmarkWithoutNavigating()
    {
        var cut = RenderRoute(typeof(PlainPage));
        var before = Services.GetRequiredService<NavigationManager>().Uri;

        cut.Find("a.skip-link").Click();

        Assert.Equal(before, Services.GetRequiredService<NavigationManager>().Uri);
        Assert.Contains(JSInterop.Invocations, i => i.Identifier.EndsWith("focus", StringComparison.Ordinal));
    }

    [Fact]
    public void AFullScreenPage_HasNoTabBar()
    {
        var cut = RenderRoute(typeof(FullScreenPage));

        Assert.Empty(cut.FindAll("nav"));
        Assert.Contains("page body", cut.Find("main").TextContent);
    }

    [Fact]
    public void APushedPage_ShowsABackArrowAndNoTabBar()
    {
        var cut = RenderRoute(typeof(PushedPage));

        Assert.Empty(cut.FindAll("nav"));
        Assert.NotNull(cut.Find("button[aria-label='Back']"));
        Assert.Contains("page body", cut.Find("main").TextContent);
    }

    [Fact]
    public void ThePushedBackArrow_GoesBackInBrowserHistory()
    {
        var cut = RenderRoute(typeof(PushedPage));

        cut.Find("button[aria-label='Back']").Click();

        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "history.back");
    }

    [Fact]
    public void TheTemporaryDevPages_UseThePushedLayout()
    {
        Assert.Equal(typeof(PushedPageLayout), LayoutOf<ComponentGallery>());
        Assert.Equal(typeof(PushedPageLayout), LayoutOf<AppearanceStub>());
    }

    private static Type? LayoutOf<T>() =>
        ((LayoutAttribute?)Attribute.GetCustomAttribute(typeof(T), typeof(LayoutAttribute)))?.LayoutType;
}
