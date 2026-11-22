# 03: Stored-data migration to the Track shape (PR 1)

**Read first:** `.scratch/a4-5-tracks/HANDOFF.md`, `docs/adr/0005-single-writer-quarantine-and-two-storage-versions.md`, `docs/adr/0007-explicit-tracks-and-format-version-2.md`, `docs/specs/a4-5-tracks.md` ("Versioning and upgrade"), `docs/specs/a3-storage.md` (format migration), `src/Core/Storage/` (`AppData.Migration.cs`, `StartupReader`, `StoredRecords`, `AppData`).

**Parent:** spec `docs/specs/a4-5-tracks.md`, PR 1.

**What to build:** stored v1 data becomes v2 at startup, safely.
- The current format version is 2. `StartupReader` already assembles stored records with the version they were written in and reads them through the versioned reader (so stored v1 Plans go through the upgrader). `AppData.Migration` (`UpgradeIfOlderAsync`) already rewrites the stored records in the current format and records the new version. Make that real for this change: confirm each stored record kind reads correctly at version 1, the rewrite writes the Track shape for Plans and leaves other records unchanged, and the record format version in `meta` becomes 2.
- **Deterministic:** the same stored v1 data always gives the same stored v2 bytes (test by migrating twice from the same input and comparing, and by re-opening after migration: nothing is rewritten).
- **Fail safe:** a write failure during the migration leaves the stored data as it was and starts as `CannotRead` (already blocks commits); a quarantined record makes the upgrade fail rather than run (existing rule; keep it and test it with a quarantined v1 Plan).
- **Commits after migration** write version 2 records, and a v2 store never needs the upgrader.
- Tests use the in-memory store seeded with real v1 stored records (build them from the existing v1 Plan, Workout and Settings test data), and the `FaultInjectingDataStore` for the failure case.
- Add a short "stored data" paragraph to `docs/data-spec.md` or `docs/specs/a3-storage.md` notes only if the behaviour described there changed.

**Blocked by:** 02.

**Status:** ready-for-agent

- [ ] A stored v1 dataset (two Plans, one active, Workouts with `sessionId`, Week schedules, Settings) opens as v2: Plans have one Main Track each (distinct IDs), results of the weekly rules for the same Workouts equal the pre-migration results.
- [ ] Migrating twice from the same input gives byte-identical output; reopening a migrated store writes nothing.
- [ ] A failed write during migration leaves the stored records and version untouched and the app starts as `CannotRead`; a quarantined v1 Plan makes the migration fail rather than drop it.
- [ ] A v1 Backup restored into a v2 store ends up in the Track shape (Restore reads through the v1 reader).
- [ ] `dotnet build` 0 warnings; earlier tests pass.
