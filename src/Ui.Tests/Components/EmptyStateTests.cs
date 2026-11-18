using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Ui.Components;

namespace Ui.Tests.Components;

public sealed class EmptyStateTests : BunitContext
{
    private IRenderedComponent<EmptyState> RenderEmpty(EmptyStateKind kind, Action<ComponentParameterCollectionBuilder<EmptyState>>? more = null) =>
        Render<EmptyState>(p =>
        {
            p.Add(e => e.Kind, kind);
            p.Add(e => e.Title, "No Guide yet");
            p.Add(e => e.Message, "Write one or import some.");
            more?.Invoke(p);
        });

    [Fact]
    public void EmptyState_ShowsTheBoldLineAndTheMutedLine()
    {
        var cut = RenderEmpty(EmptyStateKind.NoPlan);

        Assert.Equal("No Guide yet", cut.Find(".fa-title").TextContent);
        Assert.Equal("Write one or import some.", cut.Find(".fa-muted").TextContent);
    }

    [Theory]
    [InlineData(EmptyStateKind.FirstRun, "illustrations/empty-first-run.svg")]
    [InlineData(EmptyStateKind.NoPlan, "illustrations/empty-no-plan.svg")]
    [InlineData(EmptyStateKind.NoHistory, "illustrations/empty-no-history.svg")]
    [InlineData(EmptyStateKind.Offline, "illustrations/empty-offline.svg")]
    public void EmptyState_ShowsTheIllustrationForItsKind(EmptyStateKind kind, string file)
    {
        var cut = RenderEmpty(kind);

        var art = cut.Find("img");
        Assert.EndsWith(file, art.GetAttribute("src"));
        Assert.Equal("", art.GetAttribute("alt"));
    }

    [Fact]
    public void EmptyStateWithoutAnAction_ShowsNoButton()
    {
        var cut = RenderEmpty(EmptyStateKind.NoHistory);

        Assert.Empty(cut.FindAll("button"));
        Assert.Empty(cut.FindAll("a"));
    }

    [Fact]
    public void EmptyStateWithAnAction_ShowsOnePrimaryButtonThatRunsIt()
    {
        var clicks = 0;
        var cut = RenderEmpty(EmptyStateKind.NoPlan, p =>
        {
            p.Add(e => e.ActionLabel, "Write a Guide");
            p.Add(e => e.OnAction, EventCallback.Factory.Create<MouseEventArgs>(this, () => clicks++));
        });

        var button = cut.Find("button");
        Assert.Equal("Write a Guide", button.TextContent);
        Assert.Contains("fa-btn--primary", button.ClassList);
        button.Click();
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void EmptyStateWithAnActionDestination_ShowsALink()
    {
        var cut = RenderEmpty(EmptyStateKind.FirstRun, p =>
        {
            p.Add(e => e.ActionLabel, "Start");
            p.Add(e => e.ActionHref, "plans");
        });

        Assert.Equal("plans", cut.Find("a").GetAttribute("href"));
    }
}
