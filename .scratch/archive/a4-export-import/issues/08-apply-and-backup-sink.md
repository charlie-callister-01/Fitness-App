# 08: Apply an Import with an automatic Backup

**What to build:** Spec "Applying". `IBackupSink`/`BackupFile`, a recording fake, `ImportApplier.ApplyAsync`: refuse not-ready or stale previews, build and deliver the pre-Import Backup (skipped only when no readable data), stamp it, then commit (Restore via replace-all, others as one atomic batch); outcomes `Applied`, `Rejected`, `StoreFailed`, `Blocked`, `PreviewOutdated`, `BackupNotSaved`. Include the whole-loop test: export a Backup, wipe the store, restore, snapshot equals original.

**Blocked by:** 01, 06, 07.

**Status:** done

- [ ] Every Apply case and the whole-loop test in the spec's Testing Decisions pass
- [ ] Importing the same file twice gives "nothing to change"
