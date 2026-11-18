using Bunit;
using Ui.Components;

namespace Ui.Tests.Components;

public sealed class RpeSelectorTests : BunitContext
{
    [Fact]
    public void Marks_only_the_selected_value_as_pressed()
    {
        var cut = Render<RpeSelector>(p => p.Add(c => c.Value, 9));

        var pressed = cut.FindAll("button").Where(b => b.GetAttribute("aria-pressed") == "true").ToList();
        Assert.Equal("9", Assert.Single(pressed).TextContent);
    }

    [Fact]
    public void Tapping_another_value_reports_it_and_tapping_the_selected_one_reports_null()
    {
        var reported = new List<int?>();
        var cut = Render<RpeSelector>(p => p
            .Add(c => c.Value, 8)
            .Add(c => c.ValueChanged, (int? v) => reported.Add(v)));

        cut.Find("button[aria-label='RPE 10']").Click();
        cut.Find("button[aria-label='RPE 8']").Click();

        Assert.Equal([10, (int?)null], reported);
    }

    [Fact]
    public void The_toggles_form_a_labelled_group()
    {
        var cut = Render<RpeSelector>();

        var group = cut.Find("[role='group']");
        Assert.Equal("Effort (RPE)", cut.Find(".fa-label").TextContent);
        Assert.Equal(cut.Find(".fa-label").Id, group.GetAttribute("aria-labelledby"));
    }
}
