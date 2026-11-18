using Bunit;
using Ui.Components;
using Ui.Pages;

namespace Ui.Tests.Pages;

public sealed class ComponentGalleryTests : BunitContext
{
    [Fact]
    public void Gallery_ShowsEveryButtonVariantEveryEmptyStateAndEveryIcon()
    {
        var cut = Render<ComponentGallery>();

        Assert.NotEmpty(cut.FindAll(".fa-btn--primary"));
        Assert.NotEmpty(cut.FindAll(".fa-btn--secondary"));
        Assert.NotEmpty(cut.FindAll(".fa-btn--stepper"));
        Assert.NotEmpty(cut.FindAll(".fa-btn--icon"));
        Assert.Equal(4, cut.FindAll(".fa-well img").Count);
        Assert.Equal(Enum.GetValues<IconName>().Length, cut.FindAll(".icons svg").Count);
        Assert.Contains("On a Break", cut.Markup);
    }
}
