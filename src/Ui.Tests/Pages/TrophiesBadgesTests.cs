using Bunit;
using Core.Model;
using Core.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Ui.Pages;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>The Badges tab of Trophies (design S-13): tile grid, unlocked count, closest bars, and the Badge detail.</summary>
public sealed class TrophiesBadgesTests : BunitContext
{
    private static readonly Badge First = new("first", "First Workout", "sessions", 1);
    private static readonly Badge Ten = new("ten", "10 Workouts", "sessions", 10);
    private static readonly Badge Odd = new("odd", "Mystery", "nonsense", 3);

    private async Task<IRenderedComponent<Trophies>> RenderAsync(IReadOnlyList<Badge> badges, params Workout[] workouts)
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: workouts);
        if (badges.Count > 0) Assert.IsType<CommitResult.Committed>(await data.CommitAsync(badges.Select(Change.Put).ToList()));
        Wire(this, data);
        return Render<Trophies>();
    }

    [Fact]
    public async Task Before_any_history_the_Badges_tab_shows_an_empty_state_and_no_tiles()
    {
        var cut = await RenderAsync([First]);

        Assert.NotNull(cut.Find(".empty"));
        Assert.Empty(cut.FindAll(".fa-tile"));
    }

    [Fact]
    public async Task With_history_the_tabs_count_and_tiles_show_unlocked_first()
    {
        var cut = await RenderAsync([Ten, First, Odd], Finished("w1", Today, Set("a", 20, 5)));

        Assert.Equal(["Badges", "Bests", "Calendar"], cut.FindAll(".fa-trophies__tabs button").Select(b => b.TextContent.Trim()));
        Assert.Equal("Badges unlocked: 1 / 3", cut.Find(".fa-badges__count").TextContent.Trim());
        var tiles = cut.FindAll(".fa-tile");
        Assert.Equal(
            ["First Workout, unlocked, 1 / 1", "10 Workouts, in progress, 1 / 10", "Mystery, not measurable, not measurable"],
            tiles.Select(t => t.GetAttribute("aria-label")));
        Assert.Contains("fa-tile--unlocked", tiles[0].ClassName);
        Assert.Contains("fa-tile--progress", tiles[1].ClassName);
        Assert.Contains("fa-tile--locked", tiles[2].ClassName);
        Assert.Contains("badge-workouts-1.svg", tiles[0].QuerySelector("img")!.GetAttribute("src"));
        Assert.Contains("badge-workouts-10.svg", tiles[1].QuerySelector("img")!.GetAttribute("src"));
        Assert.Single(cut.FindAll("[role=progressbar]"));
    }

    [Fact]
    public async Task Tapping_a_tile_opens_the_detail_with_what_counted_and_back_returns()
    {
        var cut = await RenderAsync([Ten], Finished("w1", Today, Set("a", 20, 5)));

        cut.Find(".fa-tile").Click();

        var detail = cut.Find(".fa-badgedetail");
        Assert.Contains("10 Workouts", detail.QuerySelector(".fa-badgedetail__name")!.TextContent);
        Assert.Equal("Complete 10 Workouts", detail.QuerySelector(".fa-badgedetail__needs")!.TextContent);
        Assert.Contains("Workout: Lower", detail.QuerySelector(".fa-badgedetail__event")!.TextContent);
        Assert.Equal("1 / 10", cut.Find(".fa-pbar__value").TextContent);
        Assert.Empty(cut.FindAll(".fa-tile"));

        cut.Find(".fa-badgedetail button").Click();

        Assert.Single(cut.FindAll(".fa-tile"));
    }

    [Fact]
    public async Task A_Badge_that_cannot_be_measured_says_so_in_its_detail()
    {
        var cut = await RenderAsync([Odd], Finished("w1", Today, Set("a", 20, 5)));

        cut.Find(".fa-tile").Click();

        Assert.Contains("not measurable", cut.Find(".fa-badgedetail__unmeasurable").TextContent);
        Assert.Empty(cut.FindAll("[role=progressbar]"));
    }

    [Fact]
    public async Task Bests_lists_the_personal_best_and_Calendar_shows_the_heatmap()
    {
        var cut = await RenderAsync([First], Finished("w1", Today, Set("a", 20, 5)));

        cut.FindAll(".fa-trophies__tabs button").Single(b => b.TextContent.Trim() == "Bests").Click();
        Assert.NotNull(cut.Find(".fa-bests"));

        cut.FindAll(".fa-trophies__tabs button").Single(b => b.TextContent.Trim() == "Calendar").Click();
        Assert.NotNull(cut.Find(".fa-heat__grid"));
    }

    [Fact]
    public async Task A_badge_query_opens_that_Badges_detail_straight_away()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: Finished("w1", Today, Set("a", 20, 5)));
        await data.CommitAsync(Change.Put(Ten));
        Wire(this, data);
        Services.GetRequiredService<NavigationManager>().NavigateTo("trophies?badge=ten");

        var cut = Render<Trophies>();

        Assert.Contains("10 Workouts", cut.Find(".fa-badgedetail__name").TextContent);
    }
}
