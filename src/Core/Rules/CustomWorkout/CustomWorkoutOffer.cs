using Core.Model;
using Core.Rules.Phases;
using Core.Rules.Schedule;

namespace Core.Rules.CustomWorkout;

/// <summary>The order Exercises are offered in when building a Custom workout. "Today" is always passed in.</summary>
public static class CustomWorkoutOffer
{
    /// <summary>
    /// Every library Exercise, ordered for offering: first the planned Exercises of this week's Missed sessions (Sessions in
    /// Plan order, rows in row order, using the current Phase's Override rows when there is one), each once, then the rest
    /// of the library in library order. Planned Exercises not in the library are skipped.
    /// </summary>
    public static IReadOnlyList<Exercise> Order(
        Plan plan, IReadOnlyList<WeekSchedule> schedules, IReadOnlyList<Break> breaks,
        IReadOnlyList<Workout> workouts, IEnumerable<Exercise> library, DateOnly today)
    {
        var all = library.ToList();
        var missedIds = WeekScheduleRules.MissedInWeek(plan, schedules, breaks, workouts, today, today)
            .Select(m => m.SessionId).ToHashSet();
        var byId = all.GroupBy(e => e.Id).ToDictionary(g => g.Key, g => g.First());

        var first = plan.Sessions
            .Where(s => missedIds.Contains(s.Id))
            .SelectMany(s => PhaseRules.ExerciseRows(plan, s.Id, today))
            .Select(r => byId.GetValueOrDefault(r.ExerciseId))
            .OfType<Exercise>()
            .Distinct()
            .ToList();

        return [.. first, .. all.Except(first)];
    }
}
