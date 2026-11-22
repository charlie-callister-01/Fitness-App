using System.Text;
using System.Text.Json;
using Core.Model;
using Core.Serialisation;

namespace Core.Exchange;

/// <summary>The parts of the prompts that describe the exchange format: field lists, enum lists, worked examples and the Exercise library.</summary>
public static partial class PromptPack
{
    /// <summary>
    /// The worked example in the Plan prompt (the two-Track Plan example from the data spec). Tests read it with the real reader and preview
    /// it as a Plan Import against a library holding <c>goblet-squat</c>, <c>farmers-carry</c> and <c>rdl</c>; <c>cat-cow</c> and
    /// <c>hip-flexor-stretch</c> are the new Exercises it adds.
    /// </summary>
    public static string PlanExample { get; } = Lf("""
        {
          "formatVersion": 2,
          "exercises": [
            { "id": "goblet-squat", "name": "Goblet squat", "logKind": "load" },
            { "id": "farmers-carry", "name": "Farmers carry", "logKind": "carry" },
            { "id": "rdl", "name": "Romanian deadlift", "logKind": "load" },
            { "id": "cat-cow", "name": "Cat-cow", "logKind": "tickOnly" },
            { "id": "hip-flexor-stretch", "name": "Hip flexor stretch", "logKind": "tickOnly" }
          ],
          "plans": [
            {
              "id": "hyrox-2027",
              "name": "HYROX London",
              "isActive": true,
              "startDate": "2026-10-19",
              "goal": "HYROX London Doubles",
              "goalDate": "2027-04-24",
              "weightIncrementKg": 2,
              "tracks": [
                {
                  "id": "main",
                  "name": "Main",
                  "cadence": "weekly",
                  "weeklyTarget": 5,
                  "streakMinWorkoutsPerWeek": 4,
                  "streakCountsCustomWorkouts": false,
                  "phases": [
                    {
                      "id": "foundation",
                      "focus": "Foundation",
                      "startWeek": 1,
                      "endWeek": 8,
                      "goals": [{ "exerciseId": "goblet-squat", "weightKg": 24, "reps": 10 }]
                    },
                    {
                      "id": "build",
                      "focus": "Build",
                      "startWeek": 9,
                      "endWeek": 19,
                      "sessionOverrides": [
                        {
                          "sessionId": "a-lower",
                          "exercises": [{ "exerciseId": "rdl", "sets": 4, "reps": 6, "cue": "Hinge, don't squat" }]
                        }
                      ]
                    }
                  ]
                },
                {
                  "id": "mobility",
                  "name": "Morning mobility",
                  "cadence": "daily",
                  "dayDonePercent": 75,
                  "phases": [
                    { "id": "mobility-habit", "focus": "Habit", "startWeek": 1, "endWeek": 4 }
                  ]
                }
              ],
              "sessions": [
                {
                  "id": "a-lower",
                  "name": "A: Lower + core",
                  "trackId": "main",
                  "defaultDay": "monday",
                  "type": "strength",
                  "exercises": [
                    { "exerciseId": "goblet-squat", "sets": 3, "reps": 8, "repsMax": 12, "cue": "Chest tall" },
                    { "exerciseId": "farmers-carry", "sets": 3 }
                  ]
                },
                {
                  "id": "benchmark",
                  "name": "Benchmark test",
                  "trackId": "main",
                  "defaultDay": "saturday",
                  "type": "test",
                  "benchmarkIntervalWeeks": 4,
                  "exercises": [{ "exerciseId": "goblet-squat", "sets": 1, "reps": 5 }]
                },
                {
                  "id": "m-mobility",
                  "name": "Morning mobility",
                  "trackId": "mobility",
                  "defaultDay": "daily",
                  "type": "mobility",
                  "exercises": [
                    { "exerciseId": "cat-cow", "sets": 1 },
                    { "exerciseId": "hip-flexor-stretch", "sets": 1, "cue": "30 seconds each side" }
                  ]
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
          "formatVersion": 2,
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
        $"The file is one JSON object. Always include `\"formatVersion\": {ExchangeFormat.CurrentVersion}`. Dates are yyyy-MM-dd. IDs are letters, digits, hyphens or underscores.",
        "- Root: `formatVersion`, `exercises` (optional), `plans` (one Plan).",
        $"- Exercise: `id`, `name`, `logKind` (one of: {EnumList<LogKind>()}), `isBenchmark` (optional, true or false),",
        "  `tags` (optional: `muscleGroups`, `equipment`, `movementPatterns`, each a list of words).",
        "- Plan: `id`, `name`, `isActive` (true), `startDate`, `goal` (optional), `goalDate` (optional, on or after `startDate`),",
        "  `weightIncrementKg` (more than 0), `tracks` (one or more; the first Track is the main Track and must be `weekly`), `sessions`.",
        $"- Track: `id`, `name`, `cadence` (one of: {EnumList<TrackCadence>()}), `phases` (optional: this Track's Phases).",
        "  For `weekly` Tracks: `weeklyTarget` (Workouts a week, 1 or more), `streakMinWorkoutsPerWeek` (1 or more), `streakCountsCustomWorkouts` (true or false).",
        "  For `daily` Tracks: `dayDonePercent` (1 to 100, default 75).",
        "- Phase: `id`, `focus`, `startWeek`, `endWeek` (weeks count from 1 Plan-wide, both ends included; each Track's Phases must start at week 1 and leave no gap or overlap),",
        "  `goals` (optional: `exerciseId`, `weightKg`, `reps`), `sessionOverrides` (optional: `sessionId` of a Session on the same Track, and `exercises` rows that replace that Session's rows in this Phase).",
        $"- Session: `id`, `name`, `trackId` (must match a Track of this Plan), `defaultDay` (one of: {DayList()}; `daily` is required on a `daily` Track and not allowed on a `weekly` Track),",
        $"  `type` (one of: {EnumList<SessionType>()}), `benchmarkIntervalWeeks` (optional, with type test on the main Track: repeats every N weeks), `exercises` (rows, in order).",
        "- Exercise row: `exerciseId`, `sets` (1 or more), `reps` (optional, the rep target or the low end of a range), `repsMax` (optional, the high end), `cue` (optional, a short coaching cue).",
        "Track, Session and Phase IDs must be unique in the file. Leave out any field that does not apply; do not write null.");

    private static string GuideFormatReference() => Lines(
        $"The file is one JSON object. Always include `\"formatVersion\": {ExchangeFormat.CurrentVersion}`, and a `guides` list.",
        "- Guide: `exerciseId` (required), then any of: `works`, `whyInPlan`, `setup`, `howTo`, `easier`, `harder` (each a short text),",
        "  `cues`, `commonMistakes`, `watchOuts` (each a list of short texts), `videoUrl` (a full http:// or https:// address).",
        "Leave out any field that does not apply; do not write null.");

    /// <summary>The camelCase names of an enum, as the exchange format writes them, in declaration order.</summary>
    private static string EnumList<T>() where T : struct, Enum => CamelCaseList(Enum.GetNames<T>());

    /// <summary>The default-day values as the format writes them, Monday first, then <c>daily</c>.</summary>
    private static string DayList() =>
        CamelCaseList(Enum.GetValues<DefaultDay>().OrderBy(d => d == DefaultDay.Daily ? 7 : ((int)d + 6) % 7).Select(d => d.ToString()));

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
