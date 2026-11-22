using System.Globalization;
using Core.Model;
using Core.Rules.Badges;
using Core.Rules.Performance;
using Core.Rules.Units;
using Core.Rules.Weeks;

namespace Core.Rules.Insight;

/// <summary>The words on a Badge's detail: what it needs to unlock ("Estimated 1RM of 100 kg on Squat") and the events that counted towards it.</summary>
public static class BadgeText
{
    public static string Needs(Badge badge, string metric, BadgeHistory h, UnitConverter units)
    {
        var t = badge.Target;
        var abs = InsightFormat.Trimmed(Math.Abs(t));
        switch (metric.ToLowerInvariant())
        {
            case "sessions": return $"Complete {abs} Workouts";
            case "streak": return $"Reach a {abs}-week Streak";
            case "bestweek" or "best week": return $"Complete {abs} Workouts in one week";
            case "prs": return $"Set {abs} PRs";
            case "bodyweight":
                var shown = InsightFormat.Trimmed(Math.Abs(units.DisplayWeight(t)));
                return t < 0 ? $"Drop {shown} {units.WeightLabel} from your first Weigh-in" : $"Reach {shown} {units.WeightLabel}";
        }

        var exercise = h.Exercises.FirstOrDefault(e => e.Id == metric);
        if (exercise is null) return $"Metric \"{badge.Metric}\" is not measurable";
        if (exercise.LogKind == LogKind.Load && t > 0)
        {
            var shown = InsightFormat.Trimmed(units.DisplayWeight(t));
            return $"Estimated 1RM of {shown} {units.WeightLabel} on {exercise.Name}";
        }

        return t < 0 ? $"Drop {abs} on {exercise.Name}" : $"Reach {abs} on {exercise.Name}";
    }

    public static List<BadgeEvent> Events(string metric, BadgeHistory h, UnitConverter units)
    {
        switch (metric.ToLowerInvariant())
        {
            case "sessions":
                return Counted(h).Select(WorkoutEvent(h)).ToList();
            case "streak":
                return Counted(h)
                    .GroupBy(w => WeekCalendar.MondayOf(w.Date))
                    .OrderByDescending(g => g.Key)
                    .Select(g => new BadgeEvent(g.Key, $"Week of {InsightFormat.DayMonth(g.Key)}: {g.Count()} Workouts"))
                    .ToList();
            case "bestweek" or "best week":
                var weeks = Counted(h).GroupBy(w => WeekCalendar.MondayOf(w.Date)).ToList();
                var top = weeks.Count == 0 ? 0 : weeks.Max(g => g.Count());
                return weeks.Where(g => g.Count() == top).OrderByDescending(g => g.Key)
                    .SelectMany(g => g.OrderByDescending(w => w.Date)).Select(WorkoutEvent(h)).ToList();
            case "prs":
                return h.Exercises
                    .SelectMany(e => ExercisePerformance.Prs(e, h.Workouts).Select(p => new BadgeEvent(p.Date, $"PR: {e.Name}")))
                    .OrderByDescending(e => e.Date).ThenBy(e => e.Text, StringComparer.Ordinal).ToList();
            case "bodyweight":
                return h.WeighIns.OrderByDescending(w => w.Date).ThenBy(w => w.Id, StringComparer.Ordinal)
                    .Select(w => new BadgeEvent(w.Date, $"Weigh-in: {InsightFormat.Trimmed(units.DisplayWeight(w.WeightKg))} {units.WeightLabel}"))
                    .ToList();
        }

        var exercise = h.Exercises.FirstOrDefault(e => e.Id == metric);
        if (exercise is null) return [];
        var prs = ExercisePerformance.Prs(exercise, h.Workouts)
            .Select(p => new BadgeEvent(p.Date, $"PR: {exercise.Name}")).OrderByDescending(e => e.Date).ToList();
        if (prs.Count > 0) return prs;
        return ExercisePerformance.Best(exercise, h.Workouts) is { } best ? [new BadgeEvent(best.Date, $"Best: {exercise.Name}")] : [];
    }

    private static IEnumerable<Workout> Counted(BadgeHistory h) =>
        h.Workouts.Where(w => WeekRules.IsCounted(h.Plan, w)).OrderByDescending(w => w.Date).ThenBy(w => w.Id, StringComparer.Ordinal);

    private static Func<Workout, BadgeEvent> WorkoutEvent(BadgeHistory h) => w =>
        new BadgeEvent(w.Date, "Workout: " + (h.Plan.Sessions.FirstOrDefault(s => s.Id == w.SessionId)?.Name ?? "Custom workout"));
}
