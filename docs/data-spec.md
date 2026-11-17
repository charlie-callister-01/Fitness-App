# Data spec

The written definition of the app's data and its one JSON exchange format (the Backup, the LLM analysis Export and the Plan/Guide Import are all this same document). The code in `Core` matches this document; a conformance test parses the example below with the real reader, so the two cannot drift. Terms follow [`../GLOSSARY.md`](../GLOSSARY.md).

**Status: final for stage A1.** Every entity is specified, and every JSON example below is parsed by the real reader in the test suite.

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
| `readMe` | object | no | The "how to read this" block. See below. |
| `exportedAt` | timestamp | no | When the document was written. |
| `exportedFrom` | string | no | App build that wrote it. |
| `exercises` | array of Exercise | no | |
| `settings` | Settings | no | |
| `plans` | array of Plan | no | Exactly one active when non-empty. See Plan. |
| `weighIns` | array of Weigh-in | no | |
| `breaks` | array of Break | no | |
| `badges` | array of Badge | no | |
| `guides` | array of Guide | no | |
| `workouts` | array of Workout | no | Each holds its Sets and Pain check. See Workout. |
| `weekSchedules` | array of Week schedule | no | One-week-only Session moves. Added without a version bump. See Week schedule. |

An absent collection means "not part of this document". That is how partial documents (Plan-only, Guides-only) work with no second format. A present-but-empty collection means "none".

### The `readMe` block

The writer puts a short `readMe` object straight after `formatVersion`, so an LLM or person handed the bare file understands it. It holds plain-text strings: `about` (points at this spec for the given `formatVersion`), `formatVersion` (what the version number means), `units` (kg, km, metres, seconds, date and timestamp formats), `ids` (the ID rules) and `absentFields` (absent versus null, absent collections). The reader accepts a `readMe` of any shape and ignores it: no warning, no error, and it is not kept in the model. Written content is the same in every document, so it never affects round-tripping.

### Versioning

`formatVersion` is a single integer, bumped on any breaking change; adding optional fields does not bump it. The reader reads `formatVersion` first and dispatches to that version's reader. Old readers are kept (or migrated N to N+1) so old Backups always import.

| Input | Result |
|---|---|
| missing | error `missing_field` at `formatVersion` |
| not a whole number | error `wrong_type` |
| 0 or negative | error `invalid_version` |
| newer than the app supports | error `unsupported_version`, message says to update the app |

Unknown extra fields in a known version are warnings (`unknown_field`, with the field's path), never errors; the document is still accepted.

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

## Plan, Phase and Session

`plans` is an array of Plans. Several can exist; **exactly one has `isActive: true`** whenever the array is non-empty (an empty array means "no Plans"). A Plan is self-contained: it carries its own weekly target, Streak rules and weight increment (the Settings values of the same names are app-wide defaults).

### Plan

| Field | Type | Required | Notes |
|---|---|---|---|
| `id` | string | yes | Unique among Plans (`duplicate_id`). |
| `name` | string | yes | Non-blank label. |
| `isActive` | boolean | yes | Exactly one Plan is active (`no_active_plan`, `multiple_active_plans`; the latter is reported at the second active Plan). |
| `startDate` | date | yes | Week 1 of the Plan starts in the week containing this date. |
| `goal` | string | no | For example "HYROX London Doubles". |
| `goalDate` | date | no | On or after `startDate` (`out_of_range`). |
| `weeklyTarget` | integer >= 1 | yes | Workouts planned per week. |
| `streakMinWorkoutsPerWeek` | integer >= 1 | yes | Streak rule. |
| `streakCountsCustomWorkouts` | boolean | yes | Streak rule. |
| `weightIncrementKg` | number > 0 | yes | Weight step for Suggested targets in this Plan. |
| `phases` | array of Phase | no | See below. |
| `sessions` | array of Session | no | The Plan's default Sessions. |

### Phase

A block of weeks. Weeks are numbered from 1 and both ends are inclusive.

| Field | Type | Required | Notes |
|---|---|---|---|
| `id` | string | yes | Unique among all Phases in the document. |
| `focus` | string | yes | For example "Foundation". |
| `startWeek` | integer >= 1 | yes | |
| `endWeek` | integer >= `startWeek` | yes | |
| `goals` | array of Phase goal | no | Targets that say when to move on early. |
| `sessionOverrides` | array of Override | no | Replaces a Session's Exercise rows while this Phase is in force. A Session with no Override uses the Plan default. |

Week ranges: when a Plan has Phases, listed in any order, they must start at week 1 and follow each other with no gap (`phase_gap`) and no overlap (`phase_overlap`). Both errors are reported at the `startWeek` of the Phase after the problem. A Plan with no Phases is allowed.

**Phase goal:** `exerciseId` (must exist), `weightKg` (number > 0), `reps` (integer >= 1): "this lift hitting this weight for these reps".

**Override:** `sessionId` (must be a Session of the same Plan, `unknown_session`; at most one Override per Session in a Phase) and `exercises` (rows, see below).

### Session

| Field | Type | Required | Notes |
|---|---|---|---|
| `id` | string | yes | Unique among all Sessions in the document. |
| `name` | string | yes | For example "A: Lower + core". |
| `defaultDay` | enum | yes | `monday` to `sunday`. |
| `type` | enum | yes | `strength`, `cardio`, `body` or `test`. |
| `benchmarkIntervalWeeks` | integer >= 1 | no | Makes this a scheduled benchmark Session (use with `type: test`): repeats every N weeks, pre-filled from Benchmark exercises. |
| `exercises` | array of row | no | The planned rows, in order. |

### Exercise row

Used by Sessions and Overrides. The array order is the row order.

| Field | Type | Required | Notes |
|---|---|---|---|
| `exerciseId` | string | yes | Must be in `exercises` (`unknown_exercise`). If the document has no `exercises` collection (a Plan-only document) the check is deferred to Import. |
| `sets` | integer >= 1 | yes | |
| `reps` | integer >= 1 | no | Rep target, or the low end of a range. Omit for rows with no rep target (a run). |
| `repsMax` | integer >= `reps` | no | High end of a rep range; needs `reps`. |
| `cue` | string | no | Short coaching cue. |

### Plan example

This block is also parsed and validated by the real reader in the test suite.

```json
{
  "formatVersion": 1,
  "exercises": [
    { "id": "goblet-squat", "name": "Goblet squat", "logKind": "load" },
    { "id": "farmers-carry", "name": "Farmers carry", "logKind": "carry" },
    { "id": "rdl", "name": "Romanian deadlift", "logKind": "load" }
  ],
  "plans": [
    {
      "id": "hyrox-2027",
      "name": "HYROX London",
      "isActive": true,
      "startDate": "2026-10-19",
      "goal": "HYROX London Doubles",
      "goalDate": "2027-04-24",
      "weeklyTarget": 5,
      "streakMinWorkoutsPerWeek": 4,
      "streakCountsCustomWorkouts": false,
      "weightIncrementKg": 2,
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
      ],
      "sessions": [
        {
          "id": "a-lower",
          "name": "A: Lower + core",
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
          "defaultDay": "saturday",
          "type": "test",
          "benchmarkIntervalWeeks": 4,
          "exercises": [{ "exerciseId": "goblet-squat", "sets": 1, "reps": 5 }]
        }
      ]
    }
  ]
}
```
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

## Workout

One real occurrence of training on a date. Its Sets and its Pain check live inside it, because they have no meaning outside it.

| Field | Type | Required | Notes |
|---|---|---|---|
| `id` | string | yes | Unique among Workouts. |
| `date` | date | yes | The calendar day trained. |
| `sessionId` | string | no | The Session it was made from. Absent for a Custom workout. A Session ID is unique across the whole document, so it is checked against every Plan's Sessions (`unknown_session`); when the document has no `plans` the check is left to Import. |
| `loggedAfterwards` | boolean | no | Default `false`. `true` for a back-dated, catch-up entry. |
| `startedAt`, `endedAt` | timestamp | no | Duration is worked out from them. `endedAt` before `startedAt` is `end_before_start`. |
| `planned` | array of Exercise row | no | Snapshot of the planned rows copied when the Workout was created, so editing the Plan later never changes history. Same shape as a Session's Exercise row (see Exercise row). Empty or absent for a Custom workout. It is stored, never recomputed from the Plan. |
| `entries` | array of entry | no | What was actually done, in order. |
| `painCheck` | Pain check | no | Absent means unanswered. |

### Entry

| Field | Type | Required | Notes |
|---|---|---|---|
| `exerciseId` | string | yes | Must be in `exercises` when the document has them. For a skipped entry it is the planned Exercise left undone; for a swapped entry it is the Exercise actually done. |
| `status` | enum | yes | `done`, `skipped` or `swapped`. |
| `skipReason` | string | no | Only on a `skipped` entry (else `invalid_value`). |
| `swappedFromExerciseId` | string | when `swapped` | The planned Exercise it replaced; required for `swapped` (`missing_field`) and not allowed otherwise. Must be in `exercises` when the document has them. |
| `rpe` | integer 1 to 10 | no | Rate of perceived exertion; outside 1 to 10 is `out_of_range`. |
| `sets` | array of Set | no | A `skipped` entry must have none (`invalid_value`). |

### Set

| Field | Type | Required | Notes |
|---|---|---|---|
| `id` | string | yes | Unique among all Sets in the document. |
| `weightKg` | number >= 0 | no | Negative is `out_of_range`; 0 is allowed (an unweighted set). |
| `reps` | integer >= 1 | no | 0 reps is `out_of_range`. |
| `distanceKm` | number > 0 | no | Cardio distance. |
| `distanceM` | number > 0 | no | Carry distance. Not allowed together with `distanceKm` (`invalid_value`). |
| `durationSeconds` | integer >= 1 | no | |
| `isMax` | boolean | no | Default `false`. A Max set (a 1 or 3 rep max attempt). |

Which fields a Set should have for an Exercise's log kind is behaviour (stage A2), not checked on read.

### Pain check

| Field | Type | Required | Notes |
|---|---|---|---|
| `id` | string | yes | Unique among Pain checks. |
| `status` | enum | yes | `none` (answered: no pain) or `reported`. A Workout with no `painCheck` is unanswered, which is different from `none`. |
| `areas` | array of body area | when `reported` | At least one (`missing_field`); not allowed for `none`. Body areas: `neck`, `shoulder`, `elbow`, `wrist`, `upperBack`, `lowerBack`, `hip`, `knee`, `ankle`, `foot`, `other`. |
| `severity` | enum | when `reported` | `mild`, `moderate` or `sharp`; not allowed for `none`. |
| `note` | string | no | |
| `exerciseId` | string | no | The Exercise it happened on. Must be in `exercises` when the document has them. |

### Workout example

This block is parsed and validated by the real reader in the test suite. It has no `plans`, so its `sessionId` is not resolved here.

```json
{
  "formatVersion": 1,
  "exercises": [
    { "id": "goblet-squat", "name": "Goblet squat", "logKind": "load" },
    { "id": "farmers-carry", "name": "Farmers carry", "logKind": "carry" },
    { "id": "5k-run", "name": "5 km run", "logKind": "time" },
    { "id": "rdl", "name": "Romanian deadlift", "logKind": "load" }
  ],
  "workouts": [
    {
      "id": "w-2026-11-16",
      "date": "2026-11-16",
      "sessionId": "a-lower",
      "loggedAfterwards": false,
      "startedAt": "2026-11-16T17:30:00+00:00",
      "endedAt": "2026-11-16T18:25:00+00:00",
      "planned": [
        { "exerciseId": "goblet-squat", "sets": 3, "reps": 8, "repsMax": 12, "cue": "Chest tall" },
        { "exerciseId": "farmers-carry", "sets": 3 },
        { "exerciseId": "rdl", "sets": 3, "reps": 8 }
      ],
      "entries": [
        {
          "exerciseId": "goblet-squat",
          "status": "done",
          "rpe": 8,
          "sets": [
            { "id": "s-1", "weightKg": 24, "reps": 10 },
            { "id": "s-2", "weightKg": 28, "reps": 3, "isMax": true }
          ]
        },
        { "exerciseId": "farmers-carry", "status": "skipped", "skipReason": "Grip was gone" },
        {
          "exerciseId": "5k-run",
          "status": "swapped",
          "swappedFromExerciseId": "rdl",
          "rpe": 6,
          "sets": [{ "id": "s-3", "distanceKm": 5, "durationSeconds": 1530 }]
        }
      ],
      "painCheck": {
        "id": "p-1",
        "status": "reported",
        "areas": ["knee", "lowerBack"],
        "severity": "moderate",
        "note": "Twinge on the way up",
        "exerciseId": "goblet-squat"
      }
    },
    {
      "id": "w-2026-11-10",
      "date": "2026-11-10",
      "loggedAfterwards": true,
      "entries": [
        {
          "exerciseId": "farmers-carry",
          "status": "done",
          "sets": [{ "id": "s-4", "weightKg": 32, "distanceM": 40, "durationSeconds": 60 }]
        }
      ],
      "painCheck": { "id": "p-2", "status": "none" }
    }
  ]
}
```

## Week schedule

The one-week-only moves of Sessions to other days. A Session not listed is on its default day, so the collection holds only exceptions. "Make this the new default" is a Plan edit (the Session's `defaultDay`), not a Week schedule.

| Field | Type | Required | Notes |
|---|---|---|---|
| `weekStart` | date | yes | The week's Monday (weeks run Monday to Sunday). Not a Monday is `invalid_value`. One Week schedule per week: a repeat is `duplicate_id`. |
| `moves` | array of move | no | Absent or empty means no moves that week. |

A move has `sessionId` (required; a Session ID from a Plan, `unknown_session` otherwise, and left to Import when the document has no `plans`) and `date` (required; between `weekStart` and the Sunday after it, else `out_of_range`). A Session can be moved once per week: a second move of it is `duplicate_id` at the second move's `sessionId`.

```json
{
  "formatVersion": 1,
  "weekSchedules": [
    {
      "weekStart": "2026-11-16",
      "moves": [
        { "sessionId": "a-lower", "date": "2026-11-18" },
        { "sessionId": "c-engine", "date": "2026-11-22" }
      ]
    },
    { "weekStart": "2026-11-23", "moves": [] }
  ]
}
```

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
| `unknown_exercise`, `unknown_session` | A reference (Plan row, Phase goal, Guide, Workout entry, planned snapshot row, Pain check) to an Exercise or Session ID that does not exist. |
| `no_active_plan`, `multiple_active_plans` | Plans present but not exactly one active. |
| `phase_gap`, `phase_overlap` | Phase week ranges leave a week uncovered or share a week. |
| `not_in_planned` | A skipped entry's `exerciseId`, or a swapped entry's `swappedFromExerciseId`, is not one of the Workout's `planned` rows. Only checked when `planned` is not empty. |
| `end_before_start` | A Break's `endDate` is before its `startDate`, or a Workout's `endedAt` is before its `startedAt`. |
| `invalid_version`, `unsupported_version` | See Versioning. |
| `unknown_field` | Warning only: an unrecognised field was ignored. |
