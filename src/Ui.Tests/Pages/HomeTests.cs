using System.Text.RegularExpressions;
using Bunit;
using Core.Model;
using Core.Storage;
using Core.Tests.Storage;
using Core.Time;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Ui.Pages;
using Ui.Saving;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

public sealed class HomeTests : BunitContext
{
    private readonly SaveFailureNotice _notice = new();

    private async Task<IRenderedComponent<Home>> RenderHomeAsync(AppData? data = null, DateTimeOffset? now = null)
    {
        data ??= await AppData.OpenAsync(new InMemoryDataStore());
        Wire(this, data, _notice);
        if (now is { } clockNow)
        {
            Services.AddSingleton<IClock>(new FixedClock(clockNow));
        }

        return Render<Home>();
    }

    private string CurrentPath => Services.GetRequiredService<NavigationManager>().Uri[Services.GetRequiredService<NavigationManager>().BaseUri.Length..];

    private static bool Has(IRenderedComponent<Home> cut, string buttonText) =>
        cut.FindAll("button, a.fa-btn").Any(b => b.TextContent.Trim() == buttonText);

    private static Workout FinishedToday() =>
        new("w-done", Today, "lower", false, Now.AddHours(-1), Now.AddMinutes(-10), [new SessionExercise(Squat, 3, 8, null, null)],
            [new WorkoutEntry(Squat, EntryStatus.Done, null, null, null, [Set("s1", 20, 8)])], null);

    [Fact]
    public async Task Home_ShowsTheGreetingWithNoBuildIdentifierOrTemporaryLinks()
    {
        var cut = await RenderHomeAsync();

        Assert.Matches(new Regex("^Good (morning|afternoon|evening)$"), cut.Find("h1").TextContent.Trim());
        Assert.Empty(cut.FindAll(".build-id"));
        Assert.Empty(cut.FindAll("a[href='gallery']"));
        Assert.Empty(cut.FindAll("a[href='appearance-stub']"));
    }

    [Fact]
    public async Task With_no_active_plan_home_says_so_and_offers_no_start()
    {
        var cut = await RenderHomeAsync(await OpenSeededAsync());

        Assert.Contains("No active Plan", cut.Markup);
        Assert.False(Has(cut, "Start"));
        Assert.Empty(cut.FindAll(".fa-weekstrip"));
        Assert.Empty(cut.FindAll(".fa-nudge"));
        Assert.True(Has(cut, "Log weight"));
    }

    [Fact]
    public async Task A_plan_with_no_sessions_shows_a_rest_day_with_no_start()
    {
        var cut = await RenderHomeAsync(await OpenSeededAsync(plan: Plan(sessions: [])));

        Assert.Contains("Rest day", cut.Markup);
        Assert.False(Has(cut, "Start"));
    }

    [Fact]
    public async Task Start_names_the_session_creates_an_in_progress_workout_and_opens_its_log()
    {
        var data = await OpenSeededAsync(plan: Plan());
        var cut = await RenderHomeAsync(data);

        Assert.Contains("Lower", cut.Markup);
        cut.Find("button.fa-btn--primary").Click();

        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Workouts));
        var workout = Assert.Single(data.Snapshot.Workouts);
        Assert.Null(workout.EndedAt);
        Assert.Equal(Now, workout.StartedAt);
        Assert.Equal(Today, workout.Date);
        Assert.Equal("lower", workout.SessionId);
        Assert.Equal([Squat, Lunge], workout.Planned.Select(p => p.ExerciseId));
        Assert.Empty(workout.Entries);
        cut.WaitForAssertion(() => Assert.Equal($"log/{workout.Id}", CurrentPath));
    }

    [Fact]
    public async Task A_second_Start_tap_creates_one_workout()
    {
        var data = await OpenSeededAsync(plan: Plan());
        var cut = await RenderHomeAsync(data);

        var start = cut.Find("button.fa-btn--primary");
        start.Click();
        start.Click();

        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Workouts));
    }

    [Fact]
    public async Task With_a_workout_in_progress_home_offers_Continue_to_it_and_no_second_start()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress("w-now"));
        var cut = await RenderHomeAsync(data);

        Assert.False(Has(cut, "Start"));
        var continueButton = cut.FindAll("button").Single(b => b.TextContent.Trim() == "Continue");
        Assert.False(continueButton.HasAttribute("disabled"));
        continueButton.Click();

        cut.WaitForAssertion(() => Assert.Equal("log/w-now", CurrentPath));
        Assert.Single(data.Snapshot.Workouts);
    }

    [Fact]
    public async Task A_workout_begun_after_home_rendered_turns_Start_into_Continue()
    {
        var data = await OpenSeededAsync(plan: Plan());
        var cut = await RenderHomeAsync(data);
        await cut.InvokeAsync(() => data.CommitAsync(Change.Put(InProgress("w-other"))));

        cut.WaitForAssertion(() =>
        {
            Assert.False(Has(cut, "Start"));
            Assert.True(Has(cut, "Continue"));
        });
    }

    [Fact]
    public async Task A_failed_start_stays_on_home_and_reports_the_failure()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(store, Plan());
        var cut = await RenderHomeAsync(data);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        cut.Find("button.fa-btn--primary").Click();

        cut.WaitForAssertion(() => Assert.Contains("out of storage", _notice.Message));
        Assert.Empty(data.Snapshot.Workouts);
        Assert.Equal("", CurrentPath);
        Assert.True(Has(cut, "Start"));
    }

    [Fact]
    public async Task A_read_only_window_cannot_start_and_says_why()
    {
        var store = new InMemoryDataStore();
        await OpenSeededAsync(store, Plan());
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var cut = await RenderHomeAsync(locked);

        Assert.True(cut.Find("button.fa-btn--primary").HasAttribute("disabled"));
        Assert.Contains("read-only", cut.Markup);
    }

    // The TodayCard states, from real data.

    [Fact]
    public async Task Rest_day_shows_when_nothing_is_due()
    {
        var done = Finished("w-mon", Today, Set("s1", 20, 8));
        var cut = await RenderHomeAsync(await OpenSeededAsync(plan: Plan(), workouts: done), Now.AddDays(1));

        Assert.Contains("Rest day", cut.Markup);
        Assert.False(Has(cut, "Start"));
    }

    [Fact]
    public async Task Done_today_shows_and_View_summary_opens_the_log()
    {
        var cut = await RenderHomeAsync(await OpenSeededAsync(plan: Plan(), workouts: FinishedToday()));

        Assert.Contains("Done today", cut.Markup);
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "View summary").Click();

        cut.WaitForAssertion(() => Assert.Equal("log/w-done", CurrentPath));
    }

    [Fact]
    public async Task A_read_only_window_disables_the_alternatives_and_says_why()
    {
        var store = new InMemoryDataStore();
        await OpenSeededAsync(store, Plan());
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var cut = await RenderHomeAsync(locked);

        var different = cut.FindAll("button").Single(b => b.TextContent.Trim() == "Do a different Session");
        var past = cut.FindAll("button").Single(b => b.TextContent.Trim() == "Log a past Workout");

        Assert.True(different.HasAttribute("disabled"));
        Assert.True(past.HasAttribute("disabled"));
        Assert.Contains("read-only", cut.Find(".fa-home__main > p.fa-muted").TextContent);
    }

    [Fact]
    public async Task A_Break_shows_On_a_Break()
    {
        var data = await OpenSeededAsync(plan: Plan());
        await data.CommitAsync(Change.Put(new Break("b", Today.AddDays(-1), Today.AddDays(5), "Holiday")));

        var cut = await RenderHomeAsync(data);

        Assert.Contains("On a Break", cut.Markup);
        Assert.False(Has(cut, "Start"));
    }

    [Fact]
    public async Task A_finished_Plan_shows_Plan_finished()
    {
        var plan = WithMainPhases(Plan(), [new Phase("p", "Foundation", 1, 1, [], [])]);
        var cut = await RenderHomeAsync(await OpenSeededAsync(plan: plan), Now.AddDays(21));

        Assert.Contains("Your Plan has finished", cut.Markup);
    }

    [Fact]
    public async Task A_Plan_that_starts_later_says_when()
    {
        var plan = Plan() with { StartDate = Today.AddDays(14) };
        var cut = await RenderHomeAsync(await OpenSeededAsync(plan: plan));

        Assert.Contains("Your Plan starts on Mon 30 Nov", cut.Markup);
    }

    // Visibility rules.

    [Fact]
    public async Task Alternatives_and_Log_a_past_Workout_show_on_a_normal_day()
    {
        var cut = await RenderHomeAsync(await OpenSeededAsync(plan: Plan()));

        Assert.True(Has(cut, "Do a different Session"));
        Assert.True(Has(cut, "Build a Custom workout"));
        Assert.True(Has(cut, "Log a past Workout"));
    }

    [Fact]
    public async Task Alternatives_are_hidden_while_a_Workout_is_in_progress()
    {
        var cut = await RenderHomeAsync(await OpenSeededAsync(plan: Plan(), workouts: InProgress("w-now")));

        Assert.False(Has(cut, "Do a different Session"));
        Assert.False(Has(cut, "Build a Custom workout"));
        Assert.False(Has(cut, "Log a past Workout"));
        Assert.NotEmpty(cut.FindAll(".fa-nudge"));
    }

    [Fact]
    public async Task Alternatives_and_Nudges_are_hidden_in_a_Break_but_Log_weight_stays()
    {
        var data = await OpenSeededAsync(plan: Plan());
        await data.CommitAsync(Change.Put(new Break("b", Today.AddDays(-1), Today.AddDays(5), "Holiday")));

        var cut = await RenderHomeAsync(data);

        Assert.False(Has(cut, "Do a different Session"));
        Assert.False(Has(cut, "Log a past Workout"));
        Assert.Empty(cut.FindAll(".fa-nudge"));
        Assert.True(Has(cut, "Log weight"));
    }

    [Fact]
    public async Task The_strip_line_and_three_orbs_render_and_tapping_them_navigates()
    {
        var plan = WithMainPhases(Plan(), [new Phase("p", "Foundation", 1, 8, [], [])]) with { GoalDate = Today.AddDays(100) };
        var cut = await RenderHomeAsync(await OpenSeededAsync(plan: plan));

        Assert.Equal(7, cut.FindAll(".fa-weekstrip__tile").Count);
        Assert.Contains("This week", cut.Find(".fa-weekline").TextContent);
        Assert.Equal(3, cut.FindAll("svg[role='img']").Count);

        cut.FindAll(".fa-weekstrip__tile")[0].Click();
        Assert.Equal("week", CurrentPath);
        cut.FindAll(".fa-orbrow__orb")[0].Click();
        Assert.Equal("progress?tab=calendar", CurrentPath);
    }

    [Fact]
    public async Task The_page_shows_Recent_PRs_and_Weight_panels()
    {
        var cut = await RenderHomeAsync(await OpenSeededAsync(plan: Plan()));

        Assert.Contains("Recent PRs", cut.Markup);
        Assert.Contains("No Weigh-ins yet.", cut.Markup);
    }
}
