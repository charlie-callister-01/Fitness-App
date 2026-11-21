using AngleSharp.Dom;
using Bunit;
using Core.Model;
using Core.Storage;
using Core.Tests.Storage;
using Ui.Pages;
using Ui.Saving;
using static Ui.Tests.Pages.LogFixture;

namespace Ui.Tests.Pages;

/// <summary>Swap and Add an Exercise on the log page (design 8.4, C-14): the picker, the swapped and added states, undo and remove.</summary>
public sealed class WorkoutLogSwapAddTests : BunitContext
{
    private const string Rdl = "rdl";
    private const string Press = "press";

    private readonly SaveFailureNotice _notice = new();

    private static ExerciseTags Tags(string muscle, string equipment) => new([muscle], [equipment], []);

    /// <summary>The fixture library plus a Romanian deadlift (shares Goblet squat's tags) and a Shoulder press (shares none).</summary>
    private static async Task<AppData> SeedAsync(IDataStore? store = null, int extraLibrary = 0, params Workout[] history)
    {
        var data = await OpenSeededAsync(store, Plan(), InProgress());
        var changes = new List<Change>
        {
            Change.Put(new Exercise(Squat, "Goblet squat", LogKind.Load, Tags("legs", "kettlebell"), false)),
            Change.Put(new Exercise(Rdl, "Romanian deadlift", LogKind.Load, Tags("legs", "kettlebell"), false)),
            Change.Put(new Exercise(Press, "Shoulder press", LogKind.Load, Tags("shoulders", "dumbbell"), false)),
        };
        changes.AddRange(history.Select(Change.Put));
        changes.AddRange(Enumerable.Range(0, extraLibrary).Select(i => Change.Put(new Exercise($"x{i:00}", $"Filler {i:00}", LogKind.Reps, null, false))));
        Assert.IsType<CommitResult.Committed>(await data.CommitAsync(changes));
        return data;
    }

    private async Task<IRenderedComponent<WorkoutLog>> OpenAsync(AppData? data = null, params WorkoutEntry[] entries)
    {
        data ??= await SeedAsync();
        if (entries.Length > 0)
        {
            var open = data.Snapshot.Workouts.Single(w => w.Id == "w-now");
            Assert.IsType<CommitResult.Committed>(await data.CommitAsync([Change.Put(open with { Entries = entries })]));
        }

        Wire(this, data, _notice);
        return Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "w-now"));
    }

    private static WorkoutEntry SwappedEntry(string newId, string originalId, params WorkoutSet[] sets) =>
        new(newId, EntryStatus.Swapped, null, originalId, null, sets);

    private static IElement MenuButton(IRenderedComponent<WorkoutLog> cut, string exercise) => cut.Find($"button[aria-label='Menu for {exercise}']");

    private static IElement Item(IRenderedComponent<WorkoutLog> cut, string text) =>
        cut.FindAll("[role='dialog'] .fa-menuitem").Single(i => i.TextContent.Contains(text));

    private static void OpenPicker(IRenderedComponent<WorkoutLog> cut, string exercise, string item)
    {
        MenuButton(cut, exercise).Click();
        Item(cut, item).Click();
    }

    private static IReadOnlyList<string> PickerNames(IRenderedComponent<WorkoutLog> cut) =>
        [.. cut.FindAll("[role='dialog'] .fa-picker__row-name").Select(n => n.TextContent.Trim())];

    private static IElement PickerRow(IRenderedComponent<WorkoutLog> cut, string name) =>
        cut.FindAll("[role='dialog'] .fa-picker__row").Single(r => r.TextContent.Contains(name));

    private static IElement SheetButton(IRenderedComponent<WorkoutLog> cut, string label) =>
        cut.FindAll("[role='dialog'] .fa-sheet__actions button").Single(b => b.TextContent.Trim() == label);

    private static string ShownExercise(IRenderedComponent<WorkoutLog> cut) => cut.Find(".fa-exercise__name").TextContent;

    private static string Primary(IRenderedComponent<WorkoutLog> cut) => cut.Find(".fa-confirmbar .fa-btn--primary").TextContent.Trim();

    private static Workout Stored(AppData data) => data.Snapshot.Workouts.Single(w => w.Id == "w-now");

    // ---- The picker

    [Fact]
    public async Task The_swap_picker_is_a_labelled_dialog_listing_Similar_first_then_All_without_Exercises_already_in_the_Workout()
    {
        var cut = await OpenAsync();

        OpenPicker(cut, "Goblet squat", "Swap Exercise");

        var dialog = cut.Find("[role='dialog']");
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
        Assert.Equal("Swap Goblet squat", cut.Find("[role='dialog'] .fa-sheet__title").TextContent);
        var groups = cut.FindAll("[role='dialog'] .fa-picker__group").Select(g => g.TextContent.Trim());
        Assert.Equal(["Similar", "All Exercises"], groups);
        Assert.Equal(["Romanian deadlift", "Shoulder press", "Journal", "Plank", "Pull-up"], PickerNames(cut));
        Assert.DoesNotContain("Walking lunge", PickerNames(cut));
        Assert.DoesNotContain("Goblet squat", PickerNames(cut));
    }

    [Fact]
    public async Task A_picker_row_shows_an_icon_the_name_and_its_tags()
    {
        var cut = await OpenAsync();
        OpenPicker(cut, "Goblet squat", "Swap Exercise");

        var row = PickerRow(cut, "Romanian deadlift");

        Assert.NotNull(row.QuerySelector("svg"));
        Assert.Contains("legs", row.TextContent);
        Assert.Contains("kettlebell", row.TextContent);
    }

    [Fact]
    public async Task Cancel_closes_the_picker_and_changes_nothing()
    {
        var data = await SeedAsync();
        var cut = await OpenAsync(data);
        OpenPicker(cut, "Goblet squat", "Swap Exercise");

        SheetButton(cut, "Cancel").Click();

        Assert.Empty(cut.FindAll("[role='dialog']"));
        Assert.Empty(Stored(data).Entries);
        Assert.Equal("Goblet squat", ShownExercise(cut));
    }

    [Fact]
    public async Task A_small_library_has_no_search_box()
    {
        var cut = await OpenAsync();

        OpenPicker(cut, "Goblet squat", "Swap Exercise");

        Assert.Empty(cut.FindAll("[role='dialog'] input[type='search']"));
    }

    [Fact]
    public async Task A_library_of_more_than_20_Exercises_adds_a_search_that_filters_the_list()
    {
        var cut = await OpenAsync(await SeedAsync(extraLibrary: 20));
        OpenPicker(cut, "Goblet squat", "Swap Exercise");

        var search = cut.Find("[role='dialog'] input[type='search']");
        Assert.Equal("Search Exercises", search.GetAttribute("aria-label"));
        search.Input("deadl");

        Assert.Equal(["Romanian deadlift"], PickerNames(cut));
    }

    [Fact]
    public async Task A_search_with_no_match_says_so_and_offers_a_disabled_Add_a_new_Exercise_for_the_library_stage()
    {
        var cut = await OpenAsync(await SeedAsync(extraLibrary: 20));
        OpenPicker(cut, "Goblet squat", "Swap Exercise");

        cut.Find("[role='dialog'] input[type='search']").Input("zzzz");

        Assert.Contains("No match.", cut.Find("[role='dialog'] .fa-picker__empty").TextContent);
        var add = cut.Find("[role='dialog'] .fa-picker__empty button");
        Assert.Contains("Add a new Exercise", add.TextContent);
        Assert.True(add.HasAttribute("disabled"));
        Assert.Contains("Exercise library", cut.Find("[role='dialog'] .fa-picker__empty").TextContent);
    }

    // ---- Swapping

    [Fact]
    public async Task Swapping_saves_a_swapped_entry_for_the_original_and_keeps_the_planned_sets()
    {
        var data = await SeedAsync();
        var cut = await OpenAsync(data);
        OpenPicker(cut, "Goblet squat", "Swap Exercise");

        PickerRow(cut, "Romanian deadlift").Click();

        var entry = Assert.Single(Stored(data).Entries);
        Assert.Equal(new WorkoutEntry(Rdl, EntryStatus.Swapped, null, Squat, null, []), entry);
        Assert.Equal(3, Stored(data).Planned[0].Sets);
        Assert.Equal("Romanian deadlift", ShownExercise(cut));
        Assert.Equal(3, cut.FindAll(".fa-setrow").Count);
        Assert.Empty(cut.FindAll("[role='dialog']"));
    }

    [Fact]
    public async Task A_swapped_Exercise_says_it_was_the_original_and_the_pager_shows_the_swap_glyph()
    {
        var cut = await OpenAsync();
        OpenPicker(cut, "Goblet squat", "Swap Exercise");
        PickerRow(cut, "Romanian deadlift").Click();

        Assert.Contains("Swapped", cut.Find(".fa-swapped .fa-chip--swapped").TextContent);
        Assert.Contains("was Goblet squat", cut.Find(".fa-swapped").TextContent);
        var chip = cut.FindAll(".pager__chip")[0];
        Assert.Contains("⇄", chip.TextContent);
        Assert.Equal("Exercise 1: Romanian deadlift, swapped", chip.GetAttribute("aria-label"));
    }

    [Fact]
    public async Task The_swapped_Exercises_header_uses_its_own_history_not_the_originals()
    {
        var earlier = new Workout("w-old", Today.AddDays(-7), "lower", false, Now.AddDays(-7), Now.AddDays(-7).AddHours(1), [],
            [
                new WorkoutEntry(Squat, EntryStatus.Done, null, null, null, [Set("q1", 20, 8)]),
                new WorkoutEntry(Rdl, EntryStatus.Done, null, null, null, [Set("r1", 61, 5)]),
            ], null);
        var cut = await OpenAsync(await SeedAsync(history: earlier));
        Assert.Contains("20", cut.Find(".fa-exercise__lastbest").TextContent);

        OpenPicker(cut, "Goblet squat", "Swap Exercise");
        PickerRow(cut, "Romanian deadlift").Click();

        var lastBest = cut.Find(".fa-exercise__lastbest").TextContent;
        Assert.Contains("61", lastBest);
        Assert.DoesNotContain("20", lastBest);
    }

    [Fact]
    public async Task A_swapped_Exercise_is_logged_into_its_swapped_entry_and_counts_as_handled_for_the_primary_button_once_it_has_a_Set()
    {
        var data = await SeedAsync();
        var lunges = new WorkoutEntry(Lunge, EntryStatus.Done, null, null, null, [Set("l1", 10, 10), Set("l2", 10, 10)]);
        var cut = await OpenAsync(data, lunges);
        OpenPicker(cut, "Goblet squat", "Swap Exercise");
        PickerRow(cut, "Romanian deadlift").Click();

        cut.FindAll(".pager__chip")[1].Click();
        Assert.Equal("Walking lunge", ShownExercise(cut));
        Assert.Equal("Next: Romanian deadlift", Primary(cut));

        cut.FindAll(".pager__chip")[0].Click();
        cut.Find("input[aria-label='Weight, Set 1']").Input("40");
        cut.Find("input[aria-label='Reps, Set 1']").Input("5");
        cut.Find(".fa-confirmbar .fa-btn--primary").Click();
        cut.FindAll(".pager__chip")[1].Click();
        Assert.Equal("Finish", Primary(cut));

        var stored = Stored(data);
        Assert.Equal(2, stored.Entries.Count);
        var swapped = stored.Entries.Single(e => e.Status == EntryStatus.Swapped);
        Assert.Equal(Rdl, swapped.ExerciseId);
        Assert.Equal(Squat, swapped.SwappedFromExerciseId);
        Assert.Single(swapped.Sets);
    }

    [Fact]
    public async Task Undo_swap_restores_the_original_as_not_yet_done()
    {
        var data = await SeedAsync();
        var cut = await OpenAsync(data, SwappedEntry(Rdl, Squat));
        Assert.Equal("Romanian deadlift", ShownExercise(cut));
        MenuButton(cut, "Romanian deadlift").Click();

        Item(cut, "Undo swap").Click();

        Assert.Empty(Stored(data).Entries);
        Assert.Equal("Goblet squat", ShownExercise(cut));
        Assert.Empty(cut.FindAll(".fa-swapped"));
        Assert.DoesNotContain("⇄", cut.FindAll(".pager__chip")[0].TextContent);
    }

    private static WorkoutEntry LungeDone() =>
        new(Lunge, EntryStatus.Done, null, null, null, [Set("l1", 10, 10), Set("l2", 10, 10)]);

    [Fact]
    public async Task An_unlogged_swap_is_not_handled_so_the_last_Exercise_offers_Next_back_to_it_not_Finish()
    {
        var cut = await OpenAsync(await SeedAsync(), SwappedEntry(Rdl, Squat), LungeDone());
        cut.FindAll(".pager__chip")[1].Click();

        Assert.Equal("Next: Romanian deadlift", Primary(cut));
        Assert.Contains("1 of 2 done", cut.Find(".log__progress").TextContent);
    }

    [Fact]
    public async Task A_swap_with_a_Set_is_handled_so_the_last_Exercise_offers_Finish()
    {
        var cut = await OpenAsync(await SeedAsync(), SwappedEntry(Rdl, Squat, Set("r1", 40, 5)), LungeDone());
        cut.FindAll(".pager__chip")[1].Click();

        Assert.Equal("Finish", Primary(cut));
    }

    [Fact]
    public async Task Undo_swap_is_not_offered_once_a_Set_is_logged()
    {
        var cut = await OpenAsync(await SeedAsync(), SwappedEntry(Rdl, Squat, Set("r1", 40, 5)));
        MenuButton(cut, "Romanian deadlift").Click();

        Assert.True(Item(cut, "Undo swap").HasAttribute("disabled"));
    }

    [Fact]
    public async Task Swapping_an_Exercise_that_has_Sets_asks_first_and_Cancel_changes_nothing()
    {
        var data = await SeedAsync();
        var done = new WorkoutEntry(Squat, EntryStatus.Done, null, null, null, [Set("s1", 20, 8)]);
        var cut = await OpenAsync(data, done);
        OpenPicker(cut, "Goblet squat", "Swap Exercise");

        PickerRow(cut, "Romanian deadlift").Click();

        Assert.Contains("Sets will be removed", cut.Find("[role='dialog']").TextContent);
        Assert.Equal(done, Stored(data).Entries.Single());

        SheetButton(cut, "Cancel").Click();

        Assert.Equal(done, Stored(data).Entries.Single());
        Assert.Empty(cut.FindAll("[role='dialog']"));
    }

    [Fact]
    public async Task Confirming_the_removal_swaps_the_Exercise_and_drops_its_Sets()
    {
        var data = await SeedAsync();
        var done = new WorkoutEntry(Squat, EntryStatus.Done, null, null, null, [Set("s1", 20, 8)]);
        var cut = await OpenAsync(data, done);
        OpenPicker(cut, "Goblet squat", "Swap Exercise");
        PickerRow(cut, "Romanian deadlift").Click();

        SheetButton(cut, "Remove Sets and swap").Click();

        var entry = Assert.Single(Stored(data).Entries);
        Assert.Equal(new WorkoutEntry(Rdl, EntryStatus.Swapped, null, Squat, null, []), entry);
    }

    // ---- Adding

    [Fact]
    public async Task Add_an_Exercise_opens_the_picker_with_its_own_title()
    {
        var cut = await OpenAsync();

        OpenPicker(cut, "Goblet squat", "Add an Exercise");

        Assert.Equal("Add an Exercise", cut.Find("[role='dialog'] .fa-sheet__title").TextContent);
        Assert.Contains("Romanian deadlift", PickerNames(cut));
    }

    [Fact]
    public async Task An_added_Exercise_goes_after_the_current_one_marked_Added_with_one_empty_Set_row()
    {
        var data = await SeedAsync();
        var cut = await OpenAsync(data);
        OpenPicker(cut, "Goblet squat", "Add an Exercise");

        PickerRow(cut, "Shoulder press").Click();

        var entry = Assert.Single(Stored(data).Entries);
        Assert.Equal(new WorkoutEntry(Press, EntryStatus.Done, null, null, null, []), entry);
        Assert.Equal("Shoulder press", ShownExercise(cut));
        Assert.Contains("+ Added", cut.Find(".fa-added").TextContent);
        Assert.Single(cut.FindAll(".fa-setrow"));
        var names = cut.FindAll(".pager__chip").Select(c => c.GetAttribute("aria-label"));
        Assert.Equal(["Exercise 1: Goblet squat", "Exercise 2: Shoulder press", "Exercise 3: Walking lunge"], names);
    }

    [Fact]
    public async Task An_added_Exercise_can_be_logged_like_any_other()
    {
        var data = await SeedAsync();
        var cut = await OpenAsync(data);
        OpenPicker(cut, "Goblet squat", "Add an Exercise");
        PickerRow(cut, "Shoulder press").Click();

        cut.Find("input[aria-label='Weight, Set 1']").Input("30");
        cut.Find("input[aria-label='Reps, Set 1']").Input("8");
        cut.Find(".fa-confirmbar .fa-btn--primary").Click();

        var entry = Stored(data).Entries.Single();
        Assert.Equal(Press, entry.ExerciseId);
        Assert.Equal(EntryStatus.Done, entry.Status);
        Assert.Single(entry.Sets);
    }

    [Fact]
    public async Task An_added_Exercise_with_no_Sets_can_be_removed()
    {
        var data = await SeedAsync();
        var cut = await OpenAsync(data, new WorkoutEntry(Press, EntryStatus.Done, null, null, null, []));
        cut.FindAll(".pager__chip")[1].Click();
        Assert.Equal("Shoulder press", ShownExercise(cut));
        MenuButton(cut, "Shoulder press").Click();

        Item(cut, "Remove Exercise").Click();

        Assert.Empty(Stored(data).Entries);
        Assert.Equal(2, cut.FindAll(".pager__chip").Count);
        Assert.Empty(cut.FindAll(".fa-added"));
    }

    [Fact]
    public async Task An_added_Exercise_with_Sets_cannot_be_removed()
    {
        var added = new WorkoutEntry(Press, EntryStatus.Done, null, null, null, [Set("p1", 30, 8)]);
        var cut = await OpenAsync(await SeedAsync(), added);
        cut.FindAll(".pager__chip")[1].Click();
        MenuButton(cut, "Shoulder press").Click();

        Assert.True(Item(cut, "Remove Exercise").HasAttribute("disabled"));
    }

    [Fact]
    public async Task An_added_Exercise_with_no_Sets_does_not_hold_back_Finish()
    {
        var squats = new WorkoutEntry(Squat, EntryStatus.Done, null, null, null, [Set("a", 20, 8), Set("b", 20, 8), Set("c", 20, 8)]);
        var lunges = new WorkoutEntry(Lunge, EntryStatus.Done, null, null, null, [Set("d", 10, 10), Set("e", 10, 10)]);
        var cut = await OpenAsync(await SeedAsync(), squats, new WorkoutEntry(Press, EntryStatus.Done, null, null, null, []), lunges);

        cut.FindAll(".pager__chip")[2].Click();

        Assert.Equal("Finish", Primary(cut));
    }

    // ---- Failure, read-only, reload

    [Fact]
    public async Task A_failed_swap_shows_the_banner_and_leaves_the_screen_unchanged()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await SeedAsync(store);
        var cut = await OpenAsync(data);
        OpenPicker(cut, "Goblet squat", "Swap Exercise");
        store.FailAllWrites(StoreFailureKind.StorageFull);

        PickerRow(cut, "Romanian deadlift").Click();

        Assert.Contains("out of storage", _notice.Message);
        Assert.Empty(Stored(data).Entries);
        Assert.Equal("Goblet squat", ShownExercise(cut));
        Assert.Empty(cut.FindAll(".fa-swapped"));
    }

    [Fact]
    public async Task A_failed_add_shows_the_banner_and_leaves_the_screen_unchanged()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await SeedAsync(store);
        var cut = await OpenAsync(data);
        OpenPicker(cut, "Goblet squat", "Add an Exercise");
        store.FailAllWrites(StoreFailureKind.StorageFull);

        PickerRow(cut, "Shoulder press").Click();

        Assert.Contains("out of storage", _notice.Message);
        Assert.Empty(Stored(data).Entries);
        Assert.Equal(2, cut.FindAll(".pager__chip").Count);
    }

    [Fact]
    public async Task When_the_window_cannot_save_Swap_and_Add_are_disabled()
    {
        var store = new InMemoryDataStore();
        await SeedAsync(store);
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var cut = await OpenAsync(locked);
        MenuButton(cut, "Goblet squat").Click();

        Assert.True(Item(cut, "Swap Exercise").HasAttribute("disabled"));
        Assert.True(Item(cut, "Add an Exercise").HasAttribute("disabled"));
    }

    [Fact]
    public async Task A_swap_and_an_added_Exercise_survive_reload()
    {
        var data = await SeedAsync();
        var cut = await OpenAsync(data);
        OpenPicker(cut, "Goblet squat", "Swap Exercise");
        PickerRow(cut, "Romanian deadlift").Click();
        OpenPicker(cut, "Romanian deadlift", "Add an Exercise");
        PickerRow(cut, "Shoulder press").Click();

        using var reopened = new BunitContext();
        Wire(reopened, data, new SaveFailureNotice());
        var again = reopened.Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "w-now"));

        var labels = again.FindAll(".pager__chip").Select(c => c.GetAttribute("aria-label"));
        Assert.Equal(
            ["Exercise 1: Romanian deadlift, swapped", "Exercise 2: Shoulder press", "Exercise 3: Walking lunge"], labels);
        again.FindAll(".pager__chip")[1].Click();
        Assert.Contains("+ Added", again.Find(".fa-added").TextContent);
    }
}
