using AngleSharp.Dom;
using Bunit;
using Core.Model;
using Core.Storage;
using Core.Tests.Storage;
using Ui.Pages;
using Ui.Saving;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>Skip and un-skip an Exercise on the log page (design 8.4): the reason sheet, the pager mark, the Skipped state and where the log lands.</summary>
public sealed class WorkoutLogSkipTests : BunitContext
{
    private readonly SaveFailureNotice _notice = new();

    private async Task<(AppData Data, IRenderedComponent<WorkoutLog> Cut)> OpenAsync(AppData? data = null, params WorkoutEntry[] entries)
    {
        data ??= await OpenSeededAsync(plan: Plan(), workouts: InProgress("w-now", entries));
        Wire(this, data, _notice);
        return (data, Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "w-now")));
    }

    private static WorkoutEntry SkippedEntry(string exerciseId, string? reason = null) =>
        new(exerciseId, EntryStatus.Skipped, reason, null, null, []);

    private static IElement MenuButton(IRenderedComponent<WorkoutLog> cut, string exercise) => cut.Find($"button[aria-label='Menu for {exercise}']");

    private static IElement Item(IRenderedComponent<WorkoutLog> cut, string text) =>
        cut.FindAll("[role='dialog'] .fa-menuitem").Single(i => i.TextContent.Contains(text));

    private static IElement Reason(IRenderedComponent<WorkoutLog> cut, string label) =>
        cut.FindAll("[role='dialog'] [role='group'][aria-label='Reason'] button").Single(b => b.TextContent.Trim() == label);

    private static IElement SheetButton(IRenderedComponent<WorkoutLog> cut, string label) =>
        cut.FindAll("[role='dialog'] .fa-sheet__actions button").Single(b => b.TextContent.Trim() == label);

    private static string ShownExercise(IRenderedComponent<WorkoutLog> cut) => cut.Find(".fa-exercise__name").TextContent;

    private static string Primary(IRenderedComponent<WorkoutLog> cut) => cut.Find(".fa-confirmbar .fa-btn--primary").TextContent.Trim();

    private static Workout Stored(AppData data) => data.Snapshot.Workouts.Single(w => w.Id == "w-now");

    private static void OpenSkipSheet(IRenderedComponent<WorkoutLog> cut, string exercise)
    {
        MenuButton(cut, exercise).Click();
        Item(cut, "Skip Exercise").Click();
    }

    // ---- Skipping

    [Fact]
    public async Task The_skip_sheet_offers_the_four_reasons_and_a_plain_Skip_button()
    {
        var (_, cut) = await OpenAsync();

        OpenSkipSheet(cut, "Goblet squat");

        Assert.Single(cut.FindAll("[role='dialog']"));
        var reasons = cut.FindAll("[role='dialog'] [role='group'][aria-label='Reason'] button").Select(b => b.TextContent.Trim());
        Assert.Equal(["Short on time", "Pain", "No equipment", "Other"], reasons);
        Assert.NotNull(SheetButton(cut, "Skip"));
    }

    [Fact]
    public async Task Skipping_without_a_reason_saves_a_skipped_entry_marks_the_pager_and_lands_on_the_next_exercise()
    {
        var (data, cut) = await OpenAsync();
        OpenSkipSheet(cut, "Goblet squat");

        SheetButton(cut, "Skip").Click();

        var entry = Assert.Single(Stored(data).Entries);
        Assert.Equal(new WorkoutEntry(Squat, EntryStatus.Skipped, null, null, null, []), entry);
        Assert.Contains("—", cut.FindAll(".pager__chip")[0].TextContent);
        Assert.Equal("Exercise 1: Goblet squat, skipped", cut.FindAll(".pager__chip")[0].GetAttribute("aria-label"));
        Assert.Equal("Walking lunge", ShownExercise(cut));
        Assert.Empty(cut.FindAll("[role='dialog']"));
    }

    [Fact]
    public async Task Skipping_with_a_reason_stores_it_and_the_Skipped_state_shows_it_with_no_Sets()
    {
        var (data, cut) = await OpenAsync();
        OpenSkipSheet(cut, "Goblet squat");
        Reason(cut, "No equipment").Click();
        Assert.Equal("true", Reason(cut, "No equipment").GetAttribute("aria-pressed"));

        SheetButton(cut, "Skip").Click();
        cut.FindAll(".pager__chip")[0].Click();

        Assert.Equal("No equipment", Stored(data).Entries.Single().SkipReason);
        var chip = cut.Find(".fa-chip--skipped");
        Assert.Contains("Skipped", chip.TextContent);
        Assert.Contains("No equipment", cut.Find(".fa-skipped").TextContent);
        Assert.Empty(cut.FindAll(".fa-setrow"));
        Assert.Empty(cut.FindAll(".fa-rpe"));
    }

    [Fact]
    public async Task Tapping_the_chosen_reason_again_clears_it()
    {
        var (data, cut) = await OpenAsync();
        OpenSkipSheet(cut, "Goblet squat");
        Reason(cut, "Short on time").Click();
        Reason(cut, "Short on time").Click();

        SheetButton(cut, "Skip").Click();

        Assert.Null(Stored(data).Entries.Single().SkipReason);
    }

    [Fact]
    public async Task Choosing_Pain_as_the_reason_does_not_create_a_Pain_check()
    {
        var (data, cut) = await OpenAsync();
        OpenSkipSheet(cut, "Goblet squat");
        Reason(cut, "Pain").Click();

        SheetButton(cut, "Skip").Click();

        Assert.Equal("Pain", Stored(data).Entries.Single().SkipReason);
        Assert.Null(Stored(data).PainCheck);
    }

    [Fact]
    public async Task Cancelling_the_skip_sheet_changes_nothing()
    {
        var (data, cut) = await OpenAsync();
        OpenSkipSheet(cut, "Goblet squat");
        Reason(cut, "Pain").Click();

        SheetButton(cut, "Cancel").Click();

        Assert.Empty(Stored(data).Entries);
        Assert.Empty(cut.FindAll("[role='dialog']"));
        Assert.Equal("Goblet squat", ShownExercise(cut));
    }

    [Fact]
    public async Task Skipping_the_last_Exercise_left_lands_where_the_primary_button_is_Finish()
    {
        var (data, cut) = await OpenAsync(null, SkippedEntry(Squat));
        Assert.Equal("Walking lunge", ShownExercise(cut));
        OpenSkipSheet(cut, "Walking lunge");

        SheetButton(cut, "Skip").Click();

        Assert.Equal(2, Stored(data).Entries.Count);
        Assert.Equal("Walking lunge", ShownExercise(cut));
        Assert.Equal("Finish", Primary(cut));
    }

    [Fact]
    public async Task A_skipped_Exercise_counts_as_handled_for_the_primary_button()
    {
        var squats = new WorkoutEntry(Squat, EntryStatus.Done, null, null, null, [Set("a", 20, 8), Set("b", 20, 8), Set("c", 20, 8)]);
        var (_, cut) = await OpenAsync(null, squats, SkippedEntry(Lunge, "Pain"));

        Assert.Equal("Walking lunge", ShownExercise(cut));
        Assert.Equal("Finish", Primary(cut));
        cut.FindAll(".pager__chip")[0].Click();
        Assert.Equal("Next: Walking lunge", Primary(cut));
    }

    // ---- Un-skipping

    [Fact]
    public async Task Unskip_restores_the_Exercise_as_not_yet_done()
    {
        var (data, cut) = await OpenAsync(null, SkippedEntry(Squat, "Pain"));
        cut.FindAll(".pager__chip")[0].Click();
        MenuButton(cut, "Goblet squat").Click();

        Item(cut, "Un-skip").Click();

        Assert.Empty(Stored(data).Entries);
        Assert.Empty(cut.FindAll(".fa-chip--skipped"));
        Assert.Equal(3, cut.FindAll(".fa-setrow").Count);
        Assert.DoesNotContain("—", cut.FindAll(".pager__chip")[0].TextContent);
    }

    // ---- Skipping an Exercise that has Sets

    [Fact]
    public async Task Skipping_an_Exercise_with_Sets_asks_first_and_Cancel_changes_nothing()
    {
        var done = new WorkoutEntry(Squat, EntryStatus.Done, null, null, null, [Set("s1", 20, 8)]);
        var (data, cut) = await OpenAsync(null, done);
        OpenSkipSheet(cut, "Goblet squat");

        SheetButton(cut, "Skip").Click();

        Assert.Contains("Sets will be removed", cut.Find("[role='dialog']").TextContent);
        Assert.Equal(EntryStatus.Done, Stored(data).Entries.Single().Status);

        SheetButton(cut, "Cancel").Click();

        Assert.Equal(done, Stored(data).Entries.Single());
        Assert.Empty(cut.FindAll("[role='dialog']"));
    }

    [Fact]
    public async Task Confirming_the_removal_skips_the_Exercise_and_drops_its_Sets()
    {
        var done = new WorkoutEntry(Squat, EntryStatus.Done, null, null, null, [Set("s1", 20, 8)]);
        var (data, cut) = await OpenAsync(null, done);
        OpenSkipSheet(cut, "Goblet squat");
        Reason(cut, "Short on time").Click();
        SheetButton(cut, "Skip").Click();

        SheetButton(cut, "Remove Sets and skip").Click();

        var entry = Assert.Single(Stored(data).Entries);
        Assert.Equal(EntryStatus.Skipped, entry.Status);
        Assert.Equal("Short on time", entry.SkipReason);
        Assert.Empty(entry.Sets);
    }

    // ---- Failure, read-only, reload

    [Fact]
    public async Task A_failed_skip_shows_the_banner_and_leaves_the_screen_unchanged()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(store, Plan(), InProgress());
        var (_, cut) = await OpenAsync(data);
        OpenSkipSheet(cut, "Goblet squat");
        store.FailAllWrites(StoreFailureKind.StorageFull);

        SheetButton(cut, "Skip").Click();

        Assert.Contains("out of storage", _notice.Message);
        Assert.Empty(Stored(data).Entries);
        Assert.Empty(cut.FindAll(".fa-chip--skipped"));
        Assert.Equal("Goblet squat", ShownExercise(cut));
        Assert.DoesNotContain("—", cut.FindAll(".pager__chip")[0].TextContent);
    }

    [Fact]
    public async Task When_the_window_cannot_save_the_Skip_item_is_disabled()
    {
        var store = new InMemoryDataStore();
        await OpenSeededAsync(store, Plan(), InProgress());
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var (_, cut) = await OpenAsync(locked);
        MenuButton(cut, "Goblet squat").Click();

        Assert.True(Item(cut, "Skip Exercise").HasAttribute("disabled"));
    }

    [Fact]
    public async Task A_skip_survives_reload()
    {
        var (data, cut) = await OpenAsync();
        OpenSkipSheet(cut, "Goblet squat");
        Reason(cut, "Other").Click();
        SheetButton(cut, "Skip").Click();

        using var reopened = new BunitContext();
        Wire(reopened, data, new SaveFailureNotice());
        var again = reopened.Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "w-now"));

        Assert.Contains("—", again.FindAll(".pager__chip")[0].TextContent);
        Assert.Equal("Walking lunge", again.Find(".fa-exercise__name").TextContent);
        again.FindAll(".pager__chip")[0].Click();
        Assert.Contains("Other", again.Find(".fa-skipped").TextContent);
    }
}
