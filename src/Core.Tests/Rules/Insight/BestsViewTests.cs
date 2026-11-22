using Core.Model;
using Core.Rules.Insight;

namespace Core.Tests.Rules.Insight;

public class BestsViewTests
{
    private static readonly DateOnly Mon = new(2026, 11, 16);

    private static readonly Exercise Squat = new("squat", "Goblet squat", LogKind.Load, null, false);
    private static readonly Exercise Row = new("row", "Row 500m", LogKind.Time, null, false);
    private static readonly Exercise Plank = new("plank", "Plank", LogKind.Seconds, null, false);
    private static readonly Exercise Stretch = new("stretch", "Stretch", LogKind.TickOnly, null, false);

    private static WorkoutSet Kg(string id, double kg, int reps) => new(id, kg, reps, null, null, null, false);
    private static WorkoutSet Secs(string id, int seconds) => new(id, null, null, null, seconds, null, false);

    private static Workout W(string id, DateOnly date, string exerciseId, params WorkoutSet[] sets) =>
        new(id, date, "s1", false, null, null, [], [new WorkoutEntry(exerciseId, EntryStatus.Done, null, null, null, sets)], null);

    private static BestsView Build(IReadOnlyList<Exercise> exercises, params Workout[] workouts) =>
        BestsViewBuilder.Build(exercises, workouts, Mon);

    [Fact]
    public void No_history_is_empty()
    {
        var view = Build([Squat]);

        Assert.True(view.IsEmpty);
        Assert.Empty(view.Bests);
        Assert.Null(view.BestOfTheWeek);
    }

    [Fact]
    public void Each_Exercise_lists_its_best_and_the_date_it_was_set_and_Exercises_without_a_Best_are_omitted()
    {
        var view = Build([Squat, Stretch, Plank],
            W("w1", Mon.AddDays(-20), "squat", Kg("a", 20, 5)),
            W("w2", Mon.AddDays(-10), "squat", Kg("b", 30, 5)),
            W("w3", Mon.AddDays(-9), "squat", Kg("c", 25, 5)),
            W("w4", Mon.AddDays(-9), "stretch"));

        var best = Assert.Single(view.Bests);
        Assert.Equal("Goblet squat", best.ExerciseName);
        Assert.Equal(30, best.Result.Set.WeightKg);
        Assert.Equal(Mon.AddDays(-10), best.Date);
    }

    [Fact]
    public void Time_kinds_rank_the_lowest_time_as_best()
    {
        var view = Build([Row],
            W("w1", Mon.AddDays(-20), "row", Secs("a", 120)),
            W("w2", Mon.AddDays(-10), "row", Secs("b", 110)),
            W("w3", Mon.AddDays(-5), "row", Secs("c", 125)));

        Assert.Equal(110, view.Bests.Single().Result.Set.DurationSeconds);
    }

    [Fact]
    public void Bests_are_sorted_by_Exercise_name()
    {
        var view = Build([Squat, Plank],
            W("w1", Mon.AddDays(-3), "squat", Kg("a", 20, 5)),
            W("w2", Mon.AddDays(-2), "plank", Secs("b", 60)));

        Assert.Equal(["Goblet squat", "Plank"], view.Bests.Select(b => b.ExerciseName));
    }

    [Fact]
    public void Best_of_the_week_is_none_when_no_PR_was_set_this_week()
    {
        var view = Build([Squat],
            W("w1", Mon.AddDays(-10), "squat", Kg("a", 20, 5)),
            W("w2", Mon.AddDays(-8), "squat", Kg("b", 30, 5)),
            W("w3", Mon, "squat", Kg("c", 25, 5)));

        Assert.False(view.IsEmpty);
        Assert.Null(view.BestOfTheWeek);
    }

    [Fact]
    public void A_first_ever_result_is_a_baseline_not_a_Best_of_the_week()
    {
        var view = Build([Squat], W("w1", Mon, "squat", Kg("a", 20, 5)));

        Assert.Null(view.BestOfTheWeek);
    }

    [Fact]
    public void Best_of_the_week_is_a_PR_from_Monday_to_Sunday_of_the_current_week_only()
    {
        var outside = Build([Squat],
            W("w1", Mon.AddDays(-7), "squat", Kg("a", 20, 5)),
            W("w2", Mon.AddDays(-1), "squat", Kg("b", 30, 5)),
            W("w3", Mon.AddDays(7), "squat", Kg("c", 40, 5)));
        Assert.Null(outside.BestOfTheWeek);

        var inWeek = Build([Squat],
            W("w1", Mon.AddDays(-7), "squat", Kg("a", 20, 5)),
            W("w2", Mon.AddDays(6), "squat", Kg("b", 30, 5)));
        Assert.Equal("Goblet squat", inWeek.BestOfTheWeek!.ExerciseName);
        Assert.Equal(Mon.AddDays(6), inWeek.BestOfTheWeek.Date);
    }

    [Fact]
    public void The_strongest_PR_is_the_biggest_jump_over_the_previous_Best_across_Exercises()
    {
        // Squat 20 to 22 kg at the same reps (+10%); Plank 60 s to 90 s (+50%).
        var view = Build([Squat, Plank],
            W("w1", Mon.AddDays(-7), "squat", Kg("a", 20, 5)),
            W("w2", Mon.AddDays(-7), "plank", Secs("p", 60)),
            W("w3", Mon, "squat", Kg("b", 22, 5)),
            W("w4", Mon, "plank", Secs("q", 90)));

        Assert.Equal("Plank", view.BestOfTheWeek!.ExerciseName);
    }

    [Fact]
    public void A_lower_is_better_PR_counts_by_how_much_faster_it_is()
    {
        var view = Build([Squat, Row],
            W("w1", Mon.AddDays(-7), "squat", Kg("a", 20, 5)),
            W("w2", Mon.AddDays(-7), "row", Secs("r", 120)),
            W("w3", Mon, "squat", Kg("b", 21, 5)),
            W("w4", Mon, "row", Secs("s", 100)));

        Assert.Equal("Row 500m", view.BestOfTheWeek!.ExerciseName);
    }

    [Fact]
    public void Equal_jumps_go_to_the_latest_PR_then_to_the_Exercise_name()
    {
        // Plank 60 to 66 s and squat 20 to 22 kg at 5 reps are both +10%.
        var later = Build([Squat, Plank],
            W("w1", Mon.AddDays(-7), "plank", Secs("p", 60)),
            W("w2", Mon.AddDays(-7), "squat", Kg("a", 20, 5)),
            W("w3", Mon, "plank", Secs("q", 66)),
            W("w4", Mon.AddDays(1), "squat", Kg("b", 22, 5)));
        Assert.Equal("Goblet squat", later.BestOfTheWeek!.ExerciseName);

        var sameDay = Build([Squat, Plank],
            W("w1", Mon.AddDays(-7), "plank", Secs("p", 60)),
            W("w2", Mon.AddDays(-7), "squat", Kg("a", 20, 5)),
            W("w3", Mon, "plank", Secs("q", 66)),
            W("w4", Mon, "squat", Kg("b", 22, 5)));
        Assert.Equal("Goblet squat", sameDay.BestOfTheWeek!.ExerciseName);
    }
}
