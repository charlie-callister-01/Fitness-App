using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Ui;
using Ui.Layouts;

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

    public LayoutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<InAppHistory>();
        Services.AddSingleton<Ui.Saving.SaveFailureNotice>();
    }

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

    [Theory]
    [InlineData(typeof(PlainPage))]
    [InlineData(typeof(FullScreenPage))]
    [InlineData(typeof(PushedPage))]
    public void EveryLayout_OffersTheSkipLinkToAFocusableMainLandmark(Type page)
    {
        var cut = RenderRoute(page);

        Assert.Equal("Skip to content", cut.Find("a.skip-link").TextContent.Trim());
        Assert.Equal("-1", cut.Find("main#main-content").GetAttribute("tabindex"));
    }

    [Fact]
    public void ThePushedBackArrow_GoesBackInBrowserHistory_OnceTheAppHasNavigated()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        Services.GetRequiredService<InAppHistory>();
        var cut = RenderRoute(typeof(PushedPage));
        navigation.NavigateTo("plans");
        var before = navigation.Uri;

        cut.Find("button[aria-label='Back']").Click();

        Assert.Contains(JSInterop.Invocations, i => i.Identifier == "history.back");
        Assert.Equal(before, navigation.Uri);
    }

    [Fact]
    public void ThePushedBackArrow_GoesToTheAppRoot_WhenThereIsNothingInTheAppToGoBackTo()
    {
        var navigation = Services.GetRequiredService<NavigationManager>();
        // The app starts on a pushed page (a deep link): nothing has been navigated to since the history service was created.
        navigation.NavigateTo("plans");
        Services.GetRequiredService<InAppHistory>();
        var cut = RenderRoute(typeof(PushedPage));

        cut.Find("button[aria-label='Back']").Click();

        Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "history.back");
        Assert.Equal(navigation.BaseUri, navigation.Uri);
    }

}
