using Core.Model;
using Core.Rules.PlanEditing;
using Core.Tests.Ids;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.PlanEditing;

public class PlanEditingRulesTests
{
    private static Phase Ph(string id, string focus, int start, int end) => new(id, focus, start, end, [], []);

    private static SessionExercise Row(string id, int sets = 3, int? reps = 8, int? max = null) => new(id, sets, reps, max, null);

    private static Plan Plan() => PlanFixture.Make(start: Mon, phases: [Ph("p1", "Foundation", 1, 4), Ph("p2", "Build", 5, 8)], sessions:
    [
        PlanFixture.Session("lower", DayOfWeek.Monday, name: "Lower", exercises: [Row("squat"), Row("lunge"), Row("curl")]),
        PlanFixture.Session("upper", DayOfWeek.Wednesday, name: "Upper", exercises: [Row("press")]),
    ]);

    private static PlanSettings Settings(Plan plan)
    {
        var weekly = (TrackRhythm.Weekly)plan.Tracks[0].Rhythm;
        return new(plan.StartDate, plan.Goal, plan.GoalDate, weekly.WeeklyTarget, weekly.StreakMinWorkoutsPerWeek, weekly.StreakCountsCustomWorkouts, plan.WeightIncrementKg);
    }

    private static T Valid<T>(PlanEditResult<T> result) => Assert.IsType<PlanEditResult<T>.Valid>(result).Value;

    private static string Reason<T>(PlanEditResult<T> result) => Assert.IsType<PlanEditResult<T>.Invalid>(result).Message;

    private static string[] Ids(IEnumerable<SessionExercise> rows) => [.. rows.Select(r => r.ExerciseId)];

    private static Phase WithGoal(string exerciseId, double kg, int reps) =>
        new("g", "Strength", 1, 4, [new PhaseGoal(exerciseId, kg, reps)], []);

    [Theory]
    [InlineData("", 50, 5)]
    [InlineData("squat", -1, 5)]
    [InlineData("squat", 50, 0)]
    public void A_Phase_goal_without_an_Exercise_or_with_impossible_numbers_is_refused(string exerciseId, double kg, int reps)
    {
        var reason = PlanPhaseRules.ValidatePhases([WithGoal(exerciseId, kg, reps)]);

        Assert.NotNull(reason);
        Assert.Contains("Strength", reason);
    }

    [Fact]
    public void A_Phase_goal_with_zero_weight_is_allowed()
    {
        Assert.Null(PlanPhaseRules.ValidatePhases([WithGoal("pullup", 0, 8)]));
    }

    // Settings

    [Fact]
    public void Valid_settings_replace_the_Plan_level_values_and_blank_goal_text_is_absent()
    {
        var plan = Plan();

        var changed = Valid(PlanSettingsRules.WithSettings(plan, Settings(plan) with { Goal = "  ", WeeklyTarget = 4, StreakCountsCustomWorkouts = true }));

        Assert.Null(changed.Goal);
        var weekly = Assert.IsType<TrackRhythm.Weekly>(changed.Tracks[0].Rhythm);
        Assert.Equal(4, weekly.WeeklyTarget);
        Assert.True(weekly.StreakCountsCustomWorkouts);
        Assert.Equal(plan.Sessions, changed.Sessions);
    }

    [Theory]
    [InlineData(0, 2, "weekly target")]
    [InlineData(3, 0, "Streak minimum")]
    public void A_weekly_target_or_Streak_minimum_below_one_is_refused(int target, int streak, string expected)
    {
        var plan = Plan();

        Assert.Contains(expected, Reason(PlanSettingsRules.WithSettings(plan, Settings(plan) with { WeeklyTarget = target, StreakMinWorkoutsPerWeek = streak })));
    }

    [Fact]
    public void A_goal_date_before_the_start_is_refused_and_on_the_start_is_allowed()
    {
        var plan = Plan();

        Assert.Contains("goal date", Reason(PlanSettingsRules.WithSettings(plan, Settings(plan) with { GoalDate = Mon.AddDays(-1) })));
        Assert.Equal(Mon, Valid(PlanSettingsRules.WithSettings(plan, Settings(plan) with { GoalDate = Mon })).GoalDate);
    }

    [Fact]
    public void A_weight_increment_of_zero_is_refused() =>
        Assert.Contains("increment", Reason(PlanSettingsRules.WithSettings(Plan(), Settings(Plan()) with { WeightIncrementKg = 0 })));

    // Phases

    [Fact]
    public void Phases_that_follow_each_other_from_week_one_are_valid_in_any_order()
    {
        Assert.Null(PlanPhaseRules.ValidatePhases([Ph("b", "Build", 5, 8), Ph("a", "Foundation", 1, 4)]));
        Assert.Null(PlanPhaseRules.ValidatePhases([]));
    }

    [Fact]
    public void Overlapping_Phases_are_refused_naming_both()
    {
        var reason = PlanPhaseRules.ValidatePhases([Ph("a", "Foundation", 1, 4), Ph("b", "Build", 4, 8)]);

        Assert.Contains("Foundation", reason);
        Assert.Contains("Build", reason);
        Assert.Contains("overlaps", reason);
    }

    [Fact]
    public void A_Phase_that_ends_before_it_starts_is_refused_naming_it() =>
        Assert.Contains("Build", PlanPhaseRules.ValidatePhases([Ph("a", "Foundation", 1, 4), Ph("b", "Build", 8, 5)]));

    [Fact]
    public void A_gap_between_Phases_or_before_the_first_is_refused()
    {
        Assert.Contains("in no Phase", PlanPhaseRules.ValidatePhases([Ph("a", "Foundation", 1, 4), Ph("b", "Build", 6, 8)]));
        Assert.Contains("start at week 1", PlanPhaseRules.ValidatePhases([Ph("b", "Build", 2, 8)]));
        Assert.Contains("week 1 or later", PlanPhaseRules.ValidatePhases([Ph("b", "Build", 0, 8)]));
    }

    [Fact]
    public void WithPhases_sorts_valid_Phases_by_week_and_refuses_bad_ones_without_changing_the_Plan()
    {
        var plan = Plan();

        var sorted = Valid(PlanPhaseRules.WithPhases(plan, [Ph("b", "Build", 5, 8), Ph("a", "Foundation", 1, 4)]));
        var refused = PlanPhaseRules.WithPhases(plan, [Ph("a", "Foundation", 1, 5), Ph("b", "Build", 5, 8)]);

        Assert.Equal(["a", "b"], sorted.Tracks[0].Phases.Select(p => p.Id));
        Assert.Contains("overlaps", Reason(refused));
    }

    [Fact]
    public void Phase_dates_come_from_the_Plan_start_week()
    {
        var plan = PlanFixture.Make(start: Mon.AddDays(2), phases: [Ph("p", "Build", 2, 3)]);

        var (start, end) = PlanPhaseRules.PhaseDates(plan, plan.Tracks[0].Phases[0]);

        Assert.Equal(Mon.AddDays(7), start);
        Assert.Equal(Mon.AddDays(20), end);
    }

    // Rows

    [Theory]
    [InlineData(3, 8, null, "3 x 8")]
    [InlineData(3, 8, 8, "3 x 8")]
    [InlineData(3, 8, 12, "3 x 8 to 12")]
    [InlineData(2, null, null, "2 sets")]
    public void A_row_summary_reads_as_sets_and_reps(int sets, int? reps, int? max, string expected) =>
        Assert.Equal(expected, SessionRowRules.Summary(new SessionExercise("x", sets, reps, max, null)));

    [Theory]
    [InlineData(3, 8, null, null)]
    [InlineData(1, 1, 1, null)]
    [InlineData(3, null, null, null)]
    [InlineData(0, 8, null, "Sets")]
    [InlineData(3, 0, null, "Reps must")]
    [InlineData(3, 8, 7, "highest")]
    [InlineData(3, null, 8, "highest")]
    public void Row_numbers_are_validated(int sets, int? reps, int? max, string? expected)
    {
        var reason = SessionRowRules.ValidateRow(new SessionExercise("x", sets, reps, max, null));

        if (expected is null)
        {
            Assert.Null(reason);
        }
        else
        {
            Assert.Contains(expected, reason);
        }
    }

    [Fact]
    public void A_max_equal_to_Reps_is_allowed() => Assert.Null(SessionRowRules.ValidateRow(Row("x", reps: 8, max: 8)));

    [Fact]
    public void ValidateRows_reports_the_first_bad_row()
    {
        Assert.Null(SessionRowRules.ValidateRows([Row("a"), Row("b")]));
        Assert.Contains("Sets", SessionRowRules.ValidateRows([Row("a"), Row("b", sets: 0), Row("c", reps: 0)]));
    }

    [Fact]
    public void Move_up_and_down_swap_neighbours_and_stop_at_the_ends()
    {
        var rows = new[] { Row("a"), Row("b"), Row("c") };

        Assert.Equal(["b", "a", "c"], Ids(SessionRowRules.MoveUp(rows, 1)));
        Assert.Equal(["a", "c", "b"], Ids(SessionRowRules.MoveDown(rows, 1)));
        Assert.Equal(["a", "b", "c"], Ids(SessionRowRules.MoveUp(rows, 0)));
        Assert.Equal(["a", "b", "c"], Ids(SessionRowRules.MoveDown(rows, 2)));
        Assert.Equal(["a", "b", "c"], Ids(SessionRowRules.MoveDown(rows, 9)));
    }

    [Fact]
    public void Remove_takes_out_one_row_and_the_last_row_can_go()
    {
        var rows = new[] { Row("a"), Row("b") };

        var one = SessionRowRules.Remove(rows, 0);

        Assert.Equal(["b"], Ids(one));
        Assert.Empty(SessionRowRules.Remove(one, 0));
        Assert.Equal(["a", "b"], Ids(SessionRowRules.Remove(rows, 5)));
    }

    [Fact]
    public void Adding_from_the_library_appends_at_the_end_once()
    {
        var rows = new[] { Row("a") };

        var added = SessionRowRules.AddFromLibrary(rows, "b");

        Assert.Equal(["a", "b"], Ids(added));
        Assert.Equal((3, 8), (added[1].Sets, added[1].Reps));
        Assert.Equal(["a", "b"], Ids(SessionRowRules.AddFromLibrary(added, "a")));
    }

    [Fact]
    public void WithRows_changes_only_that_Sessions_default_rows()
    {
        var plan = Plan();

        var changed = SessionRowRules.WithRows(plan, "lower", [Row("squat")]);

        Assert.Equal(["squat"], Ids(changed.Sessions[0].Exercises));
        Assert.Equal(plan.Sessions[1], changed.Sessions[1]);
    }

    // Sessions

    [Fact]
    public void Editing_a_Session_changes_its_name_day_and_type_and_a_blank_name_is_refused()
    {
        var plan = Plan();

        var changed = Valid(SessionRowRules.EditSession(plan, "upper", "  Push  ", DefaultDay.Thursday, SessionType.Cardio));

        Assert.Equal(("Push", DefaultDay.Thursday, SessionType.Cardio), (changed.Sessions[1].Name, changed.Sessions[1].DefaultDay, changed.Sessions[1].Type));
        Assert.Equal(plan.Sessions[0], changed.Sessions[0]);
        Assert.Contains("name", Reason(SessionRowRules.EditSession(plan, "upper", " ", DefaultDay.Thursday, SessionType.Cardio)));
    }

    [Fact]
    public void Changing_a_benchmark_Session_to_another_type_drops_its_interval()
    {
        var plan = PlanFixture.Make(sessions: [PlanFixture.Session("t", DayOfWeek.Saturday, SessionType.Test, benchmarkEveryWeeks: 6)]);

        var changed = Valid(SessionRowRules.EditSession(plan, "t", "Test", DefaultDay.Saturday, SessionType.Strength));

        Assert.Null(changed.Sessions[0].BenchmarkIntervalWeeks);
    }

    [Fact]
    public void A_new_Session_is_added_empty_at_the_end_with_a_fresh_id()
    {
        var plan = Plan();

        var changed = Valid(SessionRowRules.AddSession(plan, " Run ", DefaultDay.Friday, SessionType.Cardio, new SequentialIdGenerator("s")));

        var added = changed.Sessions[^1];
        Assert.Equal(3, changed.Sessions.Count);
        Assert.Equal(("s-1", "Run", DefaultDay.Friday, SessionType.Cardio), (added.Id, added.Name, added.DefaultDay, added.Type));
        Assert.Empty(added.Exercises);
        Assert.Contains("name", Reason(SessionRowRules.AddSession(plan, "", DefaultDay.Friday, SessionType.Cardio, new SequentialIdGenerator("s"))));
    }

    // Plans

    [Fact]
    public void A_copy_has_fresh_ids_is_inactive_and_keeps_the_content()
    {
        var phase = Ph("p1", "Build", 1, 4) with { SessionOverrides = [new SessionOverride("lower", [Row("squat", sets: 5)])] };
        var plan = PlanFixture.WithMainPhases(Plan(), [phase]) with { IsActive = true };

        var copy = Valid(PlanLifecycleRules.Copy(plan, " Variation ", new SequentialIdGenerator("n")));

        Assert.False(copy.IsActive);
        Assert.Equal("Variation", copy.Name);
        Assert.NotEqual(plan.Id, copy.Id);
        Assert.Empty(copy.Sessions.Select(s => s.Id).Intersect(plan.Sessions.Select(s => s.Id)));
        Assert.Empty(copy.Tracks[0].Phases.Select(p => p.Id).Intersect(plan.Tracks[0].Phases.Select(p => p.Id)));
        Assert.Equal(copy.Sessions[0].Id, copy.Tracks[0].Phases[0].SessionOverrides[0].SessionId);
        Assert.Equal(plan.Sessions.Select(s => (s.Name, s.DefaultDay, Ids(s.Exercises))), copy.Sessions.Select(s => (s.Name, s.DefaultDay, Ids(s.Exercises))));
        Assert.Equal(plan.StartDate, copy.StartDate);
        Assert.Equal(5, copy.Tracks[0].Phases[0].SessionOverrides[0].Exercises[0].Sets);
        Assert.True(plan.IsActive);
    }

    [Fact]
    public void All_the_ids_in_a_copy_are_different_from_each_other()
    {
        var copy = Valid(PlanLifecycleRules.Copy(Plan(), "Copy", new SequentialIdGenerator("n")));

        var all = new[] { copy.Id }.Concat(copy.Tracks.Select(t => t.Id)).Concat(copy.Sessions.Select(s => s.Id)).Concat(copy.Tracks[0].Phases.Select(p => p.Id)).ToList();
        Assert.Equal(all.Count, all.Distinct().Count());
    }

    [Fact]
    public void A_new_Plan_from_scratch_is_inactive_and_empty_and_a_blank_name_is_refused()
    {
        var plan = Valid(PlanLifecycleRules.FromScratch(" Offseason ", Mon, new SequentialIdGenerator("n")));

        Assert.Equal(("n-1", "Offseason", false, Mon), (plan.Id, plan.Name, plan.IsActive, plan.StartDate));
        Assert.Empty(plan.Sessions);
        Assert.Empty(plan.Tracks[0].Phases);
        Assert.Null(PlanPhaseRules.ValidatePhases(plan.Tracks[0].Phases));
        Assert.Contains("name", Reason(PlanLifecycleRules.FromScratch("", Mon, new SequentialIdGenerator("n"))));
    }

    [Fact]
    public void Making_a_Plan_active_returns_both_changes_so_exactly_one_Plan_is_active()
    {
        var current = Plan() with { Id = "a", IsActive = true };
        var other = Plan() with { Id = "b", IsActive = false };
        var third = Plan() with { Id = "c", IsActive = false };
        IReadOnlyList<Plan> plans = [current, other, third];

        var changed = PlanLifecycleRules.MakeActive(plans, "b");

        Assert.Equal(["a", "b"], changed.Select(p => p.Id).Order());
        var after = plans.Select(p => changed.FirstOrDefault(c => c.Id == p.Id) ?? p).ToList();
        Assert.Equal(["b"], after.Where(p => p.IsActive).Select(p => p.Id));
    }

    [Fact]
    public void Making_the_active_or_an_unknown_Plan_active_changes_nothing()
    {
        IReadOnlyList<Plan> plans = [Plan() with { Id = "a", IsActive = true }, Plan() with { Id = "b", IsActive = false }];

        Assert.Empty(PlanLifecycleRules.MakeActive(plans, "a"));
        Assert.Empty(PlanLifecycleRules.MakeActive(plans, "zzz"));
    }

    // Going forward only

    [Fact]
    public void Editing_a_Plan_leaves_a_past_Workouts_own_planned_rows_alone()
    {
        var plan = Plan();
        var workout = Logged(Mon, "lower") with { Planned = [Row("squat", sets: 3), Row("lunge")] };

        var changed = SessionRowRules.WithRows(plan, "lower", [Row("deadlift", sets: 5)]);

        Assert.Equal(["deadlift"], Ids(changed.Sessions[0].Exercises));
        Assert.Equal(["squat", "lunge"], Ids(workout.Planned));
        Assert.Equal(3, workout.Planned[0].Sets);
    }
}
