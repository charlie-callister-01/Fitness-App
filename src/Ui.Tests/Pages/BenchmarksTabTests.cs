using Bunit;
using Core.Model;
using Core.Storage;
using Ui.Pages;
using static Ui.Tests.Pages.PageFixture;

namespace Ui.Tests.Pages;

/// <summary>The Benchmarks tab of Progress (design S-07): latest against previous, a ▲ when better, a target bar, the next test date.</summary>
public sealed class BenchmarksTabTests : BunitContext
{
    private static readonly Exercise Bench = new("bench", "Bench press", LogKind.Load, null, true);
    private static readonly Exercise Run = new("run", "1 km run", LogKind.Time, null, true);

    private static Plan BenchPlan() => Plan(
    [
        new("lower", "Lower", DayOfWeek.Monday, SessionType.Strength, null, [new SessionExercise(Squat, 3, 8, null, null)]),
        new("test", "Test", DayOfWeek.Saturday, SessionType.Test, 4, []),
    ]);

    private static Workout Test(string id, DateOnly date, string exerciseId, WorkoutSet set) =>
        new(id, date, "test", false, Now.AddDays(-7), Now.AddDays(-7).AddHours(1), [],
            [new WorkoutEntry(exerciseId, EntryStatus.Done, null, null, null, [set])], null);

    private async Task<IRenderedComponent<Progress>> RenderAsync(IReadOnlyList<Badge>? badges = null, bool benchmarks = true, params Workout[] workouts)
    {
        var data = await OpenSeededAsync(plan: BenchPlan());
        var extra = new List<Change>();
        if (benchmarks)
        {
            extra.Add(Change.Put(Bench));
            extra.Add(Change.Put(Run));
        }

        extra.AddRange((badges ?? []).Select(Change.Put));
        extra.AddRange(workouts.Select(Change.Put));
        if (extra.Count > 0) Assert.IsType<CommitResult.Committed>(await data.CommitAsync(extra));
        Wire(this, data);
        var cut = Render<Progress>();
        cut.FindAll(".fa-progress__tabs button")[1].Click();
        return cut;
    }

    [Fact]
    public async Task Never_tested_benchmarks_say_so_and_the_next_test_date_shows()
    {
        var cut = await RenderAsync();

        Assert.Equal(2, cut.FindAll(".fa-bench__never").Count);
        Assert.All(cut.FindAll(".fa-bench__never"), n => Assert.Equal("Never tested", n.TextContent));
        Assert.Contains("Next test:", cut.Find(".fa-bench__next").TextContent);
        Assert.Empty(cut.FindAll("[role=progressbar]"));
    }

    [Fact]
    public async Task A_better_result_shows_the_latest_the_previous_and_a_better_marker()
    {
        var cut = await RenderAsync(null, true,
            Test("t1", new DateOnly(2026, 10, 3), "bench", Set("a", 60, 5)),
            Test("t2", new DateOnly(2026, 11, 7), "bench", Set("b", 70, 5)));

        var row = cut.FindAll(".fa-bench__row").Single(r => r.TextContent.Contains("Bench press"));
        Assert.Contains("70", row.QuerySelector(".fa-bench__result")!.TextContent);
        Assert.Contains("Previous: ", row.QuerySelector(".fa-bench__previous")!.TextContent);
        Assert.Contains("60", row.QuerySelector(".fa-bench__previous")!.TextContent);
        Assert.Equal("▲Better", row.QuerySelector(".fa-bench__trend--better")!.TextContent);
    }

    [Fact]
    public async Task A_faster_time_counts_as_better()
    {
        var cut = await RenderAsync(null, true,
            Test("t1", new DateOnly(2026, 10, 3), "run", new WorkoutSet("a", null, null, null, 300, null, false)),
            Test("t2", new DateOnly(2026, 11, 7), "run", new WorkoutSet("b", null, null, null, 280, null, false)));

        var row = cut.FindAll(".fa-bench__row").Single(r => r.TextContent.Contains("1 km run"));
        Assert.NotNull(row.QuerySelector(".fa-bench__trend--better"));
    }

    [Fact]
    public async Task A_slower_time_is_worse_and_never_a_better_marker()
    {
        var cut = await RenderAsync(null, true,
            Test("t1", new DateOnly(2026, 10, 3), "run", new WorkoutSet("a", null, null, null, 280, null, false)),
            Test("t2", new DateOnly(2026, 11, 7), "run", new WorkoutSet("b", null, null, null, 300, null, false)));

        var row = cut.FindAll(".fa-bench__row").Single(r => r.TextContent.Contains("1 km run"));
        Assert.Null(row.QuerySelector(".fa-bench__trend--better"));
        Assert.NotNull(row.QuerySelector(".fa-bench__trend--worse"));
    }

    [Fact]
    public async Task A_first_test_has_no_trend_marker()
    {
        var cut = await RenderAsync(null, true, Test("t1", new DateOnly(2026, 10, 3), "bench", Set("a", 60, 5)));

        var row = cut.FindAll(".fa-bench__row").Single(r => r.TextContent.Contains("Bench press"));
        Assert.Empty(row.QuerySelectorAll(".fa-bench__trend"));
        Assert.Contains("First test", row.QuerySelector(".fa-bench__previous")!.TextContent);
    }

    [Fact]
    public async Task A_Badge_target_shows_a_progress_bar_with_its_percentage_always_visible()
    {
        var cut = await RenderAsync([new Badge("b1", "Bench 200", "bench", 200)], true,
            Test("t1", new DateOnly(2026, 11, 7), "bench", Set("a", 100, 1)));

        var bar = cut.Find(".fa-pbar");
        Assert.Equal("Target", bar.QuerySelector(".fa-pbar__label")!.TextContent);
        Assert.EndsWith("%", bar.QuerySelector(".fa-pbar__value")!.TextContent);
        Assert.Contains("fa-pbar--active", bar.ClassList);
    }

    [Fact]
    public async Task A_reached_target_shows_the_done_colour()
    {
        var cut = await RenderAsync([new Badge("b1", "Bench 50", "bench", 50)], true,
            Test("t1", new DateOnly(2026, 11, 7), "bench", Set("a", 100, 1)));

        Assert.Contains("fa-pbar--done", cut.Find(".fa-pbar").ClassList);
        Assert.Equal("100%", cut.Find(".fa-pbar__value").TextContent);
    }

    [Fact]
    public async Task With_no_benchmark_Exercises_it_shows_an_empty_state()
    {
        var cut = await RenderAsync(null, benchmarks: false);

        Assert.Contains("No benchmarks yet", cut.Find(".empty").TextContent);
        Assert.Empty(cut.FindAll(".fa-bench__row"));
    }
}
