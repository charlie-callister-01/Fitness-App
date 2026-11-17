---
labels: ready-for-agent
stage: A3
---

# A3: Storage

Parent docs: [`../technical-brief.md`](../technical-brief.md) ("Storage"), [`../adr/0003-local-first-indexeddb-storage-behind-an-interface.md`](../adr/0003-local-first-indexeddb-storage-behind-an-interface.md), [`../adr/0001-schema-first-with-permanent-ids-and-versioned-exchange-format.md`](../adr/0001-schema-first-with-permanent-ids-and-versioned-exchange-format.md), [`../data-spec.md`](../data-spec.md), [`../../GLOSSARY.md`](../../GLOSSARY.md). Builds on the A1 model and exchange format and the A2 rules.

## Problem Statement

The owner's training history is the whole value of the app, and today it lives nowhere: A1 can read and write a JSON document, and A2 can answer every rules question about one, but nothing keeps the data between launches. The owner will log Sets mid-workout on a phone, often with poor signal, so data must be saved the instant it is entered, survive the app being closed, and never be lost silently. The browser can also throw data away (storage cleared, quota full, the app not installed), and a second open tab can overwrite work. Until storage exists as a tested, UI-free seam, no screen can save anything, and A4 (Backup restore) has nothing to restore into.

## Solution

Stage A3 gives the app a memory, with a seam small enough to test almost entirely without a browser:

1. A **storage interface in `Core`** (`IDataStore`) that loads everything, saves or deletes single records, applies a batch of changes atomically, replaces the whole dataset atomically, and keeps a little device bookkeeping (such as the last Backup time).
2. An **in-memory implementation** in `Core`, used by every test and as the safe fallback.
3. An **IndexedDB implementation** in the Web host, behind a thin JavaScript module, that stores one document per record in one object store per record kind.
4. A **load-into-memory model** in `Core` (`AppData`): at startup it loads the whole dataset through the A1 reader and validator into an immutable snapshot, which A2's rules take as arguments. Every change goes through it: **save first, then publish the new snapshot**, so what the screen shows is always what is stored.
5. A **storage environment** interface (`IStorageEnvironment`) for the things only the browser knows: the persistent-storage request, free-space estimate, and "is another tab already open". It has a fake for tests.
6. **Honest failure handling.** A change that cannot be saved is reported, not swallowed; unreadable records are set aside and reported, not deleted; a browser that cleared the data is detected and the owner is pointed at restoring a Backup.

The owner sees this as: it just works and is instant; a plain-language message appears only when something has gone wrong, and says what to do.

## User Stories

### Saving and loading
1. As the owner, I want every change saved the moment I make it, so that closing the app or losing signal mid-workout never loses a Set.
2. As the owner, I want the app to open with all my data already there and no spinner longer than a moment, so that I can start logging at once.
3. As the owner, I want each change to save only the record that changed, so that logging a Set stays fast however long my history gets.
4. As the owner, I want a Workout and its Sets and Pain check to save together as one unit, so that I never end up with half a Workout.
5. As the owner, I want a change that touches several records (activating a different Plan deactivates the old one) to save all-or-nothing, so that the app never reaches a state where two Plans are active.
6. As the owner, I want a deleted record to be gone after a restart, so that deletes are real.
7. As the owner, I want my Settings, Exercises, Plans, Weigh-ins, Breaks, Badges, Guides, Workouts and Week schedules all stored, so that nothing I can edit is lost.
8. As the owner, I want the app to work fully offline, so that the gym's weak signal is irrelevant (storage is on the device, never a server).
9. As the owner, I want my data to be the same after a restart as it was before, down to every field, so that I can trust it.

### Safety and failure
10. As the owner, I want the app to ask the browser to keep my data permanently, so that the browser does not clear it when the phone is low on space.
11. As the owner, I want to be told plainly if the browser refused to keep my data permanently, with the advice to install the app and make Backups, so that I know the risk.
12. As the owner, I want a clear message if a change could not be saved (storage full), and for the change not to appear as saved, so that I never believe something is stored when it is not.
13. As the owner, I want a failed save to leave my on-screen entry intact so I can try again after freeing space, so that a bad moment does not cost me the Set I just typed.
14. As the owner, I want the app to notice when the browser has wiped my data (the app was used before but the database is empty) and to tell me, so that I do not mistake it for a fresh start and carry on.
15. As the owner, I want that message to offer restoring my latest Backup, so that recovery is one step away (the restore itself is built in A4).
16. As the owner, I want an unreadable or invalid stored record to be set aside and reported rather than crash the app, so that one bad record never locks me out of the rest.
17. As the owner, I want a record that was set aside to stay in storage untouched, so that it can still be recovered or fixed later.
18. As the owner, I want the app to tell me how many records could not be read and which kind they are, so that I know what is missing.
19. As the owner, I want the app to keep working, with a visible warning, if the browser will not give it storage at all (private browsing), so that I can still log while knowing nothing will be kept.
20. As the owner, I want a second open tab or window not to overwrite my work, so that two copies of the app never fight.
21. As the owner, I want the second tab to say "Fitness App is open in another window" and not edit, so that I know what to close.
22. As the owner, I want an app update that changes how data is stored to finish by itself, and to say what to close if another tab blocks it, so that updates never strand me.
23. As the owner, I want the date of my last Backup remembered on this device, so that the 14-day Backup Nudge works across restarts.
24. As the owner, I want to see how much space the app is using and how much is left, so that I can tell if I am near the limit.

### Replace-all (for restoring a Backup)
25. As the owner, I want restoring a Backup to replace everything in one step that either fully happens or not at all, so that a failed restore never leaves me with half of two datasets.
26. As the owner, I want the previous data left exactly as it was if a restore fails, so that a bad file cannot destroy what I have.
27. As a developer, I want a replace-all that takes a validated document, so that A4 can restore without writing any storage code.

### Format and upgrades
28. As the owner, I want my stored data to keep loading after the app is updated, even if the data format moved on, so that updates never cost me history.
29. As a developer, I want stored records read by the same versioned reader that reads Backups, so that old data upgrades through one tested path.
30. As a developer, I want the storage layout version separate from the exchange format version, so that changing one does not force a change to the other.
31. As a developer, I want a format-version bump to trigger a one-time migration at startup that rewrites records in the new version, so that old versions are not read forever.

### The in-memory model and rules
32. As a developer, I want the whole dataset exposed as one immutable snapshot, so that A2's rules run over it unchanged (they take data as arguments).
33. As a developer, I want a snapshot to never change under a reader, so that a screen rendering mid-save is never inconsistent.
34. As a developer, I want a change event after each successful commit, so that screens refresh without polling.
35. As a developer, I want every collection present (empty, not missing) in the snapshot, so that rules never have to handle "not part of this document".
36. As a developer, I want a commit to be validated against the A1 rules, and rejected with field-level reasons if it would break them (two active Plans, a Session row naming an unknown Exercise), so that the stored dataset is always valid.
37. As a developer, I want a rejected commit to change nothing, in memory or in storage, so that no partial state exists.
38. As a developer, I want "today" to come from the injected clock and never from storage, so that tests are deterministic.

### Testing and portability
39. As a developer, I want one in-memory implementation that behaves like IndexedDB (atomic batches, copies on read, same key rules), so that tests against it mean something.
40. As a developer, I want one contract test suite run against every implementation, so that the in-memory one and the IndexedDB one are provably the same.
41. As a developer, I want a way to make the in-memory store fail on demand (full, unavailable), so that failure handling is tested without a browser.
42. As a future MAUI developer, I want the interface free of any browser type, so that SQLite or a file store can implement it without touching `Core` or `Ui`.
43. As a developer, I want `Core` to stay dependency-free and the IndexedDB code to live only in the Web host, so that the architecture rules from earlier stages hold.

## Implementation Decisions

Items marked **(default)** were proposed and then confirmed by the owner (see Further Notes); each can still be changed in one place.

### Shape
- **Two small deep modules in `Core`, plus one browser-facing interface.** `IDataStore` (persistence, no rules), `AppData` (the in-memory model, commit, validation) and `IStorageEnvironment` (persist request, estimate, writer lock). The in-memory `IDataStore` and a fake `IStorageEnvironment` also live in `Core`, because the Web host's fallback and every test use them. The IndexedDB `IDataStore` and the real `IStorageEnvironment` live in the Web host with their JavaScript module. `Ui` gains nothing but the ability to inject `AppData` and read health state; it contains no storage rules.
- **One generic store, not per-kind repositories (default).** The store is addressed by a record kind and a key. The kinds are Exercise, Settings, Plan, Weigh-in, Break, Badge, Guide, Workout and Week schedule. Keys: the permanent ID, except Guide (its Exercise ID), Week schedule (its Monday date) and Settings (a fixed singleton key). A generic store keeps the interface small, makes the contract suite one suite, and makes a MAUI implementation trivial. Typed access is provided once, by `AppData`, not per kind in every implementation.
- **The unit of "save one record" is the top-level record in the exchange format (default).** A Workout is stored whole (its Sets, Entries, planned snapshot and Pain check inside it); a Plan is stored whole (its Phases and Sessions inside it). This matches what the Plan and Workout editors will save and keeps each save one atomic write. The cost, rewriting a whole Plan to change one Session, is a few kilobytes and is ignored.
- **Storage operations** (all asynchronous; none throws for an expected failure, they return a result that names the failure kind):
  - *Load all*: returns the raw stored records per kind plus the device metadata.
  - *Apply*: a batch of puts and deletes, applied as one atomic transaction. A single record save is a batch of one.
  - *Replace all*: atomically clears every kind and writes a whole validated document (for A4's restore), in one transaction.
  - *Metadata*: read and write of device bookkeeping (last Backup timestamp, the storage layout version, the stored record format version, an "initialised" marker). Metadata is not part of the exchange format and is never in a Backup; A4 calls the Backup-recorded operation after making or restoring a Backup.
- **Failure kinds** are a small closed set the UI can word: storage full, storage unavailable, another tab holds the writer lock, and unexpected (with detail for diagnostics). They are values, not exception types.

### Load-into-memory model (`AppData`)
- At startup `AppData` asks the store to load all, assembles the records into one exchange document, and runs it through the existing A1 reader and validator (the same code that reads a Backup). The result is an **immutable snapshot**: an `ExchangeDocument` with every collection present (empty rather than null) and Settings defaulted when none are stored.
- **Bad records are quarantined, not fatal (default).** If reading or validating reports errors, the errors' paths identify the record they came from; that record is left out of the snapshot but stays untouched in storage, and a load issue (kind, key where known, plain-language message) is returned in the startup result. The app starts with everything else. A document-level problem that cannot be tied to a record (for example an unsupported future format version) starts the app in a clear "cannot read your data" state with nothing written.
- **Commit is save-then-publish.** A commit takes a batch of changes (puts and deletes of typed records). `AppData` builds the next snapshot, validates it (below), asks the store to apply the batch, and only on success publishes the new snapshot and raises a change event. If the store fails, the old snapshot stays and the caller gets the failure kind. A commit never partially applies.
- **Commits are validated (default).** The candidate snapshot is run through the A1 validator before saving. Errors reject the commit with the validator's field-level issues and change nothing. This protects against two active Plans and dangling references being stored. Screens that edit a Plan are expected to hold a draft and commit when it is valid; storage does not store drafts.
- **Writes are serial.** Commits run one at a time in call order; a commit sees the snapshot left by the previous one. The snapshot is replaced by building new collection lists for only the changed kinds, sharing the rest.
- **No clock inside storage.** Timestamps stored in metadata come from the injected clock at the call site.

### Store layout and versioning
- **IndexedDB layout:** one object store per record kind, each keyed by the record's key and holding the record as JSON text in the exchange format's own serialisation; one `meta` store of key/value pairs. No secondary indexes: all queries run in memory in `Core`.
- **Two version numbers, deliberately separate.** The *layout version* is the IndexedDB database version (which object stores exist); changing it is an `onupgradeneeded` step in the JavaScript module. The *record format version* is the exchange `formatVersion` the records were written in, recorded in metadata. Records are always written in the current format version.
- **Format migration (default):** at startup, if the recorded record format version is older than the app's, `AppData` reads the stored records through the versioned reader (which still reads old versions), and a single replace-all rewrites them in the current version and updates the recorded version. Until the first format bump (version 1 now) this path has a test with a synthetic older version but no real migration.
- **An unsupported future format version** (the data was written by a newer app build, for example after an update was rolled back) stops loading with the A1 `unsupported_version` message and writes nothing.

### Persistent storage and storage health
- **When asked:** the persistent-storage request is made at startup on every launch until it is granted (default); the first launch is the first request. It is cheap and some browsers grant only after engagement or installation. The result is exposed, not hidden.
- **What is exposed:** a small storage health value: persistence granted, not granted, or unsupported; usage and quota estimate when available; whether the database is available at all; and whether this tab can write. The UI (later stages) decides how to show it; A3 guarantees the facts and their plain-language wording hooks exist.
- **If denied:** nothing is blocked. The health value says "not granted" so the UI can show the "install the app and make Backups" advice, and the Backup Nudge stays the safety net.
- **If storage is unavailable** (for example private browsing): the app starts on the in-memory store with health "unavailable", so the owner can still log and is plainly warned that nothing will be kept.

### Browser-cleared data
- On the first successful save the module also writes a small marker outside IndexedDB (browser local storage) and an "initialised" metadata record inside it (default). At startup, the marker present AND an empty database AND `Initialised` false means the browser cleared the data (the "initialised" record lives inside IndexedDB, so if it is still true the database was not wiped: an empty store is then simply `Ready`, for example after the owner deleted everything or restored an empty Backup). When the database is unavailable and the in-memory fallback is used, the marker is not consulted. `AppData` reports this as a distinct startup state (not "empty first run") so the UI can say so and offer restoring a Backup. If the marker itself was cleared too, it looks like a first run; that is unavoidable and is exactly what the Backup Nudge covers.

### Multiple tabs and blocked upgrades
- **One writer at a time (default).** The first tab takes an exclusive browser lock (Web Locks). A second tab starts read-only, its health says "another window is open", and it reports the "another window" failure kind on any commit. This avoids lost writes outright and is simple to reason about for one person on one phone.
- **Blocked upgrades:** the JavaScript module closes its connection when it receives a version-change event, so an update in one tab is never blocked by another idle tab; if an upgrade is blocked anyway, the startup result says so ("close other windows and reopen").

### First run
- An empty, never-initialised database loads as an empty snapshot with default Settings and `FirstRun` set. A3 does not seed any data (starter data is A5, delivered through Import).

## Testing Decisions

- **One seam:** the `IDataStore` contract. A single contract test suite (an abstract fixture) is run against every implementation: the in-memory one in the normal `Core.Tests` run, and the IndexedDB one in a real browser. `AppData`'s behaviour is tested through its public surface on top of the in-memory store, with a fault-injecting wrapper (full, unavailable, writer-lock-held). Nothing below that seam is tested directly, and no test touches the real clock.
- **What makes a good test:** a scenario in domain terms (save a Workout, restart, find it; apply a batch where the last put fails, find nothing changed; load with one corrupt Plan, find the app starts with the rest and one issue) asserting only what a caller can observe. No assertions on private helpers, call order or storage internals. "Restart" means building a new `AppData` over the same store instance.
- **Contract cases that must exist:** put then load round trips every kind exactly (Settings, a Guide keyed by Exercise ID, a Week schedule keyed by date, a Workout with Sets and a Pain check); put replaces by key; delete removes; deleting a missing key is not an error; a batch is all-or-nothing; replace-all is all-or-nothing and removes everything not in the new document; metadata round trips; values are copies (mutating a loaded record does not change storage); an empty store loads empty.
- **`AppData` cases:** first run is empty with defaults; startup quarantines one bad record and keeps the rest; a quarantined record is still in storage afterwards; an unsupported future format version starts nothing and writes nothing; commit is save-then-publish (a failing store leaves the snapshot unchanged and returns the failure kind); an invalid commit (two active Plans, unknown Exercise reference) is rejected with the validator's path and code and changes nothing; a multi-record commit is atomic; snapshots held by a reader do not change after a later commit; the change event fires once per successful commit and never on failure; browser-cleared detection (marker present, store empty); format migration from a synthetic older version rewrites and records the new version; "today" is only ever the injected clock.
- **The A2 integration check:** one test loads a whole-dataset fixture (reusing the A1 whole-dataset builder), restarts, and runs a representative A2 rule (Streak, Missed session, Backup Nudge with the stored last-Backup time) over the snapshot, proving the load model is a drop-in for the rules.
- **What cannot be tested without a browser, said plainly:** the real IndexedDB behaviour (transactions, `onupgradeneeded`, quota errors), the persistent-storage grant, Web Locks across two tabs, and the browser's clearing of data. bUnit cannot help: it has no real IndexedDB or browser storage. The default plan is to run the same contract suite against the IndexedDB implementation in headless Chromium through Playwright (one test page in the Web host that runs the suite and reports results), which covers layout, atomicity and upgrade steps. Quota, the persistence grant, two-tab locking and iOS Safari behaviour are covered by a short manual checklist added to the PWA manual checks. Safari/WebKit is not run in CI.
- **Prior art:** `Core.Tests/Serialisation` (builders, exact path-and-code assertions, the whole-dataset round trip) and `Core.Tests/Time` (the clock). The contract fixture follows the same fixture style; the fault-injecting wrapper is a small test helper.
- The suite stays fast and deterministic: no sleeping and no real clock; the Playwright run is a separate CI job so the normal test run is unaffected.

## Out of Scope

- Export, Import, Backup file creation and download, the automatic backup before an Import, and Exercise matching (A4). A3 supplies the replace-all and the last-Backup timestamp they will call.
- Starter data (the starting Plan, the 27 Guides, the weight history): A5.
- Any UI: the storage-health banner, the "data was cleared" screen and the "another window" screen are specified here by their facts and wording hooks only; their look is Track B.
- Sync, accounts, a backend, or cross-device anything.
- The MAUI storage implementation (the interface is kept clean for it).
- Encrypting data at rest, and storing drafts of unsaved edits.
- A change-history or undo log, and soft-delete/trash.
- Schema migrations for the exchange format beyond the mechanism and one synthetic-version test (the first real one arrives with the first format bump).

## Further Notes

- **Owner confirmed all defaults on 17 Nov 2026** ("go with your recommendations"), so every item marked **(default)** above is now a decision. The single-seam choice (the `IDataStore` contract) follows the "highest seam, ideally one" rule. Confirmed answers:
  1. **Two tabs:** the second tab is read-only with an explanation. No two-way sync.
  2. **Commit validation:** strict. A commit that breaks A1's rules is rejected with field-level reasons.
  3. **Playwright in CI:** yes. A separate CI job runs the contract suite against the IndexedDB implementation in headless Chromium.
  4. **Unreadable records:** quarantined and reported at startup. Whether to warn on every launch until resolved is a Track B UI decision; A3 only guarantees the load issues are available.
  5. **Persistence request:** asked on every launch until granted.
- **Housekeeping:** `docs/build-plan.md` and `docs/README.md` had a stale status (A1 listed as next); both are updated in the same commit as this spec. The A2 spec's nine assumptions still need checking against the old `view.js`, which is not yet in the repo.
- **Ordering inside the stage** (proposed for `/to-tickets`): the interface, the in-memory implementation and the contract suite first (no dependencies); then `AppData` load, quarantine and commit; then storage environment, health and the cleared-data detection; then the IndexedDB module and implementation; then the browser contract run and the manual checklist. The IndexedDB module and its browser test run are the likeliest to need splitting.
- Each ticket must fit one agent in roughly 150 to 200k tokens (see `CLAUDE.md`).
