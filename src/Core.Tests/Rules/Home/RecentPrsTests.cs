using Core.Model;
using Core.Rules.Home;
using Core.Storage;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.Home;

public class RecentPrsTests
{
    private static readonly DateOnly Today = Dates.Mon;

    private static readonly Exercise Squat = new("squat", "Goblet squat", LogKind.Load, null, false);
    private static readonly Exercise Bench = new("bench", "Bench press", LogKind.Load, null, false);
    private static readonly Exercise Deadlift = new("deadlift", "Deadlift", LogKind.Load, null, false);

    private static Workout MakeWorkout(
        string id,
        DateOnly date,
        DateTimeOffset? endedAt,
        string exerciseId,
        params WorkoutSet[] sets)
    {
        var entry = new WorkoutEntry(exerciseId, EntryStatus.Done, null, null, null, sets);
        return new Workout(
            id,
            date,
            "s1",
            false,
            endedAt?.AddMinutes(-45),
            endedAt,
            [],
            [entry],
            null);
    }

    [Fact]
    public void Empty_snapshot_gives_empty_list()
    {
        var prs = RecentPrs.Latest(DataSnapshot.Empty, Today);
        Assert.Empty(prs);
    }

    [Fact]
    public void Unfinished_workouts_are_ignored()
    {
        var baseline = MakeWorkout("w0", Today.AddDays(-14), DateTimeOffset.UtcNow.AddDays(-14), "squat", Set(kg: 50, reps: 5));
        var unfinished = MakeWorkout("w1", Today, null, "squat", Set(kg: 60, reps: 5));

        var snapshot = DataSnapshot.Empty with
        {
            Exercises = [Squat],
            Workouts = [baseline, unfinished],
        };

        var prs = RecentPrs.Latest(snapshot, Today);
        Assert.Empty(prs);
    }

    [Fact]
    public void No_prs_gives_empty_list()
    {
        // First ever workout is baseline, not a PR
        var baseline = MakeWorkout("w0", Today.AddDays(-7), DateTimeOffset.UtcNow.AddDays(-7), "squat", Set(kg: 50, reps: 5));

        var snapshot = DataSnapshot.Empty with
        {
            Exercises = [Squat],
            Workouts = [baseline],
        };

        var prs = RecentPrs.Latest(snapshot, Today);
        Assert.Empty(prs);
    }

    [Fact]
    public void Newest_workout_first_and_respects_ended_at_for_same_date()
    {
        var baseline = MakeWorkout("w0", Today.AddDays(-14), DateTimeOffset.Parse("2026-11-02T10:00:00Z"), "squat", Set(kg: 50, reps: 5));
        var w1 = MakeWorkout("w1", Today.AddDays(-7), DateTimeOffset.Parse("2026-11-09T10:00:00Z"), "squat", Set(kg: 55, reps: 5));
        var w2Morning = MakeWorkout("w2a", Today, DateTimeOffset.Parse("2026-11-16T09:00:00Z"), "bench", Set(kg: 60, reps: 5));
        var w2Evening = MakeWorkout("w2b", Today, DateTimeOffset.Parse("2026-11-16T18:00:00Z"), "squat", Set(kg: 60, reps: 5));
        var benchBaseline = MakeWorkout("wb0", Today.AddDays(-14), DateTimeOffset.Parse("2026-11-02T09:00:00Z"), "bench", Set(kg: 50, reps: 5));

        var snapshot = DataSnapshot.Empty with
        {
            Exercises = [Squat, Bench],
            Workouts = [baseline, benchBaseline, w1, w2Morning, w2Evening],
        };

        var prs = RecentPrs.Latest(snapshot, Today);

        Assert.Equal(3, prs.Count);
        // Newest first: w2b (evening) then w2a (morning) then w1 (-7 days)
        Assert.Equal("w2b", snapshot.Workouts.First(w => w.Date == prs[0].WorkoutDate && w.Entries[0].ExerciseId == prs[0].ExerciseId).Id);
        Assert.Equal("squat", prs[0].ExerciseId);
        Assert.Equal(60, prs[0].Set.WeightKg);

        Assert.Equal("bench", prs[1].ExerciseId);
        Assert.Equal(60, prs[1].Set.WeightKg);

        Assert.Equal("squat", prs[2].ExerciseId);
        Assert.Equal(55, prs[2].Set.WeightKg);
    }

    [Fact]
    public void Capped_at_5_by_default_and_by_count()
    {
        var baseline = MakeWorkout("w0", Today.AddDays(-20), DateTimeOffset.Parse("2026-10-27T10:00:00Z"), "squat", Set(kg: 40, reps: 5));
        var workouts = new List<Workout> { baseline };

        for (var i = 1; i <= 7; i++)
        {
            workouts.Add(MakeWorkout(
                $"w{i}",
                Today.AddDays(-15 + i),
                DateTimeOffset.Parse($"2026-11-{i:D2}T10:00:00Z"),
                "squat",
                Set(kg: 40 + i * 2, reps: 5)));
        }

        var snapshot = DataSnapshot.Empty with
        {
            Exercises = [Squat],
            Workouts = workouts,
        };

        var defaultCap = RecentPrs.Latest(snapshot, Today);
        Assert.Equal(5, defaultCap.Count);

        var customCap = RecentPrs.Latest(snapshot, Today, count: 3);
        Assert.Equal(3, customCap.Count);
    }

    [Fact]
    public void Previous_best_carried()
    {
        var baseline = MakeWorkout("w0", Today.AddDays(-7), DateTimeOffset.Parse("2026-11-09T10:00:00Z"), "squat", Set(kg: 50, reps: 5));
        var prWorkout = MakeWorkout("w1", Today, DateTimeOffset.Parse("2026-11-16T10:00:00Z"), "squat", Set(kg: 55, reps: 5));

        var snapshot = DataSnapshot.Empty with
        {
            Exercises = [Squat],
            Workouts = [baseline, prWorkout],
        };

        var prs = RecentPrs.Latest(snapshot, Today);
        var pr = Assert.Single(prs);

        Assert.NotNull(pr.PreviousBest);
        if (pr.PreviousBest is { } best)
        {
            Assert.Equal(50, best.Set.WeightKg);
            Assert.Equal(baseline.Id, best.WorkoutId);
        }
    }

    [Fact]
    public void Exercise_name_comes_from_snapshot_exercises()
    {
        var baseline = MakeWorkout("w0", Today.AddDays(-7), DateTimeOffset.Parse("2026-11-09T10:00:00Z"), "squat", Set(kg: 50, reps: 5));
        var prWorkout = MakeWorkout("w1", Today, DateTimeOffset.Parse("2026-11-16T10:00:00Z"), "squat", Set(kg: 55, reps: 5));

        var snapshot = DataSnapshot.Empty with
        {
            Exercises = [Squat],
            Workouts = [baseline, prWorkout],
        };

        var prs = RecentPrs.Latest(snapshot, Today);
        var pr = Assert.Single(prs);

        Assert.Equal("Goblet squat", pr.ExerciseName);
    }
}
