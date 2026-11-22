using AngleSharp.Dom;
using Bunit;
using Core.Model;
using Core.Storage;
using Core.Tests.Storage;
using Microsoft.Extensions.DependencyInjection;
using Ui.Pages;
using Ui.Saving;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>Logged afterwards on the log page (design 8.6): re-dating the open Workout, its banner, no timers, history as of that date and undo.</summary>
public sealed class WorkoutLogAfterwardsTests : BunitContext
{
    private static readonly DateOnly Sunday = new(2026, 11, 15);

    private readonly SaveFailureNotice _notice = new();
    private readonly FakeDevicePreferences _preferences = new();

    private async Task<(AppData Data, IRenderedComponent<WorkoutLog> Cut)> OpenAsync(
        AppData? data = null, string? restSeconds = null)
    {
        data ??= await OpenSeededAsync(plan: Plan(), workouts: InProgress());
        if (restSeconds is not null)
        {
            _preferences.Values["rest-timer-seconds"] = restSeconds;
        }

        Wire(this, data, _notice, _preferences);
        return (data, Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "w-now")));
    }

    private static Workout Stored(AppData data) => data.Snapshot.Workouts.Single(w => w.Id == "w-now");

    private static IElement Item(IRenderedComponent<WorkoutLog> cut) =>
        cut.FindAll("[role='dialog'] .fa-menuitem").Single(i => i.TextContent.Contains("ogged afterwards"));

    private static void OpenMenu(IRenderedComponent<WorkoutLog> cut) => cut.Find("button[aria-label='Workout menu']").Click();

    private static IElement DateInput(IRenderedComponent<WorkoutLog> cut) => cut.Find("[role='dialog'] input[type='date']");

    private static IElement SheetButton(IRenderedComponent<WorkoutLog> cut, string label) =>
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == label);

    private static void Redate(IRenderedComponent<WorkoutLog> cut, string isoDate)
    {
        OpenMenu(cut);
        Item(cut).Click();
        DateInput(cut).Change(isoDate);
        SheetButton(cut, "Save date").Click();
    }

    private static string? Banner(IRenderedComponent<WorkoutLog> cut) =>
        cut.FindAll(".log__afterwards").SingleOrDefault()?.TextContent.Trim();

    private static string? Rest(IRenderedComponent<WorkoutLog> cut) => cut.FindAll(".fa-confirmbar__rest").SingleOrDefault()?.TextContent.Trim();

    private static void ConfirmSet(IRenderedComponent<WorkoutLog> cut, string kg = "50", string reps = "8")
    {
        cut.Find("input[aria-label^='Weight, Set 1']").Input(kg);
        cut.Find("input[aria-label^='Reps, Set 1']").Input(reps);
        cut.Find(".fa-confirmbar .fa-btn--primary").Click();
        cut.WaitForState(() => cut.FindAll(".fa-setrow--done").Count == 1); // a click returns before the save has finished
    }

    private static Workout AfterwardsWorkout() => InProgress() with { LoggedAfterwards = true, Date = Sunday, StartedAt = null };

    // ---- Re-dating

    [Fact]
    public async Task The_menu_item_opens_a_date_input_that_cannot_pick_after_today()
    {
        var (_, cut) = await OpenAsync();
        OpenMenu(cut);

        Assert.False(Item(cut).HasAttribute("disabled"));
        Item(cut).Click();

        var input = DateInput(cut);
        Assert.Equal("2026-11-16", input.GetAttribute("max"));
        Assert.Equal("Logged afterwards", cut.Find("[role='dialog'] .fa-sheet__title").TextContent);
    }

    [Fact]
    public async Task Saving_a_past_date_re_dates_the_Workout_and_shows_the_banner()
    {
        var (data, cut) = await OpenAsync();

        Redate(cut, "2026-11-15");

        var stored = Stored(data);
        Assert.True(stored.LoggedAfterwards);
        Assert.Equal(Sunday, stored.Date);
        Assert.Null(stored.StartedAt);
        Assert.Null(stored.EndedAt);
        Assert.Empty(cut.FindAll("[role='dialog']"));
        Assert.Equal("Logged afterwards · Sun 15 Nov. No timers.", Banner(cut));
    }

    [Fact]
    public async Task There_is_no_limit_on_how_far_back()
    {
        var (data, cut) = await OpenAsync();

        Redate(cut, "2019-03-02");

        Assert.Equal(new DateOnly(2019, 3, 2), Stored(data).Date);
        Assert.Equal("Logged afterwards · Sat 2 Mar. No timers.", Banner(cut));
    }

    [Fact]
    public async Task A_future_date_is_refused_with_a_plain_message_and_nothing_changes()
    {
        var (data, cut) = await OpenAsync();

        Redate(cut, "2026-11-17");

        Assert.Contains("cannot be dated in the future", cut.Find("[role='dialog'] [role='alert']").TextContent);
        Assert.False(Stored(data).LoggedAfterwards);
        Assert.Equal(Today, Stored(data).Date);
        Assert.Null(Banner(cut));
    }

    [Fact]
    public async Task Cancel_closes_the_sheet_and_changes_nothing()
    {
        var (data, cut) = await OpenAsync();
        OpenMenu(cut);
        Item(cut).Click();

        SheetButton(cut, "Cancel").Click();

        Assert.Empty(cut.FindAll("[role='dialog']"));
        Assert.False(Stored(data).LoggedAfterwards);
    }

    [Fact]
    public async Task The_banner_comes_from_the_Workout_so_it_survives_reload()
    {
        var (data, cut) = await OpenAsync();
        Redate(cut, "2026-11-15");

        using var reopened = new BunitContext();
        Wire(reopened, data, new SaveFailureNotice());
        var again = reopened.Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "w-now"));

        Assert.Equal("Logged afterwards · Sun 15 Nov. No timers.", Banner(again));
    }

    // ---- Undo

    [Fact]
    public async Task The_same_menu_item_undoes_it_back_to_today()
    {
        var (data, cut) = await OpenAsync();
        Redate(cut, "2026-11-15");
        OpenMenu(cut);
        Assert.Contains("Undo", Item(cut).TextContent);

        Item(cut).Click();

        var stored = Stored(data);
        Assert.False(stored.LoggedAfterwards);
        Assert.Equal(Today, stored.Date);
        Assert.Null(Banner(cut));
        Assert.Empty(cut.FindAll("[role='dialog']"));
    }

    // ---- No timers

    [Fact]
    public async Task A_logged_afterwards_Workout_shows_no_rest_countdown()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: AfterwardsWorkout());
        var (_, cut) = await OpenAsync(data, "90");

        ConfirmSet(cut);

        Assert.Single(cut.FindAll(".fa-setrow--done"));
        Assert.Null(Rest(cut));
        Assert.Equal(0, Services.GetRequiredService<FakeTicker>().Waiting);
    }

    [Fact]
    public async Task Re_dating_while_a_countdown_runs_clears_it()
    {
        var (_, cut) = await OpenAsync(null, "90");
        ConfirmSet(cut);
        Assert.NotNull(Rest(cut));

        Redate(cut, "2026-11-15");

        Assert.Null(Rest(cut));
    }

    [Fact]
    public async Task A_logged_afterwards_Workout_takes_no_screen_lock_and_re_dating_releases_it()
    {
        var wakeLockOpen = await OpenAsync();
        var wakeLock = Services.GetRequiredService<RecordingWakeLock>();
        wakeLockOpen.Cut.WaitForAssertion(() => Assert.Equal(["acquire"], wakeLock.Calls));

        Redate(wakeLockOpen.Cut, "2026-11-15");
        wakeLockOpen.Cut.WaitForAssertion(() => Assert.Equal(["acquire", "release"], wakeLock.Calls));

        OpenMenu(wakeLockOpen.Cut);
        Item(wakeLockOpen.Cut).Click();
        wakeLockOpen.Cut.WaitForAssertion(() => Assert.Equal(["acquire", "release", "acquire"], wakeLock.Calls));
    }

    [Fact]
    public async Task Opening_a_logged_afterwards_Workout_never_acquires_the_screen_lock()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: AfterwardsWorkout());
        await OpenAsync(data);

        Assert.Empty(Services.GetRequiredService<RecordingWakeLock>().Calls);
    }

    // ---- History as of the date

    [Fact]
    public async Task A_Set_is_compared_with_history_up_to_the_Workout_date_only()
    {
        // Before Sunday: 20 kg. After Sunday: 30 kg. A 22 kg Set logged for Sunday beats what stood then, so it is a PR.
        var older = Finished("w-older", Today.AddDays(-7), Set("o1", 20, 8));
        var later = Finished("w-later", Today, Set("l1", 30, 8));
        var data = await OpenSeededAsync(plan: Plan(), workouts: [older, later, AfterwardsWorkout()]);
        var (_, cut) = await OpenAsync(data);

        ConfirmSet(cut, "22", "8");

        Assert.Single(cut.FindAll(".fa-pr"));
        Assert.Contains("1 PR", cut.Find(".log__progress").TextContent);
    }

    [Fact]
    public async Task The_summary_shows_no_duration_and_the_PR_against_the_best_before_that_date()
    {
        var older = Finished("w-older", Today.AddDays(-7), Set("o1", 20, 8));
        var later = Finished("w-later", Today, Set("l1", 30, 8));
        var squatDone = new WorkoutEntry(Squat, EntryStatus.Done, null, null, null, [Set("s1", 22, 8)]);
        var open = AfterwardsWorkout() with { Entries = [squatDone] };
        var data = await OpenSeededAsync(plan: Plan(), workouts: [older, later, open]);
        var (_, cut) = await OpenAsync(data);

        OpenMenu(cut);
        cut.FindAll("[role='dialog'] .fa-menuitem").Single(i => i.TextContent.Contains("Finish Workout")).Click();
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Finish").Click();
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Skip").Click();

        Assert.Equal("—", cut.Find(".fa-summary__duration").TextContent.Trim());
        Assert.Contains("20", cut.Find(".fa-summary").TextContent);
        Assert.DoesNotContain("30 kg", cut.Find(".fa-summary").TextContent);
    }

    // ---- Failure and read-only

    [Fact]
    public async Task A_failed_re_date_shows_the_banner_and_leaves_the_screen_unchanged()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(store, Plan(), InProgress());
        var (_, cut) = await OpenAsync(data);
        OpenMenu(cut);
        Item(cut).Click();
        DateInput(cut).Change("2026-11-15");
        store.FailAllWrites(StoreFailureKind.StorageFull);

        SheetButton(cut, "Save date").Click();

        Assert.Contains("out of storage", _notice.Message);
        Assert.False(Stored(data).LoggedAfterwards);
        Assert.Equal(Today, Stored(data).Date);
        Assert.Null(Banner(cut));
    }

    [Fact]
    public async Task A_failed_undo_shows_the_banner_and_keeps_the_Workout_logged_afterwards()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(store, Plan(), AfterwardsWorkout());
        var (_, cut) = await OpenAsync(data);
        OpenMenu(cut);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        Item(cut).Click();

        Assert.Contains("out of storage", _notice.Message);
        Assert.True(Stored(data).LoggedAfterwards);
        Assert.Equal(Sunday, Stored(data).Date);
        Assert.NotNull(Banner(cut));
    }

    [Fact]
    public async Task When_the_window_cannot_save_the_item_is_disabled()
    {
        var store = new InMemoryDataStore();
        await OpenSeededAsync(store, Plan(), InProgress());
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var (_, cut) = await OpenAsync(locked);
        OpenMenu(cut);

        Assert.True(Item(cut).HasAttribute("disabled"));
    }
}
