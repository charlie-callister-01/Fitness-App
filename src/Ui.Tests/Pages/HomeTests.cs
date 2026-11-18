using Bunit;
using Ui.Pages;

namespace Ui.Tests.Pages;

public sealed class HomeTests : BunitContext
{
    [Fact]
    public void Home_ShowsTheAppNameWithNoBuildIdentifierOrTemporaryLinks()
    {
        var cut = Render<Home>();

        Assert.Contains("Fitness App", cut.Find("h1").TextContent);
        Assert.Empty(cut.FindAll(".build-id"));
        Assert.Empty(cut.FindAll("a[href='gallery']"));
        Assert.Empty(cut.FindAll("a[href='appearance-stub']"));
    }
}
