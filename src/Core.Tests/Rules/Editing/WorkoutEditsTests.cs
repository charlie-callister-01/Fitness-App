using Core.Model;
using Core.Rules.Editing;
using Core.Rules.Parsing;
using Core.Tests.Ids;
using Core.Tests.Rules.Outcomes;

namespace Core.Tests.Rules.Editing;

public class WorkoutEditsTests
{
    private static readonly SequentialIdGenerator Ids = new("s");

    private static ParsedSet Parsed(double kg, int reps) => ParsedSet.Empty with { WeightKg = kg, Reps = reps };

    private static Workout Empty() => new WorkoutBuilder().Plan("squat").Build();

    private static WorkoutSet Set(string id, double kg, int reps, bool max = false) => new(id, kg, reps, null, null, null, max);

    private static Workout WithSets(params WorkoutSet[] sets) =>
        Empty() with { Entries = [new WorkoutEntry("squat", EntryStatus.Done, null, null, 8, sets)] };

    [Fact]
    public void The_first_Set_creates_the_Done_entry_with_a_fresh_id_and_the_max_flag()
    {
        var workout = WorkoutEdits.WithSet(Empty(), "squat", 0, Parsed(20, 10), true, Ids);

        var entry = Assert.Single(workout.Entries);
        Assert.Equal(EntryStatus.Done, entry.Status);
        Assert.Equal(new WorkoutSet("s-1", 20, 10, null, null, null, true), Assert.Single(entry.Sets));
    }

    [Fact]
    public void A_row_past_the_end_appends_and_keeps_the_entrys_rpe()
    {
        var workout = WorkoutEdits.WithSet(WithSets(Set("a", 20, 8)), "squat", 1, Parsed(22, 8), false, new SequentialIdGenerator("n"));

        var entry = Assert.Single(workout.Entries);
        Assert.Equal(["a", "n-1"], entry.Sets.Select(s => s.Id));
        Assert.Equal(8, entry.Rpe);
    }

    [Fact]
    public void An_existing_row_is_replaced_in_place_keeping_its_id_and_its_own_max_flag()
    {
        var workout = WorkoutEdits.WithSet(WithSets(Set("a", 20, 8, max: true)), "squat", 0, Parsed(25, 5), false, Ids);

        Assert.Equal(new WorkoutSet("a", 25, 5, null, null, null, true), Assert.Single(Assert.Single(workout.Entries).Sets));
    }

    [Fact]
    public void Other_exercises_entries_are_left_alone()
    {
        var other = new WorkoutEntry("lunge", EntryStatus.Done, null, null, 7, [Set("l", 10, 10)]);
        var start = Empty() with { Entries = [other] };

        var workout = WorkoutEdits.WithSet(start, "squat", 0, Parsed(20, 10), false, Ids);

        Assert.Equal(2, workout.Entries.Count);
        Assert.Contains(other, workout.Entries);
    }

    [Fact]
    public void Max_is_set_and_cleared_on_the_chosen_row_only()
    {
        var start = WithSets(Set("a", 20, 8), Set("b", 22, 8));

        var marked = WorkoutEdits.WithMax(start, "squat", 1, true);
        Assert.Equal([false, true], marked.Entries.Single().Sets.Select(s => s.IsMax));

        var cleared = WorkoutEdits.WithMax(marked, "squat", 1, false);
        Assert.Equal([false, false], cleared.Entries.Single().Sets.Select(s => s.IsMax));
    }

    [Fact]
    public void Marking_a_Set_that_is_not_stored_changes_nothing()
    {
        var start = WithSets(Set("a", 20, 8));

        Assert.Same(start, WorkoutEdits.WithMax(start, "squat", 1, true));
        var none = Empty();
        Assert.Same(none, WorkoutEdits.WithMax(none, "squat", 0, true));
    }

    [Fact]
    public void Rpe_is_set_and_cleared_on_the_existing_entry()
    {
        var set = WorkoutEdits.WithRpe(WithSets(Set("a", 20, 8)), "squat", 9);
        Assert.Equal(9, set.Entries.Single().Rpe);
        Assert.Single(set.Entries.Single().Sets);

        Assert.Null(WorkoutEdits.WithRpe(set, "squat", null).Entries.Single().Rpe);
    }

    [Fact]
    public void Rpe_before_any_Set_creates_an_empty_Done_entry()
    {
        var entry = Assert.Single(WorkoutEdits.WithRpe(Empty(), "squat", 7).Entries);

        Assert.Equal(EntryStatus.Done, entry.Status);
        Assert.Equal(7, entry.Rpe);
        Assert.Empty(entry.Sets);
    }

    [Fact]
    public void Only_a_Done_entry_counts_as_the_Exercises_confirmed_Sets()
    {
        var skipped = Empty() with { Entries = [new WorkoutEntry("squat", EntryStatus.Skipped, "time", null, null, [])] };

        Assert.Null(WorkoutEdits.FindDoneEntry(skipped, "squat"));
        Assert.Empty(WorkoutEdits.ConfirmedSets(skipped, "squat"));
        Assert.Equal(2, WorkoutEdits.ConfirmedSets(WithSets(Set("a", 1, 1), Set("b", 1, 1)), "squat").Count);
    }
}
