using AngleSharp.Dom;
using Bunit;
using Core.Model;
using Core.Storage;
using Core.Tests.Storage;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Ui.Pages;
using Ui.Saving;
using Ui.Theming;
using static Ui.Tests.Pages.LogFixture;

namespace Ui.Tests.Pages;

/// <summary>The Exercise menu and the Workout menu on the log page (design 8.4): opening, closing, benchmark, Theme, Mode and the rest timer.</summary>
public sealed class WorkoutLogMenusTests : BunitContext
{
    private readonly SaveFailureNotice _notice = new();
    private readonly FakeDevicePreferences _preferences = new();

    private async Task<(AppData Data, IRenderedComponent<WorkoutLog> Cut)> OpenAsync(AppData? data = null)
    {
        data ??= await OpenSeededAsync(plan: Plan(), workouts: InProgress());
        Wire(this, data, _notice, _preferences);
        return (data, Render<WorkoutLog>(p => p.Add(l => l.WorkoutId, "w-now")));
    }

    private static IElement ExerciseMenuButton(IRenderedComponent<WorkoutLog> cut) => cut.Find("button[aria-label='Menu for Goblet squat']");

    private static IElement WorkoutMenuButton(IRenderedComponent<WorkoutLog> cut) => cut.Find("button[aria-label='Workout menu']");

    private static IElement Dialog(IRenderedComponent<WorkoutLog> cut) => cut.Find("[role='dialog']");

    private static IElement Item(IRenderedComponent<WorkoutLog> cut, string text) =>
        cut.FindAll("[role='dialog'] .fa-menuitem").Single(i => i.TextContent.Contains(text));

    private static IElement Choice(IRenderedComponent<WorkoutLog> cut, string group, string label) =>
        cut.FindAll($"[role='dialog'] [role='group'][aria-label='{group}'] button").Single(b => b.TextContent.Trim() == label);

    private static Exercise Squatting(AppData data) => data.Snapshot.Exercises.Single(e => e.Id == Squat);

    // ---- Opening and closing

    [Fact]
    public async Task The_exercise_menu_button_has_an_accessible_name_and_no_menu_is_open_at_first()
    {
        var (_, cut) = await OpenAsync();

        Assert.NotNull(ExerciseMenuButton(cut));
        Assert.Empty(cut.FindAll("[role='dialog']"));
    }

    [Fact]
    public async Task The_exercise_menu_opens_by_its_button_and_lists_its_items_in_order()
    {
        var (_, cut) = await OpenAsync();

        ExerciseMenuButton(cut).Click();

        Assert.Equal("Goblet squat", cut.Find("[role='dialog'] .fa-sheet__title").TextContent);
        var items = cut.FindAll("[role='dialog'] .fa-menuitem").Select(i => i.QuerySelector(".fa-menuitem__label")!.TextContent).ToList();
        Assert.Equal(["Swap Exercise", "Skip Exercise", "Add an Exercise", "Mark as benchmark", "View Guide"], items);
    }

    [Fact]
    public async Task Items_for_later_tickets_are_present_but_disabled_and_do_nothing()
    {
        var (data, cut) = await OpenAsync();
        ExerciseMenuButton(cut).Click();

        foreach (var label in new[] { "Swap Exercise", "Add an Exercise", "View Guide" })
        {
            var item = Item(cut, label);
            Assert.True(item.HasAttribute("disabled"), label);
            item.Click();
        }

        Assert.NotNull(Dialog(cut));
        Assert.Equal(InProgress().Entries, data.Snapshot.Workouts.Single(w => w.Id == "w-now").Entries);
    }

    [Fact]
    public async Task The_exercise_menu_closes_by_Cancel_an_outside_tap_and_Escape()
    {
        var (_, cut) = await OpenAsync();

        ExerciseMenuButton(cut).Click();
        cut.Find("[role='dialog'] .fa-sheet__actions button").Click();
        Assert.Empty(cut.FindAll("[role='dialog']"));

        ExerciseMenuButton(cut).Click();
        cut.Find(".fa-sheet__backdrop").Click();
        Assert.Empty(cut.FindAll("[role='dialog']"));

        ExerciseMenuButton(cut).Click();
        Dialog(cut).KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(cut.FindAll("[role='dialog']"));
    }

    [Fact]
    public async Task The_exercise_menu_hands_focus_back_to_its_button_when_it_closes()
    {
        var (_, cut) = await OpenAsync();
        var module = JSInterop.SetupModule("./_content/Ui/Components/Sheet.razor.js");
        module.Mode = JSRuntimeMode.Loose;

        ExerciseMenuButton(cut).Click();
        module.VerifyInvoke("attach");
        Dialog(cut).KeyDown(new KeyboardEventArgs { Key = "Escape" });

        module.VerifyInvoke("detach");
    }

    [Fact]
    public async Task The_workout_menu_opens_by_its_button_and_lists_its_items()
    {
        var (_, cut) = await OpenAsync();

        WorkoutMenuButton(cut).Click();

        Assert.Equal("Workout", cut.Find("[role='dialog'] .fa-sheet__title").TextContent);
        Assert.Equal(["Rest timer", "Theme", "Mode"],
            cut.FindAll("[role='dialog'] [role='group']").Select(g => g.GetAttribute("aria-label")).ToList());
        Assert.True(Item(cut, "Logged afterwards").HasAttribute("disabled"));
        Assert.True(Item(cut, "Finish Workout").HasAttribute("disabled"));
    }

    [Fact]
    public async Task The_workout_menu_closes_by_Back_to_the_Set_an_outside_tap_and_Escape()
    {
        var (_, cut) = await OpenAsync();

        WorkoutMenuButton(cut).Click();
        cut.Find("[role='dialog'] .fa-sheet__actions button").Click();
        Assert.Empty(cut.FindAll("[role='dialog']"));

        WorkoutMenuButton(cut).Click();
        cut.Find(".fa-sheet__backdrop").Click();
        Assert.Empty(cut.FindAll("[role='dialog']"));

        WorkoutMenuButton(cut).Click();
        Dialog(cut).KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Empty(cut.FindAll("[role='dialog']"));
    }

    // ---- Benchmark

    [Fact]
    public async Task Marking_as_benchmark_saves_at_once_and_the_label_and_mark_follow()
    {
        var (data, cut) = await OpenAsync();
        Assert.Empty(cut.FindAll(".fa-exercise__benchmark"));
        ExerciseMenuButton(cut).Click();

        Item(cut, "Mark as benchmark").Click();

        Assert.True(Squatting(data).IsBenchmark);
        Assert.Contains("Benchmark", cut.Find(".fa-exercise__benchmark").TextContent);
        Assert.Contains("Unmark as benchmark", Item(cut, "benchmark").TextContent);
    }

    [Fact]
    public async Task Unmarking_clears_the_flag_and_the_mark()
    {
        var data = await OpenSeededAsync(plan: Plan(), workouts: InProgress());
        await data.CommitAsync(Change.Put(Squatting(data) with { IsBenchmark = true }));
        var (_, cut) = await OpenAsync(data);
        ExerciseMenuButton(cut).Click();

        Item(cut, "Unmark as benchmark").Click();

        Assert.False(Squatting(data).IsBenchmark);
        Assert.Empty(cut.FindAll(".fa-exercise__benchmark"));
        Assert.Contains("Mark as benchmark", Item(cut, "benchmark").TextContent);
    }

    [Fact]
    public async Task A_failed_benchmark_save_shows_the_banner_and_leaves_the_screen_unchanged()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await OpenSeededAsync(store, Plan(), InProgress());
        var (_, cut) = await OpenAsync(data);
        ExerciseMenuButton(cut).Click();
        store.FailAllWrites(StoreFailureKind.StorageFull);

        Item(cut, "Mark as benchmark").Click();

        Assert.Contains("out of storage", _notice.Message);
        Assert.False(Squatting(data).IsBenchmark);
        Assert.Empty(cut.FindAll(".fa-exercise__benchmark"));
        Assert.Contains("Mark as benchmark", Item(cut, "benchmark").TextContent);
    }

    [Fact]
    public async Task When_the_window_cannot_save_the_benchmark_item_is_disabled()
    {
        var store = new InMemoryDataStore();
        await OpenSeededAsync(store, Plan(), InProgress());
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var (_, cut) = await OpenAsync(locked);
        ExerciseMenuButton(cut).Click();

        Assert.True(Item(cut, "benchmark").HasAttribute("disabled"));
    }

    // ---- Theme and Mode

    [Fact]
    public async Task Theme_and_Mode_switch_from_the_workout_menu()
    {
        var (_, cut) = await OpenAsync();
        var themes = Services.GetRequiredService<ThemeService>();
        WorkoutMenuButton(cut).Click();
        Assert.Equal("true", Choice(cut, "Theme", "Keep").GetAttribute("aria-pressed"));

        Choice(cut, "Theme", "Clean").Click();
        Choice(cut, "Mode", "Light").Click();

        Assert.Equal(Theme.Clean, themes.Theme);
        Assert.Equal(Mode.Light, themes.Mode);
        Assert.Equal("true", Choice(cut, "Theme", "Clean").GetAttribute("aria-pressed"));
        Assert.Equal("true", Choice(cut, "Mode", "Light").GetAttribute("aria-pressed"));
    }

    // ---- Rest timer

    [Fact]
    public async Task The_rest_timer_offers_off_60_90_and_120_with_off_chosen_by_default()
    {
        var (_, cut) = await OpenAsync();

        WorkoutMenuButton(cut).Click();

        var labels = cut.FindAll("[role='dialog'] [role='group'][aria-label='Rest timer'] button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(["Off", "60 s", "90 s", "120 s"], labels);
        Assert.Equal("true", Choice(cut, "Rest timer", "Off").GetAttribute("aria-pressed"));
        Assert.Equal("false", Choice(cut, "Rest timer", "90 s").GetAttribute("aria-pressed"));
    }

    [Fact]
    public async Task Choosing_a_rest_timer_is_remembered_on_the_device()
    {
        var (_, cut) = await OpenAsync();
        WorkoutMenuButton(cut).Click();

        Choice(cut, "Rest timer", "90 s").Click();

        Assert.Equal("true", Choice(cut, "Rest timer", "90 s").GetAttribute("aria-pressed"));
        Assert.Equal("90", _preferences.Values["rest-timer-seconds"]);
    }

    [Fact]
    public async Task A_remembered_rest_timer_is_chosen_when_the_page_opens_again()
    {
        _preferences.Values["rest-timer-seconds"] = "120";
        var (_, cut) = await OpenAsync();

        WorkoutMenuButton(cut).Click();

        Assert.Equal("true", Choice(cut, "Rest timer", "120 s").GetAttribute("aria-pressed"));
        Assert.Equal("false", Choice(cut, "Rest timer", "Off").GetAttribute("aria-pressed"));
    }

    [Fact]
    public async Task A_stored_value_that_is_not_one_of_the_choices_reads_as_off()
    {
        _preferences.Values["rest-timer-seconds"] = "45";
        var (_, cut) = await OpenAsync();

        WorkoutMenuButton(cut).Click();

        Assert.Equal("true", Choice(cut, "Rest timer", "Off").GetAttribute("aria-pressed"));
    }
}
