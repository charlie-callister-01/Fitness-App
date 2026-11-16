using Core.Model;
using Core.Serialisation;

namespace Core.Tests.Serialisation;

/// <summary>Builders for valid datasets; each invalid case is a valid dataset with one thing broken.</summary>
internal static class TestData
{
    public static Settings ValidSettings() => new(
        DisplayWeightUnit: WeightUnit.Kg,
        DisplayDistanceUnit: DistanceUnit.Km,
        WeightIncrementKg: 2.5,
        RestTimerSeconds: 90,
        GoalWeightKg: 82.5,
        StreakMinWorkoutsPerWeek: 3,
        StreakCountsCustomWorkouts: true);

    public static Exercise GobletSquat() => new(
        Id: "goblet-squat",
        Name: "Goblet squat",
        LogKind: LogKind.Load,
        Tags: new ExerciseTags(["quads", "glutes"], ["kettlebell"], ["squat"]),
        IsBenchmark: true);

    public static ExchangeDocument Minimal() => new(
        FormatVersion: ExchangeFormat.CurrentVersion,
        ExportedAt: new DateTimeOffset(2026, 11, 16, 9, 30, 0, TimeSpan.FromHours(1)),
        ExportedFrom: null,
        Exercises: [GobletSquat()],
        Settings: ValidSettings());

    public static string MinimalJson() => ExchangeFormat.Write(Minimal());
}
