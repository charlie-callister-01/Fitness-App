namespace Core.Model;

/// <summary>What happened to an Exercise in a Workout.</summary>
public enum EntryStatus
{
    Done,
    Skipped,
    Swapped,
}

/// <summary>How a Pain check was answered. A Workout with no Pain check at all is unanswered.</summary>
public enum PainStatus
{
    None,
    Reported,
}

public enum PainSeverity
{
    Mild,
    Moderate,
    Sharp,
}

/// <summary>The fixed list of body areas a Pain check can name.</summary>
public enum BodyArea
{
    Neck,
    Shoulder,
    Elbow,
    Wrist,
    UpperBack,
    LowerBack,
    Hip,
    Knee,
    Ankle,
    Foot,
    Other,
}

/// <summary>
/// One continuous effort, in canonical units. Which fields are filled depends on the Exercise's log kind (checked by A2, not here).
/// Cardio distance is <see cref="DistanceKm"/>, carry distance is <see cref="DistanceM"/>; a Set never has both.
/// </summary>
public sealed record WorkoutSet(
    string Id,
    double? WeightKg,
    int? Reps,
    double? DistanceKm,
    int? DurationSeconds,
    double? DistanceM,
    bool IsMax);

/// <summary>
/// An Exercise in a Workout. For a skipped entry <see cref="ExerciseId"/> is the planned Exercise left undone;
/// for a swapped entry it is the Exercise actually done and <see cref="SwappedFromExerciseId"/> the planned original.
/// </summary>
public sealed record WorkoutEntry(
    string ExerciseId,
    EntryStatus Status,
    string? SkipReason,
    string? SwappedFromExerciseId,
    int? Rpe,
    IReadOnlyList<WorkoutSet> Sets);

/// <summary>The end-of-Workout pain form. Areas, severity, note and Exercise are only used when <see cref="Status"/> is reported.</summary>
public sealed record PainCheck(
    string Id,
    PainStatus Status,
    IReadOnlyList<BodyArea> Areas,
    PainSeverity? Severity,
    string? Note,
    string? ExerciseId);

/// <summary>
/// One real occurrence of training on a date. <see cref="Planned"/> is a copy of the planned rows taken when the Workout was created,
/// so later Plan edits never change history; it is empty for a Custom workout. A null <see cref="PainCheck"/> means unanswered.
/// </summary>
public sealed record Workout(
    string Id,
    DateOnly Date,
    string? SessionId,
    bool LoggedAfterwards,
    DateTimeOffset? StartedAt,
    DateTimeOffset? EndedAt,
    IReadOnlyList<SessionExercise> Planned,
    IReadOnlyList<WorkoutEntry> Entries,
    PainCheck? PainCheck);
