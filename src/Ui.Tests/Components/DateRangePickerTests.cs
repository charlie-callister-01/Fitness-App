using Bunit;
using Core.Exchange;
using Core.Rules.Insight;
using Ui.Components;

namespace Ui.Tests.Components;

public sealed class DateRangePickerTests : BunitContext
{
    private static readonly DateOnly Today = new(2026, 11, 16);
    private static readonly DateOnly PlanStart = new(2026, 1, 5);
    private RangeChoice? _changed;

    public DateRangePickerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./_content/Ui/Components/Sheet.razor.js").Mode = JSRuntimeMode.Loose;
    }

    private IRenderedComponent<DateRangePicker> Render12() =>
        Render<DateRangePicker>(p => p
            .Add(c => c.Value, new RangeChoice(RangePreset.Last12Weeks, RangeResolver.Resolve(RangePreset.Last12Weeks, Today, PlanStart).Range!))
            .Add(c => c.Today, Today)
            .Add(c => c.PlanStart, PlanStart)
            .Add(c => c.OnChanged, (RangeChoice c) => _changed = c));

    [Fact]
    public void The_field_shows_the_range_in_words_and_has_an_accessible_name()
    {
        var field = Render12().Find(".fa-rangefield");

        Assert.Contains("Last 12 weeks · 25 Aug – 16 Nov", field.TextContent);
        Assert.Contains("Date range", field.GetAttribute("aria-label"));
    }

    [Fact]
    public void The_sheet_is_closed_until_the_field_is_tapped_and_then_offers_the_presets()
    {
        var cut = Render12();
        Assert.Empty(cut.FindAll(".fa-rangechoices"));

        cut.Find(".fa-rangefield").Click();

        var labels = cut.FindAll(".fa-rangechoices button").Select(b => b.TextContent.Trim());
        Assert.Equal(["Last 4 weeks", "Last 12 weeks", "Whole Plan", "Custom"], labels);
    }

    [Fact]
    public void Choosing_a_preset_applies_it_and_closes_the_sheet()
    {
        var cut = Render12();
        cut.Find(".fa-rangefield").Click();

        cut.FindAll(".fa-rangechoices button").Single(b => b.TextContent.Trim() == "Last 4 weeks").Click();

        Assert.Equal(RangePreset.Last4Weeks, _changed!.Preset);
        Assert.Equal(new DateRange(new DateOnly(2026, 10, 20), Today), _changed.Range);
        Assert.Empty(cut.FindAll(".fa-rangechoices"));
    }

    [Fact]
    public void Custom_shows_two_native_date_inputs_and_applies_the_dates()
    {
        var cut = Render12();
        cut.Find(".fa-rangefield").Click();
        cut.FindAll(".fa-rangechoices button").Single(b => b.TextContent.Trim() == "Custom").Click();

        Assert.Equal(2, cut.FindAll("input[type=date]").Count);
        cut.Find("input[aria-label='Start date']").Change("2026-10-01");
        cut.Find("input[aria-label='End date']").Change("2026-10-31");
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Apply").Click();

        Assert.Equal(new RangeChoice(RangePreset.Custom, new DateRange(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31))), _changed);
    }

    [Fact]
    public void A_start_after_the_end_shows_an_inline_message_and_does_not_apply()
    {
        var cut = Render12();
        cut.Find(".fa-rangefield").Click();
        cut.FindAll(".fa-rangechoices button").Single(b => b.TextContent.Trim() == "Custom").Click();
        cut.Find("input[aria-label='Start date']").Change("2026-11-01");
        cut.Find("input[aria-label='End date']").Change("2026-10-01");

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Apply").Click();

        Assert.Equal(RangeResolver.StartAfterEnd, cut.Find(".fa-rangeerror").TextContent.Trim());
        Assert.Equal("alert", cut.Find(".fa-rangeerror").GetAttribute("role"));
        Assert.Null(_changed);
    }
}
