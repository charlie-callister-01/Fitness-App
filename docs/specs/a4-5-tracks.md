---
labels: ready-for-agent
stage: A4.5
---

# A4.5: Tracks (explicit gym, cardio and mobility paths)

Parent docs: [`../product-brief.md`](../product-brief.md) ("Planning", "Rules carried over from the old system"), [`../data-spec.md`](../data-spec.md), [`../adr/0007-explicit-tracks-and-format-version-2.md`](../adr/0007-explicit-tracks-and-format-version-2.md), [`../adr/0001-schema-first-with-permanent-ids-and-versioned-exchange-format.md`](../adr/0001-schema-first-with-permanent-ids-and-versioned-exchange-format.md), [`../adr/0005-single-writer-quarantine-and-two-storage-versions.md`](../adr/0005-single-writer-quarantine-and-two-storage-versions.md), [`a2-core-rules.md`](a2-core-rules.md), [`a4-export-import.md`](a4-export-import.md), [`editors-b4-b5.md`](editors-b4-b5.md), [`a5-starter-data.md`](a5-starter-data.md), [`../../GLOSSARY.md`](../../GLOSSARY.md).

This spec **replaces** the 19 Nov 2026 version, which kept an implicit main track inside the Plan and had no UI. The owner settled the explicit-Track design on 22 Nov 2026 (see ADR 0007). The Editors group (B4 + B5) is built and its Plan editor, Week view, Plans list and Home all assume one weekly rhythm per Plan; this stage changes that.

## Problem Statement

A Plan today is one weekly rhythm: one weekly target, one Streak, one list of Phases. The owner's real training is three rhythms: gym Sessions and cardio Sessions counted per week, and a **daily morning mobility routine** with its own daily streak and a "75% of the tick boxes counts as done" rule. One weekly target and one Streak cannot say that, and mobility would either be lost or mixed into the gym count. The owner also wants each path to progress on its own schedule (for example a mobility habit block of 4 weeks while the gym Phases run for 27), to see each path on Home, and to edit all of it on the phone.

An earlier version of this stage hid the first path inside the Plan's own fields (an "implicit main track"). That leaves two shapes for the same thing and a special case in every rule, so the owner chose to make every path an explicit Track.

## Solution

Every Plan is a list of **Tracks** plus plan-level fields. Still exactly one active Plan, one exchange format and one Import.

- A **Track** has an ID, a name, a **cadence** (`weekly` or `daily`), the rules for its cadence, and its own optional Phases.
- A **Session** belongs to exactly one Track (`trackId`, required). A daily Track's Sessions are on every day.
- The **main Track** is the first Track in the list, and it must be weekly. Custom workouts, the benchmark and Home's Today card attach to it. There is no `isMain` flag: reordering Tracks changes which one is main.
- **Streak, week progress, consistency messages, Nudges and Badge metrics** work per Track.
- Exchange `formatVersion` becomes **2**. A small Core upgrader reads v1 documents by wrapping the old Plan's weekly target, Streak fields and Phases, and every Session, into one explicit Track named "Main". A Core startup migration rewrites stored Plans into the Track shape. Only the upgrader and the migration know the old shape; every rule sees only explicit Tracks.
- Delivered in **two PRs**: PR 1 is Core, exchange, upgrader and migration, and the existing Editors Core rules made Track-aware (the screens keep working on the main Track only). PR 2 puts Tracks in the screens: Plan editor, Plans list, Week view, Home, Guide and Library.

## User Stories

Model and rhythm
1. As the owner, I want a gym-and-cardio path and a mobility path in one Plan, so that each has its own rhythm.
2. As the owner, I want each weekly Track to have its own weekly target and Streak minimum, so that three Sessions a week and a daily routine are both honest.
3. As the owner, I want a daily Track whose day counts as done when enough of the routine is ticked (default 75%), so that my morning mobility streak behaves as it did in Obsidian.
4. As the owner, I want a daily Session to be on every day, so that "today's Session" for mobility is always today.
5. As the owner, I want a missed mobility day to simply end its streak, not become a Missed session to make up, so that I am not nagged about yesterday's stretches.
6. As the owner, I want each Track to optionally have its own Phases (a 4-week habit block, then maintenance), so that paths progress independently.
7. As the owner, I want Breaks to excuse every Track, so that a holiday does not end the mobility streak either.
8. As the owner, I want the first Track to be the main one, and to change which it is with Move up and Move down, so that I never need a special "main" switch.
9. As the owner, I want Custom workouts and the benchmark to belong to the main Track, so that they behave as they always have.

Badges
10. As the owner, I want `streak`, `bestweek` and `sessions` Badges to keep their meaning (the main Track), so that nothing already built changes under me.
11. As the owner, I want Badges for one Track's streak and days (mobility week, 50 mornings), so that the old mobility Badges survive, and a Track picker in the Badge editor when that screen exists.

Exchange and upgrade
12. As the owner, I want every existing Backup, Plan file and stored Plan to keep working, so that nothing has to be converted by hand.
13. As the owner, I want an imported Plan's Tracks to get fresh IDs where mine collide and everything inside to follow, so that nothing is overwritten.
14. As the owner, I want Export (JSON and Markdown) and the prompt pack to describe Tracks, with an example that includes a daily mobility Track, so that an LLM can read and write them.

Screens (PR 2)
15. As the owner, I want a Track selector (Main, my other Tracks, + Add Track) above the Plan editor's tabs, so that Settings, Phases and Sessions are edited per Track.
16. As the owner, I want Plan settings and Track settings separated, so that I see what belongs to the whole Plan and what to one path.
17. As the owner, I want to add a Track (name and cadence), rename it, change its settings and reorder it, but not remove it, so that history never points at nothing.
18. As the owner, I want the Week view to show a daily Session as one "Every day" row above the week with today's state, never Missed and not movable, so that it does not clutter the days.
19. As the owner, I want Home to show a card per daily Track (today's Session and its daily Streak) and a week line per extra weekly Track, so that every path is visible.
20. As the owner, I want the Plans list to say "Active · week n of m" from the main Track's Phases, so that the headline stays simple.
21. As a developer, I want every rule that was "per Plan" to take the Track it applies to, so that no Track can silently use another's Streak.

## Implementation Decisions

### Data model (A1 change; `formatVersion` 2)
- **`Plan`** keeps only plan-level fields: `id`, `name`, `isActive`, `startDate`, `goal`, `goalDate`, `weightIncrementKg`. It gains **`tracks`** (required, at least one) and keeps **`sessions`** as one flat list. The Plan's `weeklyTarget`, `streakMinWorkoutsPerWeek`, `streakCountsCustomWorkouts` and `phases` move onto the Track.
- **Track**:
  | Field | Type | Required | Notes |
  |---|---|---|---|
  | `id` | string | yes | Permanent. Unique among all Tracks in the document (`duplicate_id`). Hand-written IDs allowed (`mobility`). |
  | `name` | string | yes | Non-blank label; editable. |
  | `cadence` | `weekly` or `daily` | yes | Fixed at creation (an editor rule, not a reader rule). |
  | `weeklyTarget` | integer >= 1 | weekly only | Counted Workouts planned per week. On a daily Track: `invalid_value`. |
  | `streakMinWorkoutsPerWeek` | integer >= 1 | weekly only | |
  | `streakCountsCustomWorkouts` | boolean | weekly only | Meaningful on the main Track only (Custom workouts attach there); stored on every weekly Track so the shape is uniform. |
  | `dayDonePercent` | integer 1 to 100 | daily only | A day counts when at least this share of the Workout's planned rows is done. Default 75 (the editor and the prompt pack use it; the reader requires the field). On a weekly Track: `invalid_value`. |
  | `phases` | array of Phase | no | The Track's own Phases. Absent or empty means the Track has no Phases. There is no fallback to another list. |
- **Main Track** is `tracks[0]` and must be weekly (`invalid_value` at `tracks[0].cadence`). The C# model should make the Track's rhythm one value (weekly rules or daily rules), not a bag of nullable fields; the ticket picks the shape.
- **`Session.trackId`**: required string, must be a Track of the same Plan (`missing_field`, `unknown_track` at the field).
- **`Session.type`** gains `mobility` (`strength`, `cardio`, `body`, `test`, `mobility`). Counting is decided by the Track, not the type.
- **`Session.defaultDay`** gains `daily`. Required on a Session of a daily Track and forbidden on a weekly Track (`invalid_value`).
- **Benchmark Sessions** (`benchmarkIntervalWeeks`) must be on the main Track (`invalid_value` at `trackId`).
- **Phase** is unchanged in shape. Phase IDs stay unique across the whole document, Tracks included (`duplicate_id`). The week-range rule (`phase_gap`, `phase_overlap`; the shared `PhaseRanges.Check`) runs **per Track** over that Track's `phases`; week numbering is Plan-wide (week 1 contains the Plan's `startDate`). A Phase override's `sessionId` must be a Session of that Track (`unknown_session`).
- **Week schedule** moves: a move naming a daily Session is `invalid_value`.
- **Error codes:** `unknown_track` is new. Others are reused (`missing_field`, `invalid_value`, `duplicate_id`, `unknown_session`, `phase_gap`, `phase_overlap`).
- **Plan-level fields on a v2 Plan** that now live on the Track (`weeklyTarget` and so on) are unknown fields (a warning), and a v2 Plan with no `tracks` is `missing_field` at `plans[i].tracks`.

### Versioning and upgrade (ADR 0001, ADR 0005, ADR 0007)
- `ExchangeFormat.CurrentVersion` becomes 2. The v1 reader stays and does not change what it reads; it hands its result to a **v1-to-v2 upgrader** in Core. The upgrader wraps each v1 Plan into one weekly Track with the Plan's `weeklyTarget`, `streakMinWorkoutsPerWeek`, `streakCountsCustomWorkouts` and `phases`, named "Main", ID `<planId>-main` (deterministic and unique because Plan IDs are), and gives every Session of that Plan `trackId` of that Track. Output is the v2 model; nothing downstream sees v1. The importer keeps reading v1 Backups and Plan files (ADR 0001).
- **Stored data:** the storage layout does not change (the Plan record is stored whole). The record format version in `meta` goes 1 to 2. At open, `AppData` already reads stored records through the version they were written in and then rewrites them in the current format (`AppData.Migration`; ADR 0005 notes it has never been exercised). The migration must be deterministic (same input, byte-identical output), keep every record it does not change, fail safe (a failed upgrade leaves stored data as it was and starts as CannotRead, which already blocks commits), and be tested with a real v1 stored dataset, including a Plan that was quarantined. No old shape survives in memory.
- **Compatibility promise:** a v1 file or stored dataset, read and upgraded, gives the same Streak, week progress, Phase and Home results as before for the same Workouts. That equality is the regression proof.

### Rules (A2 change)
- **Track context.** A small Core `TrackRules` gives, for a Plan: the main Track (`tracks[0]`), the Track of a Session, a Track's Sessions and its Phases. Every A2 rule that took the Plan for target, Streak or Phase now takes a Track. No rule sees a Plan-level weekly target.
- **Weekly Track.** Counted Workouts, week progress, Streak, best week, consistency message and Missed, Partial and Skipped handling are the A2 rules over the Workouts whose Session is on that Track. A **Custom workout** has no Session and counts for the **main Track only**, and only when its `streakCountsCustomWorkouts` allows it.
- **Daily Track.**
  - **A day is done** when a Workout of that Track on that date has at least `dayDonePercent` of its `planned` rows done (`done * 100 >= percent * planned`, integers, no rounding; a tick-only row counts when it has a `done` entry). A Workout with no planned rows is not done. If several Workouts of the Track fall on one date, the day is done if any one of them is.
  - **Daily Streak:** the run of consecutive done days ending today. An open today does not break it. A day inside a Break is neutral (adds nothing, breaks nothing). Days before the Plan's `startDate` do not count.
  - **Best run** and **days done** (total) are derived the same way.
  - **A daily Session is never Missed.** A missed day only ends the run. It cannot be moved (`PlanEditResult` refusal in plain words), and week rearrangement never touches it.
- **Phases per Track.** `PhaseRules` takes a Track: current Phase, "week N of M" and the manual "move to next Phase" act on that Track's `phases`. A Track with none has no current Phase. Phase goals work per Track unchanged.
- **Benchmark** and **Custom workouts** are main Track only.
- **Badges.** Unqualified `streak`, `bestweek` and `sessions` keep their meaning and now refer to the **main Track** (`sessions` therefore counts the main Track's counted Workouts, a narrowing of the old "all Workouts"). Qualified metrics target one Track: `streak:<trackId>` (best run: weeks on a weekly Track, days on a daily one), `days:<trackId>` (daily: total done days; weekly: total counted Workouts of that Track) and `bestweek:<trackId>` (weekly only; on a daily Track "not measurable"). The Track ID is matched case-insensitively. An unknown Track ID shows "not measurable". `prs`, `bodyweight` and Exercise metrics are unchanged. This is how a Badge is tied to a Track: the metric names it, and the future Badge editor offers a Track picker.
- **Nudges.** The two weekly Nudges ("One Session short of this week's target", "Session X was missed") are computed per weekly Track, and name the Track when the Plan has more than one weekly Track. **No daily-Track Nudge for now.**
- **Week rearrangement** runs per weekly Track over its own outstanding Sessions.
- **Edit rules** (the Editors Core rules, made Track-aware; each new rule returns `PlanEditResult`):
  - `PlanSettingsRules` and `PlanSettings` split: Plan settings (start date, goal, goal date, weight increment) stay on the Plan; Track settings (weekly target, Streak minimum, Custom workouts for a weekly Track; day-done percent 1 to 100 for a daily Track) move onto the Track.
  - `PlanPhaseRules` takes a Track (validate and replace that Track's Phases; Phase ID uniqueness is document-wide).
  - `SessionRowRules` takes a Track: a new Session on a daily Track gets `defaultDay: daily`; `daily` on a weekly Track is refused; editing a daily Session's day is refused; benchmark only on the main Track.
  - `PlanLifecycleRules.Copy` and `FromScratch`: Copy remaps Track IDs (with Phase and Session IDs) and keeps `trackId`s and Phase overrides pointing at the new IDs; From scratch makes one weekly Track named "Main".
  - New Track rules: **add a Track** (name, cadence; fresh ID; defaults weekly target 3, Streak minimum 2, Custom workouts off, day-done 75), **edit a Track's name** and settings, **move a Track up or down** (refused if it would put a daily Track first). **No remove, and no cadence change.**
  - `PlanSaveRules.IsDifferent` compares Tracks (and each Track's Phases) by sequence.
  - `WeekEditRules` refuses moving a daily Session and refuses "Make this the default day" for it.
  - `PlanProgress` ("Active · week n of m") reads the main Track's Phases.
  - `PhaseRules`, `WeekRules`, `WeekScheduleRules`, `HomeViewBuilder`, `HomeOrbs`, `NudgeRules`, `CustomWorkoutOffer`, `WorkoutStart` and `LibraryViewBuilder` take the Track (or the Plan and a Track) as needed.
- **Home view data (Core, PR 1).** `HomeViewBuilder` keeps the main Track's Today card, week strip, week line and orbs, and adds to its view model: one card per daily Track (today's Session, today's done state, its daily Streak) and one week line per extra weekly Track. PR 1 only builds the data; PR 2 renders it.

### Exchange, Import, Export, prompt pack (A4 change)
- **Reader and writer** for format 2 as above; the `readMe` block and the error table gain Tracks and `unknown_track`.
- **Plan Import** remaps Track IDs that collide with existing ones (fresh IDs from the injected generator). `trackId`, Track Phase overrides and qualified Badge metrics in the same file (`streak:<trackId>`, `days:<trackId>`, `bestweek:<trackId>`) follow, exactly as Phase and Session IDs do now. Importing the same file twice says "nothing to change". The preview mentions Tracks when the Plan has more than one.
- **Export** JSON and Markdown carry Tracks (Markdown: each Track as a heading with its cadence, rules and Phases). The Plan tick off leaves Workouts' `sessionId` behaviour from A4 unchanged.
- **Prompt pack:** the "Write me a Plan" reference and worked example show two Tracks, including a daily mobility Track; enum lists (`SessionType`, `DefaultDay`, cadence) are generated from the C# enums as before.
- **Merge** treats Tracks as part of the Plan. No new Import kind.

### Screens (PR 2)
- **Plan editor.** A Track selector row above the existing tabs: chips for Main (the first Track), the other Tracks, and + Add Track; Move up and Move down for the selected Track (the owner sees Track names; "Main" is the label of the first). The Settings tab splits into **Plan settings** (start date, goal, goal date, weight increment) and **Track settings** (for the selected Track by cadence). The Phases and Sessions tabs show the selected Track's. Add Track is a form sheet (name, cadence). The Track selector, the draft and the Save stay one editing session with one Save and one "Discard changes?".
- **Plans list.** "Active · week n of m" uses the main Track's Phases.
- **Week view.** A daily Session is one row labelled "Every day" above the week grid, showing today's done state; never a Missed chip; no Move button.
- **Home.** Today card, week line and orbs stay the main Track's. Each daily Track gets a card (today's Session, its done state, the daily Streak, Start or Continue). Each extra weekly Track gets its own week line.
- **Guide and Library.** Where useful, show which Track(s) an Exercise's Session belongs to (Guide header "where it sits in the Plan" and the Library's Session groups name the Track when the Plan has more than one).
- Tokens only, both Themes and Modes, 360 px; manual phone checks are added to `docs/pwa-manual-checks.md` (PR 2).

## Testing Decisions

- **Seam:** the `Core` public surface over the real reader, writer, upgrader and the existing rule entry points (now Track-aware), with the in-memory `AppData` for Import and the migration. No browser, no file system, injected time. Ui: bUnit per page, as in the Editors group.
- **Reader and writer:** a v2 Plan with two Tracks round-trips; the data-spec example parses; every rule has an exact `path` and `code` test: `unknown_track` on `trackId`, missing `trackId`, no `tracks`, a daily main Track, `daily` on a weekly Track, a daily Track's Session without `daily`, `weeklyTarget` on a daily Track, `dayDonePercent` on a weekly Track and out of 1 to 100, duplicate Track IDs and Phase IDs across Tracks, Phase gaps and overlaps inside one Track, an override naming a Session of another Track, a benchmark Session off the main Track, a Week schedule move of a daily Session.
- **Upgrader and migration:** a v1 document (the existing worked example and Plan example) reads into one Main Track with identical results from the rules; two v1 Plans get distinct Track IDs; a stored v1 dataset is migrated at open to byte-identical v2 output on a second run, keeps its other records, and a failing write leaves the data untouched (CannotRead). The same Workouts give the same Streak, week counts, Phase and Home results before and after.
- **Weekly rules:** two weekly Tracks counted separately; a Workout counts only for its Track; Custom workouts per the main Track's setting; consistency per Track; rearrangement per Track.
- **Daily rules:** done at exactly 75%, below, above; an open today does not break the run; a Break day is neutral; a missed day ends the run; best run and total days; a daily Session never Missed; a move refused.
- **Phase cases:** per Track lists, "week N of M", "move to next Phase", goals; a Track with no Phases.
- **Badge cases:** the three qualified metrics on weekly and daily Tracks, an unknown Track "not measurable", unqualified metrics on a multi-Track Plan mean the main Track.
- **Edit rules:** each returns `PlanEditResult` with a plain message on refusal; add, rename, move (daily first refused), no remove, no cadence change; Copy remaps; `IsDifferent` by sequence.
- **Exchange:** Plan Import remaps colliding Track IDs and everything pointing at them; a second identical import says "nothing to change"; Export JSON and Markdown carry Tracks; the new prompt example reads, validates and previews cleanly; a Backup round-trips a two-Track dataset and a v1 Backup imports.
- **Screens (PR 2):** bUnit for the Track selector, the split Settings, Add Track, reorder, per-Track Phases and Sessions, the Week "Every day" row, Home daily cards and extra week lines, the Plans headline; read-only and failure cases as in Editors.
- **Prior art:** `Core.Tests/Serialisation`, `Core.Tests/Rules`, `Core.Tests/Exchange`, `Core.Tests/Storage`, `Ui.Tests/Pages` (`PageFixture`).

## Out of Scope

- **Removing a Track,** changing a Track's cadence, and several active Plans.
- **A daily-Track Nudge,** per-Track Breaks, per-Track goal weight.
- **The Badge editor's Track picker** (Insight, B7). The metrics exist in PR 1 and Badges can name a Track through Import and the prompt pack.
- **Progress and calendar views per Track** (Insight, B6).
- **Starter or example data:** A5.
- **Import and Export screens:** Data and settings (B9).

## Further Notes

- **Order:** A4.5 PR 1, then A5 or Data and settings (B9 + B10), one at a time (the owner is usage-constrained: one implementer at a time), then A4.5 PR 2. A5 needs only PR 1 (model, upgrader, importer), not the screens. Data and settings needs PR 1 for the Import and Export screens' wording and can be written against the explicit Tracks.
- **Compatibility proof:** a v1 document upgrades to one Main Track and every A1 to A4 and Editors rule result is unchanged. Existing tests that build a Plan in the old shape are adjusted mechanically (one test builder); a result that changes is a finding, not something to loosen.
- **Housekeeping (PR 1 ticket 10):** `docs/data-spec.md`, `docs/build-plan.md`, `GLOSSARY.md`, `docs/README.md` are updated in the spec commit; ticket 02 makes `data-spec.md` match the code, and the conformance test parses its examples.
- **Calls to check:** upgraded Track ID is `<planId>-main`; unqualified `sessions` now counts the main Track only; `days:<id>` on a weekly Track is total counted Workouts; a day with several Workouts is done if any is; `dayDonePercent` is required by the reader (the editor and prompt pack default it to 75); new Track defaults (weekly target 3, Streak minimum 2); starting a Session of an extra weekly Track from Home is through its week line.
- **Ticket sizing:** each ticket fits one agent in roughly 150 to 200k tokens (see `CLAUDE.md`). Fifteen tickets: ten for PR 1, five for PR 2 (see `.scratch/a4-5-tracks/issues/`).
- **Workflow per PR:** tickets, then `/mattpocock-skills:code-review`, the thermo-nuclear review (applied as C#), the fix pass, `/mattpocock-skills:pr` (see `CLAUDE.md`). PR 2 does not start until PR 1's PR is written.
