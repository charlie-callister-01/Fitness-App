using Core.Model;
using Core.Rules.Editing;
using Core.Tests.Rules.Outcomes;

namespace Core.Tests.Rules.Editing;

public class SimilarExercisesTests
{
    private static Exercise Ex(string id, string name, string[]? muscle = null, string[]? equipment = null, string[]? pattern = null) =>
        new(id, name, LogKind.Load, muscle is null && equipment is null && pattern is null
            ? null
            : new ExerciseTags(muscle ?? [], equipment ?? [], pattern ?? []), false);

    private static readonly Workout Empty = new WorkoutBuilder().Build();

    private static string[] Ids(IEnumerable<Exercise> exercises) => exercises.Select(e => e.Id).ToArray();

    [Fact]
    public void More_shared_tags_rank_first()
    {
        var current = Ex("squat", "Squat", ["legs"], ["barbell"], ["squat"]);
        var library = new[]
        {
            Ex("one", "Aaa", ["legs"]),
            Ex("three", "Zzz", ["legs"], ["barbell"], ["squat"]),
            Ex("two", "Mmm", ["legs"], ["barbell"]),
        };

        Assert.Equal(["three", "two", "one"], Ids(SimilarExercises.Rank(current, library, Empty)));
    }

    [Fact]
    public void Ties_are_broken_by_name()
    {
        var current = Ex("squat", "Squat", ["legs"]);
        var library = new[] { Ex("b", "Beta", ["legs"]), Ex("a", "Alpha", ["legs"]) };

        Assert.Equal(["a", "b"], Ids(SimilarExercises.Rank(current, library, Empty)));
    }

    [Fact]
    public void Exercises_already_in_the_Workout_and_the_current_one_are_excluded()
    {
        var workout = new WorkoutBuilder().Plan("squat", "bench").Swapped("row", "pullup").Build() with
        {
            Entries = [new WorkoutEntry("pullup", EntryStatus.Swapped, null, "row", null, []), new WorkoutEntry("curl", EntryStatus.Done, null, null, null, [])],
        };
        var current = Ex("squat", "Squat", ["legs"]);
        var library = new[] { current, Ex("bench", "Bench", ["legs"]), Ex("pullup", "Pull-up", ["legs"]), Ex("curl", "Curl", ["legs"]), Ex("lunge", "Lunge", ["legs"]) };

        Assert.Equal(["lunge"], Ids(SimilarExercises.Rank(current, library, workout)));
    }

    [Fact]
    public void Untagged_Exercises_rank_last_even_when_their_name_sorts_first()
    {
        var current = Ex("squat", "Squat", ["legs"]);
        var library = new[] { Ex("plain", "Aaa"), Ex("other", "Zzz", ["arms"]), Ex("match", "Mmm", ["legs"]) };

        Assert.Equal(["match", "other", "plain"], Ids(SimilarExercises.Rank(current, library, Empty)));
    }

    [Fact]
    public void A_current_Exercise_with_no_tags_gives_plain_alphabetical_order()
    {
        var current = Ex("squat", "Squat");
        var library = new[] { Ex("c", "Charlie", ["legs"]), Ex("a", "Alpha"), Ex("b", "Bravo", ["arms"]) };

        Assert.Equal(["a", "b", "c"], Ids(SimilarExercises.Rank(current, library, Empty)));
    }

    [Fact]
    public void Tags_match_case_insensitively_per_category()
    {
        var current = Ex("squat", "Squat", ["Legs"], ["barbell"]);
        var library = new[] { Ex("x", "X", ["legs"]), Ex("y", "Y", ["barbell"]) };

        Assert.Equal(["x", "y"], Ids(SimilarExercises.Rank(current, library, Empty)));
    }

    [Fact]
    public void SharedTagCount_counts_shared_tags_and_is_zero_when_either_Exercise_has_none()
    {
        var current = Ex("squat", "Squat", ["legs"], ["barbell"]);

        Assert.Equal(2, SimilarExercises.SharedTagCount(current, Ex("a", "A", ["Legs"], ["barbell"])));
        Assert.Equal(0, SimilarExercises.SharedTagCount(current, Ex("b", "B", ["arms"])));
        Assert.Equal(0, SimilarExercises.SharedTagCount(current, Ex("c", "C")));
    }
}
