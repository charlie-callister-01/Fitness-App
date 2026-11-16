namespace Core.Model;

/// <summary>How an Exercise is measured. Meaning is defined in the data spec; behaviour (parsing, PRs, targets) is A2.</summary>
public enum LogKind
{
    Load,
    BodyweightLoad,
    Carry,
    Reps,
    Seconds,
    Minutes,
    DistanceKm,
    DistanceM,
    Time,
    BodyMeasurementKg,
    BodyMeasurementCm,
    Text,
    TickOnly,
}

/// <summary>Optional tags used for "similar Exercises" and week rearrangement.</summary>
public sealed record ExerciseTags(
    IReadOnlyList<string> MuscleGroups,
    IReadOnlyList<string> Equipment,
    IReadOnlyList<string> MovementPatterns);

/// <summary>An entry in the Exercise library. The ID is permanent; the name is only a label.</summary>
public sealed record Exercise(
    string Id,
    string Name,
    LogKind LogKind,
    ExerciseTags? Tags,
    bool IsBenchmark);
