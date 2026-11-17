using Core.Model;

namespace Core.Tests.Rules.Weeks;

/// <summary>Small fluent builder for dated Plan and Workout histories used by the week rules tests.</summary>
internal sealed class WeekHistory
{
    public const string Strength = "a-lower";
    public const string Benchmark = "bench-test";

    private readonly List<Workout> _workouts = [];
    private readonly List<Break> _breaks = [];
    private int _next;

    public Plan Plan { get; private set; } = MakePlan(new DateOnly(2026, 1, 5), target: 3, min: 2, customCounts: false);

    public IReadOnlyList<Workout> Workouts => _workouts;
    public IReadOnlyList<Break> Breaks => _breaks;

    public static Plan MakePlan(DateOnly start, int target, int min, bool customCounts) => new(
        "p", "Plan", true, start, null, null, target, min, customCounts, 2, [],
        [
            new Session(Strength, "Lower", DayOfWeek.Monday, SessionType.Strength, null, []),
            new Session(Benchmark, "Test", DayOfWeek.Saturday, SessionType.Test, 6, []),
        ]);

    public WeekHistory WithPlan(DateOnly start, int target = 3, int min = 2, bool customCounts = false)
    {
        Plan = MakePlan(start, target, min, customCounts);
        return this;
    }

    /// <summary>Strength Workouts with one completed Exercise each.</summary>
    public WeekHistory Did(params DateOnly[] dates)
    {
        foreach (var d in dates) Add(d, Strength, EntryStatus.Done);
        return this;
    }

    public WeekHistory Custom(DateOnly date) => Add(date, null, EntryStatus.Done);

    public WeekHistory BenchmarkTest(DateOnly date) => Add(date, Benchmark, EntryStatus.Done);

    /// <summary>A Workout where every Exercise was skipped.</summary>
    public WeekHistory AllSkipped(DateOnly date) => Add(date, Strength, EntryStatus.Skipped);

    public WeekHistory Swapped(DateOnly date) => Add(date, Strength, EntryStatus.Swapped);

    public WeekHistory EmptyWorkout(DateOnly date)
    {
        _workouts.Add(new Workout($"w{_next++}", date, Strength, false, null, null, [], [], null));
        return this;
    }

    public WeekHistory Break(DateOnly from, DateOnly to)
    {
        _breaks.Add(new Break($"b{_next++}", from, to, "x"));
        return this;
    }

    private WeekHistory Add(DateOnly date, string? sessionId, EntryStatus status)
    {
        var entry = new WorkoutEntry("goblet-squat", status, null, null, null, []);
        _workouts.Add(new Workout($"w{_next++}", date, sessionId, false, null, null, [], [entry], null));
        return this;
    }
}
