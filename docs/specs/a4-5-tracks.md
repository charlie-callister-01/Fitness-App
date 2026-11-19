---
labels: ready-for-agent
stage: A4.5
---

# A4.5: Tracks (gym, cardio, mobility paths)

Parent docs: [`../product-brief.md`](../product-brief.md) ("Planning", "Rules carried over from the old system"), [`../data-spec.md`](../data-spec.md), [`a2-core-rules.md`](a2-core-rules.md), [`a4-export-import.md`](a4-export-import.md), [`a5-starter-data.md`](a5-starter-data.md), [`../../GLOSSARY.md`](../../GLOSSARY.md). A change to the A1 data model and the A2 rules, made **before A5** so the starter data is written in its final shape. A4 is merged (PR #17).

## Problem Statement

A Plan today is one weekly rhythm: one weekly target, one Streak, one list of Phases. The owner's real training is three different rhythms: gym Sessions (A, B, D) and cardio Sessions (C, E) counted per week, and a **daily morning mobility routine** with its own daily streak and a "75% of the tick boxes counts as done" rule. One weekly target and one Streak cannot express that, and the phone data cannot be imported without either losing mobility or mixing it into the gym count. The owner also wants each path to be able to progress on its own schedule (for example a mobility habit block of 4 weeks while the gym Phases run for 27).

Fixing this after the starter data and the Home screen exist would mean converting stored data and building Home twice. Doing it now, while there is no real data in the app, is a small additive change.

## Solution

Add **Tracks** to the Plan: named paths (gym, cardio, mobility, or any the owner adds) that each have their own cadence, target, Streak rule and optionally their own Phases. Still exactly one active Plan; still one exchange format and one Import.

- A **Track** has an ID, a name, a **cadence** (`weekly` or `daily`), and the rules for its cadence.
- A **Session** belongs to one Track (`trackId`). A Session with no `trackId` belongs to the Plan's **main track**, which uses the Plan's own `weeklyTarget`, Streak fields and `phases`. So a Plan with no `tracks` behaves exactly as today and every existing document stays valid.
- A Track can carry its own **Phases**. A Track with none uses the Plan's Phases. Phases of one Track must be gap-free and overlap-free among themselves.
- **Streak, week progress, consistency messages, Nudges and Badge metrics** work per Track. A weekly Track's Streak is the A2 weekly Streak over that Track's counted Workouts. A daily Track's Streak is a run of consecutive days.
- No UI. Track B stages (Home, Plan editor, Progress, Trophies) read the per-track results.

## User Stories

1. As the owner, I want a gym path, a cardio path and a mobility path in one Plan, so that each has its own rhythm.
2. As the owner, I want each Track to have its own weekly target and Streak minimum, so that three gym Sessions a week and a daily routine are both honest.
3. As the owner, I want a daily Track whose day counts as done when enough of the routine is ticked (default 75%), so that my morning mobility streak behaves as it did in Obsidian.
4. As the owner, I want a daily Session to be on every day, so that "today's Session" for mobility is always today.
5. As the owner, I want a missed mobility day to simply break its streak, not become a Missed session to make up, so that I am not nagged about yesterday's stretches.
6. As the owner, I want each Track to optionally have its own Phases (for example a 4-week habit block then a maintenance block), so that paths progress independently.
7. As the owner, I want a Track with no Phases of its own to follow the Plan's Phases, so that I only set up what differs.
8. As the owner, I want Badges for a Track's streak and days (mobility week, 50 mornings), so that the old mobility Badges survive.
9. As the owner, I want the existing weekly Streak, "sessions", "best week" and "prs" Badges to keep meaning what they mean now, so that nothing already built changes under me.
10. As the owner, I want Breaks to excuse every Track, so that a holiday does not end the mobility streak either.
11. As the owner, I want my existing Plans and Backups to read unchanged, so that nothing has to be converted.
12. As the owner, I want an imported Plan's Tracks to get fresh IDs where mine collide and everything inside to follow, so that nothing is overwritten.
13. As the owner, I want Export (JSON and Markdown) and the prompt pack to describe Tracks, so that an LLM can read and write them.
14. As a developer, I want every rule that was "per Plan" to take the Track it applies to, so that no Track can silently use another's Streak.

## Implementation Decisions

Items marked **(default)** are proposals for the owner to confirm.

### Data model (A1 change, additive)
- **`Plan.tracks`**: optional array of Track. Absent or empty means "no extra Tracks": every Session is on the main track.
- **Track**:
  | Field | Type | Required | Notes |
  |---|---|---|---|
  | `id` | string | yes | Unique among all Tracks in the document (`duplicate_id`). Hand-written IDs allowed (`mobility`). |
  | `name` | string | yes | Non-blank label. |
  | `cadence` | `weekly` or `daily` | yes | |
  | `weeklyTarget` | integer >= 1 | weekly only | Counted Workouts planned per week. Not allowed on a daily Track (`invalid_value`). |
  | `streakMinWorkoutsPerWeek` | integer >= 1 | weekly only | |
  | `streakCountsCustomWorkouts` | boolean | weekly only | |
  | `dayDonePercent` | integer 1 to 100 | daily only | A day counts when at least this share of the day's planned rows is done. **Default 75.** |
  | `phases` | array of Phase | no | The Track's own Phases. Same shape and week rules as the Plan's; absent means "use the Plan's Phases". |
- **Main track:** the Plan's own `weeklyTarget`, `streakMinWorkoutsPerWeek`, `streakCountsCustomWorkouts` and `phases` describe it. Its name in the app is "Main" **(default)** until the Plan editor lets the owner rename it. It is weekly and is the home of every Session without a `trackId`.
- **`Session.trackId`**: optional string. Must be a Track of the same Plan (`unknown_track`, new code). Absent means the main track.
- **`Session.type`** gains `mobility` (strength, cardio, body, test, mobility). It only colours the calendar and lets a Session be recognised; counting is decided by the Track.
- **`Session.defaultDay`** gains `daily`. Allowed only on a Session whose Track is daily, and required there (`invalid_value` otherwise). A daily Track's Sessions are all daily.
- **Phase** is unchanged. `sessionOverrides[].sessionId` must be a Session of the **same Track** as the list the Phase sits in (the Plan's Phases govern the main track and any Track without its own Phases, so for those the Session must be on a Track that uses the Plan's Phases; `unknown_session` otherwise).
- **Phase IDs** stay unique across the whole document, including Track Phases. Phase week ranges are checked per list (`phase_gap`, `phase_overlap`). Week numbering is Plan-wide (week 1 contains the Plan's `startDate`).
- **Benchmark Sessions** (`type: test` with `benchmarkIntervalWeeks`) stay on the main track and do not count towards any Streak. A benchmark Session with a `trackId` other than main is `invalid_value` **(default)**.
- **Versioning (default):** this is additive for documents (new optional fields), but `daily` and `mobility` are new enum values that an old reader would reject. This app is the only reader and no real data is stored yet, so **`formatVersion` stays 1**. If any older build has written data, the reader error is `unknown_value`, which is already clear. Flagged for the owner.
- **Storage:** the Plan record is stored whole, so no storage layout change and no migration. Existing stored Plans read as "no Tracks".

### Rules (A2 change)
- **Track context.** A new `TrackRules` resolves, for a Plan and a Session: its Track (or main), the Track's effective Phases (own, else the Plan's), and its cadence. Every A2 rule that took the Plan for "weekly target", "Streak" or "current Phase" now takes a Track.
- **Weekly Track.** Counted Workouts, week progress, Streak, best week, consistency message ("on track", "behind", "target out of reach", "target hit") and Missed/Partial/Skipped handling are exactly the existing A2 rules, run over the Workouts whose Session is on that Track. A Custom workout has no Session and counts for the Plan's main track only when `streakCountsCustomWorkouts` allows it, as today **(default)**.
- **Daily Track.**
  - A **day is done** when a Workout of that Track on that date has at least `dayDonePercent` of its planned rows `done` (rows counted from the Workout's own `planned` snapshot). A tick-only row counts when it has a `done` entry.
  - **Daily Streak:** the run of consecutive done days ending today. Today does not break it while still open (as the current week does). A day inside a Break is excused: it neither adds to nor breaks the run.
  - **Best run** and **days done** (total) are derived the same way.
  - **A daily Session is never Missed.** A missed day only ends the run; there is nothing to make up and no Missed Nudge. The week view shows it as not done, not as an outstanding Session.
- **Phases per Track.** `PhaseRules` takes a Track: the current Phase, "week N of M", and the manual "move to next Phase" act on that Track's list. Phase goals ("move on early") work per Track list unchanged.
- **Week schedule moves** apply only to weekly Tracks; a move of a daily Session is `invalid_value`.
- **Badges.** Existing metrics are unchanged and refer to the weekly Tracks together: `sessions` counts counted Workouts across all weekly Tracks, `streak` and `bestweek` refer to the **main track** **(default, the old meaning)**. New metrics, case-insensitive, with the Track ID after a colon: `streak:<trackId>` (best run: weeks for a weekly Track, days for a daily one), `days:<trackId>` (daily: total done days; weekly: total counted Workouts of that Track), `bestweek:<trackId>`. An unknown Track ID shows as "not measurable". The old Obsidian names map to `streak:mobility` and `days:mobility`.
- **Nudges.** "One Session short of this week's target" and "Session X was missed" become per weekly Track, naming the Track when the Plan has more than one. No new Nudge for daily Tracks in this stage **(default)**.
- **Week rearrangement.** Runs per weekly Track on its own outstanding Sessions; daily Sessions are not moved.

### Exchange, Import, Export, prompt pack (A4 change)
- **Reader/writer:** Track, `trackId`, `daily`, `mobility`, `dayDonePercent` as above. The `readMe` block and error-code table gain `unknown_track`.
- **Plan Import** remaps Track IDs that collide with existing ones (fresh IDs from the injected generator), and `trackId`, Badge metrics such as `streak:<trackId>`, and everything inside the imported Plan follow, exactly as Phase and Session IDs do now.
- **Export** carries Tracks inside the Plan. When the Plan tick is off, Workouts keep their `sessionId` null behaviour from A4 unchanged. Markdown shows each Track as a heading with its cadence, target and Phases.
- **Prompt pack:** the "Write me a Plan" format reference and worked example gain a second Track (a daily mobility Track) so an LLM can write a multi-track Plan. The enum value lists are generated from the C# enums as before.
- **Merge** treats Tracks as part of the Plan (replaced with the Plan, as today). No new import kind.

### Out of scope decisions
- **Per-Track Plans** (several active Plans) are not built: it breaks "exactly one active" and the Import rule that the first Plan is active.
- **Custom Tracks beyond the three** are supported by the model (any Track ID and name) but the Plan editor UI is Track B.

## Testing Decisions

- **Seam:** `Core` public surface over the real reader/writer and the existing A2 rule entry points (now Track-aware), with the in-memory `AppData` for Import. No browser, no file system, injected time.
- **Reader/writer cases:** a Plan with no Tracks reads, writes and round-trips exactly as before (a regression pin on the existing worked example); the data-spec example with two Tracks parses; each rule has an exact `path` and `code` test (`unknown_track` on `sessionId`, `daily` on a weekly Track, `weeklyTarget` on a daily Track, `dayDonePercent` out of range, missing `defaultDay`, Phase gaps and overlaps inside a Track's own list, duplicate Track and Phase IDs across lists, a Track Phase override naming a Session on another Track, a benchmark Session on a non-main Track).
- **Rule cases (weekly):** two weekly Tracks counted separately; a Workout counts only for its Track's week and Streak; Custom workouts per the setting; consistency messages per Track.
- **Rule cases (daily):** done at exactly 75%, below it, above it; today open does not break the run; a Break day is excused; a missed day ends the run; best run and total days; a daily Session never Missed.
- **Phase cases:** a Track with its own Phases uses them; one without falls back to the Plan's; "week N of M" and "move to next Phase" per Track; goals per list.
- **Badge cases:** `streak:<id>` weekly and daily, `days:<id>`, `bestweek:<id>`, an unknown Track "not measurable", old metrics unchanged on a multi-Track Plan.
- **Exchange cases:** Plan Import remaps colliding Track IDs and updates `trackId` and Badge metrics; a second identical import says "nothing to change"; Export JSON and Markdown carry Tracks; the new prompt example reads, validates and previews cleanly; a Backup round-trips a multi-Track dataset (the whole-loop test extended).
- **Prior art:** `Core.Tests/Serialisation`, `Core.Tests/Rules`, `Core.Tests/Exchange`. No new test infrastructure.

## Out of Scope

- **Any UI:** Home cards per Track (B3), the Plan editor's Track and Phase editing (B5), per-Track charts and the calendar strip (B6), Track Badges art (B7).
- **Starter data for Tracks:** A5, which depends on this stage.
- **Several active Plans at once**, per-Track Breaks, per-Track goal weight, and a daily Track Nudge.
- **A per-Track Suggested target or PR change:** those are per Exercise and unaffected.

## Further Notes

- **Why a stage before A5.** The starter Plan, mobility Sessions, mobility Badges and past mobility Workouts are all written in this shape. Converting later would mean rewriting stored data.
- **Compatibility.** A Plan with no `tracks` and no `trackId` behaves identically, so A1 to A4 tests are expected to pass unchanged; any that do not are a finding, not something to loosen.
- **Housekeeping.** Update `docs/data-spec.md` (Track section, Session, Phase and Badge rows, error codes, a Track example that the conformance test parses), `docs/build-plan.md` (A4.5 entry; B3, B5, B6, B7 notes), `GLOSSARY.md` (Track, cadence, main track, daily Streak), `docs/README.md`.
- **Defaults to confirm:** the main track is called "Main"; `formatVersion` stays 1; daily Sessions never Missed; Custom workouts count for the main track only; old Badge metrics refer to the main track; no daily-Track Nudge yet; benchmark stays on the main track. The owner chose per-Track Phases ("option 2") on 19 Nov 2026.
- **Proposed ordering for `/to-tickets`** (each about 150 to 200k tokens): (1) model, reader, writer, validation and the data-spec section with its parsed example; (2) `TrackRules`, the weekly rules per Track and Phase lookup per Track; (3) the daily Track rules (day done, daily Streak, Breaks, never Missed); (4) Badge metrics and Nudges per Track; (5) Import remapping, Export JSON and Markdown, prompt pack and example; (6) the whole-loop test extension, regression pins and docs.
- Each ticket must fit one agent in roughly 150 to 200k tokens (see `CLAUDE.md`).
