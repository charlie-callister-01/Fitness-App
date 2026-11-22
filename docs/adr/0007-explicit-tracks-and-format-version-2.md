---
status: accepted
---

# Explicit Tracks, exchange format version 2, and a v1 upgrader

A Plan is a list of **Tracks**, each with its own cadence (weekly or daily), rules and optional Phases. Every Session belongs to one Track. The first Track in the list is the **main Track** and must be weekly; Custom workouts, the benchmark and Home's Today card attach to it. This **supersedes the 19 Nov 2026 decision** to keep an implicit main track inside the Plan's own fields. It refines [ADR 0001](0001-schema-first-with-permanent-ids-and-versioned-exchange-format.md) and [ADR 0005](0005-single-writer-quarantine-and-two-storage-versions.md).

1. **Explicit Tracks.** `Plan` keeps only plan-level fields (name, active flag, start date, goal, goal date, weight increment) plus `tracks` (required, at least one) and a flat `sessions` list. A weekly Track has a weekly target and the Streak fields; a daily Track has a day-done percentage; each has its own `phases`. `Session.trackId` is required. There is no `isMain` flag: position decides, and Tracks can be reordered, added and renamed but not removed, and their cadence never changes. Track IDs are permanent.
2. **Format version 2.** The Plan's shape breaks (fields move onto the Track), so `formatVersion` becomes 2. The v1 reader stays and its result goes through a small Core upgrader that wraps the old Plan's target, Streak fields and Phases, and all its Sessions, into one weekly Track named "Main" (ID `<planId>-main`). The importer keeps reading v1 Backups and Plan files.
3. **Stored data migrates once.** The storage layout is unchanged (the Plan record is stored whole). At open, a Core startup migration rewrites stored Plans into the Track shape and records record format version 2 (ADR 0005, point 3). It is deterministic, tested, and fails safe.
4. **Only the upgrader and the migration know the old shape.** Every rule, view and screen sees explicit Tracks.

## Considered Options

- **An implicit main track** (the Plan's own fields describe it; `tracks` optional; `formatVersion` stays 1). Rejected: the same idea would have two shapes (plan fields and a Track), every rule would need a "main or not" branch, a Session with no `trackId` is ambiguous, and the Plan editor would need a special Settings area for the main path.
- **A flag on a Track (`isMain`).** Rejected: two Tracks could claim it or none; position in the list needs no extra validation and gives Move up and Move down a clear meaning.
- **Several active Plans, one per path.** Rejected: it breaks "exactly one active Plan" and the Import rule that a first Plan becomes active.
- **No migration, ask the owner to re-import.** Rejected: the stored Plan would be unreadable, and ADR 0001 promises old data keeps reading.

## Consequences

- Every rule that was "per Plan" takes a Track. The Plan loses its weekly target and Streak fields, so a rule cannot use the wrong one by accident.
- This is the first real use of the format upgrade path ADR 0005 describes but that has never run. The migration needs a test with a real v1 stored dataset, and a failed upgrade starts the app as CannotRead rather than mixing versions.
- Two PRs: Core, exchange and migration first (screens keep working on the main Track), screens second.
- Unqualified Badge metrics (`streak`, `bestweek`, `sessions`) mean the main Track; `streak:<id>`, `days:<id>` and `bestweek:<id>` name a Track, so reordering Tracks changes what the unqualified ones measure.
- Backups written by this app from now on are version 2 and cannot be read by an older build (`unsupported_version`, which already says to update).
