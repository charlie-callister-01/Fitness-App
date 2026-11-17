using Core.Model;

namespace Core.Tests.Rules.TestData;

/// <summary>
/// The one fluent builder for Plans, Workouts and Breaks in the Rules tests. Instance methods accumulate a history;
/// the static factories make single items. Ids come from counters, never from <c>Guid.NewGuid()</c>.
/// </summary>
internal sealed class HistoryBuilder
{
    private static int _nextSet;

    private readonly List<Workout> _workouts = [];
    private readonly List<Break> _breaks = [];
    private int _next;

    public Plan Plan { get; private set; } = PlanFixture.Make();

    public IReadOnlyList<Workout> Workouts => _workouts;
    public IReadOnlyList<Break> Breaks => _breaks;

    // ---- static factories

    public static WorkoutSet Set(double? kg = null, int? reps = null, double? km = null, int? seconds = null, double? m = null, bool max = false, double? cm = null) =>
        new($"s{Interlocked.Increment(ref _nextSet)}", kg, reps, km, seconds, m, max, cm);

    /// <summary>A Workout with no entries, as logged against a Session (or none, for a Custom workout).</summary>
    public static Workout Logged(DateOnly date, string? sessionId) =>
        new($"w{date.DayNumber}{sessionId}", date, sessionId, false, null, null, [], [], null);

    /// <summary>A Workout holding one completed Exercise with one Set.</summary>
    public static Workout Lift(string id, DateOnly date, string? sessionId, string exerciseId, WorkoutSet set) =>
        new(id, date, sessionId, false, null, null, [],
            [new WorkoutEntry(exerciseId, EntryStatus.Done, null, null, null, [set])], null);

    /// <summary>A Custom Workout carrying a reported pain check for <paramref name="areas"/>.</summary>
    public static Workout Pain(DateOnly date, params BodyArea[] areas) =>
        new($"p{date.DayNumber}", date, null, false, null, null, [],
            [new WorkoutEntry("goblet-squat", EntryStatus.Done, null, null, null, [])],
            new PainCheck("pc", PainStatus.Reported, areas, PainSeverity.Mild, null, null));

    public static Break BreakOf(DateOnly from, DateOnly to) => new("b", from, to, "x");

    // ---- Plan

    public HistoryBuilder WithPlan(DateOnly start, int target = 3, int minPerWeek = 2, bool customCounts = false)
    {
        Plan = PlanFixture.Make(start, target, minPerWeek, customCounts);
        return this;
    }

    // ---- dated Workouts, by string date and explicit id

    public HistoryBuilder Done(string id, string date, string exerciseId, params WorkoutSet[] sets) =>
        Add(id, date, new WorkoutEntry(exerciseId, EntryStatus.Done, null, null, null, sets));

    public HistoryBuilder DoneWithRpe(string id, string date, string exerciseId, int rpe, params WorkoutSet[] sets) =>
        Add(id, date, new WorkoutEntry(exerciseId, EntryStatus.Done, null, null, rpe, sets));

    public HistoryBuilder Skipped(string id, string date, string exerciseId) =>
        Add(id, date, new WorkoutEntry(exerciseId, EntryStatus.Skipped, "time", null, null, []));

    /// <summary>A swapped entry: <paramref name="actualId"/> was done instead of <paramref name="plannedId"/>.</summary>
    public HistoryBuilder Swapped(string id, string date, string plannedId, string actualId, params WorkoutSet[] sets) =>
        Add(id, date, new WorkoutEntry(actualId, EntryStatus.Swapped, null, plannedId, null, sets));

    // ---- dated Workouts against the default Plan, by DateOnly and generated id

    /// <summary>Strength Workouts with one completed Exercise each.</summary>
    public HistoryBuilder Did(params DateOnly[] dates)
    {
        foreach (var d in dates) AddFor(d, PlanFixture.Strength, EntryStatus.Done);
        return this;
    }

    public HistoryBuilder Custom(DateOnly date) => AddFor(date, null, EntryStatus.Done);

    public HistoryBuilder BenchmarkTest(DateOnly date) => AddFor(date, PlanFixture.Benchmark, EntryStatus.Done);

    /// <summary>A Workout where every Exercise was skipped.</summary>
    public HistoryBuilder AllSkipped(DateOnly date) => AddFor(date, PlanFixture.Strength, EntryStatus.Skipped);

    public HistoryBuilder Swapped(DateOnly date) => AddFor(date, PlanFixture.Strength, EntryStatus.Swapped);

    public HistoryBuilder EmptyWorkout(DateOnly date)
    {
        _workouts.Add(new Workout($"w{_next++}", date, PlanFixture.Strength, false, null, null, [], [], null));
        return this;
    }

    public HistoryBuilder Break(DateOnly from, DateOnly to)
    {
        _breaks.Add(new Break($"b{_next++}", from, to, "x"));
        return this;
    }

    private HistoryBuilder Add(string id, string date, WorkoutEntry entry)
    {
        _workouts.Add(new Workout(id, DateOnly.Parse(date), null, false, null, null, [], [entry], null));
        return this;
    }

    private HistoryBuilder AddFor(DateOnly date, string? sessionId, EntryStatus status)
    {
        var entry = new WorkoutEntry("goblet-squat", status, null, null, null, []);
        _workouts.Add(new Workout($"w{_next++}", date, sessionId, false, null, null, [], [entry], null));
        return this;
    }
}
