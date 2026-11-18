using Bunit;
using Core.Model;
using Core.Storage;
using Core.Tests.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Ui.Pages;
using Ui.Saving;
using static Ui.Tests.Pages.LogFixture;

namespace Ui.Tests.Pages;

public sealed class HomeTests : BunitContext
{
    private readonly SaveFailureNotice _notice = new();

    private async Task<IRenderedComponent<Home>> RenderHomeAsync(AppData? data = null)
    {
        data ??= await AppData.OpenAsync(new InMemoryDataStore());
        Wire(this, data, _notice);
        return Render<Home>();
    }

    private string CurrentPath => Services.GetRequiredService<NavigationManager>().Uri[Services.GetRequiredService<NavigationManager>().BaseUri.Length..];

    [Fact]
    public async Task Home_ShowsTheAppNameWithNoBuildIdentifierOrTemporaryLinks()
    {
        var cut = await RenderHomeAsync();

        Assert.Contains("Fitness App", cut.Find("h1").TextContent);
        Assert.Empty(cut.FindAll(".build-id"));
        Assert.Empty(cut.FindAll("a[href='gallery']"));
        Assert.Empty(cut.FindAll("a[href='appearance-stub']"));
    }

    [Fact]
    public async Task With_no_active_plan_home_says_so_and_offers_no_start()
    {
        var cut = await RenderHomeAsync(await OpenSeededAsync());

        Assert.Contains("No active Plan", cut.Markup);
        Assert.Empty(cut.FindAll("button.fa-btn--primary"));
    }

    [Fact]
    public async Task A_plan_with_no_sessions_says_so()
    {
        var cut = await RenderHomeAsync(await OpenSeededAsync(plan: Plan(sessions: [])));

        Assert.Contains("has no Sessions", cut.Markup);
        Assert.Empty(cut.FindAll("button.fa-btn--primary"));
    }

    [Fact]
    public async Task Start_names_the_next_session_creates_an_in_progress_workout_and_opens_its_log()
    {
        var data = await OpenSeededAsync(plan: Plan());
        var cut = await RenderHomeAsync(data);

        Assert.Contains("Lower", cut.Markup);
        cut.Find("button.fa-btn--primary").Click();

        var workout = Assert.Single(data.Snapshot.Workouts);
        Assert.Null(workout.EndedAt);
        Assert.Equal(Now, workout.StartedAt);
        Assert.Equal(Today, workout.Date);
        Assert.Equal("lower", workout.SessionId);
        Assert.Equal([Squat, Lunge], workout.Planned.Select(p => p.ExerciseId));
        Assert.Empty(workout.Entries);
        Assert.Equal($"log/{workout.Id}", CurrentPath);
    }

    [Fact]
    public async Task With_a_workout_in_progress_home_offers_Continue_to_it_and_no_second_start()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress("w-now"));
        var cut = await RenderHomeAsync(data);

        Assert.Empty(cut.FindAll("button.fa-btn--primary"));
        var link = cut.Find("a.fa-btn--primary");
        Assert.Equal("Continue", link.TextContent.Trim());
        Assert.Equal("log/w-now", link.GetAttribute("href"));
        Assert.Single(data.Snapshot.Workouts);
    }

    [Fact]
    public async Task A_workout_begun_after_home_rendered_turns_Start_into_Continue()
    {
        var data = await OpenSeededAsync(plan: Plan());
        var cut = await RenderHomeAsync(data);
        await cut.InvokeAsync(() => data.CommitAsync(Change.Put(InProgress("w-other"))));

        // Home redraws from the change, so Start is gone before it can be tapped again. (StartAsync also re-checks.)
        Assert.Empty(cut.FindAll("button.fa-btn--primary"));
        Assert.Equal("log/w-other", cut.Find("a.fa-btn--primary").GetAttribute("href"));
    }

    [Fact]
    public async Task A_failed_start_stays_on_home_and_reports_the_failure()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(store, Plan());
        var cut = await RenderHomeAsync(data);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        cut.Find("button.fa-btn--primary").Click();

        Assert.Empty(data.Snapshot.Workouts);
        Assert.Contains("out of storage", _notice.Message);
        Assert.Equal("", CurrentPath);
    }

    [Fact]
    public async Task A_read_only_window_cannot_start()
    {
        var store = new InMemoryDataStore();
        await OpenSeededAsync(store, Plan());
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var cut = await RenderHomeAsync(locked);

        Assert.True(cut.Find("button.fa-btn--primary").HasAttribute("disabled"));
    }
}
