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

    private async Task<IRenderedComponent<Home>> RenderHomeAsync(
        AppData? data = null, DateTimeOffset? now = null, FakeDevicePreferences? preferences = null, FakeBrowserCapabilities? browser = null)
    {
        data ??= await OpenSeededAsync(plan: Plan());
        Wire(this, data, _notice, preferences, browser);
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
        var seeded = await OpenSeededAsync();
        Assert.IsType<CommitResult.Committed>(await seeded.CommitAsync([Change.Put(new WeighIn("w1", Today, 80))]));
        var cut = await RenderHomeAsync(seeded);

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

    // Daily Track cards and extra weekly Track week lines.

    private static Plan MultiTrackHomePlan()
    {
        var gym = new Track(MainTrack, "Gym", new TrackRhythm.Weekly(2, 2, false), []);
        var cardio = new Track("cardio", "Cardio", new TrackRhythm.Weekly(2, 1, false), []);
        var mobility = new Track("mob", "Mobility", new TrackRhythm.Daily(75), []);
        return new Plan("plan", "Plan", true, Today.AddDays(-7), null, null, 2, [gym, cardio, mobility],
        [
            Session("lower", "Lower", DayOfWeek.Monday, SessionType.Strength, null, [new SessionExercise(Squat, 3, 8, null, null)], MainTrack),
            Session("upper", "Upper", DayOfWeek.Thursday, SessionType.Strength, null, [new SessionExercise(Lunge, 2, 10, null, null)], MainTrack),
            Session("tempo", "Tempo Run", DayOfWeek.Monday, SessionType.Cardio, null, [new SessionExercise(Lunge, 2, 10, null, null)], "cardio"),
            Session("long", "Long Run", DayOfWeek.Saturday, SessionType.Cardio, null, [new SessionExercise(Squat, 2, 10, null, null)], "cardio"),
            new Session("stretch", "Morning Mobility", "mob", DefaultDay.Daily, SessionType.Mobility, null,
                [new SessionExercise(Squat, 1, 5, null, null), new SessionExercise(Lunge, 1, 5, null, null)]),
        ]);
    }

    private static Workout DailyFinished(string id, DateOnly date) =>
        new(id, date, "stretch", false,
            new DateTimeOffset(date.ToDateTime(new TimeOnly(7, 0)), TimeSpan.Zero),
            new DateTimeOffset(date.ToDateTime(new TimeOnly(7, 15)), TimeSpan.Zero),
            [new SessionExercise(Squat, 1, 5, null, null), new SessionExercise(Lunge, 1, 5, null, null)],
            [
                new WorkoutEntry(Squat, EntryStatus.Done, null, null, null, [Set($"{id}-1", 10, 5)]),
                new WorkoutEntry(Lunge, EntryStatus.Done, null, null, null, [Set($"{id}-2", 10, 5)]),
            ],
            null);

    [Fact]
    public async Task A_one_Track_Plan_renders_no_daily_Track_cards_or_extra_week_lines()
    {
        var cut = await RenderHomeAsync(await OpenSeededAsync(plan: Plan()));

        Assert.Empty(cut.FindAll(".fa-home__daily"));
        Assert.Empty(cut.FindAll(".fa-home__extraline"));
    }

    [Fact]
    public async Task Daily_Track_card_renders_in_each_state_with_its_Streak_and_Start_Continue_and_Done_behave_right()
    {
        var yesterdayDone = DailyFinished("w-yest", Today.AddDays(-1));
        var data = await OpenSeededAsync(plan: MultiTrackHomePlan(), workouts: yesterdayDone);
        var cut = await RenderHomeAsync(data);

        // Not started today, 1 day streak from yesterday (open today doesn't break it).
        var card = Assert.Single(cut.FindAll(".fa-home__daily"));
        Assert.Equal("mob", card.GetAttribute("data-track"));
        Assert.Contains("Mobility", card.TextContent);
        Assert.Contains("Morning Mobility", card.TextContent);
        Assert.Contains("Not started", card.TextContent);
        Assert.Contains("1 day", card.TextContent);

        // Tapping Start on the daily Track card creates today's Workout for that daily Session and opens its log.
        cut.Find("button[aria-label='Start Morning Mobility']").Click();
        cut.WaitForAssertion(() => Assert.Equal(2, data.Snapshot.Workouts.Count));
        var openDaily = data.Snapshot.Workouts.Single(w => w.Date == Today && w.SessionId == "stretch");
        Assert.Null(openDaily.EndedAt);
        cut.WaitForAssertion(() => Assert.Equal($"log/{openDaily.Id}", CurrentPath));

        // In progress: card shows In progress and Continue, while main Track TodayCard still offers Start for Lower.
        card = Assert.Single(cut.FindAll(".fa-home__daily"));
        Assert.Contains("In progress · 0% done", card.TextContent);
        Assert.Contains("Lower", cut.Find(".fa-todaycard").TextContent);
        Services.GetRequiredService<NavigationManager>().NavigateTo("");
        cut.Find("button[aria-label='Continue Morning Mobility']").Click();
        cut.WaitForAssertion(() => Assert.Equal($"log/{openDaily.Id}", CurrentPath));
        Assert.Equal(2, data.Snapshot.Workouts.Count);

        // Done: once finished at >= 75%, card shows Done chip, 2 days streak, and no Start or Continue button on the daily card.
        var finishedToday = DailyFinished(openDaily.Id, Today);
        await cut.InvokeAsync(() => data.CommitAsync(Change.Put(finishedToday)));
        cut.WaitForAssertion(() =>
        {
            var doneCard = Assert.Single(cut.FindAll(".fa-home__daily"));
            Assert.Equal("Done", doneCard.QuerySelector(".fa-chip__word")?.TextContent.Trim());
            Assert.Contains("100% done", doneCard.TextContent);
            Assert.Contains("2 days", doneCard.TextContent);
            Assert.Empty(doneCard.QuerySelectorAll("button"));
        });
    }

    [Fact]
    public async Task Daily_Track_card_shows_On_a_Break_with_no_button_during_a_Break()
    {
        var data = await OpenSeededAsync(plan: MultiTrackHomePlan(), workouts: DailyFinished("w-yest", Today.AddDays(-1)));
        await data.CommitAsync(Change.Put(new Break("b1", Today, Today.AddDays(2), "Rest")));
        var cut = await RenderHomeAsync(data);

        var card = Assert.Single(cut.FindAll(".fa-home__daily"));
        Assert.Contains("On a Break", card.TextContent);
        Assert.Contains("1 day", card.TextContent);
        Assert.Empty(card.QuerySelectorAll("button"));
    }

    [Fact]
    public async Task Extra_weekly_Track_week_line_renders_progress_and_Sessions_and_tapping_a_Session_starts_it()
    {
        var data = await OpenSeededAsync(plan: MultiTrackHomePlan());
        var cut = await RenderHomeAsync(data);

        var extra = Assert.Single(cut.FindAll(".fa-home__extraline"));
        Assert.Equal("cardio", extra.GetAttribute("data-track"));
        Assert.Contains("Cardio: 0 of 2 · target 2 · Streak 0 weeks", extra.TextContent);
        var sessionButtons = extra.QuerySelectorAll(".fa-home__extrasession");
        Assert.Equal(2, sessionButtons.Length);
        Assert.Contains("Tempo Run", sessionButtons[0].TextContent);
        Assert.Contains("Mon · upcoming", sessionButtons[0].TextContent);
        Assert.Contains("fa-home__extrasession--today", sessionButtons[0].ClassName);
        Assert.Contains("Long Run", sessionButtons[1].TextContent);
        Assert.Contains("Sat · upcoming", sessionButtons[1].TextContent);

        cut.Find(".fa-home__extraline button[aria-label*='Tempo Run']").Click();
        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Workouts));
        var started = Assert.Single(data.Snapshot.Workouts);
        Assert.Equal(("tempo", Today), (started.SessionId, started.Date));
        cut.WaitForAssertion(() => Assert.Equal($"log/{started.Id}", CurrentPath));
    }

    [Fact]
    public async Task Track_qualified_Nudge_renders_and_can_be_dismissed_for_the_week()
    {
        var data = await OpenSeededAsync(plan: MultiTrackHomePlan());
        // Wednesday: Monday's Lower (Gym) and Tempo Run (Cardio) were both missed -> Track-qualified nudges.
        var cut = await RenderHomeAsync(data, Now.AddDays(2));

        var nudges = cut.FindAll(".fa-nudge");
        Assert.Equal(2, nudges.Count);
        Assert.Contains("You missed Lower (Gym) on Monday.", nudges[0].TextContent);
        Assert.Contains("You missed Tempo Run (Cardio) on Monday.", nudges[1].TextContent);

        cut.FindAll(".fa-nudge__dismiss")[1].Click();
        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain(cut.FindAll(".fa-nudge"), n => n.TextContent.Contains("Tempo Run (Cardio)"));
        });
    }

    [Fact]
    public async Task Read_only_disables_the_daily_Track_card_and_extra_weekly_Track_week_line()
    {
        var store = new InMemoryDataStore();
        await OpenSeededAsync(store, MultiTrackHomePlan());
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var readOnlyCut = await RenderHomeAsync(locked);

        Assert.True(readOnlyCut.Find("button[aria-label='Start Morning Mobility']").HasAttribute("disabled"));
        Assert.All(readOnlyCut.FindAll(".fa-home__extrasession"), b => Assert.True(b.HasAttribute("disabled")));
    }

    [Fact]
    public async Task Failed_save_on_the_daily_Track_card_and_extra_weekly_Track_week_line_reports_failure_and_stays_on_Home()
    {
        var failingStore = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(failingStore, MultiTrackHomePlan());
        var cut = await RenderHomeAsync(data);
        failingStore.FailAllWrites(StoreFailureKind.StorageFull);

        cut.Find("button[aria-label='Start Morning Mobility']").Click();
        cut.WaitForAssertion(() => Assert.Contains("out of storage", _notice.Message));
        Assert.Empty(data.Snapshot.Workouts);
        Assert.Equal("", CurrentPath);

        _notice.ReportMessage(null);
        cut.Find(".fa-home__extraline button[aria-label*='Tempo Run']").Click();
        cut.WaitForAssertion(() => Assert.Contains("out of storage", _notice.Message));
        Assert.Empty(data.Snapshot.Workouts);
        Assert.Equal("", CurrentPath);
    }

    // First run (S-12) and Two quick things.
    private static async Task<AppData> OpenEmptyAsync(InMemoryStorageEnvironment? environment = null) =>
        await AppData.OpenAsync(new InMemoryDataStore(), environment);

    private static async Task<AppData> OpenPlannedAsync(InMemoryStorageEnvironment environment)
    {
        var data = await AppData.OpenAsync(new InMemoryDataStore(), environment);
        Assert.IsType<CommitResult.Committed>(await data.CommitAsync([.. PageFixture.Exercises.Select(Change.Put), Change.Put(Plan())]));
        return data;
    }

    [Fact]
    public async Task With_nothing_stored_home_welcomes_with_three_ways_to_start()
    {
        var cut = await RenderHomeAsync(await OpenEmptyAsync());

        Assert.Equal("Welcome to the Keep", cut.Find("h1").TextContent.Trim());
        Assert.Contains("only on this device", cut.Markup);
        Assert.Equal("backup?tab=import&kind=plan", cut.Find("a.fa-btn[href*='kind=plan']").GetAttribute("href"));
        Assert.Equal("Import my Plan", cut.Find("a.fa-btn[href*='kind=plan']").TextContent.Trim());
        Assert.Equal("backup?tab=import&kind=backup", cut.Find("a.fa-btn[href*='kind=backup']").GetAttribute("href"));
        Assert.Equal("Restore a Backup", cut.Find("a.fa-btn[href*='kind=backup']").TextContent.Trim());
        Assert.True(Has(cut, "Start with an empty Plan"));
        Assert.Empty(cut.FindAll(".fa-quickthings"));
    }

    [Fact]
    public async Task Starting_with_an_empty_plan_saves_an_active_plan_and_opens_its_editor()
    {
        var data = await OpenEmptyAsync();
        var cut = await RenderHomeAsync(data);

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "Start with an empty Plan").Click();

        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Plans));
        var plan = Assert.Single(data.Snapshot.Plans);
        Assert.True(plan.IsActive);
        Assert.IsType<TrackRhythm.Weekly>(Assert.Single(plan.Tracks).Rhythm);
        Assert.Equal(Today, plan.StartDate);
        cut.WaitForAssertion(() => Assert.Equal($"plans/{plan.Id}", CurrentPath));
    }

    [Fact]
    public async Task In_a_read_only_window_the_empty_plan_start_is_disabled_with_the_reason()
    {
        var data = await OpenEmptyAsync(new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var cut = await RenderHomeAsync(data);

        Assert.True(cut.FindAll("button").Single(b => b.TextContent.Trim() == "Start with an empty Plan").HasAttribute("disabled"));
        Assert.NotEmpty(cut.FindAll(".fa-well"));
        Assert.Empty(data.Snapshot.Plans);
    }

    [Fact]
    public async Task Two_quick_things_offer_install_storage_and_backup_once_a_plan_exists()
    {
        var environment = new InMemoryStorageEnvironment { Persistence = PersistenceStatus.NotGranted };
        var browser = new FakeBrowserCapabilities { CanInstall = true };
        var data = await OpenPlannedAsync(environment);
        var cut = await RenderHomeAsync(data, browser: browser);

        var card = cut.Find(".fa-quickthings");
        Assert.Contains("Two quick things", card.TextContent);
        Assert.DoesNotContain(cut.FindAll("h1"), h => h.TextContent.Contains("Welcome"));

        cut.Find(".fa-quickthings button[aria-label='Install the app']").Click();
        cut.WaitForAssertion(() => Assert.Equal(1, browser.InstallPrompts));

        var before = environment.PersistenceRequests;
        cut.Find(".fa-quickthings button[aria-label='Keep my data on this device']").Click();
        cut.WaitForAssertion(() => Assert.Equal(before + 1, environment.PersistenceRequests));

        Assert.Equal("backup", cut.Find(".fa-quickthings a[href]").GetAttribute("href"));
    }

    [Fact]
    public async Task Dismissing_two_quick_things_hides_it_for_good_on_this_device()
    {
        var preferences = new FakeDevicePreferences();
        var data = await OpenPlannedAsync(new InMemoryStorageEnvironment { Persistence = PersistenceStatus.NotGranted });
        var cut = await RenderHomeAsync(data, preferences: preferences);

        cut.Find(".fa-quickthings button.fa-home__link[aria-label='Dismiss: Two quick things']").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".fa-quickthings")));
        Assert.Contains("1", preferences.Values.Values);
    }

    [Fact]
    public async Task A_dismissed_two_quick_things_stays_hidden_on_the_next_visit()
    {
        var preferences = new FakeDevicePreferences();
        preferences.Values["quick-things-dismissed"] = "1";
        var data = await OpenPlannedAsync(new InMemoryStorageEnvironment { Persistence = PersistenceStatus.NotGranted });
        var cut = await RenderHomeAsync(data, preferences: preferences);

        Assert.Empty(cut.FindAll(".fa-quickthings"));
    }

    [Fact]
    public async Task Two_quick_things_stay_away_when_installed_and_persistent()
    {
        var data = await OpenPlannedAsync(new InMemoryStorageEnvironment());
        var cut = await RenderHomeAsync(data, browser: new FakeBrowserCapabilities { IsStandalone = true });

        Assert.Empty(cut.FindAll(".fa-quickthings"));
    }
}