using Core.Model;
using Core.Rules.LogKinds;
using Core.Rules.Performance;
using Core.Rules.Weeks;

namespace Core.Rules.Insight;

/// <summary>One Exercise's personal Best with the day it was set.</summary>
public sealed record BestRow(string ExerciseId, string ExerciseName, LogKind LogKind, BestResult Result)
{
    public DateOnly Date => Result.Date;
}

/// <summary>The PR chosen as the week's highlight.</summary>
public sealed record BestOfTheWeek(string ExerciseId, string ExerciseName, LogKind LogKind, WorkoutSet Set, DateOnly Date, BestResult? PreviousBest);

/// <summary>The Bests tab: every Exercise's Best by name, and the Best of the week (null when no PR was set this Monday-to-Sunday week).</summary>
public sealed record BestsView(IReadOnlyList<BestRow> Bests, BestOfTheWeek? BestOfTheWeek)
{
    public bool IsEmpty => Bests.Count == 0;
}

/// <summary>
/// Builds the Bests view over the existing Best and PR rules. The Best of the week is the PR in the current week with the biggest
/// relative jump over the Best before it; ties go to the later date, then to the Exercise name.
/// </summary>
public static class BestsViewBuilder
{
    public static BestsView Build(IReadOnlyList<Exercise> exercises, IReadOnlyList<Workout> workouts, DateOnly today)
    {
        var monday = WeekCalendar.MondayOf(today);
        var sunday = WeekCalendar.SundayOf(today);

        var bests = new List<BestRow>();
        var candidates = new List<(BestOfTheWeek Pr, double Jump)>();

        foreach (var exercise in exercises)
        {
            if (ExercisePerformance.Best(exercise, workouts) is { } best)
            {
                bests.Add(new BestRow(exercise.Id, exercise.Name, exercise.LogKind, best));
            }

            foreach (var pr in ExercisePerformance.Prs(exercise, workouts).Where(p => p.Date >= monday && p.Date <= sunday))
            {
                var before = workouts.Where(w => w.Date < pr.Date || (w.Date == pr.Date && w.Id != pr.WorkoutId));
                var previous = ExercisePerformance.Best(exercise, before);
                candidates.Add((new BestOfTheWeek(exercise.Id, exercise.Name, exercise.LogKind, pr.Set, pr.Date, previous), Jump(exercise.LogKind, pr.Set, previous)));
            }
        }

        var week = candidates
            .OrderByDescending(c => c.Jump)
            .ThenByDescending(c => c.Pr.Date)
            .ThenBy(c => c.Pr.ExerciseName, StringComparer.Ordinal)
            .Select(c => c.Pr)
            .FirstOrDefault();

        return new BestsView([.. bests.OrderBy(b => b.ExerciseName, StringComparer.OrdinalIgnoreCase)], week);
    }

    private static double Jump(LogKind kind, WorkoutSet set, BestResult? previous)
    {
        var rules = LogKindRules.For(kind);
        if (previous is null || rules.ChartValue(set) is not { } now || rules.ChartValue(previous.Set) is not { } before || now <= 0 || before <= 0)
        {
            return 0;
        }

        return Math.Round(rules.LowerIsBetter ? before / now : now / before, 4);
    }
}
