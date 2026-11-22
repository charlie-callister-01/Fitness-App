using AngleSharp.Dom;
using Bunit;
using Core.Model;
using Core.Storage;
using Ui.Pages;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>The Progress screen (design S-07): tabs, group chips, a LineChart per Exercise logged twice or more, one-entry rows.</summary>
public sealed class ProgressTests : BunitContext
{
    private static readonly DateOnly Nov2 = new(2026, 11, 2);
    private static readonly DateOnly Nov9 = new(2026, 11, 9);

    private static Workout Plank(string id, DateOnly date, int seconds) =>
        new(id, date, "lower", false, Now.AddDays(-7), Now.AddDays(-7).AddHours(1), [],
            [new WorkoutEntry("plank", EntryStatus.Done, null, null, null, [new WorkoutSet(id + "s", null, null, null, seconds, null, false)])], null);


    private async Task<IRenderedComponent<Progress>> RenderAsync(params Workout[] workouts)
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: workouts);
        Wire(this, data);
        return Render<Progress>();
    }

    private static IElement Chip(IRenderedComponent<Progress> cut, string label) =>
        cut.FindAll(".fa-progress__chips button").Single(b => b.TextContent.Trim() == label);

    [Fact]
    public async Task With_no_history_it_shows_an_empty_state_and_no_charts()
    {
        var cut = await RenderAsync();

        Assert.Contains("Progress", cut.Find("h1").TextContent);
        Assert.NotNull(cut.Find(".empty"));
        Assert.Empty(cut.FindAll("svg.fa-chart"));
    }

    [Fact]
    public async Task An_Exercise_logged_twice_gets_a_chart_with_its_best_point_labelled_and_the_PR_drawn_larger()
    {
        var cut = await RenderAsync(Finished("w1", Nov2, Set("a", 20, 5)), Finished("w2", Nov9, Set("b", 20, 8)));

        var chart = cut.Find("svg.fa-chart");
        Assert.Contains("Goblet squat", cut.Find(".fa-chart-card").TextContent);
        Assert.Contains("Best 25.3 · +2.0 since first entry", chart.TextContent);
        Assert.Equal(2, cut.FindAll("svg.fa-chart .fa-chart__point").Count);
        Assert.Single(cut.FindAll("svg.fa-chart .fa-chart__point--pr"));
        Assert.Equal("6", cut.Find(".fa-chart__point--pr circle.fa-chart__dot").GetAttribute("r"));
        Assert.Equal("4", cut.Find(".fa-chart__point:not(.fa-chart__point--pr) circle.fa-chart__dot").GetAttribute("r"));
    }

    [Fact]
    public async Task Tapping_a_point_shows_its_date_and_value()
    {
        var cut = await RenderAsync(Finished("w1", Nov2, Set("a", 20, 5)), Finished("w2", Nov9, Set("b", 20, 8)));

        cut.FindAll(".fa-chart__point")[1].Click();

        var tip = cut.Find(".fa-chart__tip");
        Assert.Contains("Mon 9 Nov", tip.TextContent);
        Assert.Contains("25.3 kg", tip.TextContent);
    }

    [Fact]
    public async Task The_Table_toggle_swaps_the_chart_for_the_same_data_as_a_table()
    {
        var cut = await RenderAsync(Finished("w1", Nov2, Set("a", 20, 5)), Finished("w2", Nov9, Set("b", 20, 8)));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Table").Click();

        Assert.Empty(cut.FindAll("svg.fa-chart"));
        var rows = cut.FindAll(".fa-chart-card table tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.Contains("Mon 2 Nov", rows[0].TextContent);
        Assert.Contains("23.3", rows[0].TextContent);
        Assert.Contains("PR", rows[1].TextContent);

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Chart").Click();
        Assert.NotNull(cut.Find("svg.fa-chart"));
    }

    [Fact]
    public async Task An_Exercise_with_one_entry_is_listed_as_needing_two()
    {
        var cut = await RenderAsync(Finished("w1", Nov2, Set("a", 20, 5)));

        Assert.Empty(cut.FindAll("svg.fa-chart"));
        var row = cut.Find(".fa-progress__single");
        Assert.Contains("Goblet squat", row.TextContent);
        Assert.Contains("1 entry · needs 2 for a chart", row.TextContent);
    }

    [Fact]
    public async Task Group_chips_filter_the_charts()
    {
        var cut = await RenderAsync(
            Finished("w1", Nov2, Set("a", 20, 5)), Finished("w2", Nov9, Set("b", 20, 8)),
            Plank("p1", Nov2, 30), Plank("p2", Nov9, 40));

        Assert.Equal(2, cut.FindAll("svg.fa-chart").Count);

        Chip(cut, "Bodyweight").Click();
        var cards = cut.FindAll(".fa-chart-card");
        Assert.Single(cards);
        Assert.Contains("Plank", cards[0].TextContent);
        Assert.Equal("true", Chip(cut, "Bodyweight").GetAttribute("aria-pressed"));

        Chip(cut, "Strength").Click();
        Assert.Contains("Goblet squat", cut.Find(".fa-chart-card").TextContent);

        Chip(cut, "All").Click();
        Assert.Equal(2, cut.FindAll(".fa-chart-card").Count);
    }

    [Fact]
    public async Task A_chip_with_nothing_in_it_says_so()
    {
        var cut = await RenderAsync(Finished("w1", Nov2, Set("a", 20, 5)), Finished("w2", Nov9, Set("b", 20, 8)));

        Chip(cut, "Carries").Click();

        Assert.Empty(cut.FindAll(".fa-chart-card"));
        Assert.Contains("Nothing logged in Carries yet", cut.Markup);
    }

    [Fact]
    public async Task Benchmarks_and_Calendar_tabs_exist_and_show_a_coming_state()
    {
        var cut = await RenderAsync();

        var tabs = cut.FindAll(".fa-progress__tabs button").Select(b => b.TextContent.Trim()).ToArray();
        Assert.Equal(["Progress", "Benchmarks", "Calendar"], tabs);

        cut.FindAll(".fa-progress__tabs button")[1].Click();
        Assert.Contains("Benchmarks are coming", cut.Markup);
        cut.FindAll(".fa-progress__tabs button")[2].Click();
        Assert.Contains("Finish a Workout and your Plan", cut.Markup);
        cut.FindAll(".fa-progress__tabs button")[0].Click();
        Assert.NotNull(cut.Find(".empty"));
    }
}
