# Data spec

The written definition of the app's data and its one JSON exchange format (the Backup, the LLM analysis Export and the Plan/Guide Import are all this same document). The code in `Core` matches this document; a conformance test parses the example below with the real reader, so the two cannot drift. Terms follow [`../GLOSSARY.md`](../GLOSSARY.md).

Stage A1 builds this spec up ticket by ticket. **Status: root document, Exercise, Settings, Weigh-in, Break, Badge and Guide are specified. Plans, Workouts and Pain checks are added by later A1 tickets.**

## Conventions

- **JSON style:** camelCase property names, string enums (camelCase values), ISO dates and timestamps. Output is compact for storage and indented for Export.
- **Canonical units:** weight in kg, cardio distance in km, carry distance in m, durations in seconds, body measurements in kg or cm. Every field's unit is part of its name (for example `weightIncrementKg`). Display units are a Setting only and never change stored data.
- **Dates and times:** a calendar date is `yyyy-MM-dd`. A timestamp is ISO 8601 with a time zone offset, like `2026-11-16T09:30:00+00:00` (`Z` is accepted).
- **IDs:** every record has a permanent, immutable string ID, unique within its record kind (Exercise IDs are unique among Exercises). Generated IDs are GUIDs in lowercase canonical form (`3f2504e0-4f89-11d3-9a0c-0305e82c3301`). Hand- or LLM-written files may use any non-empty string of letters, digits, hyphens and underscores (`^[A-Za-z0-9_-]+$`), such as `goblet-squat`. Names are only labels and may change freely.
- **Absent versus null:** a missing optional field and an explicit `null` mean the same thing. Unknown extra fields are ignored with a warning, never an error.
- **Errors:** reading never throws on bad data. Each problem is reported with a `path` (for example `exercises[0].name`, or `$` for the whole input), a machine `code` and a plain-language `message`.

## Root document

| Field | Type | Required | Notes |
|---|---|---|---|
| `formatVersion` | integer | yes | Starts at 1. See Versioning. |
| `exportedAt` | timestamp | no | When the document was written. |
| `exportedFrom` | string | no | App build that wrote it. |
| `exercises` | array of Exercise | no | |
| `settings` | Settings | no | |
| `weighIns` | array of Weigh-in | no | |
| `breaks` | array of Break | no | |
| `badges` | array of Badge | no | |
| `guides` | array of Guide | no | |

An absent collection means "not part of this document". That is how partial documents (Plan-only, Guides-only) work with no second format. A present-but-empty collection means "none".

### Versioning

`formatVersion` is a single integer, bumped on any breaking change; adding optional fields does not bump it. The reader reads `formatVersion` first and dispatches to that version's reader. Old readers are kept (or migrated N to N+1) so old Backups always import.

| Input | Result |
|---|---|
| missing | error `missing_field` at `formatVersion` |
| not a whole number | error `wrong_type` |
| 0 or negative | error `invalid_version` |
| newer than the app supports | error `unsupported_version`, message says to update the app |

## Exercise

An entry in the Exercise library.

| Field | Type | Required | Notes |
|---|---|---|---|
| `id` | string | yes | Permanent. See ID rules. Unique among Exercises (`duplicate_id`). |
| `name` | string | yes | Non-blank label; editable. |
| `logKind` | enum | yes | How it is measured. See Log kind. |
| `isBenchmark` | boolean | no | Default `false`. Marks a Benchmark exercise. |
| `tags` | object | no | `muscleGroups`, `equipment`, `movementPatterns`: each an optional array of non-blank strings. |

### Log kind

Defined here; the behaviour (input parsing, PRs, Suggested targets) belongs to stage A2.

| Value | Measured as | Unit |
|---|---|---|
| `load` | weight and reps | kg |
| `bodyweightLoad` | reps, optionally with added weight | kg |
| `carry` | weight and distance | kg, m |
| `reps` | repetitions only | none |
| `seconds` | a duration, higher is better | seconds |
| `minutes` | a duration, higher is better | seconds (shown as minutes) |
| `distanceKm` | distance | km |
| `distanceM` | distance | m |
| `time` | a duration, lower is better | seconds |
| `bodyMeasurementKg` | body measurement | kg |
| `bodyMeasurementCm` | body measurement | cm |
| `text` | a free text note | none |
| `tickOnly` | done or not done | none |

## Settings

One object holding app-wide settings, so a Backup restores everything.

| Field | Type | Required | Notes |
|---|---|---|---|
| `displayWeightUnit` | `kg` or `lb` | yes | Display only. |
| `displayDistanceUnit` | `km` or `mi` | yes | Display only. |
| `weightIncrementKg` | number > 0 | yes | Default weight step. A Plan may override it later. |
| `restTimerSeconds` | integer >= 0 | yes | Default rest timer. |
| `goalWeightKg` | number > 0 | no | Body-weight goal. |
| `streakMinWorkoutsPerWeek` | integer >= 1 | yes | Streak rule: Workouts needed in a week. |
| `streakCountsCustomWorkouts` | boolean | yes | Streak rule: whether Custom workouts count. |

## Weigh-in

A body-weight entry on any day, independent of any Workout (weight has exactly one home).

| Field | Type | Required | Notes |
|---|---|---|---|
| `id` | string | yes | Unique among Weigh-ins. |
| `date` | date | yes | `yyyy-MM-dd`. |
| `weightKg` | number > 0 | yes | Always kg; display unit is a Setting. |

## Break

A date range excused from the weekly target and Streak. Both ends are inclusive, so a one-day Break has equal dates.

| Field | Type | Required | Notes |
|---|---|---|---|
| `id` | string | yes | Unique among Breaks. |
| `startDate` | date | yes | |
| `endDate` | date | yes | Not before `startDate`, else `end_before_start` at `endDate`. |
| `reason` | string | yes | Non-blank label, such as "Holiday" or "Flu". |

## Badge

A user-editable achievement. Only its definition is stored; progress is always worked out from history, so there is no progress field.

| Field | Type | Required | Notes |
|---|---|---|---|
| `id` | string | yes | Unique among Badges. |
| `name` | string | yes | Non-blank. |
| `metric` | string | yes | Non-blank name of what is measured. The set of metrics and how progress is computed belong to stage A2, so A1 does not restrict the value. |
| `target` | number > 0 | yes | Value the metric must reach. |

## Guide

The reference page for one Exercise. A Guide has no ID of its own: it is keyed by `exerciseId`, so there is at most one per Exercise (`duplicate_id` at the second).

| Field | Type | Required | Notes |
|---|---|---|---|
| `exerciseId` | string | yes | The Exercise it describes. When the same document has `exercises`, it must be one of them (`unknown_exercise`). A Guides-only document has no Exercises to check against, so that check is left to Import (A4), which compares against the library. |
| `works`, `whyInPlan`, `setup`, `howTo`, `easier`, `harder` | string | no | Guide content. |
| `cues`, `commonMistakes`, `watchOuts` | array of non-blank strings | no | Guide content. |
| `videoUrl` | string | no | Absolute `http://` or `https://` URL, else `invalid_value`. |
| `personalNotes` | string | no | The owner's own notes. Not Guide content: an Import that replaces a Guide's content leaves existing personal notes alone, and a Guide written by an LLM normally omits this field. |

## Worked example

This block is parsed and validated by the real reader in the test suite.

```json
{
  "formatVersion": 1,
  "exportedAt": "2026-11-16T09:30:00+00:00",
  "exportedFrom": "fitness-app 0.1",
  "exercises": [
    {
      "id": "goblet-squat",
      "name": "Goblet squat",
      "logKind": "load",
      "isBenchmark": true,
      "tags": {
        "muscleGroups": ["quads", "glutes"],
        "equipment": ["kettlebell"],
        "movementPatterns": ["squat"]
      }
    },
    {
      "id": "farmers-carry",
      "name": "Farmers carry",
      "logKind": "carry"
    },
    {
      "id": "5k-run",
      "name": "5 km run",
      "logKind": "time"
    }
  ],
  "settings": {
    "displayWeightUnit": "kg",
    "displayDistanceUnit": "km",
    "weightIncrementKg": 2.5,
    "restTimerSeconds": 90,
    "goalWeightKg": 82.5,
    "streakMinWorkoutsPerWeek": 3,
    "streakCountsCustomWorkouts": true
  },
  "weighIns": [
    { "id": "w-2026-11-16", "date": "2026-11-16", "weightKg": 84.35 }
  ],
  "breaks": [
    { "id": "xmas", "startDate": "2026-12-20", "endDate": "2027-01-02", "reason": "Holiday" }
  ],
  "badges": [
    { "id": "squat-100", "name": "100 kg squat", "metric": "maxWeightKg", "target": 100 }
  ],
  "guides": [
    {
      "exerciseId": "goblet-squat",
      "works": "Quads and glutes",
      "whyInPlan": "Builds the squat strength HYROX wall balls and lunges need",
      "setup": "Hold the kettlebell at your chest, feet shoulder-width",
      "howTo": "Sit down between your heels, then stand tall",
      "cues": ["Chest up", "Knees out"],
      "commonMistakes": ["Heels lifting"],
      "easier": "Box squat",
      "harder": "Heavier kettlebell",
      "watchOuts": ["Lower back rounding"],
      "videoUrl": "https://example.com/goblet-squat",
      "personalNotes": "Left knee clicks at the bottom"
    }
  ]
}
```

## Error codes

| Code | Meaning |
|---|---|
| `empty_input`, `malformed_json` | Input is blank or not JSON (path `$`). |
| `missing_field` | A required field is absent or blank. |
| `wrong_type` | A field has the wrong JSON type. |
| `invalid_value`, `out_of_range` | The value is malformed or outside its allowed range. |
| `unknown_value` | An enum value that isn't one of the allowed values (the message lists them). |
| `invalid_id`, `duplicate_id` | ID has bad characters, or is reused within its record kind. |
| `end_before_start` | A Break's `endDate` is before its `startDate`. |
| `unknown_exercise` | A Guide's `exerciseId` isn't an Exercise in the same document. |
| `invalid_version`, `unsupported_version` | See Versioning. |
| `unknown_field` | Warning only: an unrecognised field was ignored. |
