namespace Core.Model;

/// <summary>What kind of training a Session is. A benchmark Session is a <see cref="Test"/> Session with a benchmark interval.</summary>
public enum SessionType
{
    Strength,
    Cardio,
    Body,
    Test,
    Mobility,
}

/// <summary>When a Session normally falls: a day of the week on a weekly Track, or every day on a daily Track.</summary>
public enum DefaultDay
{
    Sunday,
    Monday,
    Tuesday,
    Wednesday,
    Thursday,
    Friday,
    Saturday,
    Daily,
}

/// <summary>Cast-free conversions between <see cref="DayOfWeek"/> and <see cref="DefaultDay"/>.</summary>
public static class DefaultDayExtensions
{
    public static DefaultDay ToDefaultDay(this DayOfWeek day) => day switch
    {
        DayOfWeek.Sunday => DefaultDay.Sunday,
        DayOfWeek.Monday => DefaultDay.Monday,
        DayOfWeek.Tuesday => DefaultDay.Tuesday,
        DayOfWeek.Wednesday => DefaultDay.Wednesday,
        DayOfWeek.Thursday => DefaultDay.Thursday,
        DayOfWeek.Friday => DefaultDay.Friday,
        DayOfWeek.Saturday => DefaultDay.Saturday,
        _ => throw new ArgumentOutOfRangeException(nameof(day), day, null),
    };

    public static DayOfWeek? AsDayOfWeek(this DefaultDay day) => day switch
    {
        DefaultDay.Sunday => DayOfWeek.Sunday,
        DefaultDay.Monday => DayOfWeek.Monday,
        DefaultDay.Tuesday => DayOfWeek.Tuesday,
        DefaultDay.Wednesday => DayOfWeek.Wednesday,
        DefaultDay.Thursday => DayOfWeek.Thursday,
        DefaultDay.Friday => DayOfWeek.Friday,
        DefaultDay.Saturday => DayOfWeek.Saturday,
        _ => null,
    };
}

/// <summary>Whether a Track runs on a weekly target or a daily done threshold.</summary>
public enum TrackCadence
{
    Weekly,
    Daily,
}

/// <summary>The cadence-specific rules of a Track, so a weekly Track cannot carry daily rules or vice versa.</summary>
public abstract record TrackRhythm
{
    private TrackRhythm()
    {
    }

    public TrackCadence Cadence => this switch
    {
        Weekly => TrackCadence.Weekly,
        Daily => TrackCadence.Daily,
        _ => throw new InvalidOperationException(),
    };

    public sealed record Weekly(
        int WeeklyTarget,
        int StreakMinWorkoutsPerWeek,
        bool StreakCountsCustomWorkouts) : TrackRhythm;

    public sealed record Daily(int DayDonePercent = 75) : TrackRhythm;
}

/// <summary>A named training path inside a Plan with its own rhythm and optional Phases. The first Track is the main Track.</summary>
public sealed record Track(
    string Id,
    string Name,
    TrackRhythm Rhythm,
    IReadOnlyList<Phase> Phases);

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

/// <summary>A planned template in a Plan, belonging to one Track and usually tied to a day of the week (or daily).</summary>
public sealed record Session(
    string Id,
    string Name,
    string TrackId,
    DefaultDay DefaultDay,
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

/// <summary>A block of weeks inside a Track. Weeks are numbered from 1 (Plan-wide); both ends are inclusive.</summary>
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
    double WeightIncrementKg,
    IReadOnlyList<Track> Tracks,
    IReadOnlyList<Session> Sessions);
