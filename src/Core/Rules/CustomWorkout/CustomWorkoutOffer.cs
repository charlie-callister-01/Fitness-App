using Core.Model;
using Core.Rules.Phases;
using Core.Rules.Schedule;
using Core.Rules.Tracks;

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
        IReadOnlyList<Workout> workouts, IReadOnlyList<Exercise> library, DateOnly today)
    {
        var main = TrackRules.Main(plan);
        var missedIds = WeekScheduleRules.MissedInWeek(plan, main, schedules, breaks, workouts, today, today)
            .Select(m => m.SessionId).ToHashSet();
        var phase = PhaseRules.CurrentPhase(plan, TrackRules.Main(plan), today);

        var byId = new Dictionary<string, Exercise>();
        foreach (var exercise in library) byId.TryAdd(exercise.Id, exercise);

        var offeredIds = new HashSet<string>();
        var first = new List<Exercise>();
        var plannedRows = TrackRules.SessionsOf(plan, main)
            .Where(s => missedIds.Contains(s.Id))
            .SelectMany(s => PhaseRules.ExerciseRows(plan, phase, s.Id));
        foreach (var row in plannedRows)
        {
            if (byId.TryGetValue(row.ExerciseId, out var exercise) && offeredIds.Add(exercise.Id))
                first.Add(exercise);
        }

        return [.. first, .. library.Where(e => !offeredIds.Contains(e.Id))];
    }
}
