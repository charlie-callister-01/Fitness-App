using Core.Model;
using Core.Rules.Benchmarks;
using Core.Rules.Weeks;

namespace Core.Tests.Rules.Benchmarks;

public class BenchmarkRulesTests
{
    private static DateOnly D(int y, int m, int d) => new(y, m, d);

    // Plan starts Mon 2026-01-05; benchmark Test session "test" (Saturday) every 4 weeks: Plan weeks 1, 5, 9 ...
    private static readonly DateOnly Start = D(2026, 1, 5);

    private static readonly Exercise Squat = new("squat", "Squat", LogKind.Load, null, true);
    private static readonly Exercise Run = new("run", "5k run", LogKind.DistanceKm, null, true);
    private static readonly Exercise Curl = new("curl", "Curl", LogKind.Load, null, false);
    private static readonly Exercise[] Library = [Curl, Squat, Run];

    private static Plan Plan() => new(
        "p", "Plan", true, Start, null, null, 3, 2, false, 2, [],
        [
            new Session("lower", "Lower", DayOfWeek.Monday, SessionType.Strength, null, []),
            new Session("test", "Test", DayOfWeek.Saturday, SessionType.Test, 4, []),
        ]);

    private static WorkoutSet Load(double kg, int reps) => new($"s{kg}x{reps}", kg, reps, null, null, null, false);

    private static Workout W(string id, DateOnly date, string? sessionId, string exerciseId, WorkoutSet set) =>
        new(id, date, sessionId, false, null, null, [],
            [new WorkoutEntry(exerciseId, EntryStatus.Done, null, null, null, [set])], null);

    // ---- list

    [Fact]
    public void First_benchmark_has_a_best_but_no_previous_result_or_change()
    {
        var history = new[] { W("w1", D(2026, 1, 10), "test", "squat", Load(100, 1)) };

        var squat = BenchmarkRules.List(Plan(), Library, history).Single(e => e.Exercise.Id == "squat");

        Assert.Equal(100, squat.Best!.Set.WeightKg);
        Assert.Equal(100, squat.Latest!.Set.WeightKg);
        Assert.Null(squat.Previous);
        Assert.Null(squat.Change);
    }

    [Fact]
    public void Repeat_with_improvement_shows_positive_change()
    {
        var history = new[]
        {
            W("w1", D(2026, 1, 10), "test", "squat", Load(100, 1)),
            W("w2", D(2026, 2, 7), "test", "squat", Load(110, 1)),
        };

        var squat = BenchmarkRules.List(Plan(), Library, history).Single(e => e.Exercise.Id == "squat");

        Assert.Equal(110, squat.Latest!.Set.WeightKg);
        Assert.Equal(100, squat.Previous!.Set.WeightKg);
        Assert.True(squat.Change > 0);
    }

    [Fact]
    public void Repeat_with_regression_shows_negative_change_while_best_stays()
    {
        var history = new[]
        {
            W("w1", D(2026, 1, 10), "test", "squat", Load(110, 1)),
            W("w2", D(2026, 2, 7), "test", "squat", Load(100, 1)),
        };

        var squat = BenchmarkRules.List(Plan(), Library, history).Single(e => e.Exercise.Id == "squat");

        Assert.Equal(110, squat.Best!.Set.WeightKg);
        Assert.Equal(100, squat.Latest!.Set.WeightKg);
        Assert.True(squat.Change < 0);
    }

    [Fact]
    public void Lists_only_benchmark_exercises_in_library_order_and_best_includes_non_test_workouts()
    {
        var history = new[]
        {
            W("w1", D(2026, 1, 10), "test", "squat", Load(100, 1)),
            W("w2", D(2026, 1, 13), "lower", "squat", Load(120, 1)),
        };

        var list = BenchmarkRules.List(Plan(), Library, history);

        Assert.Equal(["squat", "run"], list.Select(e => e.Exercise.Id));
        Assert.Equal(120, list[0].Best!.Set.WeightKg);
        Assert.Equal(100, list[0].Latest!.Set.WeightKg);
        Assert.Null(list[1].Best);
        Assert.Null(list[1].Latest);
    }

    // ---- pre-fill

    [Fact]
    public void Prefill_has_a_row_per_benchmark_exercise_with_its_latest_test_result()
    {
        var history = new[]
        {
            W("w1", D(2026, 1, 10), "test", "squat", Load(100, 1)),
            W("w2", D(2026, 2, 7), "test", "squat", Load(110, 1)),
            W("w3", D(2026, 2, 9), "lower", "squat", Load(130, 1)),
        };

        var rows = BenchmarkRules.PreFill(Plan(), Library, history);

        Assert.Equal(["squat", "run"], rows.Select(r => r.Exercise.Id));
        Assert.Equal(110, rows[0].Previous!.Set.WeightKg);
        Assert.Null(rows[1].Previous);
    }

    // ---- next due

    [Fact]
    public void Next_benchmark_is_due_in_the_first_plan_week_when_never_tested()
    {
        var due = BenchmarkRules.NextDue(Plan(), [], [], D(2025, 12, 1));

        Assert.Equal(new BenchmarkDue("test", D(2026, 1, 10)), due);
    }

    [Fact]
    public void Next_benchmark_after_a_test_is_the_next_interval_week()
    {
        var history = new[] { W("w1", D(2026, 1, 10), "test", "squat", Load(100, 1)) };

        var due = BenchmarkRules.NextDue(Plan(), [], history, D(2026, 1, 12));

        // Plan week 5 starts Mon 2026-02-02; its Saturday is the 7th.
        Assert.Equal(new BenchmarkDue("test", D(2026, 2, 7)), due);
    }

    [Fact]
    public void Next_benchmark_is_this_weeks_when_due_and_not_yet_done()
    {
        var due = BenchmarkRules.NextDue(Plan(), [], [], D(2026, 2, 3));

        Assert.Equal(D(2026, 2, 7), due!.Date);
    }

    [Fact]
    public void Next_benchmark_honours_a_Week_schedule_move()
    {
        // Plan week 5 starts Mon 2026-02-02; the test is moved from Saturday the 7th to Wednesday the 4th.
        var schedules = new[] { new WeekSchedule(D(2026, 2, 2), [new SessionMove("test", D(2026, 2, 4))]) };

        var due = BenchmarkRules.NextDue(Plan(), schedules, [], D(2026, 2, 3));

        Assert.Equal(new BenchmarkDue("test", D(2026, 2, 4)), due);
    }

    [Fact]
    public void A_benchmark_with_a_zero_interval_is_not_a_benchmark_and_never_throws()
    {
        var plan = Plan() with { Sessions = [new Session("test", "Test", DayOfWeek.Saturday, SessionType.Test, 0, [])] };

        Assert.Null(BenchmarkRules.NextDue(plan, [], [], D(2026, 2, 3)));
        Assert.DoesNotContain(BenchmarkRules.List(plan, Library, []), e => e.Latest is not null);
    }

    [Fact]
    public void Next_benchmark_is_null_without_a_benchmark_session()
    {
        var plan = Plan() with { Sessions = [new Session("lower", "Lower", DayOfWeek.Monday, SessionType.Strength, null, [])] };

        Assert.Null(BenchmarkRules.NextDue(plan, [], [], D(2026, 2, 3)));
    }

    // ---- weekly target

    [Fact]
    public void A_benchmark_workout_does_not_count_towards_the_week()
    {
        var history = new[] { W("w1", D(2026, 1, 10), "test", "squat", Load(100, 1)) };

        Assert.Equal(0, WeekRules.CountedWorkouts(Plan(), history, D(2026, 1, 5)));
    }
}
