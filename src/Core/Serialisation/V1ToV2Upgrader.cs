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

/// <summary>Temporary format 1 document projection used by the writer until format 2 serialisation lands.</summary>
internal sealed record V1ExchangeDocument(
    int FormatVersion,
    DateTimeOffset? ExportedAt,
    string? ExportedFrom,
    IReadOnlyList<Exercise>? Exercises,
    Settings? Settings,
    IReadOnlyList<WeighIn>? WeighIns = null,
    IReadOnlyList<Break>? Breaks = null,
    IReadOnlyList<Badge>? Badges = null,
    IReadOnlyList<Guide>? Guides = null,
    IReadOnlyList<V1Plan>? Plans = null,
    IReadOnlyList<Workout>? Workouts = null,
    IReadOnlyList<WeekSchedule>? WeekSchedules = null);

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

    public static V1ExchangeDocument ToV1Document(ExchangeDocument document) => new(
        document.FormatVersion,
        document.ExportedAt,
        document.ExportedFrom,
        document.Exercises,
        document.Settings,
        document.WeighIns,
        document.Breaks,
        document.Badges,
        document.Guides,
        document.Plans?.Select(ToV1Plan).ToList(),
        document.Workouts,
        document.WeekSchedules);

    private static V1Plan ToV1Plan(Plan plan)
    {
        var main = plan.Tracks.FirstOrDefault();
        var weekly = main?.Rhythm as TrackRhythm.Weekly;
        return new V1Plan(
            plan.Id,
            plan.Name,
            plan.IsActive,
            plan.StartDate,
            plan.Goal,
            plan.GoalDate,
            weekly?.WeeklyTarget ?? 0,
            weekly?.StreakMinWorkoutsPerWeek ?? 0,
            weekly?.StreakCountsCustomWorkouts ?? false,
            plan.WeightIncrementKg,
            main?.Phases ?? [],
            [.. plan.Sessions.Select(s => new V1Session(s.Id, s.Name, s.DefaultDay, s.Type, s.BenchmarkIntervalWeeks, s.Exercises))]);
    }
}
