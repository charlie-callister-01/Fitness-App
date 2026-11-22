using Bunit;
using Core.Rules.Schedule;
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

public sealed class CustomWorkoutDraftTests : BunitContext
{
    private readonly SaveFailureNotice _notice = new();

    private async Task<IRenderedComponent<CustomWorkoutDraft>> RenderDraftAsync(AppData? data = null, string? sessionId = null, DateTimeOffset? now = null)
    {
        data ??= await OpenSeededAsync(plan: Plan());
        Wire(this, data, _notice);
        if (now is { } clockNow)
        {
            Services.AddSingleton<IClock>(new FixedClock(clockNow));
        }

        return Render<CustomWorkoutDraft>(p => p.Add(d => d.SessionId, sessionId));
    }

    private string CurrentPath => Services.GetRequiredService<NavigationManager>().Uri[Services.GetRequiredService<NavigationManager>().BaseUri.Length..];

    private static AngleSharp.Dom.IElement StartButton(IRenderedComponent<CustomWorkoutDraft> cut) =>
        cut.FindAll("button.fa-btn--primary").Single(b => b.TextContent.Trim() == "Start");

    private static string[] DraftNames(IRenderedComponent<CustomWorkoutDraft> cut) =>
        [.. cut.FindAll(".fa-draft__row span").Select(s => s.TextContent.Trim())];

    private static void AddExercise(IRenderedComponent<CustomWorkoutDraft> cut, string name)
    {
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Add Exercise").Click();
        cut.FindAll(".fa-picker__row").Single(r => r.TextContent.Contains(name)).Click();
    }

    [Fact]
    public async Task An_empty_draft_has_Start_disabled_and_a_quiet_line()
    {
        var cut = await RenderDraftAsync();

        Assert.Contains("No Exercises yet. Add some to start.", cut.Markup);
        Assert.True(StartButton(cut).HasAttribute("disabled"));
        Assert.Contains("Custom workout", cut.Find("h1").TextContent);
    }

    [Fact]
    public async Task Adding_an_Exercise_enables_Start_Remove_takes_it_out_and_it_is_not_offered_twice()
    {
        var cut = await RenderDraftAsync();

        AddExercise(cut, "Plank");

        cut.WaitForAssertion(() => Assert.Equal(["Plank"], DraftNames(cut)));
        Assert.False(StartButton(cut).HasAttribute("disabled"));
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Add Exercise").Click();
        Assert.DoesNotContain(cut.FindAll(".fa-picker__row"), r => r.TextContent.Contains("Plank"));

        cut.Find("button[aria-label='Remove Plank']").Click();

        cut.WaitForAssertion(() => Assert.Empty(DraftNames(cut)));
        Assert.True(StartButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public async Task The_picker_offers_the_Exercises_of_this_weeks_Missed_Sessions_first()
    {
        var core = new Session("core", "Core", DayOfWeek.Monday, SessionType.Body, null,
            [new SessionExercise("pullup", 3, 5, null, null), new SessionExercise("plank", 2, null, null, null)]);
        var data = await OpenSeededAsync(plan: Plan([core]));
        var cut = await RenderDraftAsync(data, now: Now.AddDays(2));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Add Exercise").Click();

        var names = cut.FindAll(".fa-picker__row-name").Select(n => n.TextContent.Trim()).ToArray();
        Assert.Equal(["Pull-up", "Plank"], names[..2]);
    }

    [Fact]
    public async Task Start_saves_a_Custom_workout_with_no_Session_and_opens_its_log()
    {
        var data = await OpenSeededAsync(plan: Plan());
        var cut = await RenderDraftAsync(data);
        AddExercise(cut, "Walking lunge");
        AddExercise(cut, "Plank");

        StartButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Workouts));

        var workout = Assert.Single(data.Snapshot.Workouts);
        Assert.Null(workout.SessionId);
        Assert.Null(workout.EndedAt);
        Assert.Equal([Lunge, "plank"], workout.Planned.Select(p => p.ExerciseId));
        Assert.Equal(2, workout.Planned[0].Sets);
        Assert.Equal($"log/{workout.Id}", CurrentPath);
    }

    [Fact]
    public async Task A_second_tap_while_saving_starts_only_one_Workout()
    {
        var data = await OpenSeededAsync(plan: Plan());
        var cut = await RenderDraftAsync(data, "lower");

        var start = StartButton(cut);
        start.Click();
        start.Click();
        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Workouts));

        Assert.Single(data.Snapshot.Workouts);
    }

    [Fact]
    public async Task Make_up_pre_fills_the_Sessions_Exercises_which_can_be_removed_and_added_to()
    {
        var data = await OpenSeededAsync(plan: Plan());
        var cut = await RenderDraftAsync(data, "lower");

        Assert.Equal(["Goblet squat", "Walking lunge"], DraftNames(cut));
        Assert.Contains("Make up Lower", cut.Find("h1").TextContent);

        cut.Find("button[aria-label='Remove Walking lunge']").Click();
        AddExercise(cut, "Plank");
        cut.WaitForAssertion(() => Assert.Equal(["Goblet squat", "Plank"], DraftNames(cut)));
        StartButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Workouts));

        var workout = Assert.Single(data.Snapshot.Workouts);
        Assert.Null(workout.SessionId);
        Assert.Equal([Squat, "plank"], workout.Planned.Select(p => p.ExerciseId));
        Assert.Equal(3, workout.Planned[0].Sets);
        Assert.Equal(8, workout.Planned[0].Reps);
    }

    [Fact]
    public async Task An_unknown_Session_shows_the_message_and_a_Back_link()
    {
        var cut = await RenderDraftAsync(sessionId: "nope");

        Assert.Contains("That Session isn't in your active Plan.", cut.Markup);
        Assert.Empty(cut.FindAll(".fa-draft__row"));
        Assert.Contains(cut.FindAll("a"), a => a.TextContent.Trim() == "Back to Home");
    }

    [Fact]
    public async Task A_Missed_Session_stays_Missed_after_making_it_up()
    {
        var data = await OpenSeededAsync(plan: Plan());
        var cut = await RenderDraftAsync(data, "lower", now: Now.AddDays(2));
        var wednesday = Today.AddDays(2);
        var plan = data.Snapshot.Plans[0];
        Assert.Single(WeekScheduleRules.MissedInWeek(plan, [], [], data.Snapshot.Workouts, wednesday, wednesday));

        StartButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Workouts));

        var missed = WeekScheduleRules.MissedInWeek(plan, [], [], data.Snapshot.Workouts, wednesday, wednesday);
        Assert.Equal("lower", Assert.Single(missed).SessionId);
    }

    [Fact]
    public async Task Read_only_disables_Start_with_a_reason_and_saves_nothing()
    {
        var store = new InMemoryDataStore();
        await OpenSeededAsync(store, Plan());
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var cut = await RenderDraftAsync(locked, "lower");

        Assert.True(StartButton(cut).HasAttribute("disabled"));
        Assert.Contains("read-only", cut.Markup);
        Assert.Empty(locked.Snapshot.Workouts);
    }

    [Fact]
    public async Task A_failed_save_leaves_the_page_unchanged_and_reports_the_failure()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(store, Plan());
        var cut = await RenderDraftAsync(data, "lower");
        store.FailAllWrites(StoreFailureKind.StorageFull);

        StartButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Contains("out of storage", _notice.Message));

        Assert.Empty(data.Snapshot.Workouts);
        Assert.Contains("out of storage", _notice.Message);
        Assert.Equal("", CurrentPath);
        Assert.Equal(["Goblet squat", "Walking lunge"], DraftNames(cut));
    }
}
