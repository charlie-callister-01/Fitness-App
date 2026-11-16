using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Ui;
using Ui.Pages;

namespace Ui.Tests.Pages;

public sealed class HomeTests : BunitContext
{
    private sealed class FixedBuildInfo(string identifier) : IBuildInfo
    {
        public string Identifier => identifier;
    }

    [Fact]
    public void Home_ShowsAppNameAndInjectedBuildIdentifier()
    {
        Services.AddSingleton<IBuildInfo>(new FixedBuildInfo("build-1234"));

        var cut = Render<Home>();

        Assert.Contains("Fitness App", cut.Find("h1").TextContent);
        Assert.Contains("build-1234", cut.Markup);
    }
}
