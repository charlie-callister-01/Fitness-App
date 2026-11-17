# 03: `AppData`: commit (save-then-publish)

**What to build:** Every change goes through `AppData`: it validates the change, saves it, and only then shows it, so what is on screen is always what is stored, and a failed save is reported rather than swallowed. Spec: `docs/specs/a3-storage.md` ("Commit is save-then-publish", "Commits are validated", "Writes are serial"; stories 1, 3 to 6, 12, 13, 23, 34, 36, 37).

**Blocked by:** 02 (`AppData`: load into an immutable snapshot)

**Status:** ready-for-agent

- [ ] A commit takes a batch of typed puts and deletes across any record kinds; a single-record save is a batch of one
- [ ] The candidate snapshot is validated with the A1 validator before saving; errors reject the commit with the validator's field-level issues and change nothing, in memory or in storage
- [ ] On success the store is applied first, then the new snapshot is published and one change event is raised; on a store failure the old snapshot stays, no event fires and the caller gets the failure kind
- [ ] A multi-record commit is atomic (for example switching the active Plan deactivates the old one in the same batch)
- [ ] Commits run one at a time in call order; each sees the snapshot left by the previous one
- [ ] A snapshot held by a reader never changes after a later commit; only changed kinds get new collections and the rest are shared
- [ ] The last-Backup timestamp can be read and recorded through metadata (the caller supplies the time from the clock); it is not part of the exchange format
- [ ] Tests (in-memory store with the fault wrapper) cover: save then restart finds it, a failing store leaves the snapshot unchanged, invalid commit (two active Plans, unknown Exercise reference) rejected with exact path and code, atomic multi-record commit, event fires once per success and never on failure, reader-held snapshot stable, last-Backup timestamp round trip
