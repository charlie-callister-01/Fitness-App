using Bunit;
using Ui.Components;

namespace Ui.Tests.Components;

/// <summary>C-19: label left, value right and always shown, state colour on the fill, an accessible name and value.</summary>
public sealed class ProgressBarTests : BunitContext
{
    private IRenderedComponent<ProgressBar> RenderBar(int percent, ProgressBarState? state = null) =>
        Render<ProgressBar>(p =>
        {
            p.Add(c => c.Label, "Squat");
            p.Add(c => c.Value, "75%");
            p.Add(c => c.Percent, percent);
            if (state is { } s) p.Add(c => c.State, s);
        });

    [Fact]
    public void The_label_and_the_value_text_are_always_shown()
    {
        var cut = RenderBar(75);

        Assert.Equal("Squat", cut.Find(".fa-pbar__label").TextContent);
        Assert.Equal("75%", cut.Find(".fa-pbar__value").TextContent);
    }

    [Fact]
    public void The_value_text_is_shown_even_when_the_bar_is_empty()
    {
        var cut = RenderBar(0);

        Assert.Equal("75%", cut.Find(".fa-pbar__value").TextContent);
        Assert.Contains("width: 0%", cut.Find(".fa-pbar__fill").GetAttribute("style"));
    }

    [Fact]
    public void The_bar_has_an_accessible_name_and_value()
    {
        var bar = RenderBar(75).Find("[role=progressbar]");

        Assert.Equal("Squat", bar.GetAttribute("aria-label"));
        Assert.Equal("75", bar.GetAttribute("aria-valuenow"));
        Assert.Equal("75%", bar.GetAttribute("aria-valuetext"));
        Assert.Equal("100", bar.GetAttribute("aria-valuemax"));
    }

    [Theory]
    [InlineData(null, "fa-pbar--active")]
    [InlineData(ProgressBarState.Done, "fa-pbar--done")]
    [InlineData(ProgressBarState.Behind, "fa-pbar--behind")]
    public void The_state_sets_the_colour_class(ProgressBarState? state, string css) =>
        Assert.Contains(css, RenderBar(50, state).Find(".fa-pbar").ClassList);

    [Theory]
    [InlineData(-5, "0")]
    [InlineData(140, "100")]
    public void A_percent_outside_the_range_is_clamped(int percent, string expected)
    {
        var cut = RenderBar(percent);

        Assert.Equal(expected, cut.Find("[role=progressbar]").GetAttribute("aria-valuenow"));
        Assert.Contains($"width: {expected}%", cut.Find(".fa-pbar__fill").GetAttribute("style"));
    }
}
