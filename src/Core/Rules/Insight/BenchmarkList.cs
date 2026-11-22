using Core.Model;
using Core.Rules.Badges;
using Core.Rules.Benchmarks;
using Core.Rules.LogKinds;
using Core.Rules.Units;

namespace Core.Rules.Insight;

/// <summary>How the latest test compares with the previous one, after allowing for lower-is-better measures (times).</summary>
public enum BenchmarkTrend
{
    None,
    Better,
    Worse,
    Same,
}

/// <summary>
/// One benchmark Exercise on the Benchmarks tab. Results are text in the owner's units. <see cref="Change"/> is latest minus previous
/// in the Exercise's chart measure (a drop in a time is negative and better). <see cref="TargetPercent"/> is null when no Badge targets the Exercise.
/// </summary>
public sealed record BenchmarkRow(
    string ExerciseId,
    string Name,
    bool NeverTested,
    string? LatestText,
    DateOnly? LatestDate,
    string? PreviousText,
    BenchmarkTrend Trend,
    double? Change,
    int? TargetPercent,
    bool TargetReached);

/// <summary>The Benchmarks tab: each benchmark Exercise with its latest result, and the date of the next test.</summary>
public sealed record BenchmarkListView(IReadOnlyList<BenchmarkRow> Rows, DateOnly? NextDue)
{
    public bool IsEmpty => Rows.Count == 0;
}

public static class BenchmarkList
{
    public static BenchmarkListView Build(
        Plan? plan,
        IReadOnlyList<Exercise> exercises,
        IReadOnlyList<Badge> badges,
        IReadOnlyList<Workout> workouts,
        IReadOnlyList<Break> breaks,
        IReadOnlyList<WeighIn> weighIns,
        IReadOnlyList<WeekSchedule> schedules,
        DateOnly today,
        UnitConverter units)
    {
        if (plan is null) return new BenchmarkListView([], null);

        var history = new BadgeHistory(plan, exercises, workouts, breaks, weighIns);
        var rows = BenchmarkRules.List(plan, exercises, workouts).Select(e => Row(e, badges, history, today, units)).ToList();
        return new BenchmarkListView(rows, BenchmarkRules.NextDue(plan, schedules, workouts, today)?.Date);
    }

    private static BenchmarkRow Row(BenchmarkEntry entry, IReadOnlyList<Badge> badges, BadgeHistory history, DateOnly today, UnitConverter units)
    {
        var kind = entry.Exercise.LogKind;
        var target = badges.FirstOrDefault(b => b.Metric.Trim() == entry.Exercise.Id && b.Target > 0);
        var progress = target is null ? null : BadgeRules.Progress(target, history, today);
        return new BenchmarkRow(
            entry.Exercise.Id,
            entry.Exercise.Name,
            entry.Latest is null,
            entry.Latest is null ? null : SetText.Result(kind, entry.Latest.Set, units),
            entry.Latest?.Date,
            entry.Previous is null ? null : SetText.Result(kind, entry.Previous.Set, units),
            TrendOf(entry),
            entry.Change,
            progress is { IsMeasurable: true } ? progress.Percent : null,
            progress is { IsMeasurable: true, Reached: true });
    }

    private static BenchmarkTrend TrendOf(BenchmarkEntry entry)
    {
        if (entry.Change is not { } change) return BenchmarkTrend.None;
        if (change == 0) return BenchmarkTrend.Same;
        var improved = LogKindRules.For(entry.Exercise.LogKind).LowerIsBetter ? change < 0 : change > 0;
        return improved ? BenchmarkTrend.Better : BenchmarkTrend.Worse;
    }
}
