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

**Track B: UX and design. DELIVERED (16 Nov 2026).** The design document and assets are in `design/` (see the Track B stages below). UI stages B0 onwards are unblocked, subject to the dependencies listed.

## Proposed stages (each gets its own `/to-spec`; boundaries are a starting proposal)

### Track A
- **A0: Scaffold and delivery. DONE (PR #7 merged, 16 Nov 2026).** The four-project solution, .NET LTS confirmed, PWA shell that installs and works offline, CI (build and test on PR), GitHub Pages deploy. The live app is at https://charlie-callister-01.github.io/Fitness-App/.
- **A1: Data model and spec. DONE (merged).** Entities and relationships (Plan, Phase, Session, Exercise, Workout, Set, Weigh-in, Pain check, Break, Badge, Guide, settings), permanent IDs, tags, canonical units, format version, JSON serialisation, validation. Written spec first.
- **A2: Core rules. DONE (PR #12 merged).** Log kinds and input parsing, PRs and Max sets, Suggested target with RPE, weeks/Streak/consistency, Missed/Partial/Skipped/Swapped handling, Badges, benchmark logic, unit conversion, week-rearrangement suggestions, Nudge rules.
- **A3: Storage. IMPLEMENTED, reviewed and fixed; draft PR #14 open.** Spec: [`specs/a3-storage.md`](specs/a3-storage.md). Delivered: `IDataStore` and the in-memory store, `AppData` (load into an immutable snapshot, save-then-publish commits, quarantine, format migration, replace-all), `IStorageEnvironment` and storage health, the IndexedDB module and store in `Web`, startup wiring, and the Playwright contract run (`src/Web.BrowserTests`, own CI job). Design: technical brief "Storage" and [ADR 0005](adr/0005-single-writer-quarantine-and-two-storage-versions.md); manual checks: [`pwa-manual-checks.md`](pwa-manual-checks.md).
- **A4: Export, Import and prompt pack. MERGED (PR #15 into a3-storage, landed on main by PR #17 on 18 Nov 2026)** (spec: [`specs/a4-export-import.md`](specs/a4-export-import.md); the owner confirmed the spec's defaults on 17 Nov 2026; 804 Core tests). Handoff for the next stage: `.scratch/a5-starter-data/HANDOFF.md`. Export picker logic (JSON and Markdown), Import for Backup / Plan / merge / Guides with previews, automatic backup, Exercise matching, error messages, versioned format reading, prompt pack text.
- **A5: Starter data.** The starting Plan converted from `Config.md`, the 27 Guides, and the weight history, all delivered through Import.

### Track B (design delivered 16 Nov 2026; UI stages)

The design document is in [`design/design-document.md`](design/design-document.md) (v1.0: tokens §4, components C-01..C-24 §5, screens S-01..S-13 §6, flows F-01..F-10 §7, log deep-dive §8, build handoff §11) with assets in `design/assets/` (manifest in `design/assets/README.md`). Decisions it settled: **plain CSS with tokens, no component library**; two Themes (Keep, Clean) and Modes via `data-theme`/`data-mode` on `<html>` ([ADR 0006](adr/0006-two-themes-keep-and-clean.md)); one Razor component per C-number; Blazor CSS isolation for layout. `design/assets/prototype-log.html` is the behavioural reference for S-02.

Each stage gets its own `/to-spec`. Each lists the design sections it implements. Stages are vertical (a screen works end to end on the real store and Core rules). Order is the build order; a stage may start once the ones it depends on are merged.

| Stage | Scope | Design refs | Depends on |
|---|---|---|---|
| **B0: Design system and app shell. MERGED (PR #18, 18 Nov 2026)** ([spec](specs/b0-design-system-and-shell.md)) | Copy `tokens.css`, `components.css`, Jersey 15 font, icons, graphics into `Web`/`Ui`; Theme and Mode switching (incl. "system" via `matchMedia`) with JS interop and persisted setting; app shell with bottom TabBar (C-12) and left rail from 900 px, routes for all five tabs and the More menu as stubs; replace placeholder PWA icons, manifest colours (`splash.json`); base components C-01 Button, C-21 EmptyState, icon component, StatusOrb C-11; responsive/touch-target rules (§9). | §3, §4, §5 (C-01, C-11, C-12, C-21), §9, §11 | A0, A3 |
| **B1: Workout log, core input. MERGED (PR #20, 18 Nov 2026)** ([spec](specs/b1-workout-log-core-input.md)) | S-02 in its main state: ExerciseHeader C-04, SetRow C-02 for every log kind (§8.1), input behaviour and validation (§8.2), RpeSelector C-05, ConfirmBar C-06 with steppers, Suggested target and Last/Best well, PR marker C-03 and PR feedback (§8.3), Exercise done state, in-progress Workout save/resume. Port prototype logic into Core where it is not already in A2. | §5 C-02..C-06, §6 S-02, §8.1-8.3, F-01 (main path) | B0, A2, A3 |
| **B2: Workout log, menus and finishing. MERGED (PR #21, 20 Nov 2026)** ([spec](specs/b2-workout-log-menus-and-finishing.md)) | Workout and Exercise menus (§8.4): skip sheet, swap picker (C-14 ExercisePicker, shared), add Exercise, rest timer and Wake Lock; PainCheckSheet C-13; Finish, Partial and summary (§8.5); Logged afterwards (§8.6); StateChips C-07; error states. | C-07, C-13, C-14, S-02 states, §8.4-8.6, F-01, F-03 (log side) | B1 |
| **B3: Home. MERGED (PR #23, 22 Nov 2026)** ([spec](specs/b3-home.md)) | S-01 in all states (TodayCard C-09, StatusOrb Streak and race countdown, WeekStrip C-24, in-progress, done, rest day, Break, Plan finished); Nudge C-08 (at most three, Home only) wired to A2 Nudge rules; Do a different Session (F-03), Build a Custom workout, Make up a Missed session (F-02); Log weight sheet entry point. | C-08, C-09, C-24, S-01, F-02, F-03 | B1, B2 |

**Remaining stages are grouped (decided 22 Nov 2026).** Specs, tickets and PRs now go by group, because the shared pieces (forms, charts, settings) were being specced in whichever single stage needed them first. The old B-numbers stay as sub-labels so existing links resolve. Build order: Editors, then Data and settings, then Insight, then Hardening. Run `/mattpocock-skills:improve-codebase-architecture` on `Ui` before the Editors spec and after each group's PR. Within a group, tickets go in waves: shared Core and components first, then the pages in parallel.

| Group | Contains | Scope | Design refs | Shared pieces that land here | Depends on |
|---|---|---|---|---|---|
| **Editors** | B4 + B5 | S-03 Guide page (view, edit, no-guide state, personal notes, F-09); S-04 library with search/filter and New Exercise, video link and offline state; the picker's "Add a new Exercise" (disabled since B2); S-05 Plans list and Plan editor (FormPatterns C-20, Phases, reorder, F-04); S-06 Week view (move a Session, Add a Break, Suggested week draft, F-08). | C-20, S-03, S-04, S-05, S-06, F-04, F-08, F-09 | FormPatterns C-20, New Exercise form | B0 to B3 |
| **Data and settings** | B9 + B10 | S-10 screens on A4's logic: export picker, paste-first Import with ImportPreview C-16, Exercise match confirmations, per-Guide keep/replace, field-level errors, Backup and Restore (F-05, F-06, F-10), prompt pack; S-11 Settings (units, Streak rules, increments, rest defaults, Theme live preview, Mode, Backup status, storage health); S-12 First run (starter Plan via Import, install prompt, persistent-storage request, backup reminder, offline state). | C-16, S-10, S-11, S-12, F-05, F-06, F-10 | ImportPreview C-16, Settings rows | Editors, A4, A5 |
| **Insight** | B6 + B7 + B8 | S-07 Progress, Benchmarks and Calendar tabs with LineChart C-17, CalendarHeatmap C-18, ProgressBar C-19, DateRangePicker C-15; S-08 Weight (chart, history, reusing the B3 Log weight sheet); S-13 Badges, detail, Bests with BadgeTile C-10, BadgeEditor C-22, UnlockToast C-23 (static fallback first); S-09 Weekly review (F-07) and Nudge copy and dismissal across screens. | C-10, C-15, C-17..C-19, C-22, C-23, C-08, S-07, S-08, S-09, S-13, F-07 | Chart components, BadgeTile | Editors, A2 |
| **Hardening** | B11 | Accessibility pass (§10 checklist, focus, reduced motion, text scaling), 360 px and tablet/desktop layouts (§9), contrast checks against §4.2 for all four Theme×Mode sets, performance on a mid-range phone, manual device checks added to `pwa-manual-checks.md`. | §9, §10 | none | all groups |

Notes for the specs:
- **Shared pieces land where first needed:** ExercisePicker (C-14) in B2, reused by B4/B5; the Log weight sheet in B3, reused by B6. Don't pre-build them in B0.
- **B1 and B2 are split** because the log is the largest screen; if B1 still exceeds one agent's budget at ticket time, split by log kind groups (reps/weight, time/distance, carry/bodyweight).
- **Theme intensity** (design §1): Keep is nearly plain on the log, full on Trophies and calendar; build skin classes once in B0, don't re-decide per screen.
- **Cheaper alternatives** the design allows (§11): static unlock toast, single tiled SVG noise or no texture, one badge frame plus glyphs. Prefer these first.
- A stage's browser/visual checks compare against `design/assets/mockups/S-xx-*.png` in both Themes and both Modes.
## Inputs still needed from the owner
- `Fitness/Config.md` (current plan, kinds, badges).
- `Fitness/_scripts/fitness/view.js` and `view.css` (all logic to port; the reference for rules).
- The 27 files in `Fitness/Exercises/*.md` (starter Guides).
- Weight history from the old daily notes (`session::`/`weight` data from 1 Feb 2026) and `Plan/Tracking/Weight.md` / `Homepage.md` logic.
- `Fitness/How to use.md` (current user flow, out of date).
- ~~The Claude Design document (Track B).~~ Delivered: `docs/design/`.

None of these block A0 to A4.

## Status and next step
**A0 is merged and deployed** (spec #1, tickets #2 to #6, PR #7; deploy fix PR #9). GitHub Pages source is set to "GitHub Actions" and the Deploy run succeeds. Still to confirm by hand, because they need a phone or a throwaway PR: install, offline open and update pickup (see `pwa-manual-checks.md`); that a deep link restores in a real browser; that CI fails on a deliberately failing test; and the `lan` run profile from a phone.

**Track B design is delivered and sliced into stages B0 to B11** (see above); the first UI stage to spec is **B0 (design system and app shell)**, which only needs A0 and A3. B9 needs A4 and B10 needs A5.

**B0 (design system and app shell) is merged and deployed** (PR #18; Core 814, Ui 103, Web 37 tests plus 47 browser tests). Delivered: Theme and Mode as Settings, the design assets and PWA identity, Theme switching, Button/Icon/StatusOrb/EmptyState, the shell (tab bar and rail, three layouts, placeholder routes), and the More page with temporary Appearance pickers (and a Debug-only component showcase). Still owed by hand on a phone: the B0 sections of `pwa-manual-checks.md`. Review items deferred: see `.scratch/b0-design-system-and-shell/pr-body.md`. **Next: B1 (Workout log, core input) and, in parallel if wanted, A5 (starter data) and B4/B6/B7 (screens that depend only on B0 and Core).** See `.scratch/b1-workout-log/HANDOFF.md`.

**A1 and A2 are merged** (539 Core and 9 Ui tests green). **A3 (Storage) is implemented** (all nine tickets). Remaining for A3: the two reviews, their fixes, and the PR. **Next stage: A4 (Export, Import and prompt pack)**, once A3's PR is written. Read `docs/README.md`, `CLAUDE.md` and `GLOSSARY.md` first. The storage manual checks (quota-full, persistence grant, cleared data, blocked upgrade, iOS Safari) still need doing by hand on a phone; see `pwa-manual-checks.md`.

**B1 (Workout log, core input) is merged** (PR #20; spec [`specs/b1-workout-log-core-input.md`](specs/b1-workout-log-core-input.md); tickets in `.scratch/b1-workout-log/issues/`; PR body `.scratch/b1-workout-log/pr-body.md`). Both reviews and their fix pass are done. Tests: Core 939, Ui 210, Web 37 (browser tests not run for B1). Owed by hand on a phone: the "Workout log manual checks (B1)" section of `pwa-manual-checks.md`. B1 alone is not yet a daily tool (no Skip, Swap, rest timer or summary); start logging real Workouts after B2.

**B2 (Workout log, menus and finishing) is merged** (PR #21, 20 Nov 2026; branch `b2-implementation`; spec [`specs/b2-workout-log-menus-and-finishing.md`](specs/b2-workout-log-menus-and-finishing.md); nine tickets in `.scratch/b2-workout-log-menus-and-finishing/issues/`, four review-fix tickets in `.../fixes/`, PR body `.../pr-body.md`). Delivered: the Exercise menu and Workout menu (§8.4: benchmark, Theme, Mode, rest timer), the shared Sheet shell, the skip sheet, the Swap and Add picker (C-14), View Guide as a sheet, the rest countdown and Wake Lock, Finish with the Partial prompt, the Pain check (C-13) and the finish summary (§8.5), reopening a finished Workout to its summary, Logged afterwards (§8.6: re-date, banner, no timers, PRs judged as of that date, undo), the unit-aware Suggested target reason, and error and read-only states for every action. Tests: Core 1048, Ui 358, Web 39 (browser tests not run for B2). Owed by hand on a phone: the "Workout log manual checks (B2)" section of `pwa-manual-checks.md` (and the B1 section, still unticked). **Both reviews and their fix pass are done.** **Next stage: B3 (Home).** Read `.scratch/b3-home/HANDOFF.md` first. A5 and B4/B6/B7 can still run in parallel.

**B3 (Home) is implemented** on the `b3-implementation` branch (spec [`specs/b3-home.md`](specs/b3-home.md); fourteen tickets in `.scratch/b3-home/issues/`). Delivered: the real Home (S-01) built from one Core seam (`HomeViewBuilder`): the TodayCard in all eight states, the week strip, week line and three orbs, up to two Nudges with per-week dismissals kept on the device, Recent PRs, the Weight panel and the Log weight sheet, Do a different Session, Build a Custom workout (also Make up a Missed session) and Log a past Workout. The first stage planned with the agent workflow trial; tickets 06 to 14 were done by the planning session itself after the implementer tool hit its usage limit (see `agents/workflow-change-log.md`). Known gap: the shell caps the content column at 560 px, so the two-column desktop layout (from 900 px) only shows once the layouts allow a wider column.

### Open items for the owner (from B2)
- **Added Exercise position is approximate.** The data model does not record which row an added Exercise follows, so it is placed after the row that owns the nearest earlier entry. After a swap or skip it can land somewhere other than where you added it. A proper fix needs a position field on the entry (a data-model change, A-stage).
- **A past-dated Workout is in progress until finished.** Logged afterwards re-dates the open Workout and does not finish it, so Home keeps offering Continue until you tap Finish.
- **Undoing Logged afterwards cannot restore the start time.** Re-dating discards it, so an undone Workout has today's date and no start time (and so no duration).
- **"Add a new Exercise" in the picker is disabled** until the Exercise library (B4).
- **Body-area chips show all 11 body areas.** The design text lists five; the data model has 11, so all are offered.
- **The Badge unlock toast on the summary is a static well.** No animation and no auto-hide; it is derived, so reopening the summary shows it again.
- **The Pain check side is saved as a note prefix** ("Left side: ..."), because the model has no side field.
- **Backdrop tap on the Pain check counts as Skip,** and on the Partial prompt as Keep going.

### Open items for the owner (from B1)
- **Distance + time kind not in the data model.** The design's distance + time row with pace (`1 km in 4:35`) cannot be saved: distance and time are separate kinds. Needs a data-model decision.
- **Text kind cannot be logged.** A Set has nowhere to keep text, so a Text Exercise shows a "cannot be logged yet" note.
- **Minutes input does not accept `12:30`.** Only a number or a `min`-suffixed number.
- **Extra unconfirmed Sets from Add Set are not persisted.** Only confirmed Sets are saved, so killing the tab loses an extra row that was added but not confirmed.
- **Tapping RPE before any Set creates a Done entry with no Sets.** The entry is created to hold the RPE.
- **Last/Best result for steppers not offered for Reps, Minutes and body measurements.** Steppers exist only for weight, carry, seconds and bodyweight, so Reps, Minutes and the body measurements have no stepper shortcut from the last result.
- **Time (lower is better) PR path is now loggable but has no page-level test.** The rule is covered in Core (ticket 02) and the Time row is tested for storage, but no page test asserts the PR marker for a faster time; it is on the manual checks.

## Known sharp edges from A3 (for the owner)
- **Only active Plan quarantined.** If the only active Plan cannot be read it is set aside, the snapshot then has a dangling reference, and the app starts as "cannot read" (`no_active_plan`), which blocks every commit. The owner can then only restore a Backup. Consider a gentler recovery path when the Plan screens are designed.
- **Backup restore is two writes.** After a restore, `RecordBackupAsync` (the last-Backup time) is a separate, non-atomic write from the replace-all. A failure between them leaves the data restored but the Backup Nudge date stale. A4 should handle or surface it.
- **Web Locks missing means "single window".** A browser without Web Locks gets the writer lock automatically, so two windows could write there. Accepted: all current target browsers have it, but it is untested outside Chromium.
- **Browser tests do not run WASM.** They run the C# store on the test host and the JavaScript in headless Chromium. The Blazor interop layer in a real WebAssembly runtime is checked only by hand.
- **Layout version is still 1.** No real layout or record-format upgrade has been exercised, only a synthetic older version in tests and the manual rehearsal in `pwa-manual-checks.md`.
- **The browser-tests CI job has not yet run on GitHub.** Expect to fix workflow details on its first run.

## Open items to settle during specs
- ~~The exact .NET LTS version and Blazor template details.~~ Settled: .NET 10 LTS, standalone Blazor WebAssembly PWA template ([ADR 0004](adr/0004-dotnet-10-lts-pinned.md)).
- ~~UI library and styling.~~ Settled by the design document: plain CSS with tokens, no component library ([design §11](design/design-document.md)).
- The precise JSON schema and version number scheme (A1).
- ~~Whether `/to-spec` should treat A0 and A1 as one spec or two.~~ Settled: two; A0 is done.
