# 07: Restore preview and atomic restore stamp

**What to build:** Spec "Restore a Backup" and "Small A3 addition". Add `AppData.ReplaceAllAsync(document, lastBackupAt)` writing the Backup date in the same atomic call (extend the store contract suite and in-memory store; the IndexedDB module/store only if the signature needs it, keep browser tests green). Restore preview: `not_a_backup` for partial files (required: all collections and settings except `weekSchedules`), what will be lost (counts, Workouts newer than `exportedAt`, set-aside records), stamp = file `exportedAt` else now.

**Blocked by:** 03, 05.

**Status:** done

- [ ] Restore cases from the spec's Testing Decisions pass, including from a `CannotRead` start
- [ ] A3's tests still pass; the atomic stamp is covered by a contract test
