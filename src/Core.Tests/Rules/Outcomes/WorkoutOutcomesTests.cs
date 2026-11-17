using Core.Rules.Outcomes;

namespace Core.Tests.Rules.Outcomes;

public class WorkoutOutcomesTests
{
    [Fact]
    public void Mixed_workout_reports_each_planned_row_exactly()
    {
        var workout = new WorkoutBuilder()
            .Plan("squat", "bench", "row", "curl")
            .Done("squat")
            .Skipped("bench", "shoulder")
            .Swapped("row", "cable-row")
            .Done("plank")
            .Build();

        var result = WorkoutOutcomes.Of(workout);

        Assert.Collection(result.Rows,
            r => { Assert.Equal("squat", r.Row.ExerciseId); Assert.Equal(RowOutcomeKind.Done, r.Kind); Assert.Null(r.SkipReason); Assert.Null(r.ReplacementExerciseId); },
            r => { Assert.Equal(RowOutcomeKind.Skipped, r.Kind); Assert.Equal("shoulder", r.SkipReason); },
            r => { Assert.Equal(RowOutcomeKind.Swapped, r.Kind); Assert.Equal("cable-row", r.ReplacementExerciseId); },
            r => { Assert.Equal("curl", r.Row.ExerciseId); Assert.Equal(RowOutcomeKind.Unrecorded, r.Kind); Assert.Null(r.SkipReason); });
        Assert.Equal(["plank"], result.Extras.Select(e => e.ExerciseId));
        Assert.True(result.IsPartial);
    }

    [Fact]
    public void Everything_done_or_swapped_is_not_partial()
    {
        var workout = new WorkoutBuilder().Plan("a", "b").Done("a").Swapped("b", "c").Build();

        var result = WorkoutOutcomes.Of(workout);

        Assert.False(result.IsPartial);
        Assert.Empty(result.Extras);
    }

    [Fact]
    public void A_skipped_row_makes_the_workout_partial()
    {
        var workout = new WorkoutBuilder().Plan("a", "b").Done("a").Skipped("b", null).Build();

        Assert.True(WorkoutOutcomes.Of(workout).IsPartial);
    }

    [Fact]
    public void A_row_with_no_entry_is_unrecorded_and_makes_the_workout_partial()
    {
        var workout = new WorkoutBuilder().Plan("a", "b").Done("a").Build();

        var result = WorkoutOutcomes.Of(workout);

        Assert.True(result.IsPartial);
        Assert.Equal(RowOutcomeKind.Unrecorded, result.Rows[1].Kind);
    }

    [Fact]
    public void Custom_workout_has_only_extras_and_is_not_partial()
    {
        var workout = new WorkoutBuilder().Done("squat").Done("bench").Build();

        var result = WorkoutOutcomes.Of(workout);

        Assert.Empty(result.Rows);
        Assert.Equal(["squat", "bench"], result.Extras.Select(e => e.ExerciseId));
        Assert.False(result.IsPartial);
    }

    [Fact]
    public void Swap_entry_naming_an_unknown_planned_row_is_an_extra_and_never_throws()
    {
        var workout = new WorkoutBuilder().Plan("a").Done("a").Swapped("ghost", "other").Build();

        var result = WorkoutOutcomes.Of(workout);

        Assert.Equal(["other"], result.Extras.Select(e => e.ExerciseId));
        Assert.False(result.IsPartial);
    }

    [Fact]
    public void Repeated_planned_exercise_consumes_one_entry_per_row()
    {
        var workout = new WorkoutBuilder().Plan("a", "a").Done("a").Build();

        var result = WorkoutOutcomes.Of(workout);

        Assert.Equal([RowOutcomeKind.Done, RowOutcomeKind.Unrecorded], result.Rows.Select(r => r.Kind));
    }
}
