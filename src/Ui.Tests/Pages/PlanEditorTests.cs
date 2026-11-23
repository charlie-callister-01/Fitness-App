using Bunit;
using Core.Model;
using Core.Storage;
using Core.Tests.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Ui.Accessibility;
using Ui.Pages;
using Ui.Saving;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

public sealed class PlanEditorTests : BunitContext
{
    private readonly SaveFailureNotice _notice = new();

    private static Phase Ph(string id, string focus, int start, int end) => new(id, focus, start, end, [], []);

    private static Plan PlanWithPhases() => WithMainPhases(Plan(), [Ph("p1", "Foundation", 1, 4), Ph("p2", "Build", 5, 8)]);

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

        cut.WaitForAssertion(() => Assert.Equal(5, MainWeekly(data.Snapshot.Plans.Single()).WeeklyTarget));
        var saved = data.Snapshot.Plans.Single();
        var weekly = MainWeekly(saved);
        Assert.Equal("HYROX London", saved.Goal);
        Assert.Equal(new DateOnly(2027, 4, 24), saved.GoalDate);
        Assert.Equal(3, weekly.StreakMinWorkoutsPerWeek);
        Assert.Equal(new DateOnly(2026, 11, 9), saved.StartDate);
        Assert.True(weekly.StreakCountsCustomWorkouts);
        Assert.Equal(1.25, saved.WeightIncrementKg);
        cut.WaitForAssertion(() => Assert.True(SaveButton(cut).HasAttribute("disabled")));
        Assert.Contains("Saved.", cut.Markup);
        Assert.Equal("Plan saved.", Services.GetRequiredService<Announcer>().Message);
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
        cut.WaitForAssertion(() => Assert.Equal(5, MainWeekly(data.Snapshot.Plans.Single()).WeeklyTarget));

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

        cut.WaitForAssertion(() => Assert.Equal("Base", data.Snapshot.Plans.Single().Tracks[0].Phases[0].Focus));
        var phases = data.Snapshot.Plans.Single().Tracks[0].Phases;
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

        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Plans.Single().Tracks[0].Phases[0].Goals));
        Assert.Equal(new PhaseGoal(Lunge, 40, 8), data.Snapshot.Plans.Single().Tracks[0].Phases[0].Goals[0]);
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
        cut.WaitForAssertion(() => Assert.Equal(3, data.Snapshot.Plans.Single().Tracks[0].Phases.Count));

        Button(cut, "Remove Phase").Click();
        SaveButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal(2, data.Snapshot.Plans.Single().Tracks[0].Phases.Count));
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
        var plan = Plan([Session("lower", "Lower", DayOfWeek.Monday, SessionType.Strength, null,
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
        Assert.Equal((DefaultDay.Friday, SessionType.Body), (session.DefaultDay, session.Type));
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
        Assert.Equal(("Run", DefaultDay.Saturday, SessionType.Cardio), (run.Name, run.DefaultDay, run.Type));
        Assert.Empty(run.Exercises);
    }

    [Theory]
    [InlineData("Sets", "0", "Sets must be at least 1.")]
    [InlineData("Sets", "lots", "Enter a whole number.")]
    [InlineData("Reps", "8x", "Enter a whole number.")]
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
    public async Task Text_that_is_not_a_number_stays_as_typed_and_the_row_keeps_its_last_good_number()
    {
        var data = await OpenWithAsync(Plan());
        var cut = OpenSessions(data);
        cut.Find("button[aria-label='Edit Goblet squat']").Click();

        Field(cut, "Sets").Change("lots");

        Assert.Equal("lots", Field(cut, "Sets").GetAttribute("value"));
        Assert.Contains("Enter a whole number.", cut.Find("[role='alert']").TextContent);
        Assert.Contains("3 x 8", cut.FindAll(".fa-row__sets")[0].TextContent);
        Assert.True(SaveButton(cut).HasAttribute("disabled"));
        Field(cut, "Sets").Change("4");
        Assert.Empty(cut.FindAll("[role='alert']"));
        Assert.Equal("4", Field(cut, "Sets").GetAttribute("value"));
        Assert.False(SaveButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public async Task Each_row_has_a_link_to_its_Guide()
    {
        var cut = OpenSessions(await OpenWithAsync(Plan()));

        cut.Find("button[aria-label='Edit Goblet squat']").Click();

        Assert.Equal($"exercises/{Squat}/guide", cut.Find("a[aria-label='Guide for Goblet squat']").GetAttribute("href"));
    }

    [Fact]
    public async Task Ticking_Benchmark_waits_for_Save_and_Discard_leaves_the_library_unchanged()
    {
        var data = await OpenWithAsync(Plan());
        var cut = OpenSessions(data);

        cut.Find("button[aria-label='Edit Goblet squat']").Click();
        cut.Find(".fa-row__check input").Change(true);

        Assert.False(data.Snapshot.Exercises.Single(e => e.Id == Squat).IsBenchmark);
        Assert.True(cut.Find(".fa-row__check input").HasAttribute("checked"));
        Assert.False(SaveButton(cut).HasAttribute("disabled"));

        Services.GetRequiredService<NavigationManager>().NavigateTo("plans");
        cut.WaitForAssertion(() => Assert.Contains("Discard changes?", cut.Markup));
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Discard").Click();

        cut.WaitForAssertion(() => Assert.Equal("plans", CurrentPath));
        Assert.False(data.Snapshot.Exercises.Single(e => e.Id == Squat).IsBenchmark);
    }

    [Fact]
    public async Task Save_commits_the_Plan_and_the_Benchmark_flag_together_once()
    {
        var data = await OpenWithAsync(Plan());
        var cut = OpenSessions(data);
        var commits = 0;
        data.Changed += () => commits++;

        cut.Find("button[aria-label='Edit Goblet squat']").Click();
        cut.Find(".fa-row__check input").Change(true);
        Field(cut, "Sets").Change("5");
        SaveButton(cut).Click();

        cut.WaitForAssertion(() => Assert.True(data.Snapshot.Exercises.Single(e => e.Id == Squat).IsBenchmark));
        Assert.Equal(5, data.Snapshot.Plans.Single().Sessions[0].Exercises[0].Sets);
        Assert.Equal(1, commits);
        Assert.True(SaveButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public async Task A_Benchmark_tick_alone_can_be_saved()
    {
        var data = await OpenWithAsync(Plan());
        var before = data.Snapshot.Plans.Single();
        var cut = OpenSessions(data);

        cut.Find("button[aria-label='Edit Goblet squat']").Click();
        cut.Find(".fa-row__check input").Change(true);
        SaveButton(cut).Click();

        cut.WaitForAssertion(() => Assert.True(data.Snapshot.Exercises.Single(e => e.Id == Squat).IsBenchmark));
        Assert.Equal(before, data.Snapshot.Plans.Single());
        cut.WaitForAssertion(() => Assert.True(SaveButton(cut).HasAttribute("disabled")));
    }

    [Fact]
    public async Task Ticking_and_unticking_Benchmark_leaves_nothing_to_save()
    {
        var cut = OpenSessions(await OpenWithAsync(Plan()));

        cut.Find("button[aria-label='Edit Goblet squat']").Click();
        cut.Find(".fa-row__check input").Change(true);
        cut.Find(".fa-row__check input").Change(false);

        Assert.True(SaveButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public async Task The_Session_sheet_is_not_read_only_just_because_a_save_is_running()
    {
        var cut = OpenSessions(await OpenWithAsync(Plan()));

        cut.Find("button[aria-label='Edit Session Lower']").Click();

        Assert.DoesNotContain("read-only", cut.Find("[role='dialog']").TextContent);
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
        Assert.Equal(3, MainWeekly(data.Snapshot.Plans.Single()).WeeklyTarget);
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
        cut.WaitForAssertion(() => Assert.Equal(5, MainWeekly(data.Snapshot.Plans.Single()).WeeklyTarget));

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
        Assert.Equal(3, MainWeekly(data.Snapshot.Plans.Single()).WeeklyTarget);
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

        cut.WaitForAssertion(() => Assert.Equal(5, MainWeekly(data.Snapshot.Plans.Single()).WeeklyTarget));
        Assert.Equal(1, saves);
    }

    // Track selector and split Settings

    private static Plan MultiTrackPlan() =>
        Plan() with
        {
            Tracks =
            [
                new Track(MainTrack, "Gym", new TrackRhythm.Weekly(3, 2, true), [Ph("p1", "Foundation", 1, 4)]),
                new Track("cardio", "Cardio", new TrackRhythm.Weekly(2, 1, false), [Ph("c1", "Engine", 1, 6)]),
                new Track("mobility", "Mobility", new TrackRhythm.Daily(75), []),
            ],
        };

    private static string[] TrackChipNames(IRenderedComponent<PlanEditor> cut) =>
        [.. cut.FindAll(".fa-planeditor__track button").Select(b => b.TextContent.Trim())];

    [Fact]
    public async Task The_Track_selector_lists_Tracks_in_order_marks_the_first_as_Main_and_has_no_Remove_or_cadence_control()
    {
        var cut = RenderEditor(await OpenWithAsync(MultiTrackPlan()));

        Assert.Equal(["Gym", "Cardio", "Mobility"], TrackChipNames(cut));
        var trackWrappers = cut.FindAll(".fa-planeditor__track");
        Assert.Equal("Main", trackWrappers[0].QuerySelector(".fa-planeditor__track-hint")?.TextContent.Trim());
        Assert.Null(trackWrappers[1].QuerySelector(".fa-planeditor__track-hint"));
        Assert.Null(trackWrappers[2].QuerySelector(".fa-planeditor__track-hint"));
        Assert.Equal("true", Button(cut, "Gym").GetAttribute("aria-pressed"));
        Assert.Equal("false", Button(cut, "Cardio").GetAttribute("aria-pressed"));
        Assert.Equal("false", Button(cut, "Mobility").GetAttribute("aria-pressed"));

        Assert.Equal(["Plan settings", "Track settings"], cut.FindAll(".fa-planform__heading").Select(h => h.TextContent.Trim()));
        Assert.DoesNotContain(cut.FindAll("button"), b => b.TextContent.Contains("Remove Track", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(cut.FindAll(".fa-planform label .fa-label"), l => l.TextContent.Trim() == "Cadence");
    }

    [Fact]
    public async Task Selecting_a_Track_shows_its_settings_by_cadence_and_only_the_main_Track_offers_Custom_workouts_counting()
    {
        var cut = RenderEditor(await OpenWithAsync(MultiTrackPlan()));

        Assert.Equal("Gym", Field(cut, "Track name").GetAttribute("value"));
        Assert.Equal("3", Field(cut, "Weekly target (Workouts)").GetAttribute("value"));
        Assert.Equal("2", Field(cut, "Streak minimum (Workouts a week)").GetAttribute("value"));
        Assert.Single(cut.FindAll(".fa-planform input[type='checkbox']"));
        Assert.Contains("Custom workouts belong to the main Track.", cut.Markup);

        Button(cut, "Cardio").Click();
        Assert.Equal("true", Button(cut, "Cardio").GetAttribute("aria-pressed"));
        Assert.Equal("Cardio", Field(cut, "Track name").GetAttribute("value"));
        Assert.Equal("2", Field(cut, "Weekly target (Workouts)").GetAttribute("value"));
        Assert.Equal("1", Field(cut, "Streak minimum (Workouts a week)").GetAttribute("value"));
        Assert.Empty(cut.FindAll(".fa-planform input[type='checkbox']"));
        Assert.Contains("Custom workouts belong to the main Track.", cut.Markup);

        Button(cut, "Mobility").Click();
        Assert.Equal("true", Button(cut, "Mobility").GetAttribute("aria-pressed"));
        Assert.Equal("Mobility", Field(cut, "Track name").GetAttribute("value"));
        Assert.Equal("75", Field(cut, "Day-done percentage").GetAttribute("value"));
        Assert.Contains("A day is done when this share of the routine is ticked.", cut.Markup);
        Assert.Empty(cut.FindAll(".fa-planform input[type='checkbox']"));
        Assert.DoesNotContain(cut.FindAll(".fa-planform label .fa-label"), l => l.TextContent.Trim() == "Weekly target (Workouts)");
    }

    [Fact]
    public async Task Add_Track_adds_weekly_and_daily_Tracks_with_rule_defaults_selects_them_and_marks_the_editor_dirty()
    {
        var data = await OpenWithAsync(Plan());
        var cut = RenderEditor(data);

        Button(cut, "+ Add Track").Click();
        Field(cut, "Name").Input("Run");
        Field(cut, "Cadence").Change(TrackCadence.Weekly.ToString());
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Add Track").Click();

        Assert.Empty(cut.FindAll("[role='dialog']"));
        Assert.Equal(["Main", "Run"], TrackChipNames(cut));
        Assert.Equal("true", Button(cut, "Run").GetAttribute("aria-pressed"));
        Assert.Equal("Run", Field(cut, "Track name").GetAttribute("value"));
        Assert.Equal("3", Field(cut, "Weekly target (Workouts)").GetAttribute("value"));
        Assert.Equal("2", Field(cut, "Streak minimum (Workouts a week)").GetAttribute("value"));
        Assert.False(SaveButton(cut).HasAttribute("disabled"));

        Button(cut, "+ Add Track").Click();
        Field(cut, "Name").Input("Mobility");
        Field(cut, "Cadence").Change(TrackCadence.Daily.ToString());
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Add Track").Click();

        Assert.Equal(["Main", "Run", "Mobility"], TrackChipNames(cut));
        Assert.Equal("true", Button(cut, "Mobility").GetAttribute("aria-pressed"));
        Assert.Equal("75", Field(cut, "Day-done percentage").GetAttribute("value"));
        SaveButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal(3, data.Snapshot.Plans.Single().Tracks.Count));
        var tracks = data.Snapshot.Plans.Single().Tracks;
        Assert.Equal(("Run", new TrackRhythm.Weekly(3, 2, false)), (tracks[1].Name, tracks[1].Rhythm));
        Assert.Equal(("Mobility", new TrackRhythm.Daily(75)), (tracks[2].Name, tracks[2].Rhythm));
    }

    [Theory]
    [InlineData("   ", "Give the Track a name.")]
    [InlineData("main", "A Track named \"Main\" already exists in this Plan.")]
    public async Task Add_Track_refuses_blank_or_duplicate_names_in_the_sheet(string name, string expected)
    {
        var cut = RenderEditor(await OpenWithAsync(Plan()));

        Button(cut, "+ Add Track").Click();
        Field(cut, "Name").Input(name);
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Add Track").Click();

        Assert.Contains(expected, cut.Find("[role='dialog'] [role='alert']").TextContent);
        Assert.Equal(["Main"], TrackChipNames(cut));
        Assert.True(SaveButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public async Task Move_up_and_Move_down_reorder_Tracks_change_which_Track_is_Main_and_enable_Save()
    {
        var data = await OpenWithAsync(MultiTrackPlan());
        var cut = RenderEditor(data);

        Button(cut, "Cardio").Click();
        Button(cut, "Move up").Click();

        Assert.Equal(["Cardio", "Gym", "Mobility"], TrackChipNames(cut));
        Assert.Equal("Main", cut.FindAll(".fa-planeditor__track")[0].QuerySelector(".fa-planeditor__track-hint")?.TextContent.Trim());
        Assert.Single(cut.FindAll(".fa-planform input[type='checkbox']"));
        Assert.False(SaveButton(cut).HasAttribute("disabled"));

        SaveButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal("Cardio", data.Snapshot.Plans.Single().Tracks[0].Name));

        Button(cut, "Move down").Click();
        Assert.Equal(["Gym", "Cardio", "Mobility"], TrackChipNames(cut));
        Assert.False(SaveButton(cut).HasAttribute("disabled"));
    }

    [Fact]
    public async Task A_daily_Track_cannot_be_moved_first_and_shows_the_rule_message()
    {
        var plan = Plan() with
        {
            Tracks =
            [
                new Track(MainTrack, "Gym", new TrackRhythm.Weekly(3, 2, false), []),
                new Track("mobility", "Mobility", new TrackRhythm.Daily(75), []),
            ],
        };
        var cut = RenderEditor(await OpenWithAsync(plan));

        Button(cut, "Mobility").Click();
        Button(cut, "Move up").Click();

        Assert.Contains("The main Track must be weekly, so a daily Track cannot be first.", cut.Find("[role='alert']").TextContent);
        Assert.Equal(["Gym", "Mobility"], TrackChipNames(cut));
        Assert.True(SaveButton(cut).HasAttribute("disabled"));

        Button(cut, "Gym").Click();
        Button(cut, "Move down").Click();

        Assert.Contains("The main Track must be weekly, so a daily Track cannot be first.", cut.Find("[role='alert']").TextContent);
        Assert.Equal(["Gym", "Mobility"], TrackChipNames(cut));
    }

    [Fact]
    public async Task Track_settings_across_multiple_Tracks_survive_Track_switches_and_persist_after_Save()
    {
        var data = await OpenWithAsync(MultiTrackPlan());
        var cut = RenderEditor(data);

        Field(cut, "Track name").Input("Strength");
        Field(cut, "Weekly target (Workouts)").Input("4");

        Button(cut, "Mobility").Click();
        Field(cut, "Track name").Input("Morning mobility");
        Field(cut, "Day-done percentage").Input("80");

        Button(cut, "Strength").Click();
        Assert.Equal("Strength", Field(cut, "Track name").GetAttribute("value"));
        Assert.Equal("4", Field(cut, "Weekly target (Workouts)").GetAttribute("value"));

        SaveButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal("Strength", data.Snapshot.Plans.Single().Tracks[0].Name));
        var saved = data.Snapshot.Plans.Single();
        Assert.Equal(4, Assert.IsType<TrackRhythm.Weekly>(saved.Tracks[0].Rhythm).WeeklyTarget);
        Assert.Equal("Morning mobility", saved.Tracks[2].Name);
        Assert.Equal(80, Assert.IsType<TrackRhythm.Daily>(saved.Tracks[2].Rhythm).DayDonePercent);
    }

    [Theory]
    [InlineData("Track name", "   ", "Give the Track a name.")]
    [InlineData("Track name", "Cardio", "A Track named \"Cardio\" already exists in this Plan.")]
    [InlineData("Day-done percentage", "0", "The day-done percentage must be between 1 and 100.")]
    [InlineData("Day-done percentage", "101", "The day-done percentage must be between 1 and 100.")]
    [InlineData("Day-done percentage", "lots", "The day-done percentage must be between 1 and 100.")]
    public async Task Invalid_Track_settings_disable_Save_with_the_rule_message(string label, string value, string expected)
    {
        var cut = RenderEditor(await OpenWithAsync(MultiTrackPlan()));
        if (label == "Day-done percentage")
        {
            Button(cut, "Mobility").Click();
        }

        Field(cut, label).Input(value);

        Assert.True(SaveButton(cut).HasAttribute("disabled"));
        Assert.Contains(expected, cut.Find("[role='alert']").TextContent);
        Assert.Contains("Fix the problem on the Settings tab", cut.Markup);
    }

    [Fact]
    public async Task Leaving_with_unsaved_Track_changes_shows_the_discard_prompt()
    {
        var cut = RenderEditor(await OpenWithAsync(MultiTrackPlan()));
        var navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("plans/plan");
        Button(cut, "Cardio").Click();
        Button(cut, "Move up").Click();

        navigation.NavigateTo("plans");

        cut.WaitForAssertion(() => Assert.Contains("Discard changes?", cut.Markup));
        Assert.Equal("plans/plan", CurrentPath);
    }

    [Fact]
    public async Task A_read_only_window_disables_Track_controls_and_settings()
    {
        var store = new InMemoryDataStore();
        await OpenWithAsync(MultiTrackPlan(), store);
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var cut = RenderEditor(locked);

        Assert.Contains("read-only", cut.Markup);
        Assert.True(Button(cut, "+ Add Track").HasAttribute("disabled"));
        Assert.True(Button(cut, "Move up").HasAttribute("disabled"));
        Assert.True(Button(cut, "Move down").HasAttribute("disabled"));
        Assert.All(cut.FindAll(".fa-planform input"), i => Assert.True(i.HasAttribute("disabled")));
    }

    [Fact]
    public async Task After_Save_the_Plans_list_and_Home_reflect_an_edited_main_Track_and_its_weekly_target()
    {
        var data = await OpenWithAsync(MultiTrackPlan());
        var cut = RenderEditor(data);

        Button(cut, "Cardio").Click();
        Field(cut, "Weekly target (Workouts)").Input("5");
        Button(cut, "Move up").Click();
        SaveButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal("Cardio", data.Snapshot.Plans.Single().Tracks[0].Name));
        Assert.Equal(5, MainWeekly(data.Snapshot.Plans.Single()).WeeklyTarget);

        var home = Render<Home>();
        Assert.Contains("target 5", home.Markup);

        var plans = Render<Plans>();
        Assert.Contains("Active · week 1 of 6", plans.Markup);
    }

    // Phases and Sessions per Track

    private static Plan MultiTrackPlanWithSessions() =>
        MultiTrackPlan() with
        {
            Sessions =
            [
                Session("lower", "Lower", DayOfWeek.Monday, SessionType.Strength, null,
                [
                    new SessionExercise(Squat, 3, 8, null, "Chest up"),
                    new SessionExercise(Lunge, 2, 10, null, null),
                ]),
                Session("tempo", "Tempo Run", DayOfWeek.Thursday, SessionType.Cardio, null,
                [
                    new SessionExercise("plank", 3, null, null, null),
                ], trackId: "cardio"),
                new Session("morning", "Morning Flow", "mobility", DefaultDay.Daily, SessionType.Mobility, null,
                [
                    new SessionExercise("plank", 2, null, null, null),
                ]),
            ],
        };

    [Fact]
    public async Task Switching_Tracks_swaps_the_Phases_and_Sessions_shown_and_a_Track_with_no_Phases_says_so_with_Add_Phase()
    {
        var cut = RenderEditor(await OpenWithAsync(MultiTrackPlanWithSessions()));

        ShowTab(cut, "Phases");
        Assert.Equal(["Foundation"], cut.FindAll(".fa-phase__name").Select(n => n.TextContent.Trim()));
        Assert.Contains("Weeks 1 to 4 · 16 Nov to 13 Dec 2026", cut.Find(".fa-phase__weeks").TextContent);

        Button(cut, "Cardio").Click();
        Assert.Equal(["Engine"], cut.FindAll(".fa-phase__name").Select(n => n.TextContent.Trim()));
        Assert.Contains("Weeks 1 to 6 · 16 Nov to 27 Dec 2026", cut.Find(".fa-phase__weeks").TextContent);

        Button(cut, "Mobility").Click();
        Assert.Empty(cut.FindAll(".fa-phase"));
        Assert.Contains("No Phases for this Track", cut.Markup);
        Assert.False(Button(cut, "Add Phase").HasAttribute("disabled"));

        ShowTab(cut, "Sessions");
        Assert.Equal(["Morning Flow"], cut.FindAll(".fa-session__name").Select(n => n.TextContent.Trim()));
        Assert.Contains("Every day · Mobility", cut.Find(".fa-session__meta").TextContent);

        Button(cut, "Cardio").Click();
        Assert.Equal(["Tempo Run"], cut.FindAll(".fa-session__name").Select(n => n.TextContent.Trim()));
        Assert.Contains("Thursday · Cardio", cut.Find(".fa-session__meta").TextContent);

        Button(cut, "Gym").Click();
        Assert.Equal(["Lower"], cut.FindAll(".fa-session__name").Select(n => n.TextContent.Trim()));
        Assert.Contains("Monday · Strength", cut.Find(".fa-session__meta").TextContent);
    }

    [Fact]
    public async Task Phase_overlap_inside_one_Track_is_refused_naming_both_Phases_while_same_weeks_in_another_Track_are_allowed()
    {
        var data = await OpenWithAsync(MultiTrackPlanWithSessions());
        var cut = RenderEditor(data);

        ShowTab(cut, "Phases");
        Button(cut, "Mobility").Click();
        Button(cut, "Add Phase").Click();
        Field(cut, "Focus").Input("Habit");
        Field(cut, "First week").Change("1");
        Field(cut, "Last week").Change("4");

        // Weeks 1 to 4 are already used by Gym's "Foundation", which is allowed on a different Track.
        Assert.Empty(cut.FindAll("[role='alert']"));
        Assert.False(SaveButton(cut).HasAttribute("disabled"));

        Button(cut, "Add Phase").Click();
        Field(cut, "Focus").Input("Maintain");
        Field(cut, "First week").Change("3");
        Field(cut, "Last week").Change("6");

        var alert = cut.Find("[role='alert']").TextContent;
        Assert.Contains("Maintain", alert);
        Assert.Contains("Habit", alert);
        Assert.Contains("overlaps", alert);
        Assert.True(SaveButton(cut).HasAttribute("disabled"));

        Field(cut, "First week").Change("5");
        Assert.Empty(cut.FindAll("[role='alert']"));
        Assert.False(SaveButton(cut).HasAttribute("disabled"));
        SaveButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal(2, data.Snapshot.Plans.Single().Tracks[2].Phases.Count));
        Assert.Equal(["Habit", "Maintain"], data.Snapshot.Plans.Single().Tracks[2].Phases.Select(p => p.Focus));
    }

    [Fact]
    public async Task Add_Session_on_a_daily_Track_has_no_day_picker_says_Every_day_and_Editing_a_daily_Session_does_not_offer_a_day_picker()
    {
        var data = await OpenWithAsync(MultiTrackPlanWithSessions());
        var cut = OpenSessions(data);

        Button(cut, "Mobility").Click();
        Button(cut, "Add Session").Click();

        var dialog = cut.Find("[role='dialog']");
        Assert.Contains("Every day", dialog.TextContent);
        Assert.DoesNotContain(dialog.QuerySelectorAll("select"), s => s.ParentElement?.QuerySelector(".fa-label")?.TextContent.Trim() == "Default day");
        Field(cut, "Name").Input("Evening Stretch");
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Add Session").Click();

        Assert.Equal(["Morning Flow", "Evening Stretch"], cut.FindAll(".fa-session__name").Select(n => n.TextContent.Trim()));
        SaveButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal(4, data.Snapshot.Plans.Single().Sessions.Count));
        var added = data.Snapshot.Plans.Single().Sessions[^1];
        Assert.Equal(("Evening Stretch", "mobility", DefaultDay.Daily, SessionType.Mobility), (added.Name, added.TrackId, added.DefaultDay, added.Type));

        cut.Find("button[aria-label='Edit Session Morning Flow']").Click();
        dialog = cut.Find("[role='dialog']");
        Assert.Contains("Every day", dialog.TextContent);
        Assert.DoesNotContain(dialog.QuerySelectorAll("select"), s => s.ParentElement?.QuerySelector(".fa-label")?.TextContent.Trim() == "Default day");
        Field(cut, "Name").Input("Morning Routine");
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Done").Click();

        SaveButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal("Morning Routine", data.Snapshot.Plans.Single().Sessions[2].Name));
        Assert.Equal(DefaultDay.Daily, data.Snapshot.Plans.Single().Sessions[2].DefaultDay);
    }

    [Fact]
    public async Task Weekly_Track_day_picker_lists_Monday_to_Sunday_without_Daily_and_Type_includes_Mobility()
    {
        var cut = OpenSessions(await OpenWithAsync(MultiTrackPlanWithSessions()));

        Button(cut, "Add Session").Click();

        var dayOptions = Field(cut, "Default day").QuerySelectorAll("option").Select(o => o.TextContent.Trim()).ToList();
        Assert.Equal(["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"], dayOptions);
        Assert.DoesNotContain("Daily", dayOptions);

        var typeOptions = Field(cut, "Type").QuerySelectorAll("option").Select(o => o.TextContent.Trim()).ToList();
        Assert.Contains("Mobility", typeOptions);
    }

    [Fact]
    public async Task Benchmark_toggle_and_interval_appear_on_the_main_Track_only()
    {
        var data = await OpenWithAsync(MultiTrackPlanWithSessions());
        var cut = OpenSessions(data);

        // Main Track row has Benchmark exercise toggle
        cut.Find("button[aria-label='Edit Goblet squat']").Click();
        Assert.Contains("Benchmark exercise", cut.Find(".fa-row__edit").TextContent);
        Assert.NotEmpty(cut.FindAll(".fa-row__check input"));

        // Main Track Session sheet offers Benchmark interval (weeks) when Type is Test
        Button(cut, "Add Session").Click();
        Field(cut, "Name").Input("Benchmark Test");
        Field(cut, "Type").Change(SessionType.Test.ToString());
        Assert.Contains("Benchmark interval (weeks)", cut.Find("[role='dialog']").TextContent);
        Field(cut, "Benchmark interval (weeks)").Input("6");
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Add Session").Click();

        // Extra weekly Track has neither Benchmark exercise toggle nor Benchmark interval field
        Button(cut, "Cardio").Click();
        cut.Find("button[aria-label='Edit Plank']").Click();
        Assert.DoesNotContain("Benchmark exercise", cut.Find(".fa-row__edit").TextContent);
        Assert.Empty(cut.FindAll(".fa-row__check input"));

        Button(cut, "Add Session").Click();
        Field(cut, "Type").Change(SessionType.Test.ToString());
        Assert.DoesNotContain("Benchmark interval (weeks)", cut.Find("[role='dialog']").TextContent);
        cut.FindAll("[role='dialog'] button").Single(b => b.TextContent.Trim() == "Cancel").Click();

        // Daily Track row has no Benchmark exercise toggle either
        Button(cut, "Mobility").Click();
        cut.Find("button[aria-label='Edit Plank']").Click();
        Assert.DoesNotContain("Benchmark exercise", cut.Find(".fa-row__edit").TextContent);
        Assert.Empty(cut.FindAll(".fa-row__check input"));

        SaveButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Equal(6, data.Snapshot.Plans.Single().Sessions.Single(s => s.Name == "Benchmark Test").BenchmarkIntervalWeeks));
    }

    [Fact]
    public async Task A_Phase_override_Session_choice_lists_only_the_selected_Tracks_Sessions()
    {
        var data = await OpenWithAsync(MultiTrackPlanWithSessions());
        var cut = RenderEditor(data);

        ShowTab(cut, "Phases");
        cut.Find("button[aria-label='Edit Foundation']").Click();
        Button(cut, "Add override").Click();

        var gymSessionOptions = Field(cut, "Override Session").QuerySelectorAll("option").Select(o => o.TextContent.Trim()).ToList();
        Assert.Equal(["Lower"], gymSessionOptions);

        Button(cut, "Cardio").Click();
        cut.Find("button[aria-label='Edit Engine']").Click();
        Button(cut, "Add override").Click();

        var cardioSessionOptions = Field(cut, "Override Session").QuerySelectorAll("option").Select(o => o.TextContent.Trim()).ToList();
        Assert.Equal(["Tempo Run"], cardioSessionOptions);

        SaveButton(cut).Click();
        cut.WaitForAssertion(() => Assert.Single(data.Snapshot.Plans.Single().Tracks[1].Phases[0].SessionOverrides));
        Assert.Equal("tempo", data.Snapshot.Plans.Single().Tracks[1].Phases[0].SessionOverrides[0].SessionId);
    }

    [Fact]
    public async Task A_problem_on_a_non_selected_Track_still_disables_Save_and_names_the_tab_and_Track()
    {
        var cut = RenderEditor(await OpenWithAsync(MultiTrackPlanWithSessions()));

        // Cause a Phase problem on Mobility, then switch back to Gym
        ShowTab(cut, "Phases");
        Button(cut, "Mobility").Click();
        Button(cut, "Add Phase").Click();
        Field(cut, "First week").Change("5");
        Field(cut, "Last week").Change("2");
        Assert.True(SaveButton(cut).HasAttribute("disabled"));
        Assert.Contains("Fix the problem on the Phases tab to save.", cut.Markup);

        Button(cut, "Gym").Click();
        Assert.True(SaveButton(cut).HasAttribute("disabled"));
        Assert.Contains("Fix the problem on the Phases (Mobility) tab to save.", cut.Markup);

        // Fix the Phase problem on Mobility, then cause a Session row problem on Cardio and switch to Gym
        Button(cut, "Mobility").Click();
        Field(cut, "First week").Change("1");
        Field(cut, "Last week").Change("4");
        Assert.False(SaveButton(cut).HasAttribute("disabled"));

        ShowTab(cut, "Sessions");
        Button(cut, "Cardio").Click();
        cut.Find("button[aria-label='Edit Plank']").Click();
        Field(cut, "Sets").Change("0");
        Assert.True(SaveButton(cut).HasAttribute("disabled"));

        Button(cut, "Gym").Click();
        Assert.True(SaveButton(cut).HasAttribute("disabled"));
        Assert.Contains("Fix the problem on the Sessions (Cardio) tab to save.", cut.Markup);
    }

    [Fact]
    public async Task Saving_multi_Track_Phase_and_Session_edits_leaves_past_Workouts_unchanged()
    {
        var past = Finished("w-past", Today.AddDays(-7), Set("s1", 20, 8));
        var data = await OpenSeededAsync(plan: MultiTrackPlanWithSessions(), workouts: past);
        var cut = OpenSessions(data);

        Button(cut, "Cardio").Click();
        cut.Find("button[aria-label='Edit Plank']").Click();
        Field(cut, "Sets").Change("5");
        SaveButton(cut).Click();

        cut.WaitForAssertion(() => Assert.Equal(5, data.Snapshot.Plans.Single().Sessions[1].Exercises[0].Sets));
        Assert.Equal(past, data.Snapshot.Workouts.Single());
    }

    [Fact]
    public async Task A_read_only_window_disables_per_Track_Phase_and_Session_edits_and_shows_the_reason()
    {
        var store = new InMemoryDataStore();
        await OpenWithAsync(MultiTrackPlanWithSessions(), store);
        var locked = await AppData.OpenAsync(store, new InMemoryStorageEnvironment { WriterLock = WriterLockResult.HeldElsewhere });
        var readOnlyCut = RenderEditor(locked);

        ShowTab(readOnlyCut, "Phases");
        Button(readOnlyCut, "Cardio").Click();
        Assert.Contains("read-only", readOnlyCut.Markup);
        Assert.True(Button(readOnlyCut, "Add Phase").HasAttribute("disabled"));

        ShowTab(readOnlyCut, "Sessions");
        Button(readOnlyCut, "Mobility").Click();
        Assert.True(Button(readOnlyCut, "Add Session").HasAttribute("disabled"));
        Assert.True(Button(readOnlyCut, "Edit Session").HasAttribute("disabled"));
    }

    [Fact]
    public async Task The_editor_header_shows_the_active_week_headline_from_the_main_Tracks_Phases_and_hides_it_for_an_inactive_Plan()
    {
        var activeWithPhases = MultiTrackPlan();
        var inactive = MultiTrackPlan() with { Id = "inactive", IsActive = false };
        var data = await OpenWithAsync(activeWithPhases);
        await data.CommitAsync(Change.Put(inactive));

        var cutWithPhases = RenderEditor(data, "plan");
        Assert.Equal("Active · week 1 of 4", cutWithPhases.Find(".fa-planeditor__active").TextContent.Trim());

        var cutInactive = Render<PlanEditor>(p => p.Add(x => x.PlanId, "inactive"));
        Assert.Empty(cutInactive.FindAll(".fa-planeditor__active"));

        var activeWithoutPhases = MultiTrackPlan() with
        {
            Tracks =
            [
                new Track(MainTrack, "Gym", new TrackRhythm.Weekly(3, 2, true), []),
                new Track("cardio", "Cardio", new TrackRhythm.Weekly(2, 1, false), [Ph("c1", "Engine", 1, 6)]),
            ],
        };
        await data.CommitAsync(Change.Put(activeWithoutPhases));
        var cutWithoutPhases = Render<PlanEditor>(p => p.Add(x => x.PlanId, "plan"));
        Assert.Equal("Active · week 1", cutWithoutPhases.Find(".fa-planeditor__active").TextContent.Trim());
    }
}
