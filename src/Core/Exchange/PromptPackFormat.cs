using System.Text;
using System.Text.Json;
using Core.Model;

namespace Core.Exchange;

/// <summary>The parts of the prompts that describe the exchange format: field lists, enum lists, worked examples and the Exercise library.</summary>
public static partial class PromptPack
{
    /// <summary>
    /// The worked example in the Plan prompt. Tests read it with the real reader and preview it as a Plan Import against a library holding
    /// <c>goblet-squat</c>, <c>farmers-carry</c> and <c>rdl</c>; <c>kb-swing</c> is the new Exercise it adds.
    /// </summary>
    public static string PlanExample { get; } = Lf("""
        {
          "formatVersion": 1,
          "exercises": [
            { "id": "goblet-squat", "name": "Goblet squat", "logKind": "load" },
            { "id": "farmers-carry", "name": "Farmers carry", "logKind": "carry" },
            { "id": "rdl", "name": "Romanian deadlift", "logKind": "load" },
            { "id": "kb-swing", "name": "Kettlebell swing", "logKind": "load" }
          ],
          "plans": [
            {
              "id": "example-plan",
              "name": "Example Plan",
              "isActive": true,
              "startDate": "2026-12-07",
              "goal": "Get stronger",
              "goalDate": "2027-03-01",
              "weeklyTarget": 3,
              "streakMinWorkoutsPerWeek": 3,
              "streakCountsCustomWorkouts": false,
              "weightIncrementKg": 2,
              "phases": [
                {
                  "id": "foundation",
                  "focus": "Foundation",
                  "startWeek": 1,
                  "endWeek": 4,
                  "goals": [{ "exerciseId": "goblet-squat", "weightKg": 24, "reps": 10 }]
                },
                {
                  "id": "build",
                  "focus": "Build",
                  "startWeek": 5,
                  "endWeek": 12,
                  "sessionOverrides": [
                    {
                      "sessionId": "a-lower",
                      "exercises": [{ "exerciseId": "rdl", "sets": 4, "reps": 6, "cue": "Hinge, don't squat" }]
                    }
                  ]
                }
              ],
              "sessions": [
                {
                  "id": "a-lower",
                  "name": "A: Lower",
                  "defaultDay": "monday",
                  "type": "strength",
                  "exercises": [
                    { "exerciseId": "goblet-squat", "sets": 3, "reps": 8, "repsMax": 12, "cue": "Chest tall" },
                    { "exerciseId": "kb-swing", "sets": 3, "reps": 15 },
                    { "exerciseId": "farmers-carry", "sets": 3 }
                  ]
                },
                {
                  "id": "b-benchmark",
                  "name": "B: Benchmark test",
                  "defaultDay": "saturday",
                  "type": "test",
                  "benchmarkIntervalWeeks": 4,
                  "exercises": [{ "exerciseId": "goblet-squat", "sets": 1, "reps": 5 }]
                }
              ]
            }
          ]
        }
        """);

    /// <summary>
    /// The worked example in the Guides prompt. Tests read it with the real reader and preview it as a Guides Import against a library
    /// holding <c>goblet-squat</c> and <c>rdl</c>.
    /// </summary>
    public static string GuidesExample { get; } = Lf("""
        {
          "formatVersion": 1,
          "guides": [
            {
              "exerciseId": "goblet-squat",
              "works": "Quads and glutes",
              "whyInPlan": "Builds the leg strength the wall balls and lunges need",
              "setup": "Hold the kettlebell at your chest, feet shoulder-width apart",
              "howTo": "Sit down between your heels, then stand tall",
              "cues": ["Chest up", "Knees out"],
              "commonMistakes": ["Heels lifting"],
              "easier": "Box squat",
              "harder": "Heavier kettlebell",
              "watchOuts": ["Lower back rounding"],
              "videoUrl": "https://example.com/goblet-squat"
            },
            {
              "exerciseId": "rdl",
              "works": "Hamstrings and glutes",
              "howTo": "Push your hips back with soft knees, then drive them forward",
              "cues": ["Hinge, don't squat"]
            }
          ]
        }
        """);

    private static string PlanFormatReference() => Lines(
        "The file is one JSON object. Always include `\"formatVersion\": 1`. Dates are yyyy-MM-dd. IDs are letters, digits, hyphens or underscores.",
        "- Root: `formatVersion`, `exercises` (optional), `plans` (one Plan).",
        $"- Exercise: `id`, `name`, `logKind` (one of: {EnumList<LogKind>()}), `isBenchmark` (optional, true or false),",
        "  `tags` (optional: `muscleGroups`, `equipment`, `movementPatterns`, each a list of words).",
        "- Plan: `id`, `name`, `isActive` (true), `startDate`, `goal` (optional), `goalDate` (optional, on or after `startDate`), `weeklyTarget` (Workouts a week, 1 or more),",
        "  `streakMinWorkoutsPerWeek` (1 or more), `streakCountsCustomWorkouts` (true or false), `weightIncrementKg` (more than 0), `phases` (optional), `sessions`.",
        "- Phase: `id`, `focus`, `startWeek`, `endWeek` (weeks count from 1, both ends included; Phases must start at week 1 and leave no gap or overlap),",
        "  `goals` (optional: `exerciseId`, `weightKg`, `reps`), `sessionOverrides` (optional: `sessionId` of a Session in this Plan, and `exercises` rows that replace that Session's rows in this Phase).",
        $"- Session: `id`, `name`, `defaultDay` (one of: {DayList()}), `type` (one of: {EnumList<SessionType>()}),",
        "  `benchmarkIntervalWeeks` (optional, with type test: repeats every N weeks), `exercises` (rows, in order).",
        "- Exercise row: `exerciseId`, `sets` (1 or more), `reps` (optional, the rep target or the low end of a range), `repsMax` (optional, the high end), `cue` (optional, a short coaching cue).",
        "Session and Phase IDs must be unique in the file. Leave out any field that does not apply; do not write null.");

    private static string GuideFormatReference() => Lines(
        "The file is one JSON object. Always include `\"formatVersion\": 1`, and a `guides` list.",
        "- Guide: `exerciseId` (required), then any of: `works`, `whyInPlan`, `setup`, `howTo`, `easier`, `harder` (each a short text),",
        "  `cues`, `commonMistakes`, `watchOuts` (each a list of short texts), `videoUrl` (a full http:// or https:// address).",
        "Leave out any field that does not apply; do not write null.");

    /// <summary>The camelCase names of an enum, as the exchange format writes them, in declaration order.</summary>
    private static string EnumList<T>() where T : struct, Enum => CamelCaseList(Enum.GetNames<T>());

    /// <summary>The days of the week as the format writes them, Monday first (<see cref="DayOfWeek"/> itself starts on Sunday).</summary>
    private static string DayList() =>
        CamelCaseList(Enum.GetValues<DayOfWeek>().OrderBy(d => ((int)d + 6) % 7).Select(d => d.ToString()));

    private static string CamelCaseList(IEnumerable<string> names) =>
        string.Join(", ", names.Select(name => JsonNamingPolicy.CamelCase.ConvertName(name)));

    /// <summary>The Exercises as compact JSON, one per line, inside a JSON array.</summary>
    private static string LibraryJson(IEnumerable<Exercise> exercises) =>
        "[\n" + string.Join(",\n", exercises.Select(e => "  " + ExerciseJson(e))) + "\n]";

    private static string ExerciseJson(Exercise exercise)
    {
        using var stream = new MemoryStream();
        using (var json = new Utf8JsonWriter(stream))
        {
            json.WriteStartObject();
            json.WriteString("id", exercise.Id);
            json.WriteString("name", exercise.Name);
            json.WriteString("logKind", JsonNamingPolicy.CamelCase.ConvertName(exercise.LogKind.ToString()));
            WriteTags(json, exercise.Tags);
            json.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteTags(Utf8JsonWriter json, ExerciseTags? tags)
    {
        if (tags is null || tags.MuscleGroups.Count + tags.Equipment.Count + tags.MovementPatterns.Count == 0)
        {
            return;
        }

        json.WriteStartObject("tags");
        WriteTagList(json, "muscleGroups", tags.MuscleGroups);
        WriteTagList(json, "equipment", tags.Equipment);
        WriteTagList(json, "movementPatterns", tags.MovementPatterns);
        json.WriteEndObject();
    }

    private static void WriteTagList(Utf8JsonWriter json, string name, IReadOnlyList<string> values)
    {
        if (values.Count == 0)
        {
            return;
        }

        json.WriteStartArray(name);
        foreach (var value in values)
        {
            json.WriteStringValue(value);
        }

        json.WriteEndArray();
    }

    private static string Lf(string text) => text.Replace("\r\n", "\n");
}
