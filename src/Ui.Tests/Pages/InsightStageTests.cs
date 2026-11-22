using Bunit;
using Core.Model;
using Core.Storage;
using Ui.Pages;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>The whole Insight stage over one data set: logged Workouts and Weigh-ins show up as a chart, a Calendar day, a Weight trend, an unlocked Badge and a review.</summary>
public sealed class InsightStageTests : BunitContext
{
    [Fact]
    public async Task Logged_Workouts_and_Weigh_ins_flow_into_every_Insight_screen()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts:
        [
            Finished("w1", Today.AddDays(-14), Set("a", 20, 5)),
            Finished("w2", Today.AddDays(-7), Set("b", 22.5, 5)),
            Finished("w3", Today, Set("c", 25, 5)),
        ]);
        Assert.IsType<CommitResult.Committed>(await data.CommitAsync(
        [
            Change.Put(new Badge("first", "First Workout", "sessions", 1)),
            Change.Put(new WeighIn("wi1", Today.AddDays(-7), 82)),
            Change.Put(new WeighIn("wi2", Today, 81)),
        ]));
        Wire(this, data);

        var progress = Render<Progress>();
        Assert.NotEmpty(progress.FindAll(".fa-chart"));

        progress.FindAll(".fa-progress__tabs button").Single(b => b.TextContent.Trim() == "Calendar").Click();
        Assert.NotNull(progress.Find("button[aria-label^='Mon 16 Nov: Lower, done']"));

        var weight = Render<Weight>();
        Assert.Equal("81.0 kg", weight.Find(".fa-weight__now").TextContent.Trim());
        Assert.NotEmpty(weight.FindAll(".fa-weight__body .fa-chart"));

        var trophies = Render<Trophies>();
        Assert.Equal("Badges unlocked: 1 / 1", trophies.Find(".fa-badges__count").TextContent.Trim());
        Assert.Contains("fa-tile--unlocked", trophies.Find(".fa-tile").ClassName);

        var review = Render<WeeklyReview>();
        Assert.Equal("1 of 3", review.Find(".fa-review__workouts").TextContent.Trim());
    }
}
