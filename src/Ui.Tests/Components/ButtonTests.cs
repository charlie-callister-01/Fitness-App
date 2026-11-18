using Bunit;
using Microsoft.AspNetCore.Components;
using Ui.Components;

namespace Ui.Tests.Components;

public sealed class ButtonTests : BunitContext
{
    private IRenderedComponent<Button> RenderButton(ButtonVariant variant, Action<ComponentParameterCollectionBuilder<Button>>? more = null) =>
        Render<Button>(p =>
        {
            p.Add(b => b.Variant, variant);
            p.AddChildContent("Confirm Set");
            more?.Invoke(p);
        });

    [Theory]
    [InlineData(ButtonVariant.Primary, "fa-btn--primary")]
    [InlineData(ButtonVariant.Secondary, "fa-btn--secondary")]
    [InlineData(ButtonVariant.Stepper, "fa-btn--stepper")]
    public void Button_ShowsItsLabelWithTheDesignsClassesForTheVariant(ButtonVariant variant, string variantClass)
    {
        var cut = RenderButton(variant);

        var button = cut.Find("button");
        Assert.Equal("Confirm Set", button.TextContent);
        Assert.Contains("fa-btn", button.ClassList);
        Assert.Contains(variantClass, button.ClassList);
    }

    [Fact]
    public void Button_RunsItsHandlerWhenClicked()
    {
        var clicks = 0;
        var cut = RenderButton(ButtonVariant.Primary, p => p.Add(b => b.OnClick, EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => clicks++)));

        cut.Find("button").Click();

        Assert.Equal(1, clicks);
    }

    [Fact]
    public void DisabledButton_IsDisabledAndDoesNotRunItsHandler()
    {
        var clicks = 0;
        var cut = RenderButton(ButtonVariant.Primary, p =>
        {
            p.Add(b => b.Disabled, true);
            p.Add(b => b.OnClick, EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => clicks++));
        });

        var button = cut.Find("button");
        Assert.True(button.HasAttribute("disabled"));
        button.Click();
        Assert.Equal(0, clicks);
    }

    [Fact]
    public void ButtonGivenADestination_RendersALinkWithTheSameClasses()
    {
        var cut = RenderButton(ButtonVariant.Secondary, p => p.Add(b => b.Href, "plans"));

        var link = cut.Find("a");
        Assert.Equal("plans", link.GetAttribute("href"));
        Assert.Equal("Confirm Set", link.TextContent);
        Assert.Contains("fa-btn", link.ClassList);
        Assert.Contains("fa-btn--secondary", link.ClassList);
        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void IconButton_ShowsTheIconAndExposesItsAccessibleName()
    {
        var cut = Render<Button>(p =>
        {
            p.Add(b => b.Variant, ButtonVariant.Icon);
            p.Add(b => b.Glyph, IconName.Back);
            p.Add(b => b.AccessibleName, "Back");
        });

        var button = cut.Find("button");
        Assert.Equal("Back", button.GetAttribute("aria-label"));
        Assert.Contains("fa-btn--icon", button.ClassList);
        Assert.NotNull(button.QuerySelector("svg"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IconButtonWithoutAnAccessibleName_RefusesToRender(string? name)
    {
        Assert.Throws<InvalidOperationException>(() => Render<Button>(p =>
        {
            p.Add(b => b.Variant, ButtonVariant.Icon);
            p.Add(b => b.Glyph, IconName.Back);
            p.Add(b => b.AccessibleName, name);
        }));
    }
}
