using AngleSharp.Dom;
using Bunit;
using Core.Model;
using Core.Serialisation;
using Core.Storage;
using Core.Tests.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Ui.Pages;
using Ui.Saving;
using static Ui.Tests.Pages.LogFixture;

namespace Ui.Tests.Pages;

public sealed class WorkoutLogTests : BunitContext
{
    private readonly SaveFailureNotice _notice = new();

    private async Task<IRenderedComponent<WorkoutLog>> OpenLogAsync(AppData? data = null, string id = "w-now", Workout? workout = null)
    {
        data ??= await OpenSeededAsync(plan: Plan(), workouts: workout ?? InProgress(id));
        Wire(this, data, _notice);
        return Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, id));
    }

    private static IElement Weight(IRenderedComponent<WorkoutLog> cut, int set) => cut.Find($"input[aria-label='Weight, set {set}']");

    private static IElement Reps(IRenderedComponent<WorkoutLog> cut, int set) => cut.Find($"input[aria-label='Reps, set {set}']");

    private static void Confirm(IRenderedComponent<WorkoutLog> cut) => cut.Find(".fa-confirmbar .fa-btn--primary").Click();

    private static Workout Stored(AppData data, string id = "w-now") => data.Snapshot.Workouts.Single(w => w.Id == id);

    private static IReadOnlyList<WorkoutSet> SquatSets(AppData data) =>
        Stored(data).Entries.Single(e => e.ExerciseId == Squat).Sets;

    // ---- Opening

    [Fact]
    public async Task Opens_with_the_session_name_progress_and_the_first_exercise_header()
    {
        var cut = await OpenLogAsync();

        Assert.Equal("Lower", cut.Find("h1").TextContent);
        Assert.Contains("0 of 2 done", cut.Find(".log__progress").TextContent);
        Assert.Contains("Goblet squat", cut.Find(".fa-exercise__name").TextContent);
        Assert.Contains("3 × 8", cut.Markup);
        Assert.Contains("Chest up", cut.Markup);
        Assert.Equal("Confirm Set", cut.Find(".fa-confirmbar .fa-btn--primary").TextContent.Trim());
        Assert.Empty(cut.FindAll(".log__notice")); // nothing logged yet: not resumed
    }

    [Fact]
    public async Task Rows_are_numbered_labelled_and_set_up_for_the_phone_keypad()
    {
        var cut = await OpenLogAsync();

        Assert.Equal(3, cut.FindAll(".fa-setrow").Count);
        Assert.Equal("decimal", Weight(cut, 2).GetAttribute("inputmode"));
        Assert.Equal("numeric", Reps(cut, 2).GetAttribute("inputmode"));
        Assert.Equal("this.select()", Weight(cut, 2).GetAttribute("onfocus"));
        Assert.Equal("this.select()", Reps(cut, 2).GetAttribute("onfocus"));
    }

    [Fact]
    public async Task With_no_history_the_fields_are_empty_and_the_planned_reps_are_the_hint()
    {
        var cut = await OpenLogAsync();

        Assert.Equal("", Weight(cut, 1).GetAttribute("value") ?? "");
        Assert.Equal("8", Reps(cut, 1).GetAttribute("placeholder"));
    }

    [Fact]
    public async Task Fields_are_prefilled_from_last_time_and_muted_until_confirmed_or_edited()
    {
        var data = await OpenSeededAsync(
            plan: Plan(),
            workouts:
            [
                Finished("w-old", Today.AddDays(-7), Set("o1", 20, 10), Set("o2", 22.5, 8)),
                InProgress(),
            ]);
        var cut = await OpenLogAsync(data);

        Assert.Equal("20", Weight(cut, 1).GetAttribute("value"));
        Assert.Equal("10", Reps(cut, 1).GetAttribute("value"));
        Assert.Equal("22.5", Weight(cut, 2).GetAttribute("value"));
        Assert.Contains("fa-input--pending", Weight(cut, 1).ClassName);

        Weight(cut, 1).Input("21");

        Assert.DoesNotContain("fa-input--pending", Weight(cut, 1).ClassName);
    }

    [Fact]
    public async Task Confirming_an_untouched_prefilled_row_saves_same_as_last()
    {
        var data = await OpenSeededAsync(
            plan: Plan(), workouts: [Finished("w-old", Today.AddDays(-7), Set("o1", 20, 10)), InProgress()]);
        var cut = await OpenLogAsync(data);

        Confirm(cut);

        var saved = Assert.Single(SquatSets(data));
        Assert.Equal(20, saved.WeightKg);
        Assert.Equal(10, saved.Reps);
    }

    // ---- Confirming

    [Fact]
    public async Task Typing_and_confirming_saves_the_set_in_canonical_units_and_shows_the_green_tick()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress());
        var cut = await OpenLogAsync(data);

        Weight(cut, 1).Input("22,5");
        Reps(cut, 1).Input("8");
        Confirm(cut);

        var saved = Assert.Single(SquatSets(data));
        Assert.Equal(22.5, saved.WeightKg);
        Assert.Equal(8, saved.Reps);
        Assert.False(saved.IsMax);
        Assert.Single(cut.FindAll(".fa-setrow--done"));
        Assert.NotNull(cut.Find("button[aria-label='Edit set 1']"));
        Assert.Contains("fa-setrow--current", cut.FindAll(".fa-setrow")[1].ClassName);
        Assert.Null(_notice.Message);
    }

    [Fact]
    public async Task Lb_users_type_pounds_and_the_stored_set_is_kilograms()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress());
        await data.CommitAsync(Change.Put(data.Snapshot.Settings with { DisplayWeightUnit = WeightUnit.Lb }));
        var cut = await OpenLogAsync(data);

        Weight(cut, 1).Input("100");
        Reps(cut, 1).Input("5");
        Confirm(cut);

        Assert.Equal(45.36, SquatSets(data).Single().WeightKg!.Value, 2);
    }

    [Fact]
    public async Task Enter_in_the_first_field_moves_to_the_second_and_Enter_in_the_second_confirms()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress());
        var cut = await OpenLogAsync(data);
        Weight(cut, 1).Input("20");
        Reps(cut, 1).Input("10");

        Weight(cut, 1).KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });
        Assert.Empty(data.Snapshot.Workouts.Single().Entries);
        JSInterop.VerifyFocusAsyncInvoke();

        Reps(cut, 1).KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });

        Assert.Single(SquatSets(data));
    }

    // ---- Validation

    [Fact]
    public async Task A_bad_value_shows_the_message_under_the_row_keeps_the_text_and_returns_focus_and_saves_nothing()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress());
        var cut = await OpenLogAsync(data);

        Weight(cut, 1).Input("abc");
        Reps(cut, 1).Input("8");
        Confirm(cut);

        Assert.NotEmpty(cut.Find(".fa-setrow__error").TextContent);
        Assert.Equal("true", Weight(cut, 1).GetAttribute("aria-invalid"));
        Assert.Null(Reps(cut, 1).GetAttribute("aria-invalid"));
        Assert.Equal("abc", Weight(cut, 1).GetAttribute("value"));
        Assert.Contains("fa-input--error", Weight(cut, 1).ClassName);
        Assert.Empty(data.Snapshot.Workouts.Single().Entries);
        JSInterop.VerifyFocusAsyncInvoke();
    }

    [Fact]
    public async Task A_bad_reps_value_marks_the_reps_field()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress());
        var cut = await OpenLogAsync(data);

        Weight(cut, 1).Input("20");
        Reps(cut, 1).Input("many");
        Confirm(cut);

        Assert.Equal("true", Reps(cut, 1).GetAttribute("aria-invalid"));
        Assert.Null(Weight(cut, 1).GetAttribute("aria-invalid"));
    }

    [Fact]
    public async Task A_blank_row_with_no_history_is_refused_and_nothing_is_checked_while_typing()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress());
        var cut = await OpenLogAsync(data);

        Weight(cut, 1).Input("abc");
        Assert.Empty(cut.FindAll(".fa-setrow__error"));

        Weight(cut, 1).Input("");
        Confirm(cut);

        Assert.Single(cut.FindAll(".fa-setrow__error"));
        Assert.Empty(data.Snapshot.Workouts.Single().Entries);
    }

    [Fact]
    public async Task Fixing_the_value_after_a_failed_confirm_saves_it_and_clears_the_message()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress());
        var cut = await OpenLogAsync(data);
        Weight(cut, 1).Input("abc");
        Reps(cut, 1).Input("8");
        Confirm(cut);

        Weight(cut, 1).Input("20");
        Confirm(cut);

        Assert.Empty(cut.FindAll(".fa-setrow__error"));
        Assert.Equal(20, SquatSets(data).Single().WeightKg);
    }

    // ---- Reopening

    [Fact]
    public async Task Tapping_the_green_tick_reopens_the_set_and_confirming_replaces_it_in_place()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress());
        var cut = await OpenLogAsync(data);
        Weight(cut, 1).Input("20");
        Reps(cut, 1).Input("10");
        Confirm(cut);
        var originalId = SquatSets(data).Single().Id;

        cut.Find("button[aria-label='Edit set 1']").Click();
        Assert.False(Weight(cut, 1).HasAttribute("readonly"));
        Reps(cut, 1).Input("9");
        Confirm(cut);

        var saved = Assert.Single(SquatSets(data));
        Assert.Equal(originalId, saved.Id);
        Assert.Equal(9, saved.Reps);
        Assert.Equal(20, saved.WeightKg);
        Assert.NotNull(cut.Find("button[aria-label='Edit set 1']"));
    }

    // ---- Next and Finish

    [Fact]
    public async Task When_the_exercise_is_complete_the_primary_says_Next_with_the_next_exercise_then_Finish_ends_the_workout()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress());
        var cut = await OpenLogAsync(data);
        for (var set = 1; set <= 3; set++)
        {
            Weight(cut, set).Input("20");
            Reps(cut, set).Input("8");
            Confirm(cut);
        }

        Assert.Equal("Next: Walking lunge", cut.Find(".fa-confirmbar .fa-btn--primary").TextContent.Trim());
        Assert.Contains("1 of 2 done", cut.Find(".log__progress").TextContent);

        Confirm(cut);
        Assert.Contains("Walking lunge", cut.Find(".fa-exercise__name").TextContent);
        for (var set = 1; set <= 2; set++)
        {
            Weight(cut, set).Input("10");
            Reps(cut, set).Input("10");
            Confirm(cut);
        }

        Assert.Equal("Finish", cut.Find(".fa-confirmbar .fa-btn--primary").TextContent.Trim());
        Confirm(cut);

        Assert.Equal(Now, Stored(data).EndedAt);
        Assert.Equal(Services.GetRequiredService<NavigationManager>().BaseUri, Services.GetRequiredService<NavigationManager>().Uri);
    }

    // ---- Pager, Up next, jumping

    private static string PrimaryLabel(IRenderedComponent<WorkoutLog> cut) => cut.Find(".fa-confirmbar .fa-btn--primary").TextContent.Trim();

    private static void ConfirmSets(IRenderedComponent<WorkoutLog> cut, int count, string kg, string reps)
    {
        for (var set = 1; set <= count; set++)
        {
            Weight(cut, set).Input(kg);
            Reps(cut, set).Input(reps);
            Confirm(cut);
        }
    }

    [Fact]
    public async Task The_pager_has_a_numbered_chip_per_exercise_marks_the_current_and_ticks_complete_ones()
    {
        var cut = await OpenLogAsync();

        var chips = cut.FindAll(".pager__chip");
        Assert.Equal(2, chips.Count);
        Assert.Equal("step", chips[0].GetAttribute("aria-current"));
        Assert.Null(chips[1].GetAttribute("aria-current"));
        Assert.Empty(cut.FindAll(".pager__chip .pager__done"));

        ConfirmSets(cut, 3, "20", "8");

        Assert.Contains("✓", cut.FindAll(".pager__chip")[0].TextContent);
        Assert.Equal("Exercise 1: Goblet squat, done", cut.FindAll(".pager__chip")[0].GetAttribute("aria-label"));
        Assert.DoesNotContain("✓", cut.FindAll(".pager__chip")[1].TextContent);
    }

    [Fact]
    public async Task Tapping_a_chip_jumps_and_coming_back_keeps_confirmed_sets_and_unconfirmed_typing()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress());
        var cut = await OpenLogAsync(data);
        Weight(cut, 1).Input("20");
        Reps(cut, 1).Input("8");
        Confirm(cut);
        Weight(cut, 2).Input("25");

        cut.FindAll(".pager__chip")[1].Click();
        Assert.Contains("Walking lunge", cut.Find(".fa-exercise__name").TextContent);
        Assert.Equal("step", cut.FindAll(".pager__chip")[1].GetAttribute("aria-current"));

        cut.FindAll(".pager__chip")[0].Click();
        Assert.Contains("Goblet squat", cut.Find(".fa-exercise__name").TextContent);
        Assert.Single(cut.FindAll(".fa-setrow--done"));
        Assert.Equal("25", Weight(cut, 2).GetAttribute("value"));
        Assert.Single(SquatSets(data));
    }

    [Fact]
    public async Task The_up_next_strip_names_the_following_exercise_and_gives_way_on_the_last()
    {
        var cut = await OpenLogAsync();
        Assert.Equal("Up next: Walking lunge", cut.Find(".fa-confirmbar__info").TextContent.Trim());

        cut.FindAll(".pager__chip")[1].Click();

        Assert.Empty(cut.FindAll(".fa-confirmbar__info"));
    }

    [Fact]
    public async Task The_strip_says_the_last_exercise_is_done_once_every_exercise_is_complete()
    {
        var cut = await OpenLogAsync();
        ConfirmSets(cut, 3, "20", "8");
        Confirm(cut);
        ConfirmSets(cut, 2, "10", "10");

        Assert.Equal("Last Exercise done", cut.Find(".fa-confirmbar__info").TextContent.Trim());
        Assert.Equal("Finish", PrimaryLabel(cut));
    }

    [Fact]
    public async Task The_primary_label_goes_Confirm_Set_then_Next_then_Finish()
    {
        var cut = await OpenLogAsync();
        Assert.Equal("Confirm Set", PrimaryLabel(cut));

        ConfirmSets(cut, 2, "20", "8");
        Assert.Equal("Confirm Set", PrimaryLabel(cut));
        Weight(cut, 3).Input("20");
        Reps(cut, 3).Input("8");
        Confirm(cut);
        Assert.Equal("Next: Walking lunge", PrimaryLabel(cut));

        Confirm(cut);
        Assert.Equal("Confirm Set", PrimaryLabel(cut));
        ConfirmSets(cut, 2, "10", "10");
        Assert.Equal("Finish", PrimaryLabel(cut));
    }

    [Fact]
    public async Task Finish_stamps_the_end_once_leaves_the_log_and_Home_no_longer_has_a_workout_in_progress()
    {
        var squats = Enumerable.Range(1, 3).Select(i => Set($"s{i}", 20, 8)).ToArray();
        var lunges = Enumerable.Range(1, 2).Select(i => Set($"l{i}", 10, 10)).ToArray();
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress("w-now",
            new WorkoutEntry(Squat, EntryStatus.Done, null, null, null, squats),
            new WorkoutEntry(Lunge, EntryStatus.Done, null, null, null, lunges)));
        var cut = await OpenLogAsync(data);

        Assert.Equal("Finish", PrimaryLabel(cut));
        Confirm(cut);
        Assert.Equal(Now, Stored(data).EndedAt);
        Assert.Empty(cut.FindAll(".fa-confirmbar"));
        Assert.DoesNotContain(data.Snapshot.Workouts, w => w.EndedAt is null);
    }

    // ---- Resuming

    [Fact]
    public async Task Reloading_restores_the_log_lands_on_the_first_incomplete_exercise_and_says_Resumed()
    {
        var squats = Enumerable.Range(1, 3).Select(i => Set($"s{i}", 20, 8)).ToArray();
        var started = InProgress("w-now", new WorkoutEntry(Squat, EntryStatus.Done, null, null, null, squats));
        var cut = await OpenLogAsync(workout: started);

        Assert.Contains("Walking lunge", cut.Find(".fa-exercise__name").TextContent);
        Assert.Contains("Resumed where you left off", cut.Find(".log__notice").TextContent);
        Assert.Contains("1 of 2 done", cut.Find(".log__progress").TextContent);
    }

    [Fact]
    public async Task A_reopened_workout_shows_its_confirmed_sets_as_done_and_continues_at_the_next_row()
    {
        var started = InProgress("w-now", new WorkoutEntry(Squat, EntryStatus.Done, null, null, null, [Set("s1", 22.5, 8)]));
        var cut = await OpenLogAsync(workout: started);

        Assert.Equal("22.5", Weight(cut, 1).GetAttribute("value"));
        Assert.Single(cut.FindAll(".fa-setrow--done"));
        Assert.Contains("fa-setrow--current", cut.FindAll(".fa-setrow")[1].ClassName);
        Assert.Contains("Resumed", cut.Markup);
    }

    // ---- Unknown, finished

    [Fact]
    public async Task An_unknown_workout_id_says_so_with_a_way_home()
    {
        var cut = await OpenLogAsync(id: "nope", workout: InProgress("w-other"));

        Assert.Contains("Workout not found", cut.Find("h1").TextContent);
        Assert.Equal("./", cut.Find("a.fa-btn").GetAttribute("href"));
        Assert.Empty(cut.FindAll(".fa-setrow"));
    }

    [Fact]
    public async Task A_finished_workout_is_not_opened_for_logging()
    {
        var finished = InProgress("w-done") with { EndedAt = Now.AddHours(1) };
        var cut = await OpenLogAsync(id: "w-done", workout: finished);

        Assert.Contains("Workout not found", cut.Find("h1").TextContent);
        Assert.Empty(cut.FindAll(".fa-setrow"));
    }

    // ---- Merging into the latest Workout

    [Fact]
    public async Task A_change_is_merged_into_the_latest_workout_not_the_one_the_page_first_saw()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress());
        var cut = await OpenLogAsync(data);
        var lungeEntry = new WorkoutEntry(Lunge, EntryStatus.Done, null, null, 8, [Set("other", 10, 10)]);
        await data.CommitAsync(Change.Put(Stored(data) with { Entries = [lungeEntry] }));

        Weight(cut, 1).Input("20");
        Reps(cut, 1).Input("8");
        Confirm(cut);

        var entries = Stored(data).Entries;
        Assert.Equal(2, entries.Count);
        Assert.Contains(entries, e => e.ExerciseId == Lunge && e.Rpe == 8);
        Assert.Contains(entries, e => e.ExerciseId == Squat);
    }

    // ---- Failure and read-only

    [Fact]
    public async Task A_failed_save_keeps_the_typed_entry_reports_the_failure_and_a_retry_succeeds()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(store, Plan(), InProgress());
        var cut = await OpenLogAsync(data);
        Weight(cut, 1).Input("22");
        Reps(cut, 1).Input("9");
        store.FailAllWrites(StoreFailureKind.StorageFull);

        Confirm(cut);

        Assert.Contains("out of storage", _notice.Message);
        Assert.Empty(Stored(data).Entries);
        Assert.Equal("22", Weight(cut, 1).GetAttribute("value"));
        Assert.Equal("9", Reps(cut, 1).GetAttribute("value"));
        Assert.Empty(cut.FindAll(".fa-setrow--done"));

        store.Recover();
        Confirm(cut);

        Assert.Equal(22, SquatSets(data).Single().WeightKg);
        Assert.Null(_notice.Message);
        Assert.Single(cut.FindAll(".fa-setrow--done"));
    }

    [Fact]
    public async Task When_another_window_holds_the_writer_lock_the_log_is_read_only_with_a_plain_message()
    {
        var store = new InMemoryDataStore();
        await OpenSeededAsync(store, Plan(), InProgress());
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var cut = await OpenLogAsync(locked);

        Assert.Contains("Another window", cut.Find(".log__notice").TextContent);
        Assert.True(cut.Find(".fa-confirmbar .fa-btn--primary").HasAttribute("disabled"));
        Assert.Contains("Goblet squat", cut.Find(".fa-exercise__name").TextContent);
    }

    [Fact]
    public async Task When_storage_cannot_be_read_the_page_says_so_and_offers_a_way_home()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        await OpenSeededAsync(store, Plan(), InProgress());
        store.FailEverything(StoreFailureKind.Unexpected);
        var unreadable = await AppData.OpenAsync(store);
        var cut = await OpenLogAsync(unreadable);

        Assert.Contains("could not be read", cut.Find(".log__notice").TextContent);
        Assert.Equal("./", cut.Find("a.fa-btn").GetAttribute("href"));
        Assert.Empty(cut.FindAll(".fa-setrow"));
    }

    // ---- Steppers

    private static IElement Stepper(IRenderedComponent<WorkoutLog> cut, string name) =>
        cut.Find($".fa-confirmbar button[aria-label='{name}']");

    private async Task<(AppData Data, IRenderedComponent<WorkoutLog> Cut)> OpenWithLastTimeAsync()
    {
        var data = await OpenSeededAsync(
            plan: Plan(), workouts: [Finished("w-old", Today.AddDays(-7), Set("o1", 20, 8), Set("o2", 20, 8), Set("o3", 20, 8)), InProgress()]);
        return (data, await OpenLogAsync(data));
    }

    [Fact]
    public async Task Steppers_have_accessible_names_with_the_plans_increment_and_the_big_target_class()
    {
        var (_, cut) = await OpenWithLastTimeAsync();

        Assert.Equal("−2", Stepper(cut, "Decrease by 2 kg").TextContent.Trim());
        Assert.Equal("+2", Stepper(cut, "Increase by 2 kg").TextContent.Trim());
        Assert.Contains("fa-btn--stepper", Stepper(cut, "Increase by 2 kg").ClassName);
    }

    [Fact]
    public async Task Stepping_changes_the_current_row_and_carries_down_to_later_unconfirmed_rows()
    {
        var (_, cut) = await OpenWithLastTimeAsync();

        Stepper(cut, "Increase by 2 kg").Click();

        Assert.Equal("22", Weight(cut, 1).GetAttribute("value"));
        Assert.Equal("22", Weight(cut, 2).GetAttribute("value"));
        Assert.Equal("22", Weight(cut, 3).GetAttribute("value"));
        Assert.Equal("8", Reps(cut, 2).GetAttribute("value"));
    }

    [Fact]
    public async Task Stepping_never_changes_a_confirmed_row_and_goes_down_as_well()
    {
        var (data, cut) = await OpenWithLastTimeAsync();
        Stepper(cut, "Increase by 2 kg").Click();
        Confirm(cut);

        Stepper(cut, "Decrease by 2 kg").Click();

        Assert.Equal("22", Weight(cut, 1).GetAttribute("value"));
        Assert.Equal("20", Weight(cut, 2).GetAttribute("value"));
        Assert.Equal("20", Weight(cut, 3).GetAttribute("value"));
        Assert.Equal(22, SquatSets(data).Single().WeightKg);
    }

    [Fact]
    public async Task Stepping_from_an_empty_row_starts_at_zero_and_never_goes_below_it()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress());
        var cut = await OpenLogAsync(data);

        Stepper(cut, "Decrease by 2 kg").Click();
        Assert.Equal("0", Weight(cut, 1).GetAttribute("value"));

        Stepper(cut, "Increase by 2 kg").Click();
        Assert.Equal("2", Weight(cut, 1).GetAttribute("value"));
    }

    [Fact]
    public async Task Steppers_show_pounds_when_units_are_lb()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress());
        await data.CommitAsync(Change.Put(data.Snapshot.Settings with { DisplayWeightUnit = WeightUnit.Lb }));
        var cut = await OpenLogAsync(data);

        Stepper(cut, "Increase by 4.5 lb").Click();

        Assert.Equal("4.5", Weight(cut, 1).GetAttribute("value"));
    }

    [Fact]
    public async Task A_stepped_value_is_what_gets_saved_on_confirm()
    {
        var (data, cut) = await OpenWithLastTimeAsync();

        Stepper(cut, "Increase by 2 kg").Click();
        Confirm(cut);

        Assert.Equal(22, SquatSets(data).Single().WeightKg);
        Assert.Equal(8, SquatSets(data).Single().Reps);
    }

    [Fact]
    public async Task Steppers_are_gone_when_every_row_is_confirmed()
    {
        var (_, cut) = await OpenWithLastTimeAsync();
        for (var set = 0; set < 3; set++)
        {
            Confirm(cut);
        }

        Assert.Empty(cut.FindAll(".fa-confirmbar .fa-btn--stepper"));
    }

    // ---- Add Set and Max set

    private static IElement Link(IRenderedComponent<WorkoutLog> cut, string text) =>
        cut.FindAll(".log__tools button").Single(b => b.TextContent.Trim() == text);

    [Fact]
    public async Task Add_Set_appends_a_row_copied_from_the_one_above_and_it_can_be_confirmed()
    {
        var (data, cut) = await OpenWithLastTimeAsync();
        for (var set = 0; set < 3; set++)
        {
            Confirm(cut);
        }

        Link(cut, "Add Set").Click();

        Assert.Equal(4, cut.FindAll(".fa-setrow").Count);
        Assert.Equal("20", Weight(cut, 4).GetAttribute("value"));
        Assert.Equal("Confirm Set", cut.Find(".fa-confirmbar .fa-btn--primary").TextContent.Trim());
        Confirm(cut);
        Assert.Equal(4, SquatSets(data).Count);
    }

    [Fact]
    public async Task An_extra_unconfirmed_row_can_be_removed_but_a_planned_or_confirmed_one_cannot()
    {
        var (data, cut) = await OpenWithLastTimeAsync();
        Assert.DoesNotContain(cut.FindAll(".log__tools button"), b => b.TextContent.Contains("Remove"));

        Link(cut, "Add Set").Click();
        Link(cut, "Remove Set 4").Click();

        Assert.Equal(3, cut.FindAll(".fa-setrow").Count);
        Assert.DoesNotContain(cut.FindAll(".log__tools button"), b => b.TextContent.Contains("Remove"));

        Link(cut, "Add Set").Click();
        Confirm(cut);
        Confirm(cut);
        Confirm(cut);
        Confirm(cut);
        Assert.Equal(4, SquatSets(data).Count);
        Assert.DoesNotContain(cut.FindAll(".log__tools button"), b => b.TextContent.Contains("Remove"));
    }

    [Fact]
    public async Task Marking_the_current_set_as_Max_labels_the_row_and_saves_it_on_confirm()
    {
        var (data, cut) = await OpenWithLastTimeAsync();

        Link(cut, "Mark Set 1 as Max set").Click();

        Assert.Contains("Max set", cut.FindAll(".fa-setrow")[0].TextContent);
        Confirm(cut);
        Assert.True(SquatSets(data).Single().IsMax);
        Assert.Contains("Max set", cut.FindAll(".fa-setrow")[0].TextContent);
    }

    [Fact]
    public async Task The_mark_can_be_taken_back_before_confirming()
    {
        var (data, cut) = await OpenWithLastTimeAsync();

        Link(cut, "Mark Set 1 as Max set").Click();
        Link(cut, "Unmark Set 1 as Max set").Click();
        Confirm(cut);

        Assert.DoesNotContain("Max set", cut.FindAll(".fa-setrow")[0].TextContent);
        Assert.False(SquatSets(data).Single().IsMax);
    }

    [Fact]
    public async Task A_confirmed_max_set_can_be_unmarked_after_reopening_and_the_change_is_saved_at_once()
    {
        var (data, cut) = await OpenWithLastTimeAsync();
        Link(cut, "Mark Set 1 as Max set").Click();
        Confirm(cut);

        cut.Find("button[aria-label='Edit set 1']").Click();
        Link(cut, "Unmark Set 1 as Max set").Click();

        Assert.False(SquatSets(data).Single().IsMax);
        Assert.DoesNotContain("Max set", cut.FindAll(".fa-setrow")[0].TextContent);
    }

    [Fact]
    public async Task Once_every_row_is_confirmed_the_link_targets_the_last_set()
    {
        var (data, cut) = await OpenWithLastTimeAsync();
        for (var set = 0; set < 3; set++)
        {
            Confirm(cut);
        }

        Link(cut, "Mark Set 3 as Max set").Click();

        Assert.True(SquatSets(data)[2].IsMax);
        Assert.False(SquatSets(data)[0].IsMax);
    }

    [Fact]
    public async Task Reopening_and_confirming_a_max_set_keeps_the_mark()
    {
        var (data, cut) = await OpenWithLastTimeAsync();
        Link(cut, "Mark Set 1 as Max set").Click();
        Confirm(cut);

        cut.Find("button[aria-label='Edit set 1']").Click();
        Weight(cut, 1).Input("25");
        Confirm(cut);

        Assert.True(SquatSets(data).Single().IsMax);
        Assert.Equal(25, SquatSets(data).Single().WeightKg);
    }

    // ---- Exercises the page cannot log yet

    [Fact]
    public async Task An_exercise_kind_not_supported_yet_shows_a_note_and_can_be_passed_with_Next()
    {
        var plan = Plan(
        [
            new("lower", "Lower", DayOfWeek.Monday, SessionType.Strength, null,
            [
                new SessionExercise("plank", 2, null, null, null),
                new SessionExercise(Squat, 3, 8, null, null),
            ]),
        ]);
        var workout = Core.Rules.Starting.WorkoutStart.Create(
            plan, plan.Sessions[0], Today, new CountingIds(), new FixedClock(Now)) with { Id = "w-now" };
        var data = await OpenSeededAsync(plan: plan, workouts: workout);
        var cut = await OpenLogAsync(data);

        Assert.Contains("cannot be logged here yet", cut.Find(".log__notice").TextContent);
        Assert.Empty(cut.FindAll(".fa-setrow"));
        Assert.Equal("Next: Goblet squat", cut.Find(".fa-confirmbar .fa-btn--primary").TextContent.Trim());

        Confirm(cut);

        Assert.Contains("Goblet squat", cut.Find(".fa-exercise__name").TextContent);
    }
}
