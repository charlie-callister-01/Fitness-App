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
    bool StreakCountsCustomWorkouts)
{
    /// <summary>What the app uses when no Settings are stored (a first run, or the owner has never changed them).</summary>
    public static Settings Default { get; } = new(
        DisplayWeightUnit: WeightUnit.Kg,
        DisplayDistanceUnit: DistanceUnit.Km,
        WeightIncrementKg: 2.5,
        RestTimerSeconds: 90,
        GoalWeightKg: null,
        StreakMinWorkoutsPerWeek: 3,
        StreakCountsCustomWorkouts: false);
}
