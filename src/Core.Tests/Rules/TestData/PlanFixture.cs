using Core.Model;

namespace Core.Tests.Rules.TestData;

/// <summary>Builds Plans and Sessions for the Rules tests. Every argument is named and has a default.</summary>
internal static class PlanFixture
{
public const string MainTrack = "p-main";
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
        IReadOnlyList<Session>? sessions = null,
        string id = "p",
        string name = "Plan",
        bool isActive = true,
        string? goal = null,
        DateOnly? goalDate = null,
        IReadOnlyList<Track>? tracks = null)
    {
        var trackId = $"{id}-main";
        var effectiveTracks = tracks ?? [new Track(trackId, "Main", new TrackRhythm.Weekly(target, minPerWeek, customCounts), phases ?? [])];
        var effectiveSessions = sessions ?? (id == "p" ? DefaultSessions : []);
        return new(id, name, isActive, start ?? DefaultStart, goal, goalDate, incrementKg, effectiveTracks, effectiveSessions);
    }

    public static Plan WithMainPhases(Plan plan, IReadOnlyList<Phase> phases) =>
        plan with { Tracks = [plan.Tracks[0] with { Phases = phases }, .. plan.Tracks.Skip(1)] };

    public static Plan WithMainWeekly(Plan plan, int? target = null, int? minPerWeek = null, bool? customCounts = null)
    {
        var main = plan.Tracks[0];
        var current = (TrackRhythm.Weekly)main.Rhythm;
        var updated = new TrackRhythm.Weekly(
            target ?? current.WeeklyTarget,
            minPerWeek ?? current.StreakMinWorkoutsPerWeek,
            customCounts ?? current.StreakCountsCustomWorkouts);
        return plan with { Tracks = [main with { Rhythm = updated }, .. plan.Tracks.Skip(1)] };
    }

    public static Session Session(
        string id,
        DefaultDay day,
        SessionType type = SessionType.Strength,
        string? name = null,
        int? benchmarkEveryWeeks = null,
        IReadOnlyList<SessionExercise>? exercises = null,
        string trackId = MainTrack) =>
        new(id, name ?? id, trackId, day, type, benchmarkEveryWeeks, exercises ?? []);

    public static Session Session(
        string id,
        DayOfWeek day,
        SessionType type = SessionType.Strength,
        string? name = null,
        int? benchmarkEveryWeeks = null,
        IReadOnlyList<SessionExercise>? exercises = null,
        string trackId = MainTrack) =>
        Session(id, day.ToDefaultDay(), type, name, benchmarkEveryWeeks, exercises, trackId);

    public static SessionExercise Row(string exerciseId, int sets = 3, int? reps = 8, int? repsMax = null) =>
        new(exerciseId, sets, reps, repsMax, null);
}
