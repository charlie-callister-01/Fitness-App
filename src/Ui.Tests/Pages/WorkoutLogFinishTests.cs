using AngleSharp.Dom;
using Bunit;
using Core.Model;
using Core.Storage;
using Core.Tests.Storage;
using Microsoft.AspNetCore.Components.Web;
using Ui.Pages;
using Ui.Saving;
using static Ui.Tests.Pages.LogFixture;

namespace Ui.Tests.Pages;

/// <summary>Finishing a Workout on the log page (design 8.5, C-13): the Partial prompt, the finish write, the Pain check and the summary.</summary>
public sealed class WorkoutLogFinishTests : BunitContext
{
    private readonly SaveFailureNotice _notice = new();

    private static WorkoutEntry SquatDone(int sets = 3, int? rpe = null) =>
        new(Squat, EntryStatus.Done, null, null, rpe, [.. Enumerable.Range(1, sets).Select(i => Set($"s{i}", 20, 8))]);

    private static WorkoutEntry LungeDone() =>
        new(Lunge, EntryStatus.Done, null, null, null, [Set("l1", 10, 10), Set("l2", 10, 10)]);

    private async Task<(AppData Data, IRenderedComponent<WorkoutLog> Cut)> OpenAsync(
        AppData? data = null, params WorkoutEntry[] entries)
    {
        data ??= await OpenSeededAsync(plan: Plan(), workouts: InProgress("w-now", entries));
        Wire(this, data, _notice);
        return (data, Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "w-now")));
    }

    private static Workout Stored(AppData data) => data.Snapshot.Workouts.Single(w => w.Id == "w-now");

    private static IElement Dialog(IRenderedComponent<WorkoutLog> cut) => cut.Find("[role='dialog']");

    private static string Title(IRenderedComponent<WorkoutLog> cut) => cut.Find("[role='dialog'] .fa-sheet__title").TextContent;

    private static IElement DialogButton(IRenderedComponent<WorkoutLog> cut, string label) =>
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == label);

    private static IElement Chip(IRenderedComponent<WorkoutLog> cut, string group, string label) =>
        cut.FindAll($"[role='dialog'] [role='group'][aria-label='{group}'] button").Single(b => b.TextContent.Trim() == label);

    private static void FinishFromMenu(IRenderedComponent<WorkoutLog> cut)
    {
        cut.Find("button[aria-label='Workout menu']").Click();
        cut.FindAll("[role='dialog'] .fa-menuitem").Single(i => i.TextContent.Contains("Finish Workout")).Click();
    }

    private static void FinishFromPrimary(IRenderedComponent<WorkoutLog> cut) =>
        cut.Find(".fa-confirmbar .fa-btn--primary").Click();

    private static bool SummaryShown(IRenderedComponent<WorkoutLog> cut) => cut.FindAll(".fa-summary").Count == 1;

    // ---- Finish and the Partial prompt

    [Fact]
    public async Task A_fully_done_Workout_finishes_without_the_Partial_prompt_and_opens_the_Pain_check()
    {
        var (data, cut) = await OpenAsync(null, SquatDone(), LungeDone());

        FinishFromPrimary(cut);

        Assert.Equal(Now, Stored(data).EndedAt);
        Assert.Equal("Any pain today?", Title(cut));
        Assert.Equal(["No", "Yes", "Skip"], cut.FindAll("[role='dialog'] .fa-sheet__body > .fa-painanswers button").Select(b => b.TextContent.Trim()));
    }

    [Fact]
    public async Task Finish_Workout_in_the_menu_is_enabled_and_asks_the_Partial_question_when_Exercises_are_left()
    {
        var (data, cut) = await OpenAsync(null, SquatDone());

        FinishFromMenu(cut);

        Assert.Equal("Finish as Partial?", Title(cut));
        Assert.Contains("1 of 2 done; the rest are recorded as skipped. Partial still counts toward your Streak.", Dialog(cut).TextContent);
        Assert.Null(Stored(data).EndedAt);
    }

    [Fact]
    public async Task Keep_going_on_the_Partial_prompt_changes_nothing()
    {
        var (data, cut) = await OpenAsync(null, SquatDone());
        var before = Stored(data);
        FinishFromMenu(cut);

        DialogButton(cut, "Keep going").Click();

        Assert.Empty(cut.FindAll("[role='dialog']"));
        Assert.Equal(before, Stored(data));
    }

    [Fact]
    public async Task Escape_on_the_Partial_prompt_is_Keep_going()
    {
        var (data, cut) = await OpenAsync(null, SquatDone());
        FinishFromMenu(cut);

        Dialog(cut).KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Empty(cut.FindAll("[role='dialog']"));
        Assert.Null(Stored(data).EndedAt);
    }

    [Fact]
    public async Task Finishing_as_Partial_records_the_rest_as_skipped_stamps_the_end_and_opens_the_Pain_check()
    {
        var (data, cut) = await OpenAsync(null, SquatDone());
        FinishFromMenu(cut);

        DialogButton(cut, "Finish").Click();

        var saved = Stored(data);
        Assert.Equal(Now, saved.EndedAt);
        Assert.Contains(saved.Entries, e => e.ExerciseId == Lunge && e.Status == EntryStatus.Skipped && e.SkipReason is null);
        Assert.Equal("Any pain today?", Title(cut));
    }

    [Fact]
    public async Task A_Workout_with_no_confirmed_Set_cannot_finish_and_says_so_plainly()
    {
        var (data, cut) = await OpenAsync();
        var before = Stored(data);

        FinishFromMenu(cut);

        Assert.Contains("Confirm at least one Set before finishing.", cut.Find(".log__notice[role='status']").TextContent);
        Assert.Empty(cut.FindAll("[role='dialog']"));
        Assert.Equal(before, Stored(data));
    }

    [Fact]
    public async Task Finishing_stops_the_screen_lock()
    {
        var (_, cut) = await OpenAsync(null, SquatDone(), LungeDone());
        var wake = Services.GetService(typeof(RecordingWakeLock)) as RecordingWakeLock;
        Assert.Contains("acquire", wake!.Calls);

        FinishFromPrimary(cut);

        Assert.Equal("release", wake.Calls.Last());
    }

    // ---- The Pain check

    [Fact]
    public async Task No_saves_a_none_Pain_check_and_shows_the_summary()
    {
        var (data, cut) = await OpenAsync(null, SquatDone(), LungeDone());
        FinishFromPrimary(cut);

        DialogButton(cut, "No").Click();

        Assert.Equal(PainStatus.None, Stored(data).PainCheck!.Status);
        Assert.Empty(cut.FindAll("[role='dialog']"));
        Assert.True(SummaryShown(cut));
        Assert.Contains("No", cut.Find(".fa-summary__pain").TextContent);
    }

    [Fact]
    public async Task Skip_saves_nothing_so_the_Pain_check_stays_not_answered()
    {
        var (data, cut) = await OpenAsync(null, SquatDone(), LungeDone());
        FinishFromPrimary(cut);

        DialogButton(cut, "Skip").Click();

        Assert.Null(Stored(data).PainCheck);
        Assert.True(SummaryShown(cut));
        Assert.Contains("Not answered", cut.Find(".fa-summary__pain").TextContent);
    }

    [Fact]
    public async Task Escape_on_the_Pain_check_is_Skip()
    {
        var (data, cut) = await OpenAsync(null, SquatDone(), LungeDone());
        FinishFromPrimary(cut);

        Dialog(cut).KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Null(Stored(data).PainCheck);
        Assert.Empty(cut.FindAll("[role='dialog']"));
        Assert.True(SummaryShown(cut));
    }

    [Fact]
    public async Task Yes_expands_in_place_with_chips_that_are_aria_pressed_toggles()
    {
        var (_, cut) = await OpenAsync(null, SquatDone(), LungeDone());
        FinishFromPrimary(cut);

        DialogButton(cut, "Yes").Click();

        Assert.Equal("Any pain today?", Title(cut));
        Assert.Contains("Knee", cut.FindAll("[role='dialog'] [role='group'][aria-label='Body area'] button").Select(b => b.TextContent.Trim()));
        Assert.Equal(["Mild", "Moderate", "Sharp"],
            cut.FindAll("[role='dialog'] [role='group'][aria-label='Severity'] button").Select(b => b.TextContent.Trim()));
        Assert.Equal("false", Chip(cut, "Body area", "Knee").GetAttribute("aria-pressed"));

        Chip(cut, "Body area", "Knee").Click();
        Assert.Equal("true", Chip(cut, "Body area", "Knee").GetAttribute("aria-pressed"));
        Chip(cut, "Body area", "Shoulder").Click();
        Assert.Equal("true", Chip(cut, "Body area", "Knee").GetAttribute("aria-pressed"));
        Chip(cut, "Body area", "Knee").Click();
        Assert.Equal("false", Chip(cut, "Body area", "Knee").GetAttribute("aria-pressed"));

        Chip(cut, "Severity", "Mild").Click();
        Chip(cut, "Severity", "Sharp").Click();
        Assert.Equal("false", Chip(cut, "Severity", "Mild").GetAttribute("aria-pressed"));
        Assert.Equal("true", Chip(cut, "Severity", "Sharp").GetAttribute("aria-pressed"));
    }

    [Fact]
    public async Task Yes_without_an_area_and_a_severity_says_so_and_saves_nothing()
    {
        var (data, cut) = await OpenAsync(null, SquatDone(), LungeDone());
        FinishFromPrimary(cut);
        DialogButton(cut, "Yes").Click();

        DialogButton(cut, "Save and see summary").Click();
        Assert.Contains("Choose at least one body area and a severity.", Dialog(cut).TextContent);
        Chip(cut, "Body area", "Knee").Click();
        DialogButton(cut, "Save and see summary").Click();

        Assert.Contains("Choose at least one body area and a severity.", Dialog(cut).TextContent);
        Assert.Null(Stored(data).PainCheck);
        Assert.Equal("Any pain today?", Title(cut));
    }

    [Fact]
    public async Task Yes_with_details_saves_a_reported_Pain_check_and_the_summary_words_it()
    {
        var (data, cut) = await OpenAsync(null, SquatDone(), LungeDone());
        FinishFromPrimary(cut);
        DialogButton(cut, "Yes").Click();
        Chip(cut, "Body area", "Knee").Click();
        Chip(cut, "Severity", "Mild").Click();
        Chip(cut, "Side", "Left").Click();
        cut.Find("[role='dialog'] select").Change(Squat);
        cut.Find("[role='dialog'] textarea").Change("twinge on the way down");

        DialogButton(cut, "Save and see summary").Click();

        var pain = Stored(data).PainCheck!;
        Assert.Equal(PainStatus.Reported, pain.Status);
        Assert.Equal([BodyArea.Knee], pain.Areas);
        Assert.Equal(PainSeverity.Mild, pain.Severity);
        Assert.Equal(Squat, pain.ExerciseId);
        Assert.StartsWith("Left side", pain.Note);
        Assert.Contains("twinge on the way down", pain.Note);
        Assert.True(SummaryShown(cut));
        Assert.Equal("Pain check: Yes: Left knee, mild (during Goblet squat)", cut.Find(".fa-summary__pain").TextContent.Trim());
    }

    [Fact]
    public async Task A_failed_Pain_check_save_keeps_the_sheet_open_with_the_banner_and_a_retry_succeeds()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(store, Plan(), InProgress("w-now", SquatDone(), LungeDone()));
        var (_, cut) = await OpenAsync(data);
        FinishFromPrimary(cut);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        DialogButton(cut, "No").Click();

        Assert.Contains("out of storage", _notice.Message);
        Assert.Null(Stored(data).PainCheck);
        Assert.Equal("Any pain today?", Title(cut));

        store.Recover();
        DialogButton(cut, "No").Click();

        Assert.Equal(PainStatus.None, Stored(data).PainCheck!.Status);
        Assert.True(SummaryShown(cut));
    }

    // ---- Killing the tab, reopening

    [Fact]
    public async Task A_tab_killed_at_the_Pain_check_leaves_a_finished_Workout_and_reopening_shows_the_summary_without_asking_again()
    {
        var (data, cut) = await OpenAsync(null, SquatDone(), LungeDone());
        FinishFromPrimary(cut);
        Assert.NotNull(Stored(data).EndedAt);
        Assert.Null(Stored(data).PainCheck);

        using var reopened = new BunitContext();
        Wire(reopened, data, new SaveFailureNotice());
        var again = reopened.Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "w-now"));

        Assert.Empty(again.FindAll("[role='dialog']"));
        Assert.Single(again.FindAll(".fa-summary"));
        Assert.Contains("Not answered", again.Find(".fa-summary__pain").TextContent);
        Assert.DoesNotContain("Workout not found", again.Markup);
    }

    [Fact]
    public async Task A_finished_Workout_with_no_start_time_shows_a_dash_for_the_duration()
    {
        var finished = InProgress("w-now", SquatDone()) with { StartedAt = null, EndedAt = Now };
        var (_, cut) = await OpenAsync(await OpenSeededAsync(plan: Plan(), workouts: finished));

        Assert.Equal("—", cut.Find(".fa-summary__duration").TextContent.Trim());
    }

    [Fact]
    public async Task A_finished_Workout_shows_its_duration_and_takes_no_screen_lock()
    {
        var finished = InProgress("w-now", SquatDone()) with { EndedAt = Now.AddMinutes(48) };
        var (_, cut) = await OpenAsync(await OpenSeededAsync(plan: Plan(), workouts: finished));

        Assert.Equal("48 min", cut.Find(".fa-summary__duration").TextContent.Trim());
        Assert.Empty(((RecordingWakeLock)Services.GetService(typeof(RecordingWakeLock))!).Calls);
    }

    // ---- The summary

    [Fact]
    public async Task The_summary_shows_Sets_a_PR_with_its_previous_best_each_Exercise_chip_and_a_Badge_unlock()
    {
        var swapped = new WorkoutEntry("pullup", EntryStatus.Swapped, null, Lunge, 8, [new WorkoutSet("p1", null, 6, null, null, null, false)]);
        var squat = new WorkoutEntry(Squat, EntryStatus.Done, null, null, 7, [Set("n1", 25, 8), Set("n2", 25, 8)]);
        var data = await OpenSeededAsync(plan: Plan(), workouts:
        [
            Finished("w-old", Today.AddDays(-7), Set("o1", 20, 8)),
            InProgress("w-now", squat, swapped) with { EndedAt = Now.AddMinutes(30) },
        ]);
        await data.CommitAsync(Change.Put(new Badge("first-two", "Twice over", "sessions", 2)));
        var (_, cut) = await OpenAsync(data);

        var summary = cut.Find(".fa-summary").TextContent;
        Assert.Contains("30 min", summary);
        Assert.Contains("3 Sets", summary);
        Assert.Contains("Goblet squat", cut.Find(".fa-summary__prs").TextContent);
        Assert.Contains("25", cut.Find(".fa-summary__prs").TextContent);
        Assert.Contains("20", cut.Find(".fa-summary__prs").TextContent);
        var rows = cut.FindAll(".fa-summary__row");
        Assert.Equal(2, rows.Count);
        Assert.Contains("Done", rows[0].TextContent);
        Assert.Contains("RPE 7", rows[0].TextContent);
        Assert.Contains("Swapped", rows[1].TextContent);
        Assert.Contains("was Walking lunge", rows[1].TextContent);
        Assert.Contains("Twice over", cut.Find(".fa-unlock[role='status']").TextContent);
    }

    [Fact]
    public async Task A_skipped_Exercise_shows_its_reason_and_the_summary_offers_Done_back_to_Home()
    {
        var skipped = new WorkoutEntry(Lunge, EntryStatus.Skipped, "Short on time", null, null, []);
        var (_, cut) = await OpenAsync(
            await OpenSeededAsync(plan: Plan(), workouts: InProgress("w-now", SquatDone(), skipped) with { EndedAt = Now.AddMinutes(30) }));

        var row = cut.FindAll(".fa-summary__row").Single(r => r.TextContent.Contains("Walking lunge"));
        Assert.Contains("Skipped", row.TextContent);
        Assert.Contains("Short on time", row.TextContent);
        Assert.Contains("Partial", cut.Find(".fa-summary").TextContent);
        Assert.Equal("./", cut.FindAll("a").Single(a => a.TextContent.Trim() == "Done").GetAttribute("href"));
        Assert.Empty(cut.FindAll(".fa-unlock"));
    }

    // ---- Read-only and failure

    [Fact]
    public async Task A_failed_finish_leaves_the_Workout_open_with_the_banner()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(store, Plan(), InProgress("w-now", SquatDone(), LungeDone()));
        var (_, cut) = await OpenAsync(data);
        store.FailAllWrites(StoreFailureKind.StorageFull);

        FinishFromPrimary(cut);

        Assert.Contains("out of storage", _notice.Message);
        Assert.Null(Stored(data).EndedAt);
        Assert.Empty(cut.FindAll("[role='dialog']"));
        Assert.Single(cut.FindAll(".fa-confirmbar"));
    }

    [Fact]
    public async Task When_the_window_cannot_save_Finish_Workout_is_disabled()
    {
        var store = new InMemoryDataStore();
        await OpenSeededAsync(store, Plan(), InProgress("w-now", SquatDone()));
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var (_, cut) = await OpenAsync(locked);
        cut.Find("button[aria-label='Workout menu']").Click();

        Assert.True(cut.FindAll("[role='dialog'] .fa-menuitem").Single(i => i.TextContent.Contains("Finish Workout")).HasAttribute("disabled"));
    }
}
