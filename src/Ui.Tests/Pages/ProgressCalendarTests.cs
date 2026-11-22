using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Core.Model;
using Ui.Pages;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>The Calendar tab of Progress (design S-07): the whole Plan as a heatmap, opening a Workout on tap.</summary>
public sealed class ProgressCalendarTests : BunitContext
{
    private async Task<IRenderedComponent<Progress>> OpenCalendarAsync(params Workout[] workouts)
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: workouts);
        Wire(this, data);
        var cut = Render<Progress>();
        cut.FindAll(".fa-progress__tabs button").Single(b => b.TextContent.Trim() == "Calendar").Click();
        return cut;
    }

    [Fact]
    public async Task With_no_history_the_Calendar_shows_an_empty_state_and_no_heatmap()
    {
        var cut = await OpenCalendarAsync();

        Assert.NotNull(cut.Find(".empty"));
        Assert.Empty(cut.FindAll(".fa-heat"));
    }

    [Fact]
    public async Task With_history_the_Calendar_shows_labelled_days_today_and_a_legend()
    {
        var cut = await OpenCalendarAsync(Finished("w1", Today, Set("a", 20, 5)));

        Assert.Empty(cut.FindAll(".empty"));
        Assert.NotNull(cut.Find("button[aria-label='Mon 16 Nov: Lower, done, today']"));
        Assert.Single(cut.FindAll(".fa-heat__grid .fa-heat__cell--today"));
        Assert.NotNull(cut.Find(".fa-heat__legend"));
    }

    [Fact]
    public async Task Tapping_a_day_opens_its_Workout()
    {
        var cut = await OpenCalendarAsync(Finished("w1", Today, Set("a", 20, 5)));

        cut.Find("button[aria-label^='Mon 16 Nov']").Click();

        Assert.EndsWith("log/w1", Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().Uri);
    }
}
