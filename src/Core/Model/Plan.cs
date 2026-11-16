namespace Core.Model;

/// <summary>What kind of training a Session is. A benchmark Session is a <see cref="Test"/> Session with a benchmark interval.</summary>
public enum SessionType
{
    Strength,
    Cardio,
    Body,
    Test,
}

/// <summary>
/// One Exercise row in a Session (or in a Phase override). Array order is the row order.
/// Reps is the target (or the low end of a range); RepsMax is the high end when the target is a range.
/// Both are null for rows with no rep target (a run, a tick-only row).
/// </summary>
public sealed record SessionExercise(
    string ExerciseId,
    int Sets,
    int? Reps,
    int? RepsMax,
    string? Cue);

/// <summary>A planned template in a Plan, usually tied to a day of the week.</summary>
public sealed record Session(
    string Id,
    string Name,
    DayOfWeek DefaultDay,
    SessionType Type,
    int? BenchmarkIntervalWeeks,
    IReadOnlyList<SessionExercise> Exercises);

/// <summary>A Phase replaces one Session's Exercise rows while it is in force; Sessions it doesn't mention use the Plan default.</summary>
public sealed record SessionOverride(
    string SessionId,
    IReadOnlyList<SessionExercise> Exercises);

/// <summary>An optional target that shows when the user is ready to move on early (a lift hitting a weight for reps).</summary>
public sealed record PhaseGoal(
    string ExerciseId,
    double WeightKg,
    int Reps);

/// <summary>A block of weeks inside a Plan. Weeks are numbered from 1; both ends are inclusive.</summary>
public sealed record Phase(
    string Id,
    string Focus,
    int StartWeek,
    int EndWeek,
    IReadOnlyList<PhaseGoal> Goals,
    IReadOnlyList<SessionOverride> SessionOverrides);

/// <summary>A complete training programme. Several can exist; exactly one is active.</summary>
public sealed record Plan(
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
    IReadOnlyList<Session> Sessions);
