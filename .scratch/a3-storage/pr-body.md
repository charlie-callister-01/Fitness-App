## Summary

Stage A3 gives the app a memory. Everything is saved the moment it changes, loaded whole into one immutable snapshot at startup (so the A2 rules run unchanged over it), and never lost silently. Spec: `docs/specs/a3-storage.md`; tickets: `.scratch/a3-storage/issues/01..09`.

```text
Core (no dependencies)                        Web host (browser only)
──────────────────────                        ───────────────────────
AppData  ── open ─► load all ─► StartupReader ─► DataSnapshot (immutable)
   │                    │          quarantines bad records, never deletes them
   │ commit             ▼
   ├─ validate (A1 validator, strict)
   ├─ IDataStore.Apply(batch)  ◄── atomic ──►  IndexedDbDataStore ─► indexeddb-store.js
   └─ publish snapshot + Changed                 one object store per kind + meta
                                                 (JSON text = Backup JSON, byte for byte)
IStorageEnvironment ◄───────────────────────  BrowserStorageEnvironment ─► storage-environment.js
   persist request · estimate · writer lock       navigator.storage / Web Locks / localStorage marker
   · cleared-data marker                        │
InMemoryDataStore  (tests, and fallback          Playwright: the SAME DataStoreContract cases
   when the browser gives no storage)            run against the real IndexedDB in Chromium
```

Startup states: `FirstRun`, `Ready`, `BrowserCleared`, `CannotRead`, `StorageFailed`. A commit is validated, saved, and only then published, so the screen always shows what is stored.

```diff
 src/
 ├── Core/
+│   └── Storage/        # IDataStore, InMemoryDataStore, AppData (load, commit, migrate, replace-all),
+│                       # per-kind RecordDescriptor, StartupReader, IStorageEnvironment, StorageHealth
 ├── Web/
+│   ├── Storage/        # IndexedDbDataStore, interop, BrowserStorageEnvironment, AddStorage()
+│   └── wwwroot/js/     # indexeddb-store.js, storage-environment.js
+├── Web.Tests/          # C# mapping tests against a fake interop
+└── Web.BrowserTests/   # Playwright (opt-in), shared contract suite in real Chromium
```

### Non-obvious decisions

- **Save first, then publish.** A failed save leaves the old snapshot and raises no event. Commits are serial and validated strictly with the A1 validator (two active Plans, unknown Exercise references and so on are rejected with field-level reasons and change nothing).
- **Bad records are quarantined, not fatal.** They are left out of the snapshot, kept untouched in storage, and reported. A commit that touches a quarantined record's key is rejected; only a Backup restore (replace-all) replaces them.
- **`BrowserCleared` = cleared-data marker present AND no records AND `Initialised` false.** `Initialised` lives inside IndexedDB, so if it survives the database was not wiped. An empty store that was initialised is `Ready` (you deleted everything, or restored an empty Backup).
- **One writer at a time.** The first window takes an exclusive Web Lock; a second window is read-only (`WriterLockHeld`). A browser without Web Locks is treated as a single window (can write) and says so in `StorageHealth`. A lock call that throws is "cannot tell" and stays read-only.
- **Two version numbers.** IndexedDB layout version (object stores; written to `meta` by the upgrade step) and the exchange `formatVersion` (how records are written). A format bump migrates at startup with one atomic rewrite; a newer-than-app version stops loading and writes nothing.
- **Replace-all works from any startup state**, including `CannotRead`, so a Backup restore (A4) is always available. It sets `Initialised` and the format version in the same atomic write.
- **Reconnects after a version change.** The module closes its connection on `versionchange`; the store reopens lazily and retries once.
- **Browser tests deviate from the spec.** There is no test page in the Web host. `Web.BrowserTests` runs the production `IndexedDbDataStore` on the test host with every JS call executed in headless Chromium, sharing the `DataStoreContract` cases (27 shared, none mirrored) plus 10 browser-only tests. Nothing test-only can ship.
- **`Settings.Default`** (kg, km, 2.5 kg step, 90 s rest, streak 3) was chosen in this stage because the spec was silent. It is now documented in `docs/data-spec.md` as owner-adjustable.
- New ADR `0005`: single writer, quarantine over fail, and the two version numbers.

### Known sharp edges (also in `docs/build-plan.md`)

- If the **only active Plan** is quarantined, startup lands in `CannotRead` (`no_active_plan`) and commits are blocked, so the only way out is a Backup restore.
- A **migration** that has to quarantine any record fails the whole migration (`CannotRead`) rather than rewriting and silently dropping it. Deliberate.
- After a restore, stamping the last-Backup time (`RecordBackupAsync`) is a separate, non-atomic write.
- The layout version is still 1, so no real `onupgradeneeded` upgrade has been exercised. The test only proves a v1 database opens and keeps its data.
- `Change.Put<T>` is generic, so a non-record argument fails at run time, not compile time.
- A throwing `Changed` handler is caught and traced, not surfaced.
- The new **browser-tests CI job has not yet run on GitHub**, so "fails when a contract case fails" is shown locally only.
- Not covered by CI, on the manual checklist in `docs/pwa-manual-checks.md`: quota-full, the real persistence grant, the browser clearing site data, the blocked upgrade, iOS Safari, the writer lock outside Chromium. No screens show storage messages yet (Track B); the checks point at `AppData.Startup` and `AppData.Health`.

## Evidence

- **Before:** `main` had A1 (format) and A2 (rules) but nothing that kept data between launches. 539 Core and 9 Ui tests.
  **After:** 648 Core, 32 Web, 9 Ui, plus 37 browser tests in headless Chromium. Build has 0 warnings.

```text
dotnet test                                   → Core 648, Web 32, Ui 9, all passed
dotnet test src/Web.BrowserTests -p:RunBrowserTests=true → 37 passed (27 shared contract cases, 10 browser-only)
```

Tests that pin the behaviour above, in pseudocode:

```text
save a Workout; open AppData again on the same store      → Workout is there, byte-identical JSON
store fails the write                                      → old snapshot stays, no Changed, StoreFailed
commit makes two active Plans                              → Rejected(no active plan / multiple_active_plans), nothing stored
one Plan record is corrupt                                 → Ready, other records load, 1 LoadIssue, bad record still in storage
commit touches that corrupt record's key                   → Rejected(record_set_aside)
marker present, store empty, Initialised false             → BrowserCleared;  Initialised true → Ready
two pages, same browser: second tryTakeWriterLock          → heldElsewhere; first still obtained
batch whose last op is bad (in Chromium)                   → whole batch rolled back, data and meta unchanged
close the DB connection, then save                         → store reopens and the save succeeds
```

Review: `/mattpocock-skills:code-review` (Standards and Spec) and the thermo-nuclear review were run, and their findings applied in three rounds (correctness, Core refactor, Web/JS refactor). Two findings are deliberately unfixed and listed above (strict migration, no layout upgrade test).

## Merge Danger

**Door:** two-way for the code; **one-way for the stored data shape once a real phone has data.**

Nothing yet ships a screen that writes data (no UI), and the new code is not exercised by any current page apart from startup. The stored record JSON is the A1 exchange format and the layout is version 1, so changing either later needs a migration (the mechanism exists and is tested with a synthetic version).

**Blast Radius:** app startup

`Program.cs` now opens storage before first render (`OpenStorageAsync`), so a bug there would block the whole app, including the PWA shell. If IndexedDB is unavailable, it falls back to in-memory instead. The service worker's asset list gains two JS files, so a stale cache could mix old and new versions until the update is picked up (see the PWA manual checks). CI gains a separate browser-tests job (not wired into deploy) that needs Chromium to be installable on the runner.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_016NV3mtKQQUTWbkJhd3xSWD
