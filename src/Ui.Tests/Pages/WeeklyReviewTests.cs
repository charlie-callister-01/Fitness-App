using Bunit;
using Core.Model;
using Core.Storage;
using Ui.Pages;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>The Weekly review (design S-09, F-07). Fixed today is Mon 16 Nov 2026; the Plan starts two weeks earlier unless a test says otherwise.</summary>
public sealed class WeeklyReviewTests : BunitContext
{
    private static readonly DateOnly LastMonday = Today.AddDays(-7);

    private static Plan EarlierPlan() => Plan() with { StartDate = Today.AddDays(-14) };

    private static Plan WithRhythm(Plan plan, int target, int minPerWeek, bool customCounts) =>
        plan with { Tracks = [plan.Tracks[0] with { Rhythm = new TrackRhythm.Weekly(target, minPerWeek, customCounts) }, .. plan.Tracks.Skip(1)] };

    private async Task<IRenderedComponent<WeeklyReview>> RenderAsync(Plan? plan, Workout[]? workouts = null, Change[]? extra = null)
    {
        var data = await OpenSeededAsync(null, plan, workouts ?? []);
        if (extra is { Length: > 0 })
        {
            await data.CommitAsync(extra);
        }

        Wire(this, data);
        return Render<WeeklyReview>();
    }

    private static string Verdict(IRenderedComponent<WeeklyReview> cut) => cut.Find(".fa-review__verdicttitle").TextContent.Trim();

    private static Workout CustomDone(string id, DateOnly date, PainCheck? pain = null) =>
        Finished(id, date, Set($"{id}-s", 20, 5)) with { SessionId = null, PainCheck = pain };

    [Fact]
    public async Task With_no_active_Plan_it_shows_an_empty_state()
    {
        var cut = await RenderAsync(null);

        Assert.Contains("No active Plan", cut.Find(".empty").TextContent);
        Assert.Empty(cut.FindAll(".fa-review__verdict"));
    }

    [Fact]
    public async Task Early_in_the_week_it_says_On_track_with_Workouts_against_target_and_the_Streak()
    {
        var cut = await RenderAsync(EarlierPlan());

        Assert.Equal("On track", Verdict(cut));
        Assert.Equal("0 of 3", cut.Find(".fa-review__workouts").TextContent.Trim());
        Assert.Equal("0 weeks", cut.Find(".fa-review__streak").TextContent.Trim());
        Assert.Contains("This week", cut.Find(".fa-review__week").TextContent);
    }

    [Fact]
    public async Task Reaching_the_target_says_Target_hit()
    {
        var plan = WithRhythm(EarlierPlan(), 3, 2, true);
        var cut = await RenderAsync(plan, [CustomDone("a", Today), CustomDone("b", Today), CustomDone("c", Today)]);

        Assert.Equal("Target hit", Verdict(cut));
        Assert.Equal("3 of 3", cut.Find(".fa-review__workouts").TextContent.Trim());
    }

    [Fact]
    public async Task A_past_week_short_of_the_target_says_Behind_and_lists_its_Missed_session()
    {
        var cut = await RenderAsync(EarlierPlan());

        cut.Find("button[aria-label='Previous week']").Click();

        Assert.Equal("Behind", Verdict(cut));
        Assert.StartsWith("Week of", cut.Find(".fa-review__week").TextContent.Trim());
        Assert.Contains("Lower", cut.Find(".fa-review__missed").TextContent);
        Assert.Contains("Mon 9 Nov", cut.Find(".fa-review__missed").TextContent);
    }

    [Fact]
    public async Task A_Break_week_says_On_a_Break_and_is_never_Behind()
    {
        var cut = await RenderAsync(EarlierPlan(), extra: [Change.Put(new Break("b", Today.AddDays(1), Today.AddDays(6), "Trip"))]);

        Assert.Equal("On a Break", Verdict(cut));
        Assert.DoesNotContain("Behind", cut.Find(".fa-review__verdict").TextContent);
    }

    [Fact]
    public async Task Target_out_of_reach_shows_when_the_days_left_cannot_cover_it()
    {
        // Nine a week cannot fit in the seven days left.
        var plan = WithRhythm(EarlierPlan(), 9, 2, false);
        var cut = await RenderAsync(plan);

        Assert.Equal("Target out of reach", Verdict(cut));
    }

    [Fact]
    public async Task Behind_shows_when_there_is_no_spare_day()
    {
        var plan = WithRhythm(EarlierPlan(), 7, 2, false);
        var cut = await RenderAsync(plan);

        Assert.Equal("Behind", Verdict(cut));
    }

    [Fact]
    public async Task Rows_show_PRs_the_weight_average_against_last_week_and_Pain_checks()
    {
        var plan = WithRhythm(EarlierPlan(), 3, 2, true);
        var pain = new PainCheck("p", PainStatus.Reported, [BodyArea.Knee], PainSeverity.Mild, null, null);
        var cut = await RenderAsync(
            plan,
            [CustomDone("a", LastMonday.AddDays(1)), CustomDone("b", Today, pain) with { Entries = [new WorkoutEntry(Squat, EntryStatus.Done, null, null, null, [Set("b-s", 30, 5)])] }],
            [Change.Put(new WeighIn("w1", Today.AddDays(-6), 92)), Change.Put(new WeighIn("w2", Today, 90))]);

        Assert.Contains("Goblet squat", cut.Find(".fa-review__prs").TextContent);
        Assert.Contains("Average 90.0 kg, last week 92.0 kg", cut.Find(".fa-review__weight").TextContent);
        Assert.Contains("Knee, mild", cut.Find(".fa-review__pain").TextContent);
    }

    [Fact]
    public async Task Empty_rows_say_so_plainly()
    {
        var cut = await RenderAsync(EarlierPlan());

        Assert.Contains("No PRs this week", cut.Find(".fa-review__prs").TextContent);
        Assert.Contains("No Weigh-ins this week", cut.Find(".fa-review__weight").TextContent);
        Assert.Contains("No pain reported", cut.Find(".fa-review__pain").TextContent);
    }

    [Fact]
    public async Task Week_selector_steps_back_and_forward_and_stops_at_this_week_and_the_Plan_start()
    {
        var cut = await RenderAsync(EarlierPlan());
        Assert.True(cut.Find("button[aria-label='Next week']").HasAttribute("disabled"));

        cut.Find("button[aria-label='Previous week']").Click();
        cut.Find("button[aria-label='Previous week']").Click();
        Assert.True(cut.Find("button[aria-label='Previous week']").HasAttribute("disabled"));

        cut.Find("button[aria-label='Next week']").Click();
        cut.Find("button[aria-label='Next week']").Click();
        Assert.Contains("This week", cut.Find(".fa-review__week").TextContent);
    }

    [Fact]
    public async Task Export_for_the_LLM_links_to_the_7_day_Markdown_preset()
    {
        var cut = await RenderAsync(EarlierPlan());

        var link = cut.Find("a[href='backup?preset=llm']");
        Assert.Contains("Export for the LLM", link.TextContent);
    }

    [Fact]
    public async Task A_Break_week_that_also_reached_the_target_says_Target_hit()
    {
        var plan = WithRhythm(EarlierPlan(), 3, 2, true);
        var cut = await RenderAsync(plan, [CustomDone("a", Today), CustomDone("b", Today), CustomDone("c", Today)],
            [Change.Put(new Break("b", Today, Today.AddDays(5), "Trip"))]);

        Assert.Equal("Target hit", Verdict(cut));
    }
}
