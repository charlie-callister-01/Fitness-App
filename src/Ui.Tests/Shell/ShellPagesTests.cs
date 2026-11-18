using Bunit;
using Microsoft.AspNetCore.Components;
using Ui.Pages;

namespace Ui.Tests.Shell;

public sealed class ShellPagesTests : BunitContext
{
    [Theory]
    [InlineData(typeof(Home), "/")]
    [InlineData(typeof(Week), "/week")]
    [InlineData(typeof(Progress), "/progress")]
    [InlineData(typeof(Trophies), "/trophies")]
    public void EachDestinationHasARouteAndAPlaceholderWithAHeadingAndEmptyState(Type page, string route)
    {
        var template = ((RouteAttribute)Attribute.GetCustomAttribute(page, typeof(RouteAttribute))!).Template;
        Assert.Equal(route, template);

        var cut = Render(builder =>
        {
            builder.OpenComponent(0, page);
            builder.CloseComponent();
        });

        Assert.NotEmpty(cut.Find("h1").TextContent);
        Assert.NotNull(cut.Find(".empty"));
    }

    [Fact]
    public void NotFound_IsFriendlyAndOffersAWayHome()
    {
        var cut = Render<NotFound>();

        Assert.NotNull(cut.Find(".empty"));
        var home = cut.Find(".empty a");
        Assert.Equal("Go to Home", home.TextContent.Trim());
        Assert.False(home.GetAttribute("href")!.StartsWith('/'));
    }
}
