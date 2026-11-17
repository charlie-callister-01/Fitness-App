using Core.Model;
using Core.Rules.Performance;
using Core.Rules.Schedule;
using Core.Rules.Weeks;

namespace Core.Rules.Benchmarks;

/// <summary>
/// One Benchmark exercise for the Benchmarks screen. <see cref="Best"/> is the best across all history;
/// <see cref="Latest"/> and <see cref="Previous"/> are the best Sets from the two most recent benchmark Workouts.
/// <see cref="Change"/> is Latest minus Previous in the Exercise's chart measure, null unless both exist.
/// </summary>
public sealed record BenchmarkEntry(Exercise Exercise, BestResult? Best, BestResult? Latest, BestResult? Previous, double? Change);

/// <summary>A row of a benchmark Session: the Exercise and the result to beat (the latest benchmark-test result).</summary>
public sealed record BenchmarkPreFillRow(Exercise Exercise, BestResult? Previous);

/// <summary>The next benchmark: which Session and the date it falls on in its week (its default day).</summary>
public sealed record BenchmarkDue(string SessionId, DateOnly Date);

/// <summary>Benchmarks screen and benchmark Session answers, derived from history. "Today" is always passed in.</summary>
public static class BenchmarkRules
{
    /// <summary>Benchmark exercises in library order with current best, latest and previous test result, and the change.</summary>
    public static IReadOnlyList<BenchmarkEntry> List(Plan plan, IEnumerable<Exercise> exercises, IEnumerable<Workout> history)
    {
        var all = history.ToList();
        var tests = TestWorkouts(plan, all);
        return exercises.Where(e => e.IsBenchmark).Select(e => Entry(e, all, tests)).ToList();
    }

    /// <summary>Rows for a benchmark Session: every Benchmark exercise with its latest test result beside it.</summary>
    public static IReadOnlyList<BenchmarkPreFillRow> PreFill(Plan plan, IEnumerable<Exercise> exercises, IEnumerable<Workout> history) =>
        List(plan, exercises, history).Select(e => new BenchmarkPreFillRow(e.Exercise, e.Latest)).ToList();

    /// <summary>
    /// The earliest benchmark Session still to do: the first week from today's that the Session is scheduled in
    /// (see <see cref="BenchmarkWeeks"/>) with no Workout of it yet. Null when the Plan has no benchmark Session.
    /// The date is the Session's day in that week's Week schedule, so a move is honoured.
    /// </summary>
    public static BenchmarkDue? NextDue(
        Plan plan, IReadOnlyList<WeekSchedule> schedules, IEnumerable<Workout> history, DateOnly today)
    {
        var all = history.ToList();
        return plan.Sessions
            .Where(BenchmarkWeeks.IsBenchmark)
            .Select(s => DueOf(plan, schedules, s, all, today))
            .OfType<BenchmarkDue>()
            .OrderBy(d => d.Date)
            .FirstOrDefault();
    }

    /// <summary>
    /// The first benchmark week (Plan weeks 1, 1+N, 1+2N) from today's with no Workout of the Session. Only as many
    /// benchmark weeks as there are Workouts can be done, so looking one past that always finds a free week.
    /// </summary>
    private static BenchmarkDue? DueOf(
        Plan plan, IReadOnlyList<WeekSchedule> schedules, Session session, List<Workout> history, DateOnly today)
    {
        if (session.BenchmarkIntervalWeeks is not int interval || interval <= 0) return null;

        var planMonday = WeekCalendar.MondayOf(plan.StartDate);
        var fromMonday = Max(WeekCalendar.MondayOf(today), planMonday);
        var weeksIn = (fromMonday.DayNumber - planMonday.DayNumber) / 7;
        var firstBenchmarkIndex = (weeksIn + interval - 1) / interval;

        return Enumerable.Range(firstBenchmarkIndex, history.Count + 1)
            .Select(index => WeekCalendar.AddWeeks(planMonday, index * interval))
            .Where(monday => !history.Any(w => w.SessionId == session.Id && WeekCalendar.InWeek(monday, w.Date)))
            .Select(monday => WeekScheduleRules.PlaceSession(plan, schedules, [], session, monday))
            .OfType<ScheduledSession>()
            .Select(placed => new BenchmarkDue(session.Id, placed.Date))
            .FirstOrDefault();
    }

    private static DateOnly Max(DateOnly a, DateOnly b) => a > b ? a : b;

    private static BenchmarkEntry Entry(Exercise exercise, List<Workout> all, List<Workout> tests)
    {
        var results = tests
            .Select(w => ExercisePerformance.Best(exercise, [w]))
            .OfType<BestResult>()
            .ToList();
        var latest = results.Count > 0 ? results[^1] : null;
        var previous = results.Count > 1 ? results[^2] : null;
        double? change = latest is not null && previous is not null
            && SetRanking.ChartValue(exercise.LogKind, latest.Set) is { } now
            && SetRanking.ChartValue(exercise.LogKind, previous.Set) is { } before
                ? now - before
                : null;
        return new BenchmarkEntry(exercise, ExercisePerformance.Best(exercise, all), latest, previous, change);
    }

    /// <summary>Workouts made from a benchmark Session, oldest first.</summary>
    private static List<Workout> TestWorkouts(Plan plan, List<Workout> all)
    {
        var ids = plan.Sessions.Where(BenchmarkWeeks.IsBenchmark).Select(s => s.Id).ToHashSet();
        return all.Where(w => w.SessionId is not null && ids.Contains(w.SessionId))
            .OrderBy(w => w.Date).ThenBy(w => w.StartedAt ?? DateTimeOffset.MinValue).ThenBy(w => w.Id, StringComparer.Ordinal)
            .ToList();
    }
}
