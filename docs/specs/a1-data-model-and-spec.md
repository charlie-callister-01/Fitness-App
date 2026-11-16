---
labels: ready-for-agent
stage: A1
---

# A1: Data model and exchange format spec

## Problem Statement

The owner is replacing an Obsidian/Dataview system whose data overlapped (weight in two places, lifts in two places) and whose names doubled as IDs, so renaming an Exercise silently erased its history. Before any storage, rules, import/export or screens are built, the app needs one written, tested definition of what data exists, how records relate, how they are identified, and how they are written to and read from the single versioned JSON format that is the Backup, the LLM analysis Export and the Plan/Guide Import. Without it, every later stage (A2 rules, A3 storage, A4 export/import, A5 starter data, all UI) would invent its own shapes.

## Solution

Stage A1 delivers, entirely inside `Core` with no UI and no storage:

1. A **written data spec** (a Markdown document in `docs/`) that defines every entity, field, relationship, enum and invariant in the glossary's terms, plus the JSON format and its version scheme. The spec is the source of truth; the code matches it.
2. The **C# data model** in `Core` implementing that spec.
3. **JSON serialisation** (`System.Text.Json`) of a whole-dataset document, with a format version, in canonical units (kg, km, m).
4. **Validation** of a parsed document that returns plain-language, **field-level errors** naming the exact path (for example `plans[0].phases[1].sessions[2].exercises[0].exerciseId`), never throwing for bad user data.
5. A **versioned-format reader** skeleton: the reader dispatches on the format version, reads the current version, and rejects unknown/newer versions with a clear message. The mechanism for keeping old versions readable is in place so later version bumps only add a migration.

## User Stories

1. As the owner, I want every record (Exercise, Plan, Phase, Session, Workout, Set, Weigh-in, Pain check, Break, Badge, Guide) to have a permanent ID, so that renaming never loses history.
2. As the owner, I want Exercise names to be only editable labels, so that I can fix a typo or rename "Goblet squat" without touching any Workout.
3. As the owner, I want an Exercise to record how it is measured (its log kind), so that PRs, targets and input parsing can behave correctly per Exercise in later stages.
4. As the owner, I want Exercises to carry optional tags for muscle group, equipment and movement pattern, so that "similar Exercises" and week-rearrangement can use them later.
5. As the owner, I want several Plans with exactly one active, so that I can try a new programme without losing the old one.
6. As the owner, I want a Plan to hold its start date, goal and goal date, weekly target, Streak rules and weight increment, so that each Plan is self-contained.
7. As the owner, I want Phases inside a Plan with a week range, focus and optional Phase goals, so that the Plan can change emphasis over time.
8. As the owner, I want a Phase to optionally override a Session's Exercise list while otherwise inheriting the Plan default, so that HYROX prep can differ from Foundation without duplicating Sessions.
9. As the owner, I want Sessions with a name, default day of the week and type (strength, cardio, body, test), so that the week can be laid out.
10. As the owner, I want a benchmark Session to carry an interval in weeks and be pre-filled from Benchmark exercises, so that retests are scheduled automatically later.
11. As the owner, I want each Session Exercise row to have order, target sets, rep target or range, and a cue, so that the planned work is fully described.
12. As the owner, I want a Workout to be a record of one real occurrence on a date, linked to the Session it came from (or none for a Custom workout), so that history is accurate.
13. As the owner, I want every Workout to keep a snapshot of what was planned at the time, so that editing the Plan later never rewrites history.
14. As the owner, I want Workouts to record whether they were logged afterwards (back-dated), so that I can tell live logs from catch-up entries.
15. As the owner, I want a Workout to hold its Exercises with their Sets, RPE, and a status of done, skipped (with optional reason) or swapped (with the original recorded), so that Partial workouts and Swapped exercises are represented faithfully.
16. As the owner, I want a Set to hold weight, reps, distance, duration, and a Max set flag, so that load, carry, cardio, time and bodyweight efforts all fit the same row.
17. As the owner, I want Sets stored in canonical units (kg, km, m, seconds), so that the display-unit switcher never changes stored data.
18. As the owner, I want a Workout's start and end times stored so that duration can be calculated automatically.
19. As the owner, I want Weigh-ins stored independently of Workouts, with a date and weight in kg, so that weight has exactly one home.
20. As the owner, I want a Pain check to record answered-no, answered-yes (body areas, severity mild/moderate/sharp, optional note, optional Exercise) or unanswered, so that skipping is different from "no pain".
21. As the owner, I want Breaks as date ranges with a reason label, so that Streak and weekly target can later excuse them without moving Plan dates.
22. As the owner, I want Badges stored as name, metric and target only, so that progress is always computed from history and never stored.
23. As the owner, I want a Guide per Exercise with the structured fields (works, why in the Plan, setup, how to do it, cues, common mistakes, easier, harder, watch-outs, video URL) plus personal notes kept separate, so that imports can replace content without overwriting my notes.
24. As the owner, I want settings (display units, Streak rules, weight increment default, rest timer, goal weight) in the same document, so that a Backup restores everything.
25. As the owner, I want the whole dataset serialised to one JSON document with a `formatVersion`, so that the Backup, the LLM Export and the Import are the same thing.
26. As the owner, I want the JSON to be human- and LLM-readable (camelCase, ISO dates, string enums), so that I can paste it into a chat tool and an LLM can write valid data back.
27. As the owner, I want dates stored as plain ISO calendar dates and times as ISO timestamps with offset, so that "which week is this Workout in" never depends on the device time zone.
28. As the owner, I want a document that is partial (for example only a Plan, only Guides, or only some Workouts) to be representable in the same format, so that Plan-only and Guide-only Imports need no second format.
29. As the owner, I want bad data rejected with a message naming the exact field and what is wrong, so that I can fix an LLM-written file quickly.
30. As the owner, I want validation to catch dangling references (a Set for an unknown Exercise ID, a Workout pointing at a missing Session), so that corrupted data is caught before it is applied.
31. As the owner, I want validation to catch duplicate IDs, a missing or multiple active Plan, a Phase week range that overlaps or leaves a gap, and out-of-range values (negative weight, zero reps, RPE outside 1 to 10), so that rules in later stages can trust their input.
32. As the owner, I want a document with an unknown newer format version to be rejected with a message saying to update the app, so that I never half-read data I don't understand.
33. As the owner, I want unknown extra fields in a known version ignored with a warning rather than failing, so that a slightly chatty LLM output still imports.
34. As the owner, I want a serialise-then-read round trip of any valid dataset to produce an identical dataset, so that Backups are lossless.
35. As the next-stage developer, I want the model to be plain immutable-friendly C# records with no dependency on storage, UI or the clock, so that A2 to A4 can build on it.
36. As the next-stage developer, I want ID generation behind a small interface, so that tests can use deterministic IDs.

## Implementation Decisions

- **Everything lives in `Core`**, namespaced by area (model, serialisation, validation). No dependencies beyond the BCL. No UI, storage or clock use; `Ui` is untouched.
- **Written data spec first.** A Markdown data spec under `docs/` is written before code and is the reference the prompt pack (A4) will later embed. It defines each entity's fields (name, type, required/optional, units, constraints), relationships and the JSON shape with a worked example. Code and spec are kept in sync by tests (see Testing Decisions).
- **IDs:** every record has an immutable ID, a string. Generated IDs are GUIDs rendered in canonical lowercase form, produced via an injectable ID generator interface. IDs are opaque to rules. Hand- or LLM-authored files may use any non-empty string matching a safe pattern (letters, digits, hyphen, underscore), unique within the record kind, so starter data and LLM output can use readable IDs such as `goblet-squat`.
- **Entities** (glossary terms exactly): Exercise (ID, name, log kind, tags, benchmark flag), Guide (keyed by Exercise ID; structured fields, video URL, personal notes held separately), Plan (settings, Phases, Sessions), Phase (focus, week range, optional Phase goals, optional per-Session Exercise overrides), Session (name, default day, type, optional benchmark interval, Exercise rows), Workout (date, optional Session ID, planned snapshot, Exercise entries, start/end, logged-afterwards flag), Set, Weigh-in, Pain check (attached to a Workout), Break, Badge, Settings.
- **Log kind** is an enum covering the old system's kinds: load, bodyweight-load, carry, reps, seconds, minutes, distance (km or m), time (lower is better), body measurement (kg or cm), text, tick-only. The enum and its meaning are defined in A1; the behaviour (parsing, PRs, targets) is A2.
- **Canonical units:** weight kg, distance km for cardio and m for carries (each field's unit fixed by the spec and named in the field), durations in seconds, body measurements kg or cm. Display units are a Setting only.
- **Planned snapshot:** a Workout copies the planned Exercise rows (Exercise ID, target sets, reps, cue) at creation, so later Plan edits cannot change history. Swapped and skipped statuses reference the planned row.
- **Pain check tri-state:** unanswered, none, or reported (one or more body areas, severity, note, optional Exercise ID). Body areas are a fixed enum defined in the spec.
- **Time:** dates are `DateOnly`-style ISO `yyyy-MM-dd`; timestamps are ISO 8601 with offset. Weeks are ISO Monday to Sunday; week derivation is A2.
- **Exchange document:** a single root object with `formatVersion` (an integer, starting at 1), `exportedAt`, optional `exportedFrom` (app build), and optional top-level collections: exercises, guides, plans, workouts, weighIns, painChecks, breaks, badges, settings. Absent collection means "not part of this document", which is how partial documents (Plan-only, Guides-only) work. A short "how to read this" header block is included in the output.
- **Serialisation:** `System.Text.Json`, camelCase, string enums, indented option for Export, compact for storage. Source-generated serialisation context is preferred for trim-safety in WebAssembly.
- **Validation:** a pure function from a parsed document to a list of errors (path, code, plain-language message). Never throws on bad user data. Checks: required fields, ID uniqueness and format, referential integrity, exactly one active Plan when plans are present, Phase week ranges contiguous and non-overlapping within a Plan, value ranges (weight ≥ 0, reps ≥ 1, RPE 1 to 10, severity enum), and enum membership. Unknown JSON properties produce warnings, not errors.
- **Versioned reader:** a reader entry point takes JSON text and returns either a validated document or errors. It reads `formatVersion` first, dispatches to the version's reader, and rejects missing, non-integer, zero or newer-than-supported versions with specific messages. Version 1 is the only reader now; the structure makes later migrations a function from version N to N+1.
- **Open decisions settled in this spec:** version scheme is a single integer bumped on any breaking change; additive optional fields do not bump it. The one active Plan is a flag on the Plan, validated as exactly one.
- **Out-of-document concerns:** nothing about IndexedDB, in-memory stores, UI, rules, PR calculation or matching is in A1.

## Testing Decisions

- **One seam, the highest available:** the exchange-format boundary in `Core`. Tests feed JSON text in and assert on either the validated document or the list of field-level errors, and feed a dataset in and assert on the JSON text out and on round-trip equality. This covers model, serialisation, validation and versioned reading through one public entry point, with no testing of private helpers.
- A good test asserts external behaviour only: the exact error path and message for a bad field, that a valid document round-trips unchanged, that an unknown version is rejected. It does not assert on internal types or call order.
- **Test data:** a small builder for a valid minimal dataset plus a rich dataset exercising every entity, so each invalid case is "valid dataset with one thing broken".
- **Cases to cover:** every user story under validation (dangling references, duplicate IDs, bad ranges, no or multiple active Plan, Phase gaps/overlaps, unknown enum values, wrong types, malformed JSON, empty input); partial documents (Plan-only, Guides-only) accepted; unknown fields warn; version missing, zero, current, newer; round trip of a rich dataset including unicode names, comma and decimal numbers, back-dated Workouts, all three Pain check states, swapped and skipped Exercises, Max sets.
- A **spec-conformance test**: the example document embedded in the data spec is parsed and validated by the real reader, so the spec cannot drift from the code.
- Prior art: xUnit in `Core.Tests` as used for the injected clock in A0. No bUnit or Playwright here; there is no UI.

## Out of Scope

- Storage interface, in-memory store and IndexedDB (A3).
- All rules: PRs, Max set precedence, Suggested targets, input parsing, weeks and Streak, Missed/Partial status derivation, Badge progress, week-rearrangement, Nudges, unit conversion (A2).
- Export picker, Markdown rendering, Import kinds and previews, Exercise matching and near-match suggestions, automatic backup, prompt pack text (A4).
- The starting Plan, 27 Guides and weight history (A5).
- Any UI, screens, or `Ui` project change.
- A "user" concept, sync, accounts.

## Further Notes

- `gh` was not on the PATH when this spec was written (it is installed at `C:\Program Files\GitHub CLI\gh.exe`), so it is saved here rather than published to GitHub Issues. Tickets are in `.scratch/a1-data-model/issues/`.
- Seams assumption: a single seam (the exchange-format reader/writer in `Core`) was chosen without live confirmation because this ran as a background job. Tell the next session if you want a lower seam as well.
- Likely tickets for `/to-tickets`: (1) written data spec; (2) core entities and enums plus ID generator; (3) JSON serialisation and round trip; (4) validation of structure and ranges; (5) referential and Plan/Phase invariants; (6) versioned reader and spec-conformance test.
