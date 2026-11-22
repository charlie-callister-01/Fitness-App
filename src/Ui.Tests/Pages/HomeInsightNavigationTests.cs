using Bunit;
using Core.Model;
using Core.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Ui.Pages;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>Home leads into Insight: the Streak orb opens the Calendar, the Phase and Race orbs open Progress, and the Sunday Today card leads to the Weekly review after the weigh-in.</summary>
public sealed class HomeInsightNavigationTests : BunitContext
{
    private static readonly DateTimeOffset Sunday = new(2026, 11, 22, 9, 0, 0, TimeSpan.Zero);

    private static Plan PlanWithPhase()
    {
        var plan = Plan();
        return plan with
        {
            Tracks = [plan.Tracks[0] with { Phases = [new Phase("p", "Foundation", 1, 8, [], [])] }, .. plan.Tracks.Skip(1)],
            GoalDate = Today.AddDays(100),
        };
    }

    private string CurrentPath => Services.GetRequiredService<NavigationManager>().Uri[Services.GetRequiredService<NavigationManager>().BaseUri.Length..];

    private async Task<IRenderedComponent<Home>> RenderAsync(DateTimeOffset? now = null, params WeighIn[] weighIns)
    {
        var plan = PlanWithPhase();
        var data = await OpenSeededAsync(plan: plan);
        if (weighIns.Length > 0)
        {
            await data.CommitAsync(weighIns.Select(Change.Put).ToList());
        }

        Wire(this, data);
        if (now is { } n)
        {
            Services.AddSingleton<Core.Time.IClock>(new FixedClock(n));
        }

        return Render<Home>();
    }

    [Fact]
    public async Task The_Streak_orb_opens_the_Calendar_tab()
    {
        var cut = await RenderAsync();

        cut.FindAll(".fa-orbrow__orb")[0].Click();

        Assert.Equal("progress?tab=calendar", CurrentPath);
    }

    [Fact]
    public async Task The_Phase_and_Race_orbs_open_Progress()
    {
        var cut = await RenderAsync();

        cut.FindAll(".fa-orbrow__orb")[1].Click();
        Assert.Equal("progress", CurrentPath);

        cut.FindAll(".fa-orbrow__orb")[2].Click();
        Assert.Equal("progress", CurrentPath);
    }

    [Fact]
    public async Task On_Sunday_after_the_weigh_in_the_Today_card_leads_to_the_Weekly_review()
    {
        var cut = await RenderAsync(Sunday, new WeighIn("wi", new DateOnly(2026, 11, 22), 80));

        cut.Find(".fa-todaycard__review button").Click();

        Assert.Equal("weekly-review", CurrentPath);
    }

    [Fact]
    public async Task On_Sunday_before_the_weigh_in_the_card_does_not_lead_to_the_review()
    {
        var cut = await RenderAsync(Sunday);

        Assert.Empty(cut.FindAll(".fa-todaycard__review"));
    }

    [Fact]
    public async Task On_other_days_the_card_does_not_lead_to_the_review_even_after_a_weigh_in()
    {
        var cut = await RenderAsync(null, new WeighIn("wi", Today, 80));

        Assert.Empty(cut.FindAll(".fa-todaycard__review"));
    }

    [Fact]
    public async Task Progress_and_Trophies_open_on_the_Calendar_tab_when_asked()
    {
        Wire(this, await OpenSeededAsync(plan: Plan()));
        Services.GetRequiredService<NavigationManager>().NavigateTo("progress?tab=calendar");

        var progress = Render<Progress>();
        Assert.Equal("Calendar", progress.Find(".fa-progress__tabs button[aria-pressed='true']").TextContent.Trim());

        Services.GetRequiredService<NavigationManager>().NavigateTo("trophies?tab=calendar");
        var trophies = Render<Trophies>();
        Assert.Equal("Calendar", trophies.Find(".fa-trophies__tabs button[aria-pressed='true']").TextContent.Trim());
    }
}
