using Bunit;
using Core.Model;
using Core.Storage;
using Core.Tests.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Ui.Pages;
using Ui.Saving;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

public sealed class PlanEditorTests : BunitContext
{
    private readonly SaveFailureNotice _notice = new();

    private static Phase Ph(string id, string focus, int start, int end) => new(id, focus, start, end, [], []);

    private static Plan PlanWithPhases() => Plan() with { Phases = [Ph("p1", "Foundation", 1, 4), Ph("p2", "Build", 5, 8)] };

    private IRenderedComponent<PlanEditor> RenderEditor(AppData data, string planId = "plan")
    {
        Wire(this, data, _notice);
        JSInterop.SetupModule("./_content/Ui/Components/Sheet.razor.js").Mode = JSRuntimeMode.Loose;
        return Render<PlanEditor>(p => p.Add(x => x.PlanId, planId));
    }

    private static async Task<AppData> OpenWithAsync(Plan plan, IDataStore? store = null)
    {
        var data = await AppData.OpenAsync(store ?? new InMemoryDataStore());
        await data.CommitAsync([.. PageFixture.Exercises.Select(Change.Put), Change.Put(plan)]);
        return data;
    }

    private static AngleSharp.Dom.IElement Field(IRenderedComponent<PlanEditor> cut, string label) =>
        cut.FindAll("label").First(l => l.QuerySelector(".fa-label")?.TextContent.Trim() == label).QuerySelector("input, select")!;

    private static AngleSharp.Dom.IElement Button(IRenderedComponent<PlanEditor> cut, string text) =>
        cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    private static AngleSharp.Dom.IElement SaveButton(IRenderedComponent<PlanEditor> cut) => Button(cut, "Save");

    private static void ShowTab(IRenderedComponent<PlanEditor> cut, string tab) => Button(cut, tab).Click();

    private string CurrentPath => Services.GetRequiredService<NavigationManager>().Uri[Services.GetRequiredService<NavigationManager>().BaseUri.Length..];

    // Page shape

    [Fact]
    public async Task The_editor_shows_the_banner_the_three_tabs_and_the_Plan_name()
    {
        var cut = RenderEditor(await OpenWithAsync(Plan()));

        Assert.Contains("Edits apply going forward. Past Workouts are unchanged.", cut.Markup);
        Assert.Equal(["Settings", "Phases", "Sessions"], cut.FindAll(".fa-planeditor__tabs button").Select(b => b.TextContent.Trim()));
        Assert.Equal("Plan", cut.Find("h1").TextContent);
        Assert.Equal("true", Button(cut, "Settings").GetAttribute("aria-pressed"));
    }

    [Fact]
    public async Task An_unknown_Plan_says_so_and_offers_the_way_back()
    {
        var cut = RenderEditor(await OpenWithAsync(Plan()), "missing");

        Assert.Contains("That Plan isn't here any more.", cut.Markup);
        Assert.Contains(cut.FindAll("a"), a => a.TextContent.Trim() == "Back to Plans");
    }

    // Settings

    [Fact]
    public async Task Save_starts_disabled_and_settings_edits_persist()
    {
        var data = await OpenWithAsync(Plan());
        var cut = RenderEditor(data);
        Assert.True(SaveButton(cut).HasAttribute("disabled"));

        Field(cut, "Goal").Input("HYROX London");
        Field(cut, "Goal date").Change("2027-04-24");
        Field(cut, "Weekly target (Workouts)").Input("5");
        Field(cut, "Streak minimum (Workouts a week)").Input("3");
        Field(cut, "Start date").Change("2026-11-09");
        cut.Find("input[type='checkbox']").Change(true);
        Field(cut, "Weight increment (kg)").Input("1,25");
        Assert.False(SaveButton(cut).HasAttribute("disabled"));
        SaveButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal(5, data.Snapshot.Plans.Single().WeeklyTarget));
        var saved = data.Snapshot.Plans.Single();
        Assert.Equal("HYROX London", saved.Goal);
        Assert.Equal(new DateOnly(2027, 4, 24), saved.GoalDate);
        Assert.Equal(3, saved.StreakMinWorkoutsPerWeek);
        Assert.Equal(new DateOnly(2026, 11, 9), saved.StartDate);
        Assert.True(saved.StreakCountsCustomWorkouts);
        Assert.Equal(1.25, saved.WeightIncrementKg);
        cut.WaitForAssertion(() => Assert.True(SaveButton(cut).HasAttribute("disabled")));
        Assert.Contains("Saved.", cut.Markup);
    }

    [Theory]
    [InlineData("Weekly target (Workouts)", "0", "weekly target")]
    [InlineData("Weekly target (Workouts)", "lots", "weekly target")]
    [InlineData("Streak minimum (Workouts a week)", "", "Streak minimum")]
    [InlineData("Weight increment (kg)", "0", "weight increment")]
    public async Task An_invalid_value_disables_Save_with_an_inline_message(string label, string value, string expected)
    {
        var cut = RenderEditor(await OpenWithAsync(Plan()));

        Field(cut, label).Input(value);

        Assert.True(SaveButton(cut).HasAttribute("disabled"));
        Assert.Contains(expected, cut.Find("[role='alert']").TextContent);
        Assert.Contains("Fix the problem on the Settings tab", cut.Markup);
    }

    [Fact]
    public async Task A_goal_date_before_the_start_date_disables_Save()
    {
        var cut = RenderEditor(await OpenWithAsync(Plan()));

        Field(cut, "Goal date").Change("2026-01-01");

        Assert.True(SaveButton(cut).HasAttribute("disabled"));
        Assert.Contains("goal date cannot be before the start date", cut.Find("[role='alert']").TextContent);
    }

    [Fact]
    public async Task Typed_settings_survive_a_switch_of_tab()
    {
        var cut = RenderEditor(await OpenWithAsync(Plan()));

        Field(cut, "Weekly target (Workouts)").Input("6");
        ShowTab(cut, "Phases");
        ShowTab(cut, "Settings");

        Assert.Equal("6", Field(cut, "Weekly target (Workouts)").GetAttribute("value"));
        Assert.False(SaveButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public async Task Home_reflects_the_new_weekly_target_after_Save()
    {
        var data = await OpenWithAsync(Plan());
        var cut = RenderEditor(data);
        Field(cut, "Weekly target (Workouts)").Input("5");
        SaveButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal(5, data.Snapshot.Plans.Single().WeeklyTarget));

        var home = Render<Home>();

        Assert.Contains("target 5", home.Markup);
    }

    // Phases

    [Fact]
    public async Task Phases_list_their_weeks_and_the_dates_they_cover()
    {
        var cut = RenderEditor(await OpenWithAsync(PlanWithPhases()));

        ShowTab(cut, "Phases");

        var rows = cut.FindAll(".fa-phase");
        Assert.Equal(2, rows.Count);
        Assert.Contains("Foundation", rows[0].TextContent);
        Assert.Contains("Weeks 1 to 4 · 16 Nov to 13 Dec 2026", rows[0].TextContent);
        Assert.Contains("Weeks 5 to 8 · 14 Dec to 10 Jan 2027", rows[1].TextContent);
    }

    [Fact]
    public async Task Editing_a_Phase_expands_its_row_and_the_new_weeks_are_saved()
    {
        var data = await OpenWithAsync(PlanWithPhases());
        var cut = RenderEditor(data);
        ShowTab(cut, "Phases");

        cut.Find("button[aria-label='Edit Foundation']").Click();
        Field(cut, "Focus").Input("Base");
        Field(cut, "Last week").Change("6");
        Field(cut, "First week").Change("1");
        Assert.True(SaveButton(cut).HasAttribute("disabled"));
        Assert.Contains("overlaps", cut.Find("[role='alert']").TextContent);

        cut.FindAll(".fa-phase")[1].QuerySelector("button")!.Click();
        Field(cut, "First week").Change("7");
        Assert.False(SaveButton(cut).HasAttribute("disabled"));
        SaveButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal("Base", data.Snapshot.Plans.Single().Phases[0].Focus));
        var phases = data.Snapshot.Plans.Single().Phases;
        Assert.Equal((1, 6), (phases[0].StartWeek, phases[0].EndWeek));
        Assert.Equal((7, 8), (phases[1].StartWeek, phases[1].EndWeek));
    }

    [Fact]
    public async Task Overlapping_Phases_are_refused_with_both_names_and_Save_waits()
    {
        var cut = RenderEditor(await OpenWithAsync(PlanWithPhases()));
        ShowTab(cut, "Phases");

        cut.Find("button[aria-label='Edit Build']").Click();
        Field(cut, "First week").Change("3");

        var message = cut.Find("[role='alert']").TextContent;
        Assert.Contains("Build", message);
        Assert.Contains("Foundation", message);
        Assert.True(SaveButton(cut).HasAttribute("disabled"));
        Assert.Contains("Fix the problem on the Phases tab", cut.Markup);
    }

    [Fact]
    public async Task A_reversed_range_is_refused_naming_the_Phase()
    {
        var cut = RenderEditor(await OpenWithAsync(PlanWithPhases()));
        ShowTab(cut, "Phases");

        cut.Find("button[aria-label='Edit Build']").Click();
        Field(cut, "Last week").Change("2");

        Assert.Contains("\"Build\" ends (week 2) before it starts (week 5)", cut.Find("[role='alert']").TextContent);
        Assert.True(SaveButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public async Task A_Phase_goal_can_be_added_edited_and_saved()
    {
        var data = await OpenWithAsync(PlanWithPhases());
        var cut = RenderEditor(data);
        ShowTab(cut, "Phases");

        cut.Find("button[aria-label='Edit Foundation']").Click();
        Button(cut, "Add goal").Click();
        Field(cut, "Weight (kg)").Change("40");
        Field(cut, "Reps").Change("8");
        Field(cut, "Goal Exercise").Change(Lunge);
        SaveButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Plans.Single().Phases[0].Goals));
        Assert.Equal(new PhaseGoal(Lunge, 40, 8), data.Snapshot.Plans.Single().Phases[0].Goals[0]);
    }

    [Fact]
    public async Task A_Phase_goal_with_no_reps_holds_Save_back()
    {
        var cut = RenderEditor(await OpenWithAsync(PlanWithPhases()));
        ShowTab(cut, "Phases");
        cut.Find("button[aria-label='Edit Foundation']").Click();
        Button(cut, "Add goal").Click();

        Field(cut, "Reps").Change("0");

        Assert.True(SaveButton(cut).HasAttribute("disabled"));
        Assert.Contains("Phase goal", cut.Find("[role='alert']").TextContent);
    }

    [Fact]
    public async Task Phases_can_be_added_and_removed()
    {
        var data = await OpenWithAsync(PlanWithPhases());
        var cut = RenderEditor(data);
        ShowTab(cut, "Phases");

        Button(cut, "Add Phase").Click();
        Assert.Equal(3, cut.FindAll(".fa-phase").Count);
        Assert.Contains("Weeks 9 to 12", cut.FindAll(".fa-phase")[2].TextContent);
        SaveButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal(3, data.Snapshot.Plans.Single().Phases.Count));

        Button(cut, "Remove Phase").Click();
        SaveButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal(2, data.Snapshot.Plans.Single().Phases.Count));
    }

    [Fact]
    public async Task A_Plan_with_no_Phases_says_so()
    {
        var cut = RenderEditor(await OpenWithAsync(Plan()));

        ShowTab(cut, "Phases");

        Assert.Contains("No Phases yet", cut.Markup);
    }

    // Sessions

    private static string[] RowNames(IRenderedComponent<PlanEditor> cut) =>
        [.. cut.FindAll(".fa-row .fa-row__name").Select(n => n.TextContent.Trim())];

    private IRenderedComponent<PlanEditor> OpenSessions(AppData data)
    {
        var cut = RenderEditor(data);
        ShowTab(cut, "Sessions");
        return cut;
    }

    [Fact]
    public async Task Sessions_show_their_rows_with_a_hidden_drag_handle_name_and_summary()
    {
        var plan = Plan([new Session("lower", "Lower", DayOfWeek.Monday, SessionType.Strength, null,
        [
            new SessionExercise(Squat, 3, 8, null, "Chest up"),
            new SessionExercise(Lunge, 3, 8, 12, null),
            new SessionExercise("plank", 2, null, null, null),
        ])]);
        var cut = OpenSessions(await OpenWithAsync(plan));

        Assert.Equal("Lower", cut.Find(".fa-session__name").TextContent);
        Assert.Contains("Monday", cut.Find(".fa-session__meta").TextContent);
        Assert.Equal(["Goblet squat", "Walking lunge", "Plank"], RowNames(cut));
        Assert.Equal(["3 x 8", "3 x 8 to 12", "2 sets"], cut.FindAll(".fa-row__sets").Select(s => s.TextContent.Trim()));
        Assert.All(cut.FindAll(".fa-row__handle"), h => Assert.Equal("true", h.GetAttribute("aria-hidden")));
        Assert.DoesNotContain("draggable", cut.Markup);
    }

    [Fact]
    public async Task Editing_a_row_expands_it_and_the_new_numbers_and_cue_are_saved()
    {
        var data = await OpenWithAsync(Plan());
        var cut = OpenSessions(data);

        cut.Find("button[aria-label='Edit Goblet squat']").Click();
        Field(cut, "Sets").Change("4");
        Field(cut, "Reps").Change("6");
        Field(cut, "Highest Reps").Change("10");
        Field(cut, "Cue").Input("Slow down");
        Assert.Contains("4 x 6 to 10", cut.FindAll(".fa-row__sets")[0].TextContent);
        Assert.False(SaveButton(cut).HasAttribute("disabled"));
        SaveButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal(4, data.Snapshot.Plans.Single().Sessions[0].Exercises[0].Sets));
        Assert.Equal(new SessionExercise(Squat, 4, 6, 10, "Slow down"), data.Snapshot.Plans.Single().Sessions[0].Exercises[0]);
        Assert.Equal(new SessionExercise(Lunge, 2, 10, null, null), data.Snapshot.Plans.Single().Sessions[0].Exercises[1]);
    }

    [Fact]
    public async Task A_row_has_no_Step_field_because_the_data_has_no_per_row_step()
    {
        var cut = OpenSessions(await OpenWithAsync(Plan()));

        cut.Find("button[aria-label='Edit Goblet squat']").Click();

        Assert.DoesNotContain("Step", cut.Find(".fa-row__edit").TextContent);
    }

    [Fact]
    public async Task Move_up_and_Move_down_reorder_rows_and_the_order_is_saved()
    {
        var data = await OpenWithAsync(Plan());
        var cut = OpenSessions(data);

        cut.Find("button[aria-label='Edit Walking lunge']").Click();
        Assert.True(Button(cut, "Move down").HasAttribute("disabled"));
        Button(cut, "Move up").Click();

        Assert.Equal(["Walking lunge", "Goblet squat"], RowNames(cut));
        SaveButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal(Lunge, data.Snapshot.Plans.Single().Sessions[0].Exercises[0].ExerciseId));

        Assert.True(Button(cut, "Move up").HasAttribute("disabled"));
        Button(cut, "Move down").Click();
        Assert.Equal(["Goblet squat", "Walking lunge"], RowNames(cut));
    }

    [Fact]
    public async Task Remove_takes_the_row_out_and_Save_stores_it()
    {
        var data = await OpenWithAsync(Plan());
        var cut = OpenSessions(data);

        cut.Find("button[aria-label='Edit Goblet squat']").Click();
        Button(cut, "Remove").Click();

        Assert.Equal(["Walking lunge"], RowNames(cut));
        SaveButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Plans.Single().Sessions[0].Exercises));
        Assert.Equal(Lunge, data.Snapshot.Plans.Single().Sessions[0].Exercises[0].ExerciseId);
    }

    [Fact]
    public async Task Add_Exercise_from_library_appends_the_picked_Exercise_at_the_end()
    {
        var data = await OpenWithAsync(Plan());
        var cut = OpenSessions(data);

        Button(cut, "Add Exercise from library").Click();
        cut.FindAll("[role='dialog'] .fa-picker__row").Single(r => r.TextContent.Contains("Plank")).Click();

        cut.WaitForAssertion(() => Assert.Equal(["Goblet squat", "Walking lunge", "Plank"], RowNames(cut)));
        Assert.Empty(cut.FindAll("[role='dialog']"));
        SaveButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal(3, data.Snapshot.Plans.Single().Sessions[0].Exercises.Count));
        Assert.Equal(new SessionExercise("plank", 3, 8, null, null), data.Snapshot.Plans.Single().Sessions[0].Exercises[2]);
    }

    [Fact]
    public async Task The_picker_does_not_offer_Exercises_the_Session_already_has()
    {
        var cut = OpenSessions(await OpenWithAsync(Plan()));

        Button(cut, "Add Exercise from library").Click();

        var offered = cut.FindAll("[role='dialog'] .fa-picker__row-name").Select(n => n.TextContent.Trim()).ToList();
        Assert.DoesNotContain("Goblet squat", offered);
        Assert.DoesNotContain("Walking lunge", offered);
        Assert.Contains("Plank", offered);
    }

    [Fact]
    public async Task A_new_Exercise_made_in_the_picker_lands_in_the_Session()
    {
        var data = await OpenWithAsync(Plan());
        var cut = OpenSessions(data);
        Button(cut, "Add Exercise from library").Click();

        cut.Find("[role='dialog'] .fa-picker__new button").Click();
        cut.Find("[role='dialog'] input[aria-label='Name']").Input("Cable row");
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Add Exercise").Click();

        cut.WaitForAssertion(() => Assert.Equal(["Goblet squat", "Walking lunge", "Cable row"], RowNames(cut)));
        Assert.Contains(data.Snapshot.Exercises, e => e.Name == "Cable row");
    }

    [Fact]
    public async Task A_Session_can_be_renamed_and_moved_to_another_day_and_type()
    {
        var data = await OpenWithAsync(Plan());
        var cut = OpenSessions(data);

        cut.Find("button[aria-label='Edit Session Lower']").Click();
        Field(cut, "Name").Input("Legs");
        Field(cut, "Default day").Change(DayOfWeek.Friday.ToString());
        Field(cut, "Type").Change(SessionType.Body.ToString());
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Done").Click();

        Assert.Equal("Legs", cut.Find(".fa-session__name").TextContent);
        SaveButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal("Legs", data.Snapshot.Plans.Single().Sessions[0].Name));
        var session = data.Snapshot.Plans.Single().Sessions[0];
        Assert.Equal((DayOfWeek.Friday, SessionType.Body), (session.DefaultDay, session.Type));
        Assert.Equal(2, session.Exercises.Count);
    }

    [Fact]
    public async Task A_blank_Session_name_is_refused_in_the_sheet_and_nothing_changes()
    {
        var cut = OpenSessions(await OpenWithAsync(Plan()));

        cut.Find("button[aria-label='Edit Session Lower']").Click();
        Field(cut, "Name").Input("  ");
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Done").Click();

        Assert.Contains("Give the Session a name.", cut.Find("[role='dialog'] [role='alert']").TextContent);
        Assert.True(SaveButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public async Task A_new_Session_can_be_added_with_a_name_day_and_type()
    {
        var data = await OpenWithAsync(Plan());
        var cut = OpenSessions(data);

        Button(cut, "Add Session").Click();
        Field(cut, "Name").Input("Run");
        Field(cut, "Default day").Change(DayOfWeek.Saturday.ToString());
        Field(cut, "Type").Change(SessionType.Cardio.ToString());
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Add Session").Click();

        Assert.Equal(["Lower", "Run"], cut.FindAll(".fa-session__name").Select(n => n.TextContent));
        SaveButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal(2, data.Snapshot.Plans.Single().Sessions.Count));
        var run = data.Snapshot.Plans.Single().Sessions[1];
        Assert.Equal(("Run", DayOfWeek.Saturday, SessionType.Cardio), (run.Name, run.DefaultDay, run.Type));
        Assert.Empty(run.Exercises);
    }

    [Theory]
    [InlineData("Sets", "0", "Sets must be at least 1.")]
    [InlineData("Sets", "lots", "Sets must be at least 1.")]
    [InlineData("Reps", "0", "Reps must be at least 1.")]
    [InlineData("Highest Reps", "4", "The highest Reps cannot be below Reps.")]
    public async Task Invalid_sets_or_reps_show_a_message_and_hold_Save_back(string field, string value, string expected)
    {
        var cut = OpenSessions(await OpenWithAsync(Plan()));
        cut.Find("button[aria-label='Edit Goblet squat']").Click();

        Field(cut, field).Change(value);

        Assert.Contains(expected, cut.Find("[role='alert']").TextContent);
        Assert.Contains("Goblet squat", cut.Find("[role='alert']").TextContent);
        Assert.True(SaveButton(cut).HasAttribute("disabled"));
        Assert.Contains("Fix the problem on the Sessions tab", cut.Markup);
    }

    [Fact]
    public async Task Fixing_the_numbers_lets_Save_through_again()
    {
        var cut = OpenSessions(await OpenWithAsync(Plan()));
        cut.Find("button[aria-label='Edit Goblet squat']").Click();
        Field(cut, "Sets").Change("0");

        Field(cut, "Sets").Change("5");

        Assert.Empty(cut.FindAll("[role='alert']"));
        Assert.False(SaveButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public async Task Marking_a_row_as_a_Benchmark_exercise_changes_the_library_Exercise()
    {
        var data = await OpenWithAsync(Plan());
        var cut = OpenSessions(data);

        cut.Find("button[aria-label='Edit Goblet squat']").Click();
        cut.Find(".fa-row__check input").Change(true);

        cut.WaitForAssertion(() => Assert.True(data.Snapshot.Exercises.Single(e => e.Id == Squat).IsBenchmark));
        Assert.False(data.Snapshot.Exercises.Single(e => e.Id == Lunge).IsBenchmark);
        Assert.True(SaveButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public async Task Saving_the_Sessions_leaves_past_Workouts_unchanged()
    {
        var past = Finished("w-past", Today.AddDays(-7), Set("s1", 20, 8));
        var data = await OpenSeededAsync(plan: Plan(), workouts: past);
        var cut = OpenSessions(data);

        cut.Find("button[aria-label='Edit Goblet squat']").Click();
        Button(cut, "Remove").Click();
        SaveButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Plans.Single().Sessions[0].Exercises));
        Assert.Equal(past, data.Snapshot.Workouts.Single());
    }

    [Fact]
    public async Task Reordering_works_from_the_keyboard_because_the_buttons_are_real_focusable_buttons()
    {
        var cut = OpenSessions(await OpenWithAsync(Plan()));

        cut.Find("button[aria-label='Edit Walking lunge']").Click();

        var up = cut.Find("button[aria-label='Move Walking lunge up']");
        Assert.Equal("button", up.TagName.ToLowerInvariant());
        Assert.False(up.HasAttribute("tabindex"));
        Assert.False(up.HasAttribute("disabled"));
    }

    [Fact]
    public async Task A_read_only_window_disables_Session_edits_and_the_page_says_why()
    {
        var store = new InMemoryDataStore();
        await OpenWithAsync(Plan(), store);
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var cut = OpenSessions(locked);

        Assert.Contains("read-only", cut.Markup);
        cut.Find("button[aria-label='Edit Goblet squat']").Click();
        Assert.All(cut.FindAll(".fa-row__edit input"), i => Assert.True(i.HasAttribute("disabled")));
        foreach (var text in new[] { "Move up", "Move down", "Remove", "Add Exercise from library", "Add Session", "Edit Session" })
        {
            Assert.True(Button(cut, text).HasAttribute("disabled"), text);
        }

        Assert.True(SaveButton(cut).HasAttribute("disabled"));
    }

    // Leaving

    [Fact]
    public async Task Leaving_with_unsaved_changes_asks_and_Keep_editing_stays()
    {
        var cut = RenderEditor(await OpenWithAsync(Plan()));
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("plans/plan");
        Field(cut, "Weekly target (Workouts)").Input("5");

        navigation.NavigateTo("plans");

        cut.WaitForAssertion(() => Assert.Contains("Discard changes?", cut.Markup));
        Assert.Equal("plans/plan", CurrentPath);
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Keep editing").Click();
        cut.WaitForAssertion(() => Assert.DoesNotContain("Discard changes?", cut.Markup));
        Assert.Equal("5", Field(cut, "Weekly target (Workouts)").GetAttribute("value"));
    }

    [Fact]
    public async Task Discard_leaves_and_drops_the_edits()
    {
        var data = await OpenWithAsync(Plan());
        var cut = RenderEditor(data);
        var navigation = Services.GetRequiredService<NavigationManager>();
        Field(cut, "Weekly target (Workouts)").Input("5");

        navigation.NavigateTo("plans");
        cut.WaitForAssertion(() => Assert.Contains("Discard changes?", cut.Markup));
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Discard").Click();

        cut.WaitForAssertion(() => Assert.Equal("plans", CurrentPath));
        Assert.Equal(3, data.Snapshot.Plans.Single().WeeklyTarget);
    }

    [Fact]
    public async Task Leaving_with_nothing_changed_does_not_ask()
    {
        var cut = RenderEditor(await OpenWithAsync(Plan()));

        Services.GetRequiredService<NavigationManager>().NavigateTo("plans");

        Assert.Equal("plans", CurrentPath);
        Assert.DoesNotContain("Discard changes?", cut.Markup);
    }

    [Fact]
    public async Task Leaving_after_Save_does_not_ask()
    {
        var data = await OpenWithAsync(Plan());
        var cut = RenderEditor(data);
        Field(cut, "Weekly target (Workouts)").Input("5");
        SaveButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal(5, data.Snapshot.Plans.Single().WeeklyTarget));

        Services.GetRequiredService<NavigationManager>().NavigateTo("plans");

        Assert.Equal("plans", CurrentPath);
    }

    // Read-only, failure, double tap

    [Fact]
    public async Task A_read_only_window_disables_everything_and_says_why()
    {
        var store = new InMemoryDataStore();
        await OpenWithAsync(PlanWithPhases(), store);
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var cut = RenderEditor(locked);

        Assert.Contains("read-only", cut.Markup);
        Assert.All(cut.FindAll(".fa-planform input"), i => Assert.True(i.HasAttribute("disabled")));
        Assert.True(SaveButton(cut).HasAttribute("disabled"));
        ShowTab(cut, "Phases");
        cut.Find("button[aria-label='Edit Foundation']").Click();
        Assert.All(cut.FindAll(".fa-phase__edit input"), i => Assert.True(i.HasAttribute("disabled")));
        Assert.True(Button(cut, "Add Phase").HasAttribute("disabled"));
        Assert.True(Button(cut, "Remove Phase").HasAttribute("disabled"));
    }

    [Fact]
    public async Task A_failed_save_keeps_the_edits_on_screen_and_Save_available()
    {
        var store = new FaultInjectingDataStore(new InMemoryDataStore());
        var data = await OpenWithAsync(Plan(), store);
        var cut = RenderEditor(data);
        Field(cut, "Weekly target (Workouts)").Input("5");
        store.FailAllWrites(StoreFailureKind.StorageFull);

        SaveButton(cut).Click();

        cut.WaitForAssertion(() => Assert.NotNull(_notice.Message));
        Assert.Equal(3, data.Snapshot.Plans.Single().WeeklyTarget);
        Assert.Equal("5", Field(cut, "Weekly target (Workouts)").GetAttribute("value"));
        Assert.False(SaveButton(cut).HasAttribute("disabled"));
        Assert.DoesNotContain("Saved.", cut.Markup);
    }

    [Fact]
    public async Task Tapping_Save_twice_saves_once()
    {
        var data = await OpenWithAsync(Plan());
        var cut = RenderEditor(data);
        var saves = 0;
        data.Changed += () => saves++;
        Field(cut, "Weekly target (Workouts)").Input("5");

        var save = SaveButton(cut);
        save.Click();
        save.Click();

        cut.WaitForAssertion(() => Assert.Equal(5, data.Snapshot.Plans.Single().WeeklyTarget));
        Assert.Equal(1, saves);
    }
}
