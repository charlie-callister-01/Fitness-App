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

/// <summary>Home's sheets, Nudge actions and Log weight.</summary>
public sealed class HomeActionsTests : BunitContext
{
    private static readonly DateTimeOffset Wednesday = Now.AddDays(2);
    private readonly SaveFailureNotice _notice = new();
    private readonly FakeDevicePreferences _preferences = new();

    private static Plan TwoSessionPlan() => Plan(
    [
        Session("lower", "Lower", DayOfWeek.Monday, SessionType.Strength, null, [new SessionExercise(Squat, 3, 8, null, null)]),
        Session("upper", "Upper", DayOfWeek.Wednesday, SessionType.Strength, null, [new SessionExercise(Lunge, 2, 10, null, null)]),
    ]);

    private IRenderedComponent<Home> RenderHome(AppData data, DateTimeOffset? now = null)
    {
        Wire(this, data, _notice, _preferences);
        if (now is { } clockNow)
        {
            Services.AddSingleton<IClock>(new FixedClock(clockNow));
        }

        return Render<Home>();
    }

    private string CurrentPath => Services.GetRequiredService<NavigationManager>().Uri[Services.GetRequiredService<NavigationManager>().BaseUri.Length..];

    private static void ClickButton(IRenderedComponent<Home> cut, string text, string scope = "button") =>
        cut.FindAll(scope).First(b => b.TextContent.Trim() == text).Click();

    private static void ClickInSheet(IRenderedComponent<Home> cut, string text) =>
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == text).Click();

    // Do a different Session.

    [Fact]
    public async Task Do_a_different_Session_lists_undone_Sessions_and_picking_one_starts_it_today()
    {
        var data = await OpenSeededAsync(plan: TwoSessionPlan());
        var cut = RenderHome(data, Wednesday.AddHours(-9));
        // Wednesday today: Upper is due; Lower (Monday) is Missed and undone.

        ClickButton(cut, "Do a different Session");
        var rows = cut.FindAll(".fa-sessionrow");
        Assert.Equal(2, rows.Count);
        rows.Single(r => r.TextContent.Contains("Lower")).Click();
        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Workouts));

        var workout = Assert.Single(data.Snapshot.Workouts);
        Assert.Equal("lower", workout.SessionId);
        Assert.Equal(Today.AddDays(2), workout.Date);
        Assert.Equal($"log/{workout.Id}", CurrentPath);
    }

    [Fact]
    public async Task With_nothing_left_the_sheet_offers_Build_a_Custom_workout()
    {
        var done = Finished("w-mon", Today, Set("s1", 20, 8));
        var cut = RenderHome(await OpenSeededAsync(plan: Plan(), workouts: done), Now.AddDays(1));

        ClickButton(cut, "Do a different Session");
        Assert.Contains("No Sessions left this week.", cut.Markup);
        ClickInSheet(cut, "Build a Custom workout");

        Assert.Equal("custom", CurrentPath);
    }

    [Fact]
    public async Task Build_a_Custom_workout_navigates_to_the_draft_page()
    {
        var cut = RenderHome(await OpenSeededAsync(plan: Plan()));

        ClickButton(cut, "Build a Custom workout");

        Assert.Equal("custom", CurrentPath);
    }

    // Log a past Workout.

    [Fact]
    public async Task Log_a_past_Workout_starts_a_Logged_afterwards_Workout_for_the_chosen_day_and_Session()
    {
        var data = await OpenSeededAsync(plan: Plan());
        var cut = RenderHome(data);

        ClickButton(cut, "Log a past Workout");
        cut.Find("input[type='date']").Change("2026-11-14");
        cut.FindAll(".fa-pastrow").Single(r => r.TextContent.Contains("Lower")).Click();
        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Workouts));

        var workout = Assert.Single(data.Snapshot.Workouts);
        Assert.True(workout.LoggedAfterwards);
        Assert.Equal(new DateOnly(2026, 11, 14), workout.Date);
        Assert.Equal("lower", workout.SessionId);
        Assert.Equal($"log/{workout.Id}", CurrentPath);
    }

    [Fact]
    public async Task Log_a_past_Workout_Custom_has_no_Session()
    {
        var data = await OpenSeededAsync(plan: Plan());
        var cut = RenderHome(data);

        ClickButton(cut, "Log a past Workout");
        cut.FindAll(".fa-pastrow").Single(r => r.TextContent.Contains("Custom workout")).Click();
        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Workouts));

        Assert.Null(Assert.Single(data.Snapshot.Workouts).SessionId);
    }

    [Fact]
    public async Task A_future_date_is_refused_and_nothing_is_saved()
    {
        var data = await OpenSeededAsync(plan: Plan());
        var cut = RenderHome(data);

        ClickButton(cut, "Log a past Workout");
        cut.Find("input[type='date']").Change("2026-11-20");
        cut.FindAll(".fa-pastrow").First().Click();
        cut.WaitForAssertion(() => Assert.Contains("That day hasn't happened yet.", cut.Markup));

        Assert.Contains("That day hasn't happened yet.", cut.Markup);
        Assert.Empty(data.Snapshot.Workouts);
        Assert.Equal("", CurrentPath);
    }

    [Fact]
    public async Task A_failed_past_save_is_reported_and_the_sheet_stays_open()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(store, Plan());
        var cut = RenderHome(data);
        ClickButton(cut, "Log a past Workout");
        store.FailAllWrites(StoreFailureKind.StorageFull);

        cut.FindAll(".fa-pastrow").First().Click();
        cut.WaitForAssertion(() => Assert.Contains("out of storage", _notice.Message));

        Assert.Empty(data.Snapshot.Workouts);
        Assert.Contains("out of storage", _notice.Message);
        Assert.NotEmpty(cut.FindAll("[role='dialog']"));
    }

    // Nudges.

    [Fact]
    public async Task Make_up_opens_the_draft_for_that_Session()
    {
        var cut = RenderHome(await OpenSeededAsync(plan: TwoSessionPlan()), Wednesday.AddHours(-9));

        ClickButton(cut, "Make up");

        Assert.Equal("custom/makeup/lower", CurrentPath);
    }

    [Fact]
    public async Task Back_up_goes_to_the_Backup_page()
    {
        var cut = RenderHome(await OpenSeededAsync(plan: Plan()));

        ClickButton(cut, "Back up");

        Assert.Equal("backup", CurrentPath);
    }

    [Fact]
    public async Task Dismiss_hides_that_Nudge_keeps_the_other_and_persists_across_a_reload()
    {
        var data = await OpenSeededAsync(plan: TwoSessionPlan());
        var cut = RenderHome(data, Wednesday.AddHours(-9));
        Assert.Equal(2, cut.FindAll(".fa-nudge").Count);

        cut.FindAll(".fa-nudge__dismiss").First().Click();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll(".fa-nudge")));
        Assert.Single(_preferences.Values, v => v.Key.StartsWith("nudge-dismissed:"));
        var reloaded = Render<Home>();
        reloaded.WaitForAssertion(() => Assert.Single(reloaded.FindAll(".fa-nudge")));
    }

    [Fact]
    public async Task A_dismissed_Nudge_comes_back_in_a_new_week()
    {
        var data = await OpenSeededAsync(plan: Plan());
        var cut = RenderHome(data);
        cut.FindAll(".fa-nudge__dismiss").Single().Click();
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".fa-nudge")));

        var nextWeek = Render<Home>(); // same clock: still dismissed
        nextWeek.WaitForAssertion(() => Assert.Empty(nextWeek.FindAll(".fa-nudge")));
        Services.GetRequiredService<TestClock>().Advance(TimeSpan.FromDays(7));
        var later = Render<Home>();

        later.WaitForAssertion(() => Assert.Single(later.FindAll(".fa-nudge")));
    }

    // Log weight.

    [Fact]
    public async Task Log_weight_is_prefilled_with_the_last_value_and_saves_a_new_Weigh_in()
    {
        var data = await OpenSeededAsync(plan: Plan());
        await data.CommitAsync(Change.Put(new WeighIn("old", Today.AddDays(-1), 86.3)));
        var cut = RenderHome(data);

        ClickButton(cut, "Log weight", "header button");
        Assert.Equal("86.3", cut.Find("[role='dialog'] input").GetAttribute("value"));
        Assert.Contains("Last: 86.3 kg on Sun 15 Nov", cut.Markup);
        cut.Find("[role='dialog'] input").Input("85.9");
        ClickInSheet(cut, "Save");

        cut.WaitForAssertion(() => Assert.Equal(2, data.Snapshot.WeighIns.Count));
        Assert.Contains(data.Snapshot.WeighIns, w => w.Date == Today && w.WeightKg == 85.9);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role='dialog']")));
    }

    [Fact]
    public async Task A_second_Weigh_in_on_the_same_day_is_added_not_replaced()
    {
        var data = await OpenSeededAsync(plan: Plan());
        var cut = RenderHome(data);

        for (var i = 0; i < 2; i++)
        {
            ClickButton(cut, "Log weight", "header button");
            cut.Find("[role='dialog'] input").Input(i == 0 ? "86" : "87");
            ClickInSheet(cut, "Save");
            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("[role='dialog']")));
        }

        Assert.Equal(2, data.Snapshot.WeighIns.Count);
    }

    [Fact]
    public async Task Weight_typed_in_pounds_is_saved_in_kilograms()
    {
        var data = await OpenSeededAsync(plan: Plan());
        await data.CommitAsync(Change.Put(Settings.Default with { DisplayWeightUnit = WeightUnit.Lb }));
        var cut = RenderHome(data);

        ClickButton(cut, "Log weight", "header button");
        cut.Find("[role='dialog'] input").Input("190");
        ClickInSheet(cut, "Save");

        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.WeighIns));
        Assert.Equal(86.18, data.Snapshot.WeighIns[0].WeightKg, 2);
    }

    [Fact]
    public async Task Invalid_weight_shows_the_message_and_saves_nothing()
    {
        var data = await OpenSeededAsync(plan: Plan());
        var cut = RenderHome(data);

        ClickButton(cut, "Log weight", "header button");
        cut.Find("[role='dialog'] input").Input("abc");
        ClickInSheet(cut, "Save");

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.Find("[role='alert']").TextContent));
        Assert.Empty(data.Snapshot.WeighIns);
        Assert.NotEmpty(cut.FindAll("[role='dialog']"));
    }

    [Fact]
    public async Task A_failed_weight_save_keeps_the_sheet_open_and_reports_the_failure()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(store, Plan());
        var cut = RenderHome(data);
        ClickButton(cut, "Log weight", "header button");
        cut.Find("[role='dialog'] input").Input("86");
        store.FailAllWrites(StoreFailureKind.StorageFull);

        ClickInSheet(cut, "Save");

        Assert.Empty(data.Snapshot.WeighIns);
        Assert.Contains("out of storage", _notice.Message);
        Assert.NotEmpty(cut.FindAll("[role='dialog']"));
    }

    [Fact]
    public async Task Log_weight_is_available_with_a_Workout_in_progress_and_the_empty_panel_button_opens_it()
    {
        var cut = RenderHome(await OpenSeededAsync(plan: Plan(), workouts: InProgress("w-now")));

        cut.FindAll("section[aria-label='Weight'] button").Single().Click();

        Assert.NotEmpty(cut.FindAll("[role='dialog']"));
    }

    [Fact]
    public async Task Read_only_disables_the_Log_weight_save_and_writes_nothing()
    {
        var store = new InMemoryDataStore();
        await OpenSeededAsync(store, Plan());
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var cut = RenderHome(locked);

        ClickButton(cut, "Log weight", "header button");

        var save = cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Save");
        Assert.True(save.HasAttribute("disabled"));
        Assert.Empty(locked.Snapshot.WeighIns);
    }
}
