using Bunit;
using Core.Rules.Home;
using Core.Rules.Nudges;
using Ui.Components;

namespace Ui.Tests.Components;

public sealed class NudgeCardTests : BunitContext
{
    private static HomeNudge Item(NudgeAction action) =>
        new(new Nudge(NudgeKind.MissedSession, "You missed Lower."), "2026-11-16|MissedSession|lower", action);

    [Theory]
    [InlineData(NudgeAction.MakeUp, "Make up")]
    [InlineData(NudgeAction.DifferentSession, "Pick a Session")]
    [InlineData(NudgeAction.BackUp, "Back up")]
    public void Shows_the_message_the_action_and_Dismiss(NudgeAction action, string label)
    {
        var cut = Render<NudgeCard>(p => p.Add(c => c.Item, Item(action)));

        Assert.Contains("You missed Lower.", cut.Markup);
        Assert.Equal("status", cut.Find(".fa-nudge").GetAttribute("role"));
        Assert.Equal([label, "Dismiss"], cut.FindAll("button").Select(b => b.TextContent.Trim()));
        Assert.Equal("Dismiss: You missed Lower.", cut.Find(".fa-nudge__dismiss").GetAttribute("aria-label"));
    }

    [Fact]
    public void None_has_no_action_button()
    {
        var cut = Render<NudgeCard>(p => p.Add(c => c.Item, Item(NudgeAction.None)));

        Assert.Equal(["Dismiss"], cut.FindAll("button").Select(b => b.TextContent.Trim()));
    }

    [Fact]
    public void Action_and_Dismiss_raise_their_callbacks_with_the_item()
    {
        var item = Item(NudgeAction.MakeUp);
        HomeNudge? acted = null;
        HomeNudge? dismissed = null;
        var cut = Render<NudgeCard>(p => p.Add(c => c.Item, item)
            .Add(c => c.OnAction, i => acted = i)
            .Add(c => c.OnDismiss, i => dismissed = i));

        cut.FindAll("button")[0].Click();
        cut.FindAll("button")[1].Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(item, acted);
            Assert.Equal(item, dismissed);
        });
    }
}
