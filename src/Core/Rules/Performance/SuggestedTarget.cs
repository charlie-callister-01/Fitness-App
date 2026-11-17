namespace Core.Rules.Performance;

/// <summary>
/// The next target for an Exercise, with a one-line reason. Only the fields that apply to the Exercise's kind are set
/// (weight and reps for load, weight and distance for a carry, a duration for time, and so on). The weight step is in kg.
/// </summary>
public sealed record SuggestedTarget(
    double? WeightKg,
    int? Reps,
    double? DistanceM,
    int? DurationSeconds,
    double? DistanceKm,
    string Reason);
