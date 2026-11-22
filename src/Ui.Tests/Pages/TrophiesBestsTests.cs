using Bunit;
using Core.Model;
using Ui.Pages;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>The Bests tab of Trophies (design S-13): each Exercise's personal Best with its date, and the Best of the week.</summary>
public sealed class TrophiesBestsTests : BunitContext
{
    private async Task<IRenderedComponent<Trophies>> RenderBestsAsync(params Workout[] workouts)
    {
        Wire(this, await OpenSeededAsync(plan: Plan(), workouts: workouts));
        var cut = Render<Trophies>();
        cut.FindAll(".fa-trophies__tabs button").Single(b => b.TextContent.Trim() == "Bests").Click();
        return cut;
    }

    [Fact]
    public async Task Before_any_history_Bests_shows_an_empty_state()
    {
        var cut = await RenderBestsAsync();

        Assert.NotNull(cut.Find(".empty"));
        Assert.Empty(cut.FindAll(".fa-bests__row"));
        Assert.Empty(cut.FindAll(".fa-bests__week"));
    }

    [Fact]
    public async Task Each_Exercise_with_a_Best_shows_its_result_and_date()
    {
        var cut = await RenderBestsAsync(
            Finished("w1", Today.AddDays(-14), Set("a", 20, 5)),
            Finished("w2", Today.AddDays(-9), Set("b", 30, 5)));

        var row = Assert.Single(cut.FindAll(".fa-bests__row"));
        Assert.Contains("Goblet squat", row.TextContent);
        Assert.Contains("30", row.TextContent);
        Assert.Contains("7 Nov 2026", row.TextContent);
        Assert.Contains("No PR this week", cut.Find(".fa-bests__week").TextContent);
    }

    [Fact]
    public async Task Best_of_the_week_names_the_PR_set_this_week()
    {
        var cut = await RenderBestsAsync(
            Finished("w1", Today.AddDays(-7), Set("a", 20, 5)),
            Finished("w2", Today, Set("b", 25, 5)));

        var week = cut.Find(".fa-bests__week").TextContent;
        Assert.Contains("Best of the week", week);
        Assert.Contains("Goblet squat", week);
        Assert.Contains("25", week);
    }
}
