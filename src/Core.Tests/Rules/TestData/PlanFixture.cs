using Core.Model;

namespace Core.Tests.Rules.TestData;

/// <summary>Builds Plans and Sessions for the Rules tests. Every argument is named and has a default.</summary>
internal static class PlanFixture
{
    public const string Strength = "a-lower";
    public const string Benchmark = "bench-test";

    public static readonly DateOnly DefaultStart = Dates.D(2026, 1, 5);

    /// <summary>A Strength Session on Mondays and a benchmark Test Session on Saturdays (every 6 weeks).</summary>
    public static IReadOnlyList<Session> DefaultSessions { get; } =
    [
        Session(Strength, DayOfWeek.Monday, name: "Lower"),
        Session(Benchmark, DayOfWeek.Saturday, SessionType.Test, name: "Test", benchmarkEveryWeeks: 6),
    ];

    public static Plan Make(
        DateOnly? start = null,
        int target = 3,
        int minPerWeek = 2,
        bool customCounts = false,
        double incrementKg = 2,
        IReadOnlyList<Phase>? phases = null,
        IReadOnlyList<Session>? sessions = null) =>
        new("p", "Plan", true, start ?? DefaultStart, null, null, target, minPerWeek, customCounts, incrementKg,
            phases ?? [], sessions ?? DefaultSessions);

    public static Session Session(
        string id,
        DayOfWeek day,
        SessionType type = SessionType.Strength,
        string? name = null,
        int? benchmarkEveryWeeks = null,
        IReadOnlyList<SessionExercise>? exercises = null) =>
        new(id, name ?? id, day, type, benchmarkEveryWeeks, exercises ?? []);

    public static SessionExercise Row(string exerciseId, int sets = 3, int? reps = 8, int? repsMax = null) =>
        new(exerciseId, sets, reps, repsMax, null);
}
