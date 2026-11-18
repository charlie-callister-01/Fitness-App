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

/// <summary>The look of the app. Keep is the warm default; Clean is the neutral alternative.</summary>
public enum Theme
{
    Keep,
    Clean,
}

/// <summary>Whether the app is dark or light. System follows the phone's colour scheme.</summary>
public enum Mode
{
    System,
    Dark,
    Light,
}

/// <summary>
/// App-wide settings. Display units, Theme and Mode affect presentation only; stored data is always canonical (kg, km, m).
/// Theme and Mode were added after the first format: optional in the exchange format, defaulting to Keep and System.
/// </summary>
public sealed record Settings(
    WeightUnit DisplayWeightUnit,
    DistanceUnit DisplayDistanceUnit,
    double WeightIncrementKg,
    int RestTimerSeconds,
    double? GoalWeightKg,
    int StreakMinWorkoutsPerWeek,
    bool StreakCountsCustomWorkouts,
    Theme Theme = Theme.Keep,
    Mode Mode = Mode.System)
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
