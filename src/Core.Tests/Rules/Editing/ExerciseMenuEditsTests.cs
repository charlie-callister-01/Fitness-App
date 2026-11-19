using Core.Model;
using Core.Rules.Editing;
using Core.Rules.Outcomes;
using Core.Tests.Rules.Outcomes;

namespace Core.Tests.Rules.Editing;

public class ExerciseMenuEditsTests
{
    private static WorkoutSet Set(string id) => new(id, 20, 10, null, null, null, false);

    private static Workout Planned() => new WorkoutBuilder().Plan("squat", "bench", "row").Build();

    private static Workout WithDoneSets(string exerciseId, int? rpe = null) =>
        Planned() with { Entries = [new WorkoutEntry(exerciseId, EntryStatus.Done, null, null, rpe, [Set("a"), Set("b")])] };

    // Skip

    [Fact]
    public void Skip_stores_a_Skipped_entry_with_the_reason_and_no_Sets_or_RPE()
    {
        var workout = WorkoutEdits.Skip(Planned(), "squat", "rack taken");

        Assert.Equal(new WorkoutEntry("squat", EntryStatus.Skipped, "rack taken", null, null, []), Assert.Single(workout.Entries));
    }

    [Fact]
    public void Skip_without_a_reason_stores_a_null_reason()
    {
        var workout = WorkoutEdits.Skip(Planned(), "squat", null);

        Assert.Null(Assert.Single(workout.Entries).SkipReason);
    }

    [Fact]
    public void Skipping_an_Exercise_with_confirmed_Sets_replaces_them_and_the_RPE()
    {
        var workout = WorkoutEdits.Skip(WithDoneSets("squat", rpe: 8), "squat", null);

        var entry = Assert.Single(workout.Entries);
        Assert.Equal(EntryStatus.Skipped, entry.Status);
        Assert.Empty(entry.Sets);
        Assert.Null(entry.Rpe);
    }

    [Fact]
    public void Skipping_twice_keeps_one_entry_with_the_latest_reason()
    {
        var workout = WorkoutEdits.Skip(WorkoutEdits.Skip(Planned(), "squat", "a"), "squat", "b");

        Assert.Equal("b", Assert.Single(workout.Entries).SkipReason);
    }

    [Fact]
    public void Un_skip_removes_the_Skipped_entry_so_the_Exercise_is_not_yet_done()
    {
        var workout = WorkoutEdits.Unskip(WorkoutEdits.Skip(Planned(), "squat", "x"), "squat");

        Assert.Empty(workout.Entries);
        Assert.Equal(RowOutcomeKind.Unrecorded, WorkoutOutcomes.Of(workout).Rows[0].Kind);
    }

    [Fact]
    public void Un_skip_leaves_other_entries_alone()
    {
        var workout = WorkoutEdits.Unskip(WithDoneSets("bench"), "squat");

        Assert.Equal(2, Assert.Single(workout.Entries).Sets.Count);
    }

    // Swap

    [Fact]
    public void Swap_stores_a_Swapped_entry_for_the_new_Exercise_naming_the_original()
    {
        var workout = WorkoutEdits.Swap(Planned(), "squat", "legpress");

        Assert.Equal(new WorkoutEntry("legpress", EntryStatus.Swapped, null, "squat", null, []), Assert.Single(workout.Entries));
    }

    [Fact]
    public void Swap_leaves_the_planned_rows_unchanged_and_the_outcomes_rule_reports_the_swap()
    {
        var before = Planned();
        var workout = WorkoutEdits.Swap(before, "squat", "legpress");

        Assert.Equal(before.Planned, workout.Planned);
        var row = WorkoutOutcomes.Of(workout).Rows[0];
        Assert.Equal(RowOutcomeKind.Swapped, row.Kind);
        Assert.Equal("legpress", row.ReplacementExerciseId);
    }

    [Fact]
    public void Swapping_again_replaces_the_earlier_swap()
    {
        var workout = WorkoutEdits.Swap(WorkoutEdits.Swap(Planned(), "squat", "legpress"), "squat", "lunge");

        var entry = Assert.Single(workout.Entries);
        Assert.Equal("lunge", entry.ExerciseId);
        Assert.Equal("squat", entry.SwappedFromExerciseId);
    }

    [Fact]
    public void Swapping_an_Exercise_with_confirmed_Sets_replaces_them()
    {
        var workout = WorkoutEdits.Swap(WithDoneSets("squat"), "squat", "legpress");

        var entry = Assert.Single(workout.Entries);
        Assert.Equal(EntryStatus.Swapped, entry.Status);
        Assert.Empty(entry.Sets);
    }

    [Fact]
    public void Swapping_a_skipped_Exercise_replaces_the_skip()
    {
        var workout = WorkoutEdits.Swap(WorkoutEdits.Skip(Planned(), "squat", "x"), "squat", "legpress");

        Assert.Equal(EntryStatus.Swapped, Assert.Single(workout.Entries).Status);
    }

    [Fact]
    public void Undo_swap_restores_the_original_as_not_yet_done()
    {
        var workout = WorkoutEdits.UndoSwap(WorkoutEdits.Swap(Planned(), "squat", "legpress"), "squat");

        Assert.Empty(workout.Entries);
    }

    [Theory]
    [InlineData("bench")]
    [InlineData("squat")]
    public void Swapping_to_an_Exercise_already_in_the_Workout_is_refused(string target)
    {
        var before = Planned();

        Assert.Same(before, WorkoutEdits.Swap(before, "squat", target));
    }

    [Fact]
    public void Swapping_to_an_Exercise_added_or_already_swapped_in_is_refused()
    {
        var workout = WorkoutEdits.AddExercise(Planned(), "squat", "curl");
        Assert.Same(workout, WorkoutEdits.Swap(workout, "bench", "curl"));

        var swapped = WorkoutEdits.Swap(Planned(), "squat", "legpress");
        Assert.Same(swapped, WorkoutEdits.Swap(swapped, "bench", "legpress"));
    }

    // Add

    [Fact]
    public void Add_inserts_an_empty_Done_entry_after_the_given_Exercises_entry()
    {
        var start = Planned() with
        {
            Entries =
            [
                new WorkoutEntry("squat", EntryStatus.Done, null, null, null, [Set("a")]),
                new WorkoutEntry("bench", EntryStatus.Done, null, null, null, [Set("b")]),
            ],
        };

        var workout = WorkoutEdits.AddExercise(start, "squat", "curl");

        Assert.Equal(["squat", "curl", "bench"], workout.Entries.Select(e => e.ExerciseId));
        Assert.Equal(new WorkoutEntry("curl", EntryStatus.Done, null, null, null, []), workout.Entries[1]);
    }

    [Fact]
    public void Add_after_an_Exercise_with_no_entry_appends()
    {
        var workout = WorkoutEdits.AddExercise(Planned(), "squat", "curl");

        Assert.Equal(["curl"], workout.Entries.Select(e => e.ExerciseId));
    }

    [Fact]
    public void An_added_Exercise_is_reported_as_an_extra_by_the_outcomes_rule_and_is_not_partial()
    {
        var workout = WorkoutEdits.AddExercise(new WorkoutBuilder().Plan("squat").Done("squat").Build(), "squat", "curl");

        var outcome = WorkoutOutcomes.Of(workout);
        Assert.Equal(["curl"], outcome.Extras.Select(e => e.ExerciseId));
        Assert.False(outcome.IsPartial);
    }

    [Fact]
    public void Adding_an_Exercise_already_in_the_Workout_is_refused()
    {
        var before = Planned();

        Assert.Same(before, WorkoutEdits.AddExercise(before, "squat", "bench"));
    }

    [Fact]
    public void Remove_drops_an_added_Exercise_with_no_Sets()
    {
        var workout = WorkoutEdits.RemoveAdded(WorkoutEdits.AddExercise(Planned(), "squat", "curl"), "curl");

        Assert.Empty(workout.Entries);
    }

    [Fact]
    public void Remove_refuses_an_added_Exercise_that_has_Sets()
    {
        var before = WorkoutEdits.AddExercise(Planned(), "squat", "curl") with
        {
            Entries = [new WorkoutEntry("curl", EntryStatus.Done, null, null, null, [Set("a")])],
        };

        Assert.Same(before, WorkoutEdits.RemoveAdded(before, "curl"));
    }

    [Fact]
    public void Remove_refuses_a_planned_Exercise()
    {
        var before = Planned() with { Entries = [new WorkoutEntry("squat", EntryStatus.Done, null, null, null, [])] };

        Assert.Same(before, WorkoutEdits.RemoveAdded(before, "squat"));
    }

    // Benchmark

    [Fact]
    public void Toggling_the_benchmark_flag_flips_only_the_flag()
    {
        var exercise = new Exercise("squat", "Squat", LogKind.Load, new ExerciseTags(["legs"], [], []), false);

        var on = ExerciseEdits.WithBenchmark(exercise, true);

        Assert.Equal(exercise with { IsBenchmark = true }, on);
        Assert.Equal(exercise, ExerciseEdits.WithBenchmark(on, false));
    }
}
