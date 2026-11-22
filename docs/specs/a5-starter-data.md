---
labels: ready-for-agent
stage: A5
---

# A5: Example and test dataset (converted from the Obsidian data)

Parent docs: [`../product-brief.md`](../product-brief.md) ("Starting data (via Import)", "Starting Plan"), [`../technical-brief.md`](../technical-brief.md), [`../data-spec.md`](../data-spec.md), [`a4-export-import.md`](a4-export-import.md), [`a4-5-tracks.md`](a4-5-tracks.md), [`../adr/0007-explicit-tracks-and-format-version-2.md`](../adr/0007-explicit-tracks-and-format-version-2.md), [`../../GLOSSARY.md`](../../GLOSSARY.md). Builds on the A1 exchange format (version 2), the A2 rules and the A4 Restore. **Depends on [A4.5 PR 1](a4-5-tracks.md) only** (the model, the upgrader and the importer; not the screens). The stage's name is kept for its links.

**Repurposed on 22 Nov 2026.** This stage used to deliver the owner's real starting data (public starter files plus a private history file, through three Imports). The owner will now **wipe all current data** and collect new real data when the app goes into use (via the prompt pack and an LLM). The old Obsidian data, converted, becomes an **example and test dataset**: one complete Backup-style file that is restored to exercise and test the app.

## Problem Statement

Once the screens exist, nothing exercises them with realistic data: an empty app cannot show a 27-week Plan with Phases, a daily mobility Track, 50 Exercises, Guides, Badges, Weigh-ins and past Workouts, and tests written against tiny fixtures miss what a real-sized dataset does to Home, the Week view, the Plan editor and the rules. The owner has about two months of data in the Obsidian/Dataview system (a Plan in `Config.md`, 33 Guide files, a few weigh-ins, 13 Log files of which 4 hold real numbers) in formats nothing in the app can read.

The data has two privacy characters. The Plan, Exercises, Guides and Badges are the programme, but the Workouts and Weigh-ins are the owner's health history, and the GitHub repo is **public**. The whole example dataset therefore stays out of the repo.

## Solution

Stage A5 adds a small tested converter and the file it produces. No UI.

1. **A converter**, `tools/LegacyConverter` (a .NET console project that references `Core`, in the solution, with its own tests). It reads an Obsidian `Fitness` folder (`Config.md`, `Exercises/*.md`, `Log/*.md`, optional `weights.csv`) and writes **one exchange-format document through the real writer** (format 2). It is deterministic: the same input gives byte-identical output with hand-stable IDs.
2. **One output, `example-dataset.json`**: a complete Backup-style document (Exercises, Settings, the Plan with its Tracks, Guides, Badges, Weigh-ins, Workouts). It is **never committed** and **not bundled**; the converter writes it to a git-ignored folder. The owner restores it on the phone (or the pre-B9 test harness) through Restore, which replaces everything after the automatic Backup.
3. **Proof against the real code:** the produced document goes through the real `ImportInput.Inspect`, `ImportPreviewer.Preview` (Restore kind) and `ImportApplier.ApplyAsync` against an in-memory `AppData`; tests assert it is ready, the counts, that the snapshot validates, and that the rules produce the expected results on it.
4. **Synthetic fixtures** are the only committed test inputs; a real-folder check is opt-in.

Because the owner will start again with new real data, the earlier public starter assets (`starter-plan.json`, `starter-guides.json`, `starter-badges.json` bundled for First run) are **dropped**. B10's First run will not offer a bundled starter Plan; its content is decided when Data and settings is specced (a Plan through Import or the prompt pack).

## User Stories

1. As the owner, I want my old Plan (Sessions A to E, three Phases, benchmark every 4 weeks, start 19 Oct 2026, race 24 Apr 2027) and my daily mobility routine in one example dataset, so that the app can be exercised with a realistic Plan with two Tracks.
2. As the owner, I want every Exercise from `Config.md` in the library with the right log kind, so that logging works the way it did.
3. As the owner, I want my existing Guides on the Exercises they covered, so that the knowledge base is full.
4. As the owner, I want a Guide that covered several Exercises to appear on each of them, so that every Exercise's page has content.
5. As the owner, I want "My notes" text kept only when it has content, so that empty bullets are not imported.
6. As the owner, I want my Badges carried over, with the mobility Badges using the Track metrics, and a Badge the new rules cannot measure left out and listed.
7. As the owner, I want my real weigh-ins and waist reading and my five logged Workouts (flagged "logged afterwards") in the dataset, so that charts and Home have history to show.
8. As the owner, I want empty Log files ignored.
9. As the owner, I want the whole file kept out of the public repo, so that my health history stays mine.
10. As the owner, I want to restore it with one Restore after a wipe and to restore it again at will to reset the app to a known state.
11. As the owner, I want re-running the converter to give byte-identical output, so that I can fix the conversion and try again safely.
12. As the owner, I want a report of everything the converter dropped or guessed, so that I can check it in one read.
13. As a developer, I want the dataset proved against the real importer and the real rules in tests, so that the converter, the format and the importer cannot drift.

## Implementation Decisions

Items marked **(default)** are proposals for the owner to confirm (see Further Notes).

### Shape
- **`tools/LegacyConverter`** (console, .NET 10, references `Core` only) plus `tools/LegacyConverter.Tests`. Not in `Core` (the old formats are not part of the product) and not a UI. Run by hand: `dotnet run --project tools/LegacyConverter -- <FitnessFolder> <outputFolder>`. **(default)**
- **Output** goes to `.scratch/a5-starter-data/out/` (git-ignored; add the entry to `.gitignore`): `example-dataset.json` and `conversion-report.md`. Nothing is written under `src/` and nothing is bundled.
- **Parsing is internal; the public surface is one function** `LegacyConverter.Convert(LegacyFolder) -> ConversionResult` (the dataset document and a list of `ConversionNote`s). `LegacyFolder` is an in-memory set of file names and text, so tests need no file system. The console `Main` only reads the folder and writes the files.
- **Writing goes through `ExchangeFormat.Write`** (format 2), so output is always valid and indented. Nothing is hand-assembled JSON.
- **Settings in the document:** the Backup includes Settings, built from `Settings.Default` with the Plan's Streak values copied (minimum 4, Custom workouts off) and weight increment 2 kg; no goal weight (the old data has none).

### The Plan (explicit Tracks)
- **Plan:** `id: hyrox-2027`, name "HYROX London", `startDate 2026-10-19`, `goal "HYROX London Doubles"`, `goalDate 2027-04-24`, `weightIncrementKg 2`, active.
- **Main Track:** ID `main`, name "Main", weekly, `weeklyTarget 5`, `streakMinWorkoutsPerWeek 4`, `streakCountsCustomWorkouts false`. It holds A to E and the benchmark. Phases: Foundation (1 to 8), Build (9 to 19), HYROX prep (20 to 27); IDs `foundation`, `build`, `hyrox-prep`; no overrides, no Phase goals (the old system had none). **Default:** gym and cardio stay combined on the main Track because that is how the old Streak counted (4 of 5); splitting them is a Plan edit the owner can make later.
- **Mobility Track:** ID `mobility`, name "Morning mobility", `cadence: daily`, `dayDonePercent: 75` (from the old `mobility done percent`). Holds the daily Session `m-mobility` (type `mobility`, `defaultDay: daily`) with the ten old M rows in their old order. **No Phases of its own** (it simply has none; there is no fallback to the main Track's).
- **Sessions:** A Lower + core (monday, strength), B Upper + shoulders (tuesday, strength), C Engine + core (wednesday, cardio), D Full body (thursday, strength), E Long session (saturday, cardio), the benchmark and the daily Morning mobility. IDs `a-lower`, `b-upper`, `c-engine`, `d-full-body`, `e-long`, `benchmark`, `m-mobility`. Cardio Sessions map to `type: cardio`; the old `body` type (W, Weigh-in) is not a Session (below). Every Session has its `trackId`.
- **Benchmark Session:** `type: test`, `benchmarkIntervalWeeks: 4`, rows from the old T rows, on the main Track. **`defaultDay` is `friday` (default):** the old config says only "Every 4 weeks", and Friday is the free day.
- **Exercise rows:** every old row becomes a row in each Session that lists it (a row with `C, E, T` becomes a row in three Sessions). `sets` and `reps`/`repsMax` come from the old "Sets × reps" text by an explicit table in the converter, not a guess: `3 × 8–10` is sets 3, reps 8, repsMax 10; `2 × 8 per side` is sets 2, reps 8 with "per side" appended to the cue; `5 min`, `20–25 min` and similar duration text has no rep target and goes into the cue. A row it cannot read is a `ConversionNote` and a failing test, never a silent default. Old row text that is not a number (such as "From week 3. Rotate: SkiErg ...") stays in the cue unchanged.
- **The Weigh-in Session (W) is not carried over.** A Weigh-in is a standalone action in the app, so a Session for it would double count. The Bodyweight and Waist rows become Exercises (below).

### Exercises
- **One Exercise per distinct old name:** `Config.md` has 48 distinct names; dropping `Bodyweight` (a standalone Weigh-in) leaves 47, including the 10 mobility ones, plus 3 retired Exercises that only have Guides (Chest-supported row, Farmers carry, Incline DB press), kept so their Guides and old history survive. **IDs are the lowercase-hyphen form of the name** (`goblet-squat`, `kb-or-trap-bar-deadlift`, `skierg-1000-m`), from one function pinned by a golden list in the tests.
- **Log kinds** map: `load` to `load`; `carry` to `carry`; `reps` to `reps`; `secs` to `seconds`; `mins` to `minutes`; `km` to `distanceKm`; `m` to `distanceM`; `time` to `time`; `kg` to `bodyMeasurementKg`; `cm` to `bodyMeasurementCm`; `text` to `text`; `-` to `tickOnly`. The converter fails on any other value.
- **Benchmark Exercises** (`isBenchmark: true`): the Exercises on the old T Session: SkiErg 1000 m, Plank max, Push-up max, Longest continuous run.
- **Tags** (muscle groups, equipment, movement pattern) come from a table in the converter, written by the implementing agent from each Guide's "Works:" line and reviewed by the owner. **(default)** Empty tags are allowed, and a tag the table lacks is a note, not an error.
- **Retired Exercises** are plain library Exercises (kind `load` for the two lifts, `carry` for Farmers carry).

### Guides
- **Source:** each `Exercises/*.md`. The front-matter `exercises:` list names the old Exercise names it covers; the converter parses the headed sections (`Works`, `Why it's in your plan`, `Setup`, `How to do it`, `Cues`, `Common mistakes`, `Easier / harder`, `Watch-outs for you`, `My notes`) into the Guide fields (`works`, `whyInPlan`, `setup`, `howTo`, `cues`, `commonMistakes`, `easier`, `harder`, `watchOuts`, `personalNotes`). `Easier / harder` is split on its two bold labels. Numbered lists stay as text in `howTo`.
- **A Guide that covers several Exercises is written once per Exercise** with identical content **(default)**.
- **`personalNotes`** are kept only when "My notes" has content beyond an empty bullet.
- **Privacy:** the dataset is private, so "Watch-outs for you" (the owner's knee and back notes) are kept. The `--no-personal-watchouts` flag is no longer needed and is dropped.
- **No `videoUrl`** (the old Guides had none).

### Badges
- **Mapped (25 of 26):** `sessions` 1, 10, 25, 50, 100 (`first-rep`, `ten-down`, `quarter-century`, `half-century`, `centurion`); `bestweek` 5; `streak` 4, 8, 16 (the main Track, the old meaning); `prs` 10, 50; the four mobility Badges using the Track metrics (`Mobility week` is `streak:mobility` 7, `Mobility month` `streak:mobility` 30, `50 mornings` `days:mobility` 50, `100 mornings` `days:mobility` 100); `Desk hero` (`desk-resets`, target 6); and the Exercise-metric Badges with the metric set to the Exercise ID and the target in canonical units: `Longest continuous run` 20 minutes is 1200 seconds; `Plank max` 60 seconds; `Running distance` 5 and 10 km; `Push-up max` 20; `SkiErg 1000 m` 270 and 240 seconds (lower is better, handled by A2); `Waist` -3 and -6 (drop by this much).
- **Left out and listed in the report (1):** `Bodyweight deadlift`, whose target is "your bodyweight" (a Badge target is a number). IDs are the lowercase-hyphen form of the old name.

### Mobility (the daily Track)
The phone export has a daily Morning mobility Session `M` (day `Daily`), its own daily streak setting (`mobility done percent: 75`), four mobility Badges, a Desk resets counter and ten mobility Exercises. They become the `mobility` Track above. `Desk resets` is a `reps` Exercise; `Lunch walk` and the stretches are tick-only; the `Morning mobility routine` Guide is written to each of its eight Exercises. Only `2026-10-27 M` has a value (Cat-cow ticked), so it becomes one past mobility Workout (1 of 10 rows, below the 75% line, so not a done day).

### History (Weigh-ins and Workouts, in the same document)
- **Weigh-ins:** the start weight (113 kg, 2026-02-01, from `Life Settings`) and every `bodyweight` value in `Log/*.md` (86.5 kg on 2026-10-18), plus extra weigh-ins from an optional `weights.csv` (`date,kg`), which is where the 85.5 kg reading of 26 Sep 2026 comes from. IDs are `w-yyyy-MM-dd`. The converter does not read the Dataview daily notes; the 1 Feb to 25 Sep weigh-ins are missing (open item, not a blocker: this is example data).
- **Workouts:** every Log file with at least one real value becomes a Workout, **always `loggedAfterwards: true`**. Empty shells are skipped and listed. A tick-only file with a ticked row (the `2026-10-27 M` file) is kept.
  - `id: w-yyyy-MM-dd-<session code lower>`; `date` from the file; `sessionId` from the code (A to E, M), absent for W; no `startedAt`/`endedAt`; `planned` is copied from the Plan's rows for that Session at conversion time (the snapshot rule).
  - Entries: each front-matter key is a lowercase-underscore Exercise name, mapped back through the ID function. A key with a value becomes a `done` entry; an empty value is skipped (not a `skipped` entry).
  - **Values:** `12×10` is one Set (`weightKg 12`, `reps 10`); `35 × 10` the same; `bwx8` is a Set with `weightKg 0` and `reps 8`; a plain number for a `seconds` Exercise is that many seconds; for `minutes`, minutes times 60; for `distanceKm` or `distanceM` the distance; `m:ss` for `time`.
  - **Side plank `2.3` is 2 Sets of 30 s** (confirmed 19 Nov 2026): a named special case with a test; any other value of that shape is a note.
  - The old `done:` list (warm-up and similar tick items) becomes `done` entries with no Sets **(default)**.
  - **The waist/bodyweight W file** becomes a Weigh-in (86.5 kg) and a Workout with no Session holding a `waist` entry (`measurementCm 90`).
- **Effect on rules:** A2 counts these Workouts for Streak and Badges, which is correct for real past training; documented, not special-cased.

### Delivery
The owner restores `example-dataset.json` through Restore (B9; until then, the test harness). Restore replaces everything after the automatic Backup, so it is also the way to reset the app to this known state at any time. There is no ordering problem (one document), and no First-run involvement.

### Report
`conversion-report.md` beside the output (git-ignored): counts, every dropped item with the reason (the W Session, Bodyweight deadlift, empty Log files), every guess (benchmark day, tags), and any unknown Guide section text.

## Testing Decisions

- **One seam, two layers.** (a) `LegacyConverter.Convert` over an in-memory `LegacyFolder` fixture (a small `Config.md`, three Guides, four Logs), asserting the document and notes. (b) The produced document through the real `Inspect`, `Preview` (Restore) and `Apply` over an in-memory `AppData`, then the real rules over the resulting snapshot.
- **Fixtures are small, synthetic and committed.** The real owner files are **not** test inputs. A second test, **skipped unless an environment variable names the real folder**, runs the converter on the real files and asserts the counts below, so the owner can check locally.
- **Plan cases:** Phases gap-free on the main Track; the mobility Track is daily with 75 and no Phases; every Session has a `trackId`; Sessions and rows match the table; every old "Sets × reps" form parses; an unreadable form fails; benchmark interval 4 on the main Track; W is not a Session; the Plan is active and valid; reading the output with the v2 reader gives no issues.
- **Exercise cases:** the ID function pinned by a golden list; every log kind maps; an unknown kind fails; benchmark flags; retired Exercises exist.
- **Guide cases:** a complete Guide, a Guide missing sections, a multi-Exercise Guide written once per Exercise, an empty and a populated "My notes".
- **Badge cases:** each mapped metric and unit conversion, the Track metrics reference the `mobility` Track, the dropped Badge is listed.
- **History cases:** `12×10`, `35 × 10`, `bwx8`, plain `secs`/`mins`/`km`, `m:ss`; the `2.3` case; empty keys skipped; empty Logs skipped; every Workout `loggedAfterwards: true`; the W file; `planned` copied; IDs stable; the mobility Workout is below the day-done line.
- **Restore and rules cases:** the document previews as ready with no open choices, applies into an in-memory `AppData`, the snapshot validates, a second Restore gives the same snapshot; the daily mobility run, days done, main Track Streak and Phase position come out as the fixture predicts; byte-identical output on a second `Convert`.
- **Real-folder check (opt-in):** 47 + 3 = 50 Exercises, 7 Sessions on 2 Tracks, 3 Phases on the main Track, Guides on each covered Exercise, 25 Badges, 5 Workouts and 2 Weigh-ins from the folder plus those in `weights.csv`.
- Fast and deterministic: no clock, no file system in the main tests, no browser.

## Out of Scope

- **Public or bundled starter files and First run content:** dropped (B10 decides).
- **The Track model and the v2 format:** A4.5. A5 only writes data in that shape.
- **Any UI:** the Restore screen is B9.
- **Reading Dataview daily notes** or any other Obsidian format beyond `Config.md`, the Guide files, the Log files and `weights.csv`.
- **Real data for use:** the owner will collect and generate new data later (prompt pack and an LLM).
- **Weigh-ins from 1 Feb to 25 Sep 2026:** not available.
- **Distance + time rows and the Text kind:** open data-model items from B1.

## Further Notes

- **Order and queueing.** Depends on A4.5 PR 1 only. Runs one implementer at a time, never in parallel with other implementers (the owner is usage-constrained); it can be queued right after PR 1 or after Data and settings, before A4.5 PR 2. The seven tickets below form a chain, each blocked by the one before.
- **Inputs found (19 Nov 2026).** `Fitness (2).zip` (the phone export): `Config.md` (newer than the vault copy), 33 Guide files, 13 Log files, `view.js`, `view.css`. The Obsidian vault: the older `Config.md`, 27 Guide files, `Plan/Tracking/Weight.md`, `Life Settings.md` and one daily note (2026-09-26, 85.5 kg). Copies are under `.scratch/a5-starter-data/legacy/` in the **main checkout** (git-excluded, so absent from a fresh worktree; `phone-2026-11-19/` is the newest). **They must never be committed (the repo is public).** If the folder is gone, the originals are `Fitness (2).zip` in Downloads and the vault at `Documents\Obsidian Brain\Second Brain\Beta\99 Life Planning\`.
- **Owner's answers (19 Nov 2026), still standing:** benchmark on Friday; Guides copied onto each covered Exercise; `Bodyweight deadlift` dropped; side plank `2.3` is two 30 s Sets; all past data `loggedAfterwards: true`.
- **Defaults to confirm (change any in one place):** (1) gym and cardio combined on the main Track, mobility its own daily Track; (2) benchmark Session on Friday; (3) multi-Exercise Guides copied to each Exercise; (4) tick-only `done:` items imported as empty `done` entries; (5) a converter project under `tools/` rather than a one-off script; (6) tags written by the implementing agent for the owner to review; (7) **the whole dataset is private and not bundled, and the earlier bundled starter files are dropped** (so B10 First run has no starter Plan: to confirm when Data and settings is specced).
- **A2 assumptions.** The A2 spec's nine assumptions can be checked against `view.js` (in the legacy folder); the last ticket includes a short pass, and any difference becomes an open item, not a silent change.
- **Proposed tickets for `/to-tickets`** (a sequential chain, each about 150 to 200k tokens): (1) converter project, `LegacyFolder`, config table parsing, the ID function, log-kind map and Exercises; (2) the Plan with its two Tracks, Phases, Sessions and "Sets × reps" parsing; (3) Guide parsing and the multi-Exercise rule; (4) Badges; (5) history (Log parsing, value rules, Weigh-ins, the `2.3` case, `weights.csv`); (6) the single-document assembly, the Restore and rules proof tests, the opt-in real-folder check and the report; (7) the A2 assumption pass against `view.js` and the docs.
- Each ticket must fit one agent in roughly 150 to 200k tokens (see `CLAUDE.md`).
