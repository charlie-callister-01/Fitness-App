# Build plan and workflow

## Workflow (every stage; also in `../CLAUDE.md`)

1. **`/to-spec`**: write the spec for the stage.
2. **`/to-tickets`**: break the spec into tickets. Each ticket must fit one agent in roughly **150-200k tokens** of context (reading, writing, tests and fixes). Split anything bigger.
3. **Implement** the tickets (test-first; each ticket includes its tests).
4. **`/mattpocock-skills:code-review`**: review of the stage's changes. Always this skill, not the built-in `/code-review`.
5. **`/thermo-nuclear-code-quality-review`**: strict maintainability review (`../.claude/skills/thermo-nuclear-code-quality-review/SKILL.md`). User-invoked only; if it can't be run as a skill, read the file and apply it. Its rules are TypeScript-flavoured; apply the same intent to C# (nullable abuse, `object`, casts, files over 1k lines, ad-hoc branching).
6. **Apply the fixes** from both reviews.
7. **`/pr`**: write the PR body with extra explanation of what changed and why, and any non-obvious decisions.

Don't skip or merge steps, and don't start the next stage until the current stage's PR is written. Issues live in GitHub Issues (`gh` CLI; see `agents/issue-tracker.md`). Labels: needs-triage, needs-info, ready-for-agent, ready-for-human, wontfix.

## Two tracks

**Track A: no UI (can start immediately).** Data spec and core logic, storage, export/import, starter data.

**Track B: UX and design (owner-led).** Give [`design-brief.md`](design-brief.md) to the design agent; it lists requirements, constraints and exactly what to return (a `design-document.md` plus an `assets/` folder). When it comes back, put both under `docs/design/` and add them to this plan. The owner will produce a design document with Claude Design, covering screens, key flows (log a Workout, make up a missed Session, import a Plan, weekly review), a clickable prototype of the log screen to try on a phone, and the UI library/styling choice. It will be added to the build plan when ready. **No UI stage starts until Track B is finished.**

## Proposed stages (each gets its own `/to-spec`; boundaries are a starting proposal)

### Track A
- **A0: Scaffold and delivery. DONE (PR #7 merged, 16 Nov 2026).** The four-project solution, .NET LTS confirmed, PWA shell that installs and works offline, CI (build and test on PR), GitHub Pages deploy. The live app is at https://charlie-callister-01.github.io/Fitness-App/.
- **A1: Data model and spec. DONE (merged).** Entities and relationships (Plan, Phase, Session, Exercise, Workout, Set, Weigh-in, Pain check, Break, Badge, Guide, settings), permanent IDs, tags, canonical units, format version, JSON serialisation, validation. Written spec first.
- **A2: Core rules. DONE (PR #12 merged).** Log kinds and input parsing, PRs and Max sets, Suggested target with RPE, weeks/Streak/consistency, Missed/Partial/Skipped/Swapped handling, Badges, benchmark logic, unit conversion, week-rearrangement suggestions, Nudge rules.
- **A3: Storage. IMPLEMENTED, reviewed and fixed; draft PR #14 open.** Spec: [`specs/a3-storage.md`](specs/a3-storage.md). Delivered: `IDataStore` and the in-memory store, `AppData` (load into an immutable snapshot, save-then-publish commits, quarantine, format migration, replace-all), `IStorageEnvironment` and storage health, the IndexedDB module and store in `Web`, startup wiring, and the Playwright contract run (`src/Web.BrowserTests`, own CI job). Design: technical brief "Storage" and [ADR 0005](adr/0005-single-writer-quarantine-and-two-storage-versions.md); manual checks: [`pwa-manual-checks.md`](pwa-manual-checks.md).
- **A4: Export, Import and prompt pack. IMPLEMENTED, reviewed and fixed; draft PR open** (spec: [`specs/a4-export-import.md`](specs/a4-export-import.md); built with the spec's recommended defaults, which the owner still has to confirm). Export picker logic (JSON and Markdown), Import for Backup / Plan / merge / Guides with previews, automatic backup, Exercise matching, error messages, versioned format reading, prompt pack text.
- **A5: Starter data.** The starting Plan converted from `Config.md`, the 27 Guides, and the weight history, all delivered through Import.

### Track B-dependent (after the design document)
Likely stages, to be re-cut once the design exists: Workout log and home dashboard; Guide pages and Exercise library; Plan editor and week view; Progress, Benchmarks, Trophies and weight section; Weekly review and Nudges; Export/Import/Backup screens and Settings; install prompt, Wake Lock, polish.

## Inputs still needed from the owner
- `Fitness/Config.md` (current plan, kinds, badges).
- `Fitness/_scripts/fitness/view.js` and `view.css` (all logic to port; the reference for rules).
- The 27 files in `Fitness/Exercises/*.md` (starter Guides).
- Weight history from the old daily notes (`session::`/`weight` data from 1 Feb 2026) and `Plan/Tracking/Weight.md` / `Homepage.md` logic.
- `Fitness/How to use.md` (current user flow, out of date).
- The Claude Design document (Track B).

None of these block A0 to A4.

## Status and next step
**A0 is merged and deployed** (spec #1, tickets #2 to #6, PR #7; deploy fix PR #9). GitHub Pages source is set to "GitHub Actions" and the Deploy run succeeds. Still to confirm by hand, because they need a phone or a throwaway PR: install, offline open and update pickup (see `pwa-manual-checks.md`); that a deep link restores in a real browser; that CI fails on a deliberately failing test; and the `lan` run profile from a phone.

**A1 and A2 are merged** (539 Core and 9 Ui tests green). **A3 (Storage) is implemented** (all nine tickets). A3's reviews and fixes are done and its draft PR (#14) is open. **A4 (Export, Import and prompt pack) is implemented and reviewed** (draft PR open, based on A3's branch). Next stage: A5 (Starter data), once A4's PR is written and the owner has confirmed the A4 defaults. Read `docs/README.md`, `CLAUDE.md` and `GLOSSARY.md` first. The storage manual checks (quota-full, persistence grant, cleared data, blocked upgrade, iOS Safari) still need doing by hand on a phone; see `pwa-manual-checks.md`.

## Known sharp edges from A3 (for the owner)
- **Only active Plan quarantined.** If the only active Plan cannot be read it is set aside, the snapshot then has a dangling reference, and the app starts as "cannot read" (`no_active_plan`), which blocks every commit. The owner can then only restore a Backup. Consider a gentler recovery path when the Plan screens are designed.
- **Backup restore is two writes.** After a restore, `RecordBackupAsync` (the last-Backup time) is a separate, non-atomic write from the replace-all. A failure between them leaves the data restored but the Backup Nudge date stale. A4 should handle or surface it.
- **Web Locks missing means "single window".** A browser without Web Locks gets the writer lock automatically, so two windows could write there. Accepted: all current target browsers have it, but it is untested outside Chromium.
- **Browser tests do not run WASM.** They run the C# store on the test host and the JavaScript in headless Chromium. The Blazor interop layer in a real WebAssembly runtime is checked only by hand.
- **Layout version is still 1.** No real layout or record-format upgrade has been exercised, only a synthetic older version in tests and the manual rehearsal in `pwa-manual-checks.md`.
- **The browser-tests CI job has not yet run on GitHub.** Expect to fix workflow details on its first run.

## Open items to settle during specs
- ~~The exact .NET LTS version and Blazor template details.~~ Settled: .NET 10 LTS, standalone Blazor WebAssembly PWA template ([ADR 0004](adr/0004-dotnet-10-lts-pinned.md)).
- UI library and styling (waits for Track B).
- The precise JSON schema and version number scheme (A1).
- ~~Whether `/to-spec` should treat A0 and A1 as one spec or two.~~ Settled: two; A0 is done.
