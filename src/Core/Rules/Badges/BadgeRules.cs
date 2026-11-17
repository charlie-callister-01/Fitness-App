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
        var value = wantsDrop ? m.Drop : m.Value;
        if (value is not { } current)
            return new BadgeProgress(badge.Id, badge.Metric, true, 0, badge.Target, 0, false);

        // A drop is a rising number (how far it has fallen), so it is never "lower is better".
        var reading = new Reading(current, Math.Abs(badge.Target), m.LowerIsBetter && !wantsDrop);
        return new BadgeProgress(badge.Id, badge.Metric, true, current, badge.Target, reading.Percent, reading.Reached);
    }

    /// <summary>A reading that exists, against a target. Higher is better unless <paramref name="LowerIsBetter"/> (a time).</summary>
    private readonly record struct Reading(double Value, double Target, bool LowerIsBetter)
    {
        public bool Reached => LowerIsBetter ? Value <= Target : Value >= Target;

        public int Percent
        {
            get
            {
                if (Target <= 0 || Value <= 0) return 0;
                var ratio = LowerIsBetter ? Target / Value : Value / Target;
                return (int)Math.Floor(Math.Clamp(ratio, 0, 1) * 100);
            }
        }
    }

    /// <summary>
    /// A metric's current value, and for measurements the drop from the start (positive when the reading has fallen).
    /// Null values mean no data yet.
    /// </summary>
    private sealed record Measured(double? Value, double? Drop = null, bool LowerIsBetter = false);

    private static Measured? Measure(string metric, BadgeHistory h, DateOnly today)
    {
        switch (metric.ToLowerInvariant())
        {
            case "sessions": return new Measured(WeekRules.CountedWorkoutsByWeek(h.Plan, h.Workouts).Values.Sum());
            case "streak" or "bestweek" or "best week":
                var streak = WeekRules.Streak(h.Plan, h.Workouts, h.Breaks, today);
                return new Measured(metric.ToLowerInvariant() == "streak" ? streak.BestRun : streak.BestWeekCount);
            case "prs": return new Measured(h.Exercises.Sum(e => ExercisePerformance.Prs(e, h.Workouts).Count));
            case "bodyweight": return Weights(h);
        }

        var exercise = h.Exercises.FirstOrDefault(e => e.Id == metric);
        return exercise is null ? null : ExerciseMeasure(exercise, h);
    }

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
            return new Measured(change.Latest, change.Start - change.Latest);

        var best = ExercisePerformance.Best(exercise, h.Workouts);
        double? value = best is null ? null : SetRanking.ChartValue(exercise.LogKind, best.Set);
        return new Measured(value, null, exercise.LogKind == LogKind.Time);
    }
}
