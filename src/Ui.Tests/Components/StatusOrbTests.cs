using System.Globalization;
using Bunit;
using Ui.Components;

namespace Ui.Tests.Components;

public sealed class StatusOrbTests : BunitContext
{
    private IRenderedComponent<StatusOrb> RenderOrb(double fraction, Action<ComponentParameterCollectionBuilder<StatusOrb>>? more = null) =>
        Render<StatusOrb>(p =>
        {
            p.Add(o => o.Value, "4");
            p.Add(o => o.Fraction, fraction);
            p.Add(o => o.Caption, "weeks");
            p.Add(o => o.AccessibleName, "Streak, 4 weeks, 2 of 3 this week");
            more?.Invoke(p);
        });

    /// <summary>How much of the ring the arc covers, read from its dash pattern (0 to 1).</summary>
    private static double ArcCoverage(IRenderedComponent<StatusOrb> cut)
    {
        var arc = cut.FindAll("circle.fa-orb__arc").SingleOrDefault();
        if (arc is null)
        {
            return 0;
        }
        var parts = arc.GetAttribute("stroke-dasharray")!.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
        return parts[0] / (parts[0] + parts[1]);
    }

    [Fact]
    public void StatusOrb_ShowsValueAndCaptionAndExposesTheAccessibleLabel()
    {
        var cut = RenderOrb(0.5);

        var svg = cut.Find("svg");
        Assert.Equal("img", svg.GetAttribute("role"));
        Assert.Equal("Streak, 4 weeks, 2 of 3 this week", svg.GetAttribute("aria-label"));
        Assert.Equal("4", cut.Find("text").TextContent);
        Assert.Contains("weeks", cut.Markup);
    }

    [Theory]
    [InlineData(0.25, 0.25)]
    [InlineData(1.0, 1.0)]
    [InlineData(1.7, 1.0)]
    [InlineData(-0.4, 0.0)]
    public void StatusOrb_ClampsTheRingFractionToZeroToOne(double fraction, double expected)
    {
        var cut = RenderOrb(fraction);

        Assert.Equal(expected, ArcCoverage(cut), 3);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void StatusOrb_RendersTheEmptyRingForANonFiniteFraction(double fraction)
    {
        var cut = RenderOrb(fraction);

        Assert.Equal(0, ArcCoverage(cut));
        Assert.Equal("4", cut.Find("text").TextContent);
    }

    [Fact]
    public void StatusOrbOnABreak_KeepsItsNumberAndSaysOnABreak()
    {
        var cut = RenderOrb(0.5, p => p.Add(o => o.Greyed, true));

        Assert.Equal("4", cut.Find("text").TextContent);
        Assert.Contains("On a Break", cut.Markup);
        Assert.DoesNotContain("weeks", cut.Markup.Replace("Streak, 4 weeks, 2 of 3 this week", ""));
        Assert.Contains("fa-orb__arc--greyed", cut.Find("circle.fa-orb__arc").ClassList);
    }

    [Theory]
    [InlineData(OrbTone.Green, "fa-orb__fill--green")]
    [InlineData(OrbTone.Red, "fa-orb__fill--red")]
    [InlineData(OrbTone.Ochre, "fa-orb__fill--ochre")]
    public void StatusOrb_ColoursItsArcWithTheDesignsToneClass(OrbTone tone, string expectedClass)
    {
        var cut = RenderOrb(0.5, p => p.Add(o => o.Tone, tone));

        Assert.Contains(expectedClass, cut.Find("circle.fa-orb__arc").ClassList);
    }
}
