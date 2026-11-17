using System.Text.Json.Nodes;
using Core.Model;
using Core.Serialisation;

namespace Core.Tests.Serialisation;

/// <summary>A rich valid dataset with Workouts (planned, back-dated, Custom; every entry status and Pain check state).</summary>
internal static class WorkoutTestData
{
    public static readonly DateTimeOffset Start = new(2026, 11, 16, 17, 30, 0, TimeSpan.Zero);

    public static ExchangeDocument Rich() => PlanTestData.Rich() with
    {
        Workouts =
        [
            new Workout(
                Id: "w-1",
                Date: new DateOnly(2026, 11, 16),
                SessionId: "a-lower",
                LoggedAfterwards: false,
                StartedAt: Start,
                EndedAt: Start.AddMinutes(55),
                Planned:
                [
                    new SessionExercise("goblet-squat", 3, 8, 12, "Chest tall"),
                    new SessionExercise("farmers-carry", 3, null, null, null),
                    new SessionExercise("rdl", 3, 8, null, null),
                ],
                Entries:
                [
                    new WorkoutEntry("goblet-squat", EntryStatus.Done, null, null, 8,
                    [
                        new WorkoutSet("s-1", WeightKg: 24.5, Reps: 10, DistanceKm: null, DurationSeconds: null, DistanceM: null, IsMax: false),
                        new WorkoutSet("s-2", 28, 3, null, null, null, true),
                    ]),
                    new WorkoutEntry("farmers-carry", EntryStatus.Skipped, "Grip was gone", null, null, []),
                    new WorkoutEntry("5k-run", EntryStatus.Swapped, null, "rdl", 6,
                        [new WorkoutSet("s-3", null, null, 5, 1530, null, false)]),
                ],
                PainCheck: new PainCheck("p-1", PainStatus.Reported, [BodyArea.Knee, BodyArea.LowerBack], PainSeverity.Moderate, "Twinge on the way up", "goblet-squat")),
            new Workout("w-2", new DateOnly(2026, 11, 10), null, true, null, null, [],
                [
                    new WorkoutEntry("farmers-carry", EntryStatus.Done, null, null, null, [new WorkoutSet("s-4", 32, null, null, 60, 40, false)]),
                    new WorkoutEntry("waist", EntryStatus.Done, null, null, null, [new WorkoutSet("s-5", null, null, null, null, null, false, MeasurementCm: 86.5)]),
                ],
                new PainCheck("p-2", PainStatus.None, [], null, null, null)),
            new Workout("w-3", new DateOnly(2026, 11, 17), "c-engine", false, null, null,
                [new SessionExercise("5k-run", 1, null, null, "Easy pace")], [], null),
        ],
    };

    public static string RichJson() => ExchangeFormat.Write(Rich());

    public static string Broken(Action<JsonObject> edit)
    {
        var root = JsonNode.Parse(RichJson())!.AsObject();
        edit(root);
        return root.ToJsonString();
    }

    public static JsonObject Workout(JsonObject root, int i) => root["workouts"]![i]!.AsObject();

    public static JsonObject Entry(JsonObject root, int workout, int entry) => Workout(root, workout)["entries"]![entry]!.AsObject();

    public static JsonObject Set(JsonObject root, int workout, int entry, int set) => Entry(root, workout, entry)["sets"]![set]!.AsObject();
}
