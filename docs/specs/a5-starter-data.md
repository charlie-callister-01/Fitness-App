---
labels: ready-for-agent
stage: A5
---

# A5: Starter data

Parent docs: [`../product-brief.md`](../product-brief.md) ("Starting data (via Import)", "Starting Plan"), [`../technical-brief.md`](../technical-brief.md), [`../data-spec.md`](../data-spec.md), [`a4-export-import.md`](a4-export-import.md), [`a2-core-rules.md`](a2-core-rules.md), [`../../GLOSSARY.md`](../../GLOSSARY.md). Builds on A4's `Core.Exchange` (Plan, Merge and Guides Import) and A1's exchange format. A4 is merged to `main`; A5 branches from `main`.

## Problem Statement

The owner has run the training for a month in an Obsidian/Dataview vault: a `Config.md` that holds the Plan, 36 Guide notes, 13 log notes and a Dataview script. The new app starts empty. Without the old data the owner would retype 38 Exercises, seven Sessions, 21 Badges and 41 Guides, and would lose the weight and Workout history already logged, which defeats "replacing the old system". The data cannot be copied as it is: the old system uses its own shapes (a Markdown table for the Plan, front-matter keys per log note, Dataview-flavoured Guide notes, a Session type the new model does not have), and the old prose prescriptions ("20-30 s per side", "5 min", "every hour") do not fit the Exercise row (`sets` and an optional rep range).

The conversion must be repeatable, checked, and delivered through the normal Import, so that what the owner pastes is exactly what the importer was tested against and a re-run says "nothing to change". The repo is **public** and deploys to GitHub Pages, so the owner's weight, Workouts and health-related Guide text must never be committed or bundled into the app.

## Solution

Stage A5 adds one new project, **`StarterData`** (a C# class library with a thin console entry point), and its tests. It reads the old vault folder and writes the starter data as exchange-format JSON through `ExchangeFormat.Write`, so every file is valid by construction. There is no UI and nothing in `Core` changes (one possible exception is noted in Further Notes).

The converter produces three files, delivered through three of the four Import kinds in this order:

| Order | File | Import kind | Carries |
|---|---|---|---|
| 1 | `starter-plan.json` | **Plan** | the starting Plan (Phases, Sessions, Exercise rows) and the Exercise library |
| 2 | `starter-guides.json` | **Guides** | one Guide per Exercise |
| 3 | `starter-history.json` | **Merge** | the Badges, the Weigh-ins and the back-filled Workouts |

Settings are not part of any of these (no non-Restore kind imports Settings); see Further Notes.

The owner sees this as: run one command against the vault folder, then paste three files into the Import screen in order, read each preview and confirm. Until Track B's Import screen (B9) exists, A5's proof is its tests, which push each converted file through the real `ImportPreviewer` and `ImportApplier` into an empty `AppData`.

## User Stories

### The starting Plan
1. As the owner, I want the Plan, Phases, Sessions and race date I have now to arrive as a normal Plan Import, so that I keep my current training without retyping it.
2. As the owner, I want Sessions A to E, the Sunday weigh-in and the benchmark Session (every 4 weeks), with the Exercise rows they have today, so that Home shows the Session I actually do.
3. As the owner, I want each row's sets and rep range read from the old "Sets × reps" text, so that the Suggested target logic has real numbers to work with.
4. As the owner, I want any prescription the format cannot express (seconds, minutes, "per side", rounds) kept word for word at the front of the row's cue, so that nothing I wrote is lost.
5. As the owner, I want the Exercise rows that appear in several old Sessions (Warm-up, Finisher, Longest continuous run) to become one Exercise used in each Session, so that its history is one history.
6. As the owner, I want the old "Log as" kinds mapped to the new log kinds one for one, so that "20x10" style history keeps its meaning.
7. As the owner, I want the four benchmark tests (Longest continuous run, SkiErg 1000 m, Plank max, Push-up max) marked as Benchmark exercises, so that the benchmark Session is pre-filled.
8. As the owner, I want the Plan's weekly target (5), Streak minimum (4), custom workouts not counting and weight step (2 kg) set on the Plan, so that the Streak behaves as it does today.

### Guides
9. As the owner, I want my 27 written Guides and the shared routine notes to arrive as Guides keyed to Exercises, so that the library is not empty on day one.
10. As the owner, I want each old section (Works, Why it's in your plan, Setup, How to do it, Cues, Common mistakes, Easier / harder, Watch-outs for you) put into its matching Guide field, so that the Guide screen shows the same advice.
11. As the owner, I want any old section with no matching field (Rules, Pacing, What to log, and so on) kept inside the Guide's How-to text under its heading, so that no advice is dropped.
12. As the owner, I want a note that covers several Exercises (Running, SkiErg, Push-up) to give each of them its Guide, so that tapping any of them shows the help.
13. As the owner, I want my own notes ("My notes") carried over when they hold anything and left absent when they are empty, so that an empty bullet never becomes a personal note.

### History
14. As the owner, I want my 21 working Badges to arrive as editable Badges with the right metric and target, so that Trophies shows progress from day one.
15. As the owner, I want my real log entries (the Workouts I actually recorded) back-filled as Workouts flagged "logged afterwards", so that last-time and best-so-far numbers are there.
16. As the owner, I want empty log notes (a Session button tapped, nothing entered) not to become Workouts, so that the Streak and PRs are not polluted.
17. As the owner, I want my Weigh-ins, including the history before the vault's logs begin, to arrive as Weigh-ins, so that the weight chart starts at the real start weight.
18. As the owner, I want values I typed in odd forms (`12×10`, `35 × 10`, `bwx8`, `4:25`) read as the old app read them, so that nothing silently changes meaning.
19. As the owner, I want every value the converter could not read or does not believe listed in a report, naming the file and the key, so that I decide rather than the script guessing.

### Safety and repeatability
20. As the owner, I want the same IDs every run, so that re-running the converter and re-importing says "nothing to change".
21. As the owner, I want importing the three files in order into an empty app to produce no blockers, no open choices and the counts the report promised, so that a bad conversion is caught before it reaches the phone.
22. As the owner, I want the converted files never committed to the repo (it is public), so that my weight and health notes stay private.
23. As the owner, I want the converter to report what it left out on purpose (mobility, the old Bodyweight row), so that "where did X go" has an answer.

### Quality
24. As a developer, I want the whole stage testable through one seam (a small fake vault in, exchange JSON and a report out, then the real importer), with no real vault, no network and no clock.

## Implementation Decisions

Items marked **(default)** are proposals for the owner to confirm (see Further Notes); each is one rule in one place.

### Shape and delivery
- **A new project, `src/StarterData`** (class library plus a console `Program`), referencing `Core` only, and **`src/StarterData.Tests`**. Not in `Core` (the old formats must not leak into the domain), and not a PowerShell/Node script (the owner works in C#, and output built from the Core model plus `ExchangeFormat.Write` cannot be malformed). **(default)** Add both to the solution so CI builds and tests them.
- **Surface:** `VaultConverter.Convert(VaultFiles input, ConversionOptions options)` returns `ConversionResult { PlanFile, GuidesFile, HistoryFile, Report }`. `VaultFiles` is an in-memory description of the vault (file name to text), so tests need no disk. The console `Program` reads a vault folder (and an optional weight file), calls `Convert`, writes the three files and prints the report. The one deliberate exit: `Convert` takes no clock; the Plan's start date comes from the vault's Settings and nothing depends on today's date.
- **Delivery (default): the owner runs the converter locally and pastes the three files through the Import screen in order.** Nothing is bundled into the app, because the site is public and Guides carry health notes ("watch-outs for you"). The converter writes into a `starter-data/` folder that is added to `.gitignore`. A short runbook (`docs/starter-data.md`) states the order, why the order matters (the Guides Import needs the Exercises the Plan Import added), and that the Plan Import only makes the Plan active when the library has no Plan, so it is run **before** logging anything real in the app.
- **First-run offer (Track B, B10):** any "start with my old data" prompt must send the owner to Import with these files; it cannot ship a bundled copy. Recorded here so B10's spec starts from it.
- **Stable hand-written IDs**, kebab-case (`^[A-Za-z0-9_-]+$`): see below. Same input gives byte-identical output (stable order, no timestamps, `exportedAt` left absent) so a re-import is "nothing to change" and a diff shows only real changes.

### Reading the vault
- **Config:** headings split sections; the Settings are `- key: value` lines; Phases, Sessions, Exercises and Badges are the Markdown tables. The Exercise table's columns are Session, Exercise, Sets × reps, Log as, Cue; the Session column is a comma list (`A, B, D`). Backticks are stripped from cells. The parser follows the old `view.js` closely (header words, `-` for a tick box, the kind aliases) so both agree on what the vault says.
- **Guides:** a note is front matter (`exercises:` list of the Exercises it covers) plus Markdown. The Dataview code block is dropped.
- **Logs:** a note is front matter only (`session`, `date`, one key per Exercise, `done:` list). The key is the Exercise name with each run of non-alphanumerics turned into `_` and lower-cased (`db_romanian_deadlift`), the same rule the old script used, so converter and vault agree.
- **Weight history:** see Further Notes (input missing). The converter accepts a plain `date,kg` text file (one Weigh-in per line) so that whatever the owner extracts from the old daily notes can be fed in without a new parser per source.

### Exercises
- **One Exercise per distinct name** in the Config table (48 names), minus the mobility group (see Sessions), plus the three Exercises that have a Guide but no row in the current Config (Chest-supported row, Incline DB press, Farmers carry: they were in an earlier version of Session B and the owner's logs still carry their keys). That gives 41. The brief's "about 40 Exercises" agrees. **(default)**
- **ID** is the name lower-cased with non-alphanumerics collapsed to `-` (`goblet-squat`, `skierg-1000-m`, `glute-bridge-bird-dog`). A collision stops the run with an error naming both names.
- **Log kind mapping:** `load` to `load`, `carry` to `carry`, `reps` to `reps`, `secs` to `seconds`, `mins` to `minutes`, `km` to `distanceKm`, `m` to `distanceM`, `time` to `time`, `kg` to `bodyMeasurementKg`, `cm` to `bodyMeasurementCm`, `text` to `text`, `-` to `tickOnly`. An unknown kind stops the run. The mapping is one table.
- **Benchmark:** `isBenchmark: true` for Longest continuous run, SkiErg 1000 m, Plank max and Push-up max, i.e. the Exercises of the old test Session T.
- **Tags:** none **(default)**. The old Config has no tags and deriving them from the free text "Works:" lines would be a guess. The owner can fill them later through the "Write Guides" style prompt or by hand. Flagged because B4's library filter works on tags.

### Plan, Phases and Sessions
- **Plan:** `id: hyrox-london-2027`, name from the old race name ("HYROX London (Doubles)"), `isActive: true` (the Plan Import makes the first Plan active anyway), `startDate` 2026-10-19, `goal` the race name, `goalDate` 2027-04-24, `weeklyTarget` 5 (Settings `sessions per week`), `streakMinWorkoutsPerWeek` 4, `streakCountsCustomWorkouts: false`, `weightIncrementKg` 2.
- **Phases:** Foundation 1-8, Build 9-19, HYROX prep 20-27, ids `foundation`, `build`, `hyrox-prep`. No Phase goals and no Session overrides: the old Config has neither (it says to edit the Exercises table when Phase 2 starts). Those arrive when the owner writes the next Phase.
- **Sessions:** `a-lower` "A: Lower + core" Monday, `b-upper` "B: Upper + shoulders" Tuesday, `c-engine` "C: Engine + core" Wednesday, `d-full` "D: Full body" Thursday, `e-long` "E: Long session" Saturday, `w-weigh-in` "W: Weigh-in" Sunday (type `body`), `t-benchmark` "T: Benchmark test" (type `test`, `benchmarkIntervalWeeks: 4`, day Saturday **(default)**, because the old "Every 4 weeks" has no weekday and Saturday is the long, unhurried day). Session names use the data spec's "A: Lower + core" form. The old cardio/strength types map to `strength`/`cardio`/`body`/`test` one for one.
- **Mobility is left out (default).** The old Session M ("Morning mobility", type `mobility`, daily) has no counterpart in the new model (the Session types are strength, cardio, body and test) and the product brief's starting Plan does not list it. Left out with it: its ten rows (Cat-cow, Bird dog, Half-kneeling hip flexor stretch, Thoracic open book, Deep squat hold, Wall slides, Doorway chest stretch, Wrist stretches, Lunch walk, Desk resets), the five mobility Badges, the mobility-only Guide notes (Morning mobility routine, Desk resets), and the mobility log notes. The report lists each omission so the owner can overrule it; adding it back means a new Session type, which is an A1 change and an owner decision.
- **Row order** is the Config table order within each Session. A row listed for several Sessions becomes one row in each.
- **Bodyweight is not an Exercise (default).** Weight has one home, the Weigh-in (data spec). The old W Session's Bodyweight row is therefore dropped from the Plan, and W carries one row: Waist (`bodyMeasurementCm`, monthly in the cue).
- **Reading "Sets × reps" into a row.** Deterministic, in this order, one function with a table of cases as its test:
  1. `N × A` or `N × A–B` with nothing after it: `sets` N, `reps` A, `repsMax` B. (`3 × 8–10`, `3 × 10`)
  2. Otherwise `sets` is the leading `N ×` if there is one, else 1; no `reps`; and the **original text is placed first in the cue**, then a full stop and the old cue. (`2 × 8 per side` gives sets 2, cue "2 × 8 per side. Start bodyweight...". `45 s per side` gives sets 1, cue "45 s per side. Squeeze...". `5 min`, `20-25 min`, `every hour`, `1 all-out`, `3 rounds of 6/4/2`, `2 × 10 / 2 × 6 per side` all go this way.)
  3. An empty prescription (`× 8 slow` has no leading number) gives sets 1 and the text kept in the cue the same way.
  Per-side and timed prescriptions are therefore lossless but not machine-readable. A row field for them (`target` text) would be an optional field that needs no format-version bump; flagged as a possible A1 follow-up, not done here.

### Badges
- **21 of the 26 old Badges** (the five mobility ones are left out with Mobility). Ids are the lower-cased name with non-alphanumerics collapsed (`first-rep`, `gate-1-20-min-run`).
- **Metric mapping:** `sessions` to `sessions`; `streak` to `streak`; `best week` to `bestweek`; `prs` to `prs`; an Exercise name to that Exercise's ID.
- **Targets** are in the canonical unit of the Exercise, never the display form. This is where the conversion is easy to get wrong: a `time` target `4:30` is 270 (seconds, lower is better, which `BadgeRules` already handles); `Longest continuous run` is a `minutes` Exercise whose stored unit is seconds, so `20` becomes 1200 **(to be pinned by a test against `BadgeRules`)**; `Plank max` 60 is seconds; `Running distance` 5 and 10 are km; Waist `-3` and `-6` stay negative ("drop by this much", which `BadgeRules` supports for a cm Exercise).
- **"Bodyweight deadlift" (default):** the old target `bodyweight` meant "your latest weigh-in", and the Badge target is a fixed number. Converted to the latest known weigh-in at conversion time (86.5 kg from the vault's 18 Oct entry unless the weight file supplies a newer one), reported as an approximation for the owner to adjust.

### Guides
- **One Guide per Exercise that exists in the library and has a source note.** All 38 non-mobility Exercises in the current Config have one, as do the three extra Exercises. A note covering several Exercises (`exercises:` list) is **copied in full to each covered Exercise** **(default)**, which matches how the old app showed one Running note for four entries; the alternative (full text on the first Exercise, a pointer on the rest) is cheaper to store but makes three Guides near-empty.
- **Section to field:** `**Works:**` to `works`; `**Why it's in your plan:**` to `whyInPlan`; `## Setup` to `setup`; `## How to do it` (numbered list kept as text) to `howTo`; `## Cues`, `## Common mistakes`, `## Watch-outs for you` (bullet lists) to `cues`, `commonMistakes`, `watchOuts`; `## Easier / harder` to `easier` and `harder` (the `**Easier:**` and `**Harder:**` bullets); `## My notes` to `personalNotes` only when it holds text beyond an empty bullet.
- **Everything else is kept, not dropped:** any other section, and any `**When:**`-style line, is appended to `howTo` under its own heading, in file order. The nine free-form notes (routines, Running, Measuring, Finishers and the rest) are mostly such sections.
- **`videoUrl`:** none of the 36 notes has a link, so none is written.
- **Notes for Exercises left out (mobility)** are not converted, and the report lists them.

### History
- **Which log notes become Workouts.** A note is a Workout only if it has at least one entry with a value, or one ticked item that is an Exercise in the new library. Notes with no values and no ticks (a button tapped and nothing entered) are skipped and counted in the report. Session `M` notes are skipped with Mobility. A note that has only Waist and Bodyweight (the Sunday `W` note) gives one Weigh-in plus one Workout holding the Waist entry.
- **IDs:** Workout `workout-<date>-<session code, lower-case>`, Weigh-in `w-<date>`, Sets `s-<workout id>-<n>` (Set IDs are unique across the document).
- **Workout shape:** `date` and `sessionId` from the note; `loggedAfterwards: true` (the entries were made in the old system, not live in the app); no `startedAt`/`endedAt`; `planned` is a snapshot of the converted Plan's rows for that Session **(default)**, because the old Session B list differed on 3 Nov (it had Chest-supported row, Incline DB press and Farmers carry) and the old notes do not record what was planned; one entry per Exercise with a value, status `done`, one Set; no Pain check (unanswered).
- **Value reading follows `view.js` exactly:** `load` accepts `20x10`, `20 x 10`, `12×10`, `20*10`, a comma decimal, optional `kg`, and `bw` as 0 kg; `carry` is `kg x metres` or just kg; `time` is `m:ss` or plain seconds; `secs`, `mins`, `km`, `m` are plain numbers (so `mins` becomes seconds on write); `kg` writes `weightKg` and `cm` writes `measurementCm`; `text` has no field on a Set (see below).
- **Ticks:** a `done:` list item whose key is a `tickOnly` Exercise becomes a `done` entry with no Sets (Warm-up, Dead bug and so on), so a note with only ticks still counts as a Workout, as it did.
- **What does not convert, reported rather than guessed:** an unreadable value; a value in the future; an entry whose key is not in the library; a `text` entry (a Set has nowhere to keep text, the same A1 gap B1 reported, so the text goes in the report for the owner); and implausible values. **The one known case is `side_plank: 2.3` on 2 Nov**, an Exercise whose range is "20-30 s" and whose kind is seconds. Default: the entry is written as `done` with no Set and listed, rather than storing 2 seconds as a fact.
- **Weigh-ins:** 18 Oct 2026, 86.5 kg is the only weight in the vault zip. The brief's history from 1 Feb 2026 (113 kg) comes from files that were not supplied (Further Notes). Duplicate dates keep the later line and are reported.
- **Settings are not converted.** The four Import kinds do not import Settings (only a full Backup restore does) and the Plan already carries the Streak and increment rules. The goal weight is entered in Settings (B10); the report prints the start weight for reference.

### Reporting
- `ConversionReport` is a list of plain lines grouped as **Converted** (counts per file: 41 Exercises, 7 Sessions, N Guides, 21 Badges, N Weigh-ins, N Workouts), **Left out on purpose** (mobility group, Bodyweight row) and **Needs a look** (unreadable values, approximations, duplicates, source files not found). It is returned and printed; it is never written into the JSON.
- The run stops with an error (no files written) only for structural problems: a missing Config, an unknown Log-as kind, an ID collision, a Session referencing an unknown code, an output that fails the Core reader. Content problems go to the report.

## Testing Decisions

- **One seam: `VaultConverter.Convert` on a small fake vault, then the real Core importer.** A hand-made fixture vault of a few dozen lines (a trimmed Config with every kind of row, three Guide notes of different shapes, four log notes: a filled one, an empty one, a tick-only one and one with an unreadable value) lives in `StarterData.Tests`. It is made up, not copied from the owner's vault (public repo). Tests assert what a caller sees: the three JSON documents and the report.
- **Contract tests (the important ones):** each output reads back through `ExchangeFormat.Read` with no errors and no warnings; the three files imported in order into an empty `AppData` through `ImportInput.Inspect`, `ImportPreviewer.Preview` and `ImportApplier.ApplyAsync` give `IsReady` with no blockers and no open choices at every step and the counts the report states; importing each file a second time says "nothing to change"; running `Convert` twice gives byte-identical files.
- **Reading rules, as tables of cases:** the "Sets × reps" cases (the full list from Implementation Decisions); the value parser (the forms in the old script, including `×`, spaces, `bw`, comma decimals, `m:ss`); kind mapping; Exercise ID derivation and collision; Session and Phase output; Badge metric and canonical-unit targets, with a check that each converted Badge produces a sensible progress through `BadgeRules` on a tiny history (a time badge reached by a faster time, a minutes badge by 20 minutes, a Waist drop badge).
- **Guide cases:** every standard section lands in its field; unknown sections appear in `howTo` in order; a multi-Exercise note produces one Guide per covered Exercise; an empty "My notes" leaves `personalNotes` absent; a Dataview block never reaches the output.
- **History cases:** an empty note is skipped and counted; a tick-only note is a Workout; a Session `M` note is skipped; the `W` note gives a Weigh-in and a Waist Workout; an unreadable value is reported and the entry omitted; `planned` equals the converted Session's rows.
- **Not tested here:** the owner's real vault. It is checked once, by hand, by running the converter and reading the report, then Importing on a phone once Track B's Import screen exists. This is recorded in `pwa-manual-checks.md`.
- Fast, deterministic: no disk beyond the console entry point (which has one smoke test over a temp folder), no clock, no network.

## Out of Scope

- Any UI: the Import screen (B9) and the first-run offer (B10).
- Bundling starter data into the app, or committing the owner's data to the repo.
- Mobility: the Session, its Exercises, Badges and log history (an A1 change and an owner decision first).
- Settings import (the format has no non-Restore path) and a goal weight.
- Exercise tags, Phase goals and Phase session overrides.
- Checking the A2 spec's nine assumptions against the old `view.js` (see Further Notes: the script is now available, but that is its own small task).
- Converting the old dashboard, calendar, charts or CSS.
- Re-running the converter against future vault changes as a sync: this is a one-time migration, though it is repeatable.

## Further Notes

- **Owner inputs: what was found and what is missing.** The owner supplied `Fitness (2).zip` (in Downloads), a newer vault snapshot than the handoff described. **Found:** `Config.md`, `How to use.md`, `_scripts/fitness/view.js` and `view.css`, the 36 Guide notes in `Exercises/`, the 13 log notes in `Log/` (18 Oct to 17 Nov 2026), and the stub notes (Dashboard, Progress, Trophies, Exercise library). **Missing:** the weight history from 1 Feb 2026 (113 kg start) and `Plan/Tracking/Weight.md`; `Homepage.md`; the pre-plan. The only weight in the zip is 86.5 kg (and Waist 90 cm) on 18 Oct 2026. **This blocks the real Weigh-in conversion, not the spec, the tickets or the converter**, which accepts a plain `date,kg` file; the owner needs to extract the old daily-note weights into that form (or supply the files and the converter gets a reader). Until then the history file holds one Weigh-in.
- **The brief is out of date in three places, found while reading the zip:** (1) "No Workout history exists to migrate" is wrong: three real Workouts exist (2 Nov A, 3 Nov B, 5 Nov D) plus the 18 Oct Waist entry; this spec migrates them. (2) "The 27 Guides" is the count of notes with the standard shape; the vault has 36 notes and the converter produces one Guide per kept Exercise (41, because shared notes are copied to each Exercise they cover). (3) "About 40 Exercises" is right only with mobility removed (48 names in the Config, 41 after). The product brief should be corrected when this spec's decisions are confirmed.
- **Decisions needing the owner (each has a default above; none blocks `/to-tickets`):**
  1. **Is mobility dropped?** Default yes (the brief's starting Plan omits it; the new Session types have no mobility). If not, it needs a new Session type first.
  2. **Weight history source** (blocking for the real run, see above).
  3. **Migrate the real Workouts?** Default yes, as logged-afterwards Workouts.
  4. **`side_plank: 2.3`:** written as done with no Set (default) or a value you tell me (2.3 minutes? 23 seconds?).
  5. **Bodyweight not an Exercise:** the W Session carries Waist only (default), because weight is a Weigh-in.
  6. **Shared Guides** copied to each covered Exercise (default) or kept on one.
  7. **"Bodyweight deadlift" Badge** target fixed at the latest weigh-in (default).
  8. **Benchmark day** Saturday (default).
  9. **No tags** in the starter data (default).
  10. **Converted files stay out of git** (default, because the repo is public). Confirm that no bundled starter data is wanted.
- **Settings.** A first run uses `Settings.Default` (2.5 kg step, Streak minimum 3, no goal weight); the Plan overrides the step and Streak minimum, so the Streak behaves correctly with no Settings import. The goal weight has to be typed in Settings in B10. If the owner wants everything in one paste, the alternative is a complete Backup file built from these three, which Restore accepts and which includes Settings; it replaces everything, so it is only safe on a first run. Not done here (default); say so if wanted.
- **Possible small Core change, not in this stage unless the owner agrees:** an optional row-level text target (`target`) for per-side and timed prescriptions, and a Set field for text results. Both are optional fields (no format-version bump) and both are the same gaps B1's open items list. A5 works around them (cue text, report) and does not depend on them.
- **A2 assumption check.** `view.js` is now available. A first look shows the PR ranking matches (estimated one-rep max by the Epley formula, with bodyweight sets below loaded sets, which A2 already implements) and the Streak and week logic reads the same. The nine named assumptions in `a2-core-rules.md` (especially 4, the per-kind target steps) have not been compared line by line; doing so is a separate half-day task that needs no new inputs.
- **Proposed ordering for `/to-tickets`** (each about 150 to 200k tokens): (1) scaffold `StarterData` and `StarterData.Tests`, the Config reader (settings and tables), kind mapping, Exercise IDs, the "Sets × reps" reader and the fixture vault; (2) Plan, Phases, Sessions, rows and Badges output; (3) Guides conversion; (4) log notes, value parser, Weigh-ins and Workouts; (5) the report, the console entry point, the importer contract tests (three files in order, second import, byte-identical runs), the runbook `docs/starter-data.md`, `.gitignore`, and the manual-check entry. Ticket 4 is the likeliest to need splitting (value parsing versus Workout assembly).
- Each ticket must fit one agent in roughly 150 to 200k tokens (see `CLAUDE.md`).
