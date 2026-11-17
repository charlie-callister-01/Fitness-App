using Core.Model;
using Core.Rules.Performance;
using Core.Rules.Weeks;

namespace Core.Rules.Badges;

/// <summary>Everything Badge progress is worked out from. Nothing here is a Badge's own state: progress is never stored.</summary>
public sealed record BadgeHistory(
    Plan Plan,
    IReadOnlyList<Exercise> Exercises,
    IReadOnlyList<Workout> Workouts,
    IReadOnlyList<Break> Breaks,
    IReadOnlyList<WeighIn> WeighIns);

/// <summary>
/// Where a Badge stands. For a metric that cannot be measured <see cref="IsMeasurable"/> is false, <see cref="Metric"/> carries
/// the name, and Value and Percent are zero and Reached is false. <see cref="Percent"/> is a whole number from 0 to 100.
/// </summary>
public sealed record BadgeProgress(
    string BadgeId,
    string Metric,
    bool IsMeasurable,
    double Value,
    double Target,
    int Percent,
    bool Reached);

/// <summary>
/// Badge progress. Supported metrics (case-insensitive): <c>sessions</c> (counted Workouts ever), <c>streak</c> (best-ever Streak run),
/// <c>bestweek</c> (also accepted as <c>best week</c>, the product brief's prose name; most counted Workouts in one week), <c>prs</c> (PRs across all Exercises), <c>bodyweight</c> (Weigh-ins),
/// or an Exercise ID (its best value in its own measure; a body measurement uses its latest reading).
/// A negative target on a measurement means a drop of that size from its first reading.
/// </summary>
public static class BadgeRules
{
    public static BadgeProgress Progress(Badge badge, BadgeHistory history, DateOnly today)
    {
        if (Measure(badge.Metric.Trim(), history, today) is not { } m)
            return new BadgeProgress(badge.Id, badge.Metric, false, 0, badge.Target, 0, false);

        var wantsDrop = badge.Target < 0;
        var reading = wantsDrop ? m.Drop : m.Value;
        var value = reading ?? 0;
        var target = Math.Abs(badge.Target);
        var lowerIsBetter = m.LowerIsBetter && !wantsDrop;

        var reached = reading is not null && (lowerIsBetter ? value <= target : value >= target);
        return new BadgeProgress(badge.Id, badge.Metric, true, value, badge.Target, PercentOf(reading is not null, lowerIsBetter, value, target), reached);
    }

    private static int PercentOf(bool hasData, bool lowerIsBetter, double value, double target)
    {
        if (!hasData || target <= 0 || value <= 0) return 0;
        var ratio = lowerIsBetter ? target / value : value / target;
        return (int)Math.Floor(Math.Clamp(ratio, 0, 1) * 100);
    }

    /// <summary>A metric's current value, and for measurements the drop from the start. Null values mean no data yet.</summary>
    private sealed record Measured(double? Value, double? Drop = null, bool LowerIsBetter = false);

    private static Measured? Measure(string metric, BadgeHistory h, DateOnly today)
    {
        switch (metric.ToLowerInvariant())
        {
            case "sessions": return new Measured(CountedWorkoutsEver(h));
            case "streak": return new Measured(WeekRules.Streak(h.Plan, h.Workouts, h.Breaks, today).BestRun);
            case "bestweek" or "best week": return new Measured(WeekRules.Streak(h.Plan, h.Workouts, h.Breaks, today).BestWeekCount);
            case "prs": return new Measured(h.Exercises.Sum(e => ExercisePerformance.Prs(e, h.Workouts).Count));
            case "bodyweight": return Weights(h);
        }

        var exercise = h.Exercises.FirstOrDefault(e => e.Id == metric);
        return exercise is null ? null : ExerciseMeasure(exercise, h);
    }

    private static int CountedWorkoutsEver(BadgeHistory h) =>
        h.Workouts
            .Select(w => WeekCalendar.MondayOf(w.Date))
            .Distinct()
            .Sum(monday => WeekRules.CountedWorkouts(h.Plan, h.Workouts, monday));

    private static Measured Weights(BadgeHistory h)
    {
        var ordered = h.WeighIns.OrderBy(w => w.Date).ThenBy(w => w.Id, StringComparer.Ordinal).ToList();
        return ordered.Count == 0
            ? new Measured(null)
            : new Measured(ordered[^1].WeightKg, ordered[0].WeightKg - ordered[^1].WeightKg);
    }

    private static Measured ExerciseMeasure(Exercise exercise, BadgeHistory h)
    {
        if (ExercisePerformance.BodyChange(exercise, h.Workouts) is { } change)
            return new Measured(change.Latest, -change.Change);

        var best = ExercisePerformance.Best(exercise, h.Workouts);
        double? value = best is null ? null : SetRanking.ChartValue(exercise.LogKind, best.Set);
        return new Measured(value, null, exercise.LogKind == LogKind.Time);
    }
}
