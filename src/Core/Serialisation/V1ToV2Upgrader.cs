using Core.Model;

namespace Core.Serialisation;

/// <summary>The format version 1 shape of a Session (no <c>trackId</c>).</summary>
internal sealed record V1Session(
    string Id,
    string Name,
    DefaultDay DefaultDay,
    SessionType Type,
    int? BenchmarkIntervalWeeks,
    IReadOnlyList<SessionExercise> Exercises);

/// <summary>The format version 1 shape of a Plan (weekly target, Streak settings and Phases on the Plan).</summary>
internal sealed record V1Plan(
    string Id,
    string Name,
    bool IsActive,
    DateOnly StartDate,
    string? Goal,
    DateOnly? GoalDate,
    int WeeklyTarget,
    int StreakMinWorkoutsPerWeek,
    bool StreakCountsCustomWorkouts,
    double WeightIncrementKg,
    IReadOnlyList<Phase> Phases,
    IReadOnlyList<V1Session> Sessions);

/// <summary>
/// Converts a version 1 Plan into the Track-based model: one weekly Track named "Main" with ID <c>&lt;planId&gt;-main</c>,
/// holding the old weekly target, Streak fields and Phases, with every Session pointing at it.
/// </summary>
internal static class V1ToV2Upgrader
{
    public static Plan UpgradePlan(V1Plan v1)
    {
        var trackId = $"{v1.Id}-main";
        var mainTrack = new Track(
            trackId,
            "Main",
            new TrackRhythm.Weekly(v1.WeeklyTarget, v1.StreakMinWorkoutsPerWeek, v1.StreakCountsCustomWorkouts),
            v1.Phases);
        var sessions = v1.Sessions
            .Select(s => new Session(s.Id, s.Name, trackId, s.DefaultDay, s.Type, s.BenchmarkIntervalWeeks, s.Exercises))
            .ToList();
        return new Plan(v1.Id, v1.Name, v1.IsActive, v1.StartDate, v1.Goal, v1.GoalDate, v1.WeightIncrementKg, [mainTrack], sessions);
    }
}
