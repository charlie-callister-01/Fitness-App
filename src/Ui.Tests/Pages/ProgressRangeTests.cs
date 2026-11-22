using Bunit;
using Core.Model;
using Core.Storage;
using Ui.Pages;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>The Progress screen's date range (design C-15): the charts and their headline use only the points inside the range.</summary>
public sealed class ProgressRangeTests : BunitContext
{
    private static readonly DateOnly Sep14 = new(2026, 9, 14);
    private static readonly DateOnly Nov2 = new(2026, 11, 2);
    private static readonly DateOnly Nov9 = new(2026, 11, 9);

    public ProgressRangeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./_content/Ui/Components/Sheet.razor.js").Mode = JSRuntimeMode.Loose;
    }

    private async Task<IRenderedComponent<Progress>> RenderAsync(params Workout[] workouts)
    {
        var data = await OpenSeededAsync(plan: Plan() with { StartDate = new DateOnly(2026, 1, 5) }, workouts: workouts);
        Wire(this, data);
        return Render<Progress>();
    }

    private static void Pick(IRenderedComponent<Progress> cut, string preset)
    {
        cut.Find(".fa-rangefield").Click();
        cut.FindAll(".fa-rangechoices button").Single(b => b.TextContent.Trim() == preset).Click();
    }

    [Fact]
    public async Task The_range_defaults_to_the_last_12_weeks()
    {
        var cut = await RenderAsync(Finished("w1", Nov2, Set("a", 20, 5)), Finished("w2", Nov9, Set("b", 20, 8)));

        Assert.Contains("Last 12 weeks · 25 Aug – 16 Nov", cut.Find(".fa-rangefield").TextContent);
        Assert.Single(cut.FindAll("svg.fa-chart"));
    }

    [Fact]
    public async Task A_shorter_range_drops_older_points_and_the_headline_uses_only_the_points_left()
    {
        var cut = await RenderAsync(Finished("w0", Sep14, Set("z", 40, 5)), Finished("w1", Nov2, Set("a", 20, 5)), Finished("w2", Nov9, Set("b", 20, 8)));
        Assert.Equal(3, cut.FindAll("svg.fa-chart .fa-chart__point").Count);

        Pick(cut, "Last 4 weeks");

        Assert.Contains("Last 4 weeks", cut.Find(".fa-rangefield").TextContent);
        Assert.Equal(2, cut.FindAll("svg.fa-chart .fa-chart__point").Count);
        Assert.Contains("Best 25.3 · +2.0 since W", System.Text.RegularExpressions.Regex.Replace(cut.Find("svg.fa-chart").TextContent, @"\s+", " "), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_range_with_no_entries_shows_a_plain_message_and_keeps_the_field()
    {
        var cut = await RenderAsync(Finished("w1", Sep14, Set("a", 20, 5)), Finished("w2", Sep14.AddDays(7), Set("b", 20, 8)));

        Pick(cut, "Last 4 weeks");

        Assert.Equal("Nothing logged in this range.", cut.Find("p.fa-muted").TextContent);
        Assert.Empty(cut.FindAll("svg.fa-chart"));
        Assert.NotNull(cut.Find(".fa-rangefield"));
    }

    [Fact]
    public async Task Whole_Plan_brings_back_everything_since_the_Plan_start()
    {
        var cut = await RenderAsync(Finished("w1", new DateOnly(2026, 3, 2), Set("a", 20, 5)), Finished("w2", new DateOnly(2026, 3, 9), Set("b", 20, 8)));
        Assert.Empty(cut.FindAll("svg.fa-chart"));

        Pick(cut, "Whole Plan");

        Assert.Single(cut.FindAll("svg.fa-chart"));
    }

    [Fact]
    public async Task A_range_never_makes_its_first_visible_point_a_PR_and_the_label_names_that_point()
    {
        var cut = await RenderAsync(Finished("w0", Sep14, Set("z", 40, 5)), Finished("w1", Nov2, Set("a", 20, 5)), Finished("w2", Nov9, Set("b", 20, 8)));

        Pick(cut, "Last 4 weeks");

        Assert.Equal(2, cut.FindAll("svg.fa-chart .fa-chart__point").Count);
        Assert.Empty(cut.FindAll("svg.fa-chart .fa-chart__point--pr"));
        Assert.Contains("since W44", System.Text.RegularExpressions.Regex.Replace(cut.Find("svg.fa-chart").TextContent, @"\s+", " "), StringComparison.Ordinal);
    }
}
