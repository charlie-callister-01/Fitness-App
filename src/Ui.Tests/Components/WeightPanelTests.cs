using Bunit;
using Core.Model;
using Core.Rules.Units;
using Ui.Components;
using PanelModel = Core.Rules.Home.WeightPanel;

namespace Ui.Tests.Components;

public sealed class WeightPanelTests : BunitContext
{
    private static readonly WeighIn Latest = new("w", new DateOnly(2026, 11, 15), 86.3);

    private IRenderedComponent<Ui.Components.WeightPanel> Show(PanelModel panel, Action<ComponentParameterCollectionBuilder<Ui.Components.WeightPanel>>? more = null) =>
        Render<Ui.Components.WeightPanel>(p =>
        {
            p.Add(c => c.Panel, panel).Add(c => c.Converter, new UnitConverter(Settings.Default)).Add(c => c.Today, new DateOnly(2026, 11, 18));
            more?.Invoke(p);
        });

    [Fact]
    public void Shows_the_latest_with_its_date_the_average_and_a_positive_change()
    {
        var cut = Show(new PanelModel(Latest, 86.0, 0.5));

        Assert.Contains("86.3 kg · Sun 15 Nov", cut.Markup);
        Assert.Contains("This week avg 86.0 kg", cut.Markup);
        Assert.Contains("+0.5 kg", cut.Markup);
    }

    [Fact]
    public void A_negative_change_has_a_minus_sign()
    {
        var cut = Show(new PanelModel(Latest, 86.0, -0.5));

        Assert.Contains("-0.5 kg", cut.Markup);
    }

    [Fact]
    public void A_zero_change_reads_no_change()
    {
        var cut = Show(new PanelModel(Latest, 86.0, 0.0));

        Assert.Contains("no change", cut.Markup);
    }

    [Fact]
    public void A_null_change_and_average_are_omitted()
    {
        var cut = Show(new PanelModel(Latest, null, null));

        Assert.DoesNotContain("This week avg", cut.Markup);
        Assert.Empty(cut.FindAll(".fa-weightpanel__change"));
    }

    [Fact]
    public void No_Weigh_ins_shows_the_empty_state_and_a_Log_weight_button()
    {
        var raised = false;
        var cut = Show(new PanelModel(null, null, null), p => p.Add(c => c.OnLogWeight, () => raised = true));

        Assert.Contains("No Weigh-ins yet.", cut.Markup);
        cut.Find("button").Click();
        Assert.Equal("Log weight", cut.Find("button").TextContent.Trim());
        cut.WaitForAssertion(() => Assert.True(raised));
    }

    [Fact]
    public void Pounds_are_shown_when_the_display_unit_is_lb()
    {
        var cut = Render<Ui.Components.WeightPanel>(p => p.Add(c => c.Panel, new PanelModel(new WeighIn("w", new DateOnly(2026, 11, 15), 100), null, null))
            .Add(c => c.Converter, new UnitConverter(Settings.Default with { DisplayWeightUnit = WeightUnit.Lb })));

        Assert.Contains("220.5 lb", cut.Markup);
    }
}
