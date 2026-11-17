using System.Text.Json.Nodes;
using Core.Model;
using Core.Serialisation;

namespace Core.Tests.Serialisation;

/// <summary>A rich valid Plan dataset (overrides, Phase goals, benchmark Session) and a helper to break one thing in it.</summary>
internal static class PlanTestData
{
    public static Exercise Exercise(string id, LogKind kind = LogKind.Load) => new(id, id, kind, null, false);

    public static ExchangeDocument Rich() => new(
        FormatVersion: ExchangeFormat.CurrentVersion,
        ExportedAt: null,
        ExportedFrom: null,
        Exercises: [Exercise("goblet-squat"), Exercise("farmers-carry", LogKind.Carry), Exercise("5k-run", LogKind.Time), Exercise("rdl"), Exercise("waist", LogKind.BodyMeasurementCm)],
        Settings: null,
        Plans:
        [
            new Plan(
                Id: "hyrox-2027",
                Name: "HYROX London",
                IsActive: true,
                StartDate: new DateOnly(2026, 10, 19),
                Goal: "HYROX London Doubles",
                GoalDate: new DateOnly(2027, 4, 24),
                WeeklyTarget: 5,
                StreakMinWorkoutsPerWeek: 4,
                StreakCountsCustomWorkouts: false,
                WeightIncrementKg: 2,
                Phases:
                [
                    new Phase("foundation", "Foundation", 1, 8,
                        [new PhaseGoal("goblet-squat", 24, 10)],
                        []),
                    new Phase("build", "Build", 9, 19, [],
                        [new SessionOverride("a-lower", [new SessionExercise("rdl", 4, 6, null, "Hinge, don't squat")])]),
                    new Phase("hyrox-prep", "HYROX prep", 20, 27, [], []),
                ],
                Sessions:
                [
                    new Session("a-lower", "A: Lower + core", DayOfWeek.Monday, SessionType.Strength, null,
                    [
                        new SessionExercise("goblet-squat", 3, 8, 12, "Chest tall"),
                        new SessionExercise("farmers-carry", 3, null, null, null),
                    ]),
                    new Session("c-engine", "C: Engine", DayOfWeek.Wednesday, SessionType.Cardio, null,
                        [new SessionExercise("5k-run", 1, null, null, "Easy pace")]),
                    new Session("bench", "Benchmark test", DayOfWeek.Saturday, SessionType.Test, 4,
                        [new SessionExercise("goblet-squat", 1, 5, null, null)]),
                ]),
            new Plan("old-plan", "Old plan", false, new DateOnly(2026, 1, 5), null, null, 3, 3, true, 2.5, [], []),
        ]);

    public static string RichJson() => ExchangeFormat.Write(Rich());

    /// <summary>Returns the rich document's JSON after applying <paramref name="edit"/> to its parsed tree.</summary>
    public static string Broken(Action<JsonObject> edit)
    {
        var root = JsonNode.Parse(RichJson())!.AsObject();
        edit(root);
        return root.ToJsonString();
    }

    public static JsonObject Plan0(JsonObject root) => root["plans"]![0]!.AsObject();
}
