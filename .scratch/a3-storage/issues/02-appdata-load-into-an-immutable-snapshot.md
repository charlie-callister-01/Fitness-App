# 02: `AppData`: load into an immutable snapshot

**What to build:** At startup the whole dataset is loaded through the A1 reader and validator into one immutable snapshot that A2's rules take as arguments. Bad records are set aside and reported instead of stopping the app. Spec: `docs/specs/a3-storage.md` ("Load-into-memory model", "First run"; stories 2, 7, 9, 16 to 18, 32, 33, 35, 38).

**Blocked by:** 01 (Storage interface, in-memory store and contract suite)

**Status:** ready-for-agent

- [ ] `AppData` loads all stored records, assembles them into one exchange document and reads and validates it with the existing A1 code, so stored records and Backups share one reader
- [ ] The snapshot is immutable and has every collection present (empty, never missing); default Settings when none are stored
- [ ] An empty, never-initialised store loads as an empty snapshot with a `FirstRun` state; nothing is seeded
- [ ] A record that fails reading or validation is left out of the snapshot but stays untouched in storage; the error path identifies the record; a load issue (kind, key where known, plain-language message) is returned in the startup result
- [ ] A document-level problem that cannot be tied to a record (for example an unsupported future format version) gives a clear "cannot read your data" state and writes nothing
- [ ] "Restart" is modelled by building a new `AppData` over the same store; data survives it exactly
- [ ] "Today" never comes from storage; anything time-related uses the injected clock
- [ ] Tests (through `AppData`'s public surface on the in-memory store) cover: first run, whole-dataset round trip across a restart, one bad record quarantined with the rest kept and the bad record still in storage, unsupported future version, snapshot shape (all collections present, default Settings)
- [ ] One integration test reuses the A1 whole-dataset builder, restarts, and runs a Streak, a Missed session and the Backup Nudge (with the stored last-Backup time) over the snapshot
