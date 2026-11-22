using Core.Model;
using Core.Rules.Insight;
using Core.Rules.Units;
using Core.Tests.Rules.TestData;
using static Core.Tests.Rules.TestData.Dates;
using static Core.Tests.Rules.TestData.HistoryBuilder;

namespace Core.Tests.Rules.Insight;

public class BenchmarkListTests
{
    private static readonly DateOnly Start = D(2026, 1, 5);
    private static readonly UnitConverter Kg = new(WeightUnit.Kg, DistanceUnit.Km, 2);

    private static readonly Exercise Squat = new("squat", "Squat", LogKind.Load, null, true);
    private static readonly Exercise Run = new("run", "1 km run", LogKind.Time, null, true);
    private static readonly Exercise Curl = new("curl", "Curl", LogKind.Load, null, false);

    private static Plan Plan() => PlanFixture.Make(start: Start, sessions:
    [
        PlanFixture.Session("test", DayOfWeek.Saturday, SessionType.Test, name: "Test", benchmarkEveryWeeks: 4),
    ]);

    private static BenchmarkListView Build(IReadOnlyList<Workout> history, IReadOnlyList<Badge>? badges = null, DateOnly? today = null) =>
        BenchmarkList.Build(Plan(), [Curl, Squat, Run], badges ?? [], history, [], [], [], today ?? D(2026, 1, 12), Kg);

    private static BenchmarkRow Row(BenchmarkListView view, string id) => view.Rows.Single(r => r.ExerciseId == id);

    [Fact]
    public void Only_benchmark_Exercises_are_listed()
    {
        var view = Build([]);

        Assert.Equal(["squat", "run"], view.Rows.Select(r => r.ExerciseId));
    }

    [Fact]
    public void A_never_tested_Exercise_has_no_results_and_no_trend()
    {
        var row = Row(Build([]), "squat");

        Assert.True(row.NeverTested);
        Assert.Null(row.LatestText);
        Assert.Equal(BenchmarkTrend.None, row.Trend);
    }

    [Fact]
    public void A_first_test_has_a_result_but_no_previous_and_no_trend()
    {
        var row = Row(Build([Lift("w1", D(2026, 1, 10), "test", "squat", Set(kg: 100, reps: 1))]), "squat");

        Assert.False(row.NeverTested);
        Assert.NotNull(row.LatestText);
        Assert.Null(row.PreviousText);
        Assert.Equal(BenchmarkTrend.None, row.Trend);
        Assert.Equal(D(2026, 1, 10), row.LatestDate);
    }

    [Fact]
    public void A_higher_result_is_better_for_a_higher_is_better_kind()
    {
        var row = Row(Build(
        [
            Lift("w1", D(2026, 1, 10), "test", "squat", Set(kg: 100, reps: 1)),
            Lift("w2", D(2026, 2, 7), "test", "squat", Set(kg: 110, reps: 1)),
        ]), "squat");

        Assert.Equal(BenchmarkTrend.Better, row.Trend);
        Assert.True(row.Change > 0);
        Assert.NotNull(row.PreviousText);
    }

    [Fact]
    public void A_lower_result_is_worse_for_a_higher_is_better_kind()
    {
        var row = Row(Build(
        [
            Lift("w1", D(2026, 1, 10), "test", "squat", Set(kg: 110, reps: 1)),
            Lift("w2", D(2026, 2, 7), "test", "squat", Set(kg: 100, reps: 1)),
        ]), "squat");

        Assert.Equal(BenchmarkTrend.Worse, row.Trend);
    }

    [Fact]
    public void A_drop_in_a_time_is_better()
    {
        var row = Row(Build(
        [
            Lift("w1", D(2026, 1, 10), "test", "run", Set(seconds: 300)),
            Lift("w2", D(2026, 2, 7), "test", "run", Set(seconds: 280)),
        ]), "run");

        Assert.Equal(BenchmarkTrend.Better, row.Trend);
        Assert.Equal(-20, row.Change);
    }

    [Fact]
    public void A_rise_in_a_time_is_worse()
    {
        var row = Row(Build(
        [
            Lift("w1", D(2026, 1, 10), "test", "run", Set(seconds: 280)),
            Lift("w2", D(2026, 2, 7), "test", "run", Set(seconds: 300)),
        ]), "run");

        Assert.Equal(BenchmarkTrend.Worse, row.Trend);
    }

    [Fact]
    public void Equal_results_have_the_same_trend()
    {
        var row = Row(Build(
        [
            Lift("w1", D(2026, 1, 10), "test", "squat", Set(kg: 100, reps: 1)),
            Lift("w2", D(2026, 2, 7), "test", "squat", Set(kg: 100, reps: 1)),
        ]), "squat");

        Assert.Equal(BenchmarkTrend.Same, row.Trend);
    }

    [Fact]
    public void Without_a_Badge_for_the_Exercise_there_is_no_target_progress()
    {
        var row = Row(Build([Lift("w1", D(2026, 1, 10), "test", "squat", Set(kg: 100, reps: 1))],
            [new Badge("b1", "Sessions", "sessions", 10)]), "squat");

        Assert.Null(row.TargetPercent);
    }

    [Fact]
    public void A_Badge_on_the_Exercise_gives_target_progress()
    {
        var history = new[] { Lift("w1", D(2026, 1, 10), "test", "squat", Set(kg: 100, reps: 1)) };

        var row = Row(Build(history, [new Badge("b1", "Squat", "squat", 200)]), "squat");

        Assert.InRange(row.TargetPercent!.Value, 1, 99);
        Assert.False(row.TargetReached);
    }

    [Fact]
    public void A_target_at_or_below_the_best_is_reached_at_100_percent()
    {
        var history = new[] { Lift("w1", D(2026, 1, 10), "test", "squat", Set(kg: 100, reps: 1)) };

        var row = Row(Build(history, [new Badge("b1", "Squat", "squat", 50)]), "squat");

        Assert.Equal(100, row.TargetPercent);
        Assert.True(row.TargetReached);
    }

    [Fact]
    public void The_next_due_date_is_the_next_benchmark_Session_date()
    {
        Assert.Equal(D(2026, 2, 7), Build([], today: D(2026, 1, 12)).NextDue);
    }

    [Fact]
    public void With_no_benchmark_Exercises_the_view_is_empty()
    {
        var view = BenchmarkList.Build(Plan(), [Curl], [], [], [], [], [], D(2026, 1, 12), Kg);

        Assert.True(view.IsEmpty);
    }
}
