namespace Core.Model;

public enum WeightUnit
{
    Kg,
    Lb,
}

public enum DistanceUnit
{
    Km,
    Mi,
}

/// <summary>App-wide settings. Display units affect presentation only; stored data is always canonical (kg, km, m).</summary>
public sealed record Settings(
    WeightUnit DisplayWeightUnit,
    DistanceUnit DisplayDistanceUnit,
    double WeightIncrementKg,
    int RestTimerSeconds,
    double? GoalWeightKg,
    int StreakMinWorkoutsPerWeek,
    bool StreakCountsCustomWorkouts);
