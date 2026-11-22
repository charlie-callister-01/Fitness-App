using Bunit;
using Core.Rules.Home;
using Ui.Components;

namespace Ui.Tests.Components;

public sealed class OrbRowTests : BunitContext
{
    private static HomeOrbs Orbs(bool onBreak = false, bool phase = true, bool race = true) => new(
        new StreakOrb(4, 1, 2, onBreak),
        phase ? new PhaseOrb(5, 8) : null,
        race ? new RaceOrb(120, 0.4) : null);

    private static string[] Names(IRenderedComponent<OrbRow> cut) =>
        [.. cut.FindAll("svg[role='img']").Select(s => s.GetAttribute("aria-label")!)];

    [Fact]
    public void All_three_orbs_render_with_names()
    {
        var cut = Render<OrbRow>(p => p.Add(o => o.Orbs, Orbs()));

        Assert.Equal(["Streak: 4 weeks", "Phase week 5 of 8", "120 days to race"], Names(cut));
        Assert.Contains("Streak (weeks)", cut.Markup);
        Assert.Contains("week 5 of 8", cut.Markup);
        Assert.Contains("Days to race", cut.Markup);
    }

    [Fact]
    public void Phase_and_Race_are_omitted_when_null()
    {
        var cut = Render<OrbRow>(p => p.Add(o => o.Orbs, Orbs(phase: false, race: false)));

        Assert.Equal(["Streak: 4 weeks"], Names(cut));
    }

    [Fact]
    public void A_Break_greys_the_Streak_orb_and_keeps_its_number()
    {
        var cut = Render<OrbRow>(p => p.Add(o => o.Orbs, Orbs(onBreak: true, phase: false, race: false)));

        Assert.Equal(["Streak: 4 weeks, on a Break"], Names(cut));
        Assert.Contains("On a Break", cut.Markup);
        Assert.Equal("4", cut.Find(".fa-orb__value").TextContent.Trim());
        Assert.NotNull(cut.Find(".fa-orb__arc--greyed"));
    }

    [Fact]
    public void Each_orb_is_a_button_that_raises_OnOpenProgress()
    {
        var count = 0;
        var cut = Render<OrbRow>(p => p.Add(o => o.Orbs, Orbs()).Add(o => o.OnOpenProgress, () => count++));

        var buttons = cut.FindAll("button.fa-orbrow__orb");
        Assert.Equal(3, buttons.Count);
        foreach (var button in buttons)
        {
            button.Click();
        }

        cut.WaitForAssertion(() => Assert.Equal(3, count));
    }
}
