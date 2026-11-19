---
labels: ready-for-agent
stage: A5
---

# A5: Starter data

Parent docs: [`../product-brief.md`](../product-brief.md) ("Starting data (via Import)", "Starting Plan"), [`../technical-brief.md`](../technical-brief.md), [`../data-spec.md`](../data-spec.md), [`a4-export-import.md`](a4-export-import.md), [`../../GLOSSARY.md`](../../GLOSSARY.md). Builds on the A1 exchange format, the A2 rules and the A4 Import. A4 is merged (PR #17).

## Problem Statement

The app is empty on first run. The owner has about two months of real data in the Obsidian/Dataview system: a training Plan in `Config.md`, 33 Guide files, a few weigh-ins, and 13 Log files of which 4 hold real numbers. Nothing in the app can read those formats, and the app must not need them forever: Obsidian is being retired. Without starter data the owner would retype a 48-row Plan, Guides for 51 Exercises and 26 Badges on a phone, and the first-run screen (B10) would have nothing to offer.

The data also has two different privacy characters. The Plan, Exercises, Guides and Badges are the programme: they have to ship inside the public PWA anyway for First run. The Workouts and Weigh-ins are the owner's health history, and the GitHub repo is **public**, so they must never be committed or bundled.

## Solution

Stage A5 adds a small tested converter and the starter files it produces. No UI.

1. **A converter**, `tools/LegacyConverter` (a .NET console project that references `Core`, in the solution, with its own tests). It reads an Obsidian `Fitness` folder (`Config.md`, `Exercises/*.md`, `Log/*.md`) and writes exchange-format JSON through the real writer. It is deterministic: the same input gives byte-identical output, with hand-stable IDs, so re-importing says "nothing to change".
2. **Two public starter files**, committed and bundled as app assets for B10's First run:
   - `starter-plan.json`: a Plan-kind document with the Exercises, Phases, Sessions and the Plan. First Plan in an empty library, so it becomes active.
   - `starter-guides.json`: a Guides-kind document.
   - `starter-badges.json`: a Merge-kind document with the 20 Badges that map (below).
3. **One private file, never committed:** `my-history.json`, a Merge-kind document with the owner's Weigh-ins and past Workouts, every Workout flagged `loggedAfterwards: true`. The converter writes it to a git-ignored folder; the owner imports it on the phone through the Import screen (or the pre-B9 test harness).
4. **One seam for tests:** each generated file is run through the real `ImportInput.Inspect`, `ImportPreviewer.Preview` and `ImportApplier.ApplyAsync` against an in-memory `AppData`. A golden check asserts it is ready with no open choices, the expected counts, and a second import says "nothing to change".

## User Stories

1. As the owner, I want my current Plan (Sessions A to E, three Phases, benchmark every 4 weeks, start 19 Oct 2026, race 24 Apr 2027) to arrive through the normal Import, so that I never retype it.
2. As the owner, I want every Exercise from `Config.md` in the library with the right log kind, so that logging works the way it did.
3. As the owner, I want my existing Guides as starter Guides, so that the knowledge base is not empty on day one.
4. As the owner, I want a Guide that covered several Exercises (for example "Mobility and stretches") to appear on each of them, so that every Exercise's page has its content.
5. As the owner, I want my "My notes" text to arrive as personal notes only when it has content, so that empty bullets are not imported.
6. As the owner, I want my Badges carried over and editable, so that nothing I was working towards is lost.
7. As the owner, I want a Badge the new rules cannot measure left out and listed, not imported as a dead Badge.
8. As the owner, I want my real weigh-ins (113 kg start on 1 Feb 2026, 85.5 kg on 26 Sep, 86.5 kg on 18 Oct) and my waist reading in the app, so that the weight chart starts with my history.
9. As the owner, I want my four logged Workouts (A, B, D, and the waist day) in the app flagged "logged afterwards", so that past data is clearly past and the Streak is not rewritten by it.
10. As the owner, I want empty Log files ignored, so that no Workout with nothing in it appears.
11. As the owner, I want my personal data kept out of the public repo, so that my health history stays mine.
12. As the owner, I want re-running the converter and re-importing to report "nothing to change", so that I can fix the conversion and try again safely.
13. As the owner, I want a report of everything the converter dropped or guessed, so that I can check it in one read.
14. As a developer, I want the starter files proved against the real importer in tests, so that the converter, the format and the importer cannot drift.

## Implementation Decisions

Items marked **(default)** are proposals for the owner to confirm (see Further Notes).

### Shape
- **`tools/LegacyConverter`** (console, .NET 10, references `Core` only) plus `tools/LegacyConverter.Tests`. Not in `Core` (the old formats are not part of the product) and not a UI. Run by hand: `dotnet run --project tools/LegacyConverter -- <FitnessFolder> <outputFolder>`. **(default)**
- **Public output** goes to `src/Web/wwwroot/starter/` (committed, bundled, cached by the service worker). **Private output** goes to `.scratch/a5-starter-data/out/`, which is git-ignored (add the entry to `.gitignore`).
- **Parsing is internal; the public surface is one function** `LegacyConverter.Convert(LegacyFolder) -> ConversionResult` (the three starter documents, the private document, and a list of `ConversionNote`s). `LegacyFolder` is an in-memory set of file names and text, so tests need no file system. The console `Main` only reads the folder and writes the files.
- **Writing goes through `ExchangeFormat.Write`**, so output is always valid and indented. Nothing is hand-assembled JSON.

### The Plan (`starter-plan.json`)
- **Plan:** `id: hyrox-2027`, name "HYROX London", `startDate 2026-10-19`, `goal "HYROX London Doubles"`, `goalDate 2027-04-24`, `weeklyTarget 5`, `streakMinWorkoutsPerWeek 4`, `streakCountsCustomWorkouts false`, `weightIncrementKg 2`, active.
- **Phases:** Foundation (1 to 8), Build (9 to 19), HYROX prep (20 to 27). IDs `foundation`, `build`, `hyrox-prep`. No overrides and no Phase goals: the old system had none.
- **Sessions:** A Lower + core (monday, strength), B Upper + shoulders (tuesday, strength), C Engine + core (wednesday, cardio), D Full body (thursday, strength), E Long session (saturday, cardio), plus the benchmark. IDs `a-lower`, `b-upper`, `c-engine`, `d-full-body`, `e-long`, `benchmark`. **Cardio Sessions map to `type: cardio`; the old `body` type (W, Weigh-in) is not a Session** (see below).
- **Benchmark Session:** `type: test`, `benchmarkIntervalWeeks: 4`, rows from the old T rows. **Its `defaultDay` is `friday` (default):** the old config says only "Every 4 weeks", and Friday is the free day. Flagged for the owner.
- **Exercise rows:** every old row becomes a row in each Session that lists it (a row with `C, E, T` becomes a row in three Sessions). `sets` and `reps`/`repsMax` come from the old "Sets × reps" text by an explicit table in the converter, not a guess: `3 × 8–10` is sets 3, reps 8, repsMax 10; `2 × 8 per side` is sets 2, reps 8 with "per side" appended to the cue; `5 min`, `20–25 min` and similar duration text has no rep target and goes into the cue. A row it cannot read is a `ConversionNote` and a failing test, never a silent default.
- **Old row text that is not a number** (such as "From week 3. Rotate: SkiErg ...") stays in the cue unchanged.
- **The Weigh-in Session (W) is not carried over.** In the app a Weigh-in is a standalone action on any day with a Sunday prompt (product brief), so a Session for it would be double counting. The Bodyweight and Waist rows become Exercises (below).

### Exercises
- **One Exercise per distinct old name:** `Config.md` has 48 distinct names; dropping the 10 mobility ones and `Bodyweight` (a standalone Weigh-in, not an Exercise) leaves 37, plus 3 retired Exercises that only have Guides (Chest-supported row, Farmers carry, Incline DB press), kept so their Guides and old history survive. **IDs are the lowercase-hyphen form of the name** (`goblet-squat`, `kb-or-trap-bar-deadlift`, `skierg-1000-m`), generated by one function and pinned by a golden list in the tests.
- **Log kinds** map: `load` to `load`; `carry` to `carry`; `reps` to `reps`; `secs` to `seconds`; `mins` to `minutes`; `km` to `distanceKm`; `m` to `distanceM`; `time` to `time`; `kg` to `bodyMeasurementKg`; `cm` to `bodyMeasurementCm`; `text` to `text`; `-` to `tickOnly`. The converter fails on any other value.
- **Benchmark Exercises** (`isBenchmark: true`): the Exercises on the old T Session: SkiErg 1000 m, Plank max, Push-up max, Longest continuous run.
- **Tags** (muscle groups, equipment, movement pattern) are added by a table in the converter, one row per Exercise, written by the implementing agent from the Guide's "Works:" line and reviewed by the owner. The old system had none. **(default)** Empty tags are allowed, and a tag the table lacks is a note, not an error.
- **Retired Exercises** (Guide but no Session) are plain library Exercises. Kind is `load` for the two lifts and `carry` for Farmers carry.

### Guides (`starter-guides.json`)
- **Source:** each `Exercises/*.md`. The front-matter `exercises:` list names the old Exercise names it covers; the converter parses the headed sections (`Works`, `Why it's in your plan`, `Setup`, `How to do it`, `Cues`, `Common mistakes`, `Easier / harder`, `Watch-outs for you`, `My notes`) into the Guide fields (`works`, `whyInPlan`, `setup`, `howTo`, `cues`, `commonMistakes`, `easier`, `harder`, `watchOuts`, `personalNotes`). `Easier / harder` is split on its two bold labels. Numbered lists stay as text in `howTo`.
- **A Guide that covers several Exercises is written once per Exercise** with identical content **(default)**. The alternative (a Guide on the first Exercise only) leaves most pages empty.
- **`personalNotes`** are kept only when the "My notes" section has content beyond an empty bullet.
- **Privacy default:** the "Watch-outs for you" sections name the owner's own knee and back concerns. They ship in the public PWA. **Default: ship them** (the app is a single-user tool and the text is generic coaching); the owner can strip them with one converter flag, `--no-personal-watchouts`, which the tests cover. Flagged below.
- **No `videoUrl`** (the old Guides had none).

### Badges (`starter-badges.json`, a Merge document)
- **Mapped (20 of 26):** `sessions` 1, 10, 25, 50, 100 (`first-rep`, `ten-down`, `quarter-century`, `half-century`, `centurion`); `bestweek` 5; `streak` 4, 8, 16; `prs` 10, 50; and the Exercise-metric Badges with the metric set to the Exercise ID and the target in canonical units: `Longest continuous run` 20 minutes is 1200 seconds; `Plank max` 60 seconds; `Running distance` 5 and 10 km; `Push-up max` 20; `SkiErg 1000 m` 270 and 240 seconds (lower is better, handled by A2); `Waist` -3 and -6 (drop by this much).
- **Left out and listed in the report (6):** the four Badges tied to mobility (`Mobility week`, `Mobility month`, `50 mornings`, `100 mornings`), plus `Desk hero` which is tied to the deferred mobility Exercise **(default, see mobility)**; and `Bodyweight deadlift`, whose target is "your bodyweight". A Badge target is a number, so that one cannot be expressed. **Open choice:** replace it with a fixed number (the owner's current bodyweight, 86.5 kg) or drop it. Default: drop, noted.
- **Delivery is Merge,** because the Plan kind ignores Badges. IDs are the lowercase-hyphen form of the old name.

### Mobility: deferred (the biggest default)
The phone export contains a daily Morning mobility Session `M` (type `mobility`, day `Daily`), a separate daily mobility streak with its own setting (`mobility done percent: 75`), four mobility Badges, a Desk resets counter and ten stretch Exercises. **None of this exists in the data model or the rules:** there is no `mobility` Session type, no "daily" default day, no mobility streak and no `mobility days` Badge metric, and the product brief does not mention it. Adding it is a data-model and rules change (A1 and A2), not starter data.

**Default: A5 leaves mobility out entirely** and records it in the report and in `build-plan.md` as an open item for the owner. The ten mobility Exercises and the Morning mobility routine Guide are not imported. The 5 logged M Log files hold only empty or tick-only data, which is consistent with dropping them. **If the owner wants mobility kept, that is a separate stage** (A1/A2 extension plus its converter additions) which A5's converter is written to accept: the mobility rows are parsed and ignored, not skipped by omission, so adding them later is one switch.

### Private history (`my-history.json`, never committed)
- **Weigh-ins:** the start weight (113 kg, 2026-02-01, from `Life Settings`) and every `bodyweight` value in `Log/*.md` (86.5 kg on 2026-10-18), plus any extra weigh-ins the owner passes in a small `weights.csv` (`date,kg`). IDs are `w-yyyy-MM-dd`. The 85.5 kg reading of 26 Sep 2026 comes from the old daily note, so it is in `weights.csv`, not a Log file.
- **The converter does not read the Dataview daily notes.** Only one daily note survives on this machine and the older ones are not available, so history before 18 Oct is whatever the owner puts in `weights.csv`. **Open item:** the 1 Feb to 25 Sep weigh-ins are missing.
- **Workouts:** every Log file with at least one real value becomes a Workout, **always `loggedAfterwards: true`** (the owner's instruction: all past data is flagged as past). Empty shells and tick-only mobility files are skipped and listed.
  - `id: w-yyyy-MM-dd-<session code lower>`; `date` from the file; `sessionId` from the code (A to E), absent for W; no `startedAt`/`endedAt`; `planned` is copied from the Plan's rows for that Session at conversion time, so the snapshot rule holds.
  - Entries: each key in the Log's front matter is a lowercase-underscore Exercise name; the converter maps it back through the same ID function. A key with a value becomes a `done` entry; a key with an empty value is skipped (not a `skipped` entry: the owner never said it was skipped).
  - **Values:** `12×10` is one Set (`weightKg 12`, `reps 10`); `35 × 10` the same; `bwx8` is a Set with `weightKg 0` and `reps 8`; a plain number for a `seconds` Exercise is that many seconds; a plain number for `minutes` is minutes times 60; for `distanceKm` or `distanceM` the distance; `m:ss` for `time`.
  - **Side plank `2.3` is 2 Sets of 30 s** (confirmed by the owner on 19 Nov 2026): two Sets with `durationSeconds 30`. This is a one-off rule for that old value, written as a named special case with a test, because the old format's `2.3` is "sets . seconds / 10", not a general rule. Any other `secs` value of the same shape is a note, not a guess.
  - The old `done:` list (warm-up and similar tick items) is ignored: those rows are `tickOnly` Exercises and the old system stored them as a separate list. They are written as `done` entries with no Sets **(default)** so the Exercise counts as completed; the alternative is dropping them.
  - **The waist/bodyweight W file** becomes a Weigh-in (86.5 kg) and a Workout with no Session holding a `waist` entry (`measurementCm 90`), flagged `loggedAfterwards: true`.
- **Effect on rules:** A2 counts Workouts for Streak and Badges, so importing these four would count them. They are real past training, so that is correct; the Streak starts at the Plan start (19 Oct), which they all fall after. **(default)** Documented, not special-cased.
- **Delivery:** Merge, with a **preview** the owner reads before confirming. There is no ID overlap with starter data, so the preview is "adds N Workouts, M Weigh-ins".

### Delivery and order
First run (B10) imports `starter-plan.json` (Plan kind), `starter-guides.json` (Guides kind) and `starter-badges.json` (Merge kind), in that order, each with its normal preview. The owner then imports `my-history.json` once. The order matters because Guides and Badges refer to Exercises that the Plan file introduces; an Import in the wrong order is refused with `unknown_exercise`, which is already tested in A4 and needs no new rule.

### Report
The converter writes `conversion-report.md` beside the output: counts, every dropped item with the reason (mobility, the W Session, Bodyweight deadlift, empty Log files), every guess (benchmark day, tags), and any unknown Guide section text. The report is private and git-ignored.

## Testing Decisions

- **One seam, two layers.** (a) `LegacyConverter.Convert` over an in-memory `LegacyFolder` fixture (a small `Config.md`, three Guides, four Logs), asserting the JSON documents and notes. (b) Each produced document through the real `Inspect`, `Preview`, `Apply` over an in-memory `AppData`.
- **Fixtures are small and synthetic, committed.** The real owner files are **not** test inputs, because they are private and the repo is public. A second test, **skipped unless an environment variable names the real folder**, runs the converter on the owner's real files and asserts the counts below, so the owner can check the real conversion locally without committing it.
- **Plan cases:** Phases are gap-free; Sessions and rows match the table (a row listing `C, E, T` lands in three Sessions); every old "Sets × reps" form in the config parses to the table's value; an unreadable form fails; benchmark Session has interval 4; W is not a Session; the Plan is active and valid.
- **Exercise cases:** the ID function is pinned by a golden list of all names; every old log kind maps; an unknown log kind fails; benchmark flags; retired Exercises exist.
- **Guide cases:** section parsing for a complete Guide, a Guide missing sections, a multi-Exercise Guide written once per Exercise, an empty "My notes" bullet, a populated one; the `--no-personal-watchouts` flag.
- **Badge cases:** each mapped metric and canonical-unit conversion (minutes to seconds, lower-is-better time), each dropped Badge listed, the `Bodyweight deadlift` note.
- **History cases:** `12×10`, `35 × 10`, `bwx8`, plain `secs`/`mins`/`km`, `m:ss`; the `2.3` side-plank special case yields two 30 s Sets; empty keys skipped; empty Log files skipped; every Workout `loggedAfterwards: true`; the W file yields a Weigh-in and a waist Workout; `planned` snapshot copied; IDs stable.
- **Importer cases:** each document previews as ready with no open choices and the expected counts, applies, and a second import says "nothing to change". The order Plan, Guides, Badges, history is applied end to end into one `AppData` and the snapshot validates.
- **Real-folder check (opt-in):** 37 + 3 = 40 Exercises, 6 Sessions, 3 Phases, Guides on each covered Exercise, 20 Badges, 4 Workouts and 2 Weigh-ins from the folder (113 kg start and 86.5 kg), plus those in `weights.csv`.
- Fast and deterministic: no clock, no file system in the main tests, no browser.

## Out of Scope

- **Mobility** (Session type, daily default day, mobility streak, its Badges and Exercises): a model and rules decision for the owner; see above.
- **Any UI:** the First-run screen and the import screens are B9 and B10. A5 only supplies the files and proves the importer takes them.
- **Reading Dataview daily notes** or any other Obsidian format beyond `Config.md`, the Guide files and the Log files.
- **Settings import** (Merge ignores Settings): the Plan carries the Streak minimum and increment; the goal weight is not set (the old data has none).
- **Per-Phase progression rules, Phase goals and Session overrides:** none existed.
- **Weigh-ins from 1 Feb to 25 Sep 2026:** not available (see Further Notes).
- **Distance + time rows and the Text kind:** open data-model items already recorded from B1; the finisher `text` rows import but cannot yet be logged.

## Further Notes

- **Inputs found (19 Nov 2026).** `Fitness (2).zip` (the phone export): `Config.md` (newer than the vault copy), 33 Guide files, 13 Log files, `view.js`, `view.css`. The Obsidian vault: the older `Config.md`, 27 Guide files, `Plan/Tracking/Weight.md`, `Life Settings.md` and one daily note (2026-09-26, 85.5 kg). The pre-plan is in Downloads. Copies are under `.scratch/a5-starter-data/legacy/` (git-excluded, and not present in a fresh worktree).
- **Missing:** the weigh-ins from 1 Feb to 25 Sep 2026, and the Dataview daily notes. The phone export held none.
- **Defaults to confirm (change any in one place):**
  1. Mobility deferred entirely (the biggest one: it is the one place the old system is ahead of the app).
  2. Benchmark Session on **Friday**.
  3. Multi-Exercise Guides copied to each Exercise.
  4. "Watch-outs for you" (knee and back notes) shipped in the public app.
  5. `Bodyweight deadlift` Badge dropped (a Badge target must be a number).
  6. Tick-only items in the old `done:` list imported as empty `done` entries.
  7. Starter Badges delivered as a separate Merge file, since the Plan kind ignores them.
  8. A converter project (`tools/`) rather than a one-off script, so the conversion is tested and repeatable.
  9. Tags written by the implementing agent from each Guide's "Works" line, for the owner to review.
- **Session type mapping.** The old `cardio` and `strength` types count toward the week and Streak, which matches A2.
- **Public repo.** The repo is public. Only synthetic fixtures are committed. The real Log files and the weight history stay in `.scratch/` (ignored) and `my-history.json` is generated locally.
- **A2 assumptions.** The A2 spec's nine assumptions can now be checked against `view.js`, which is in `.scratch/a5-starter-data/legacy/phone-2026-11-19/_scripts/fitness/`. A5's tickets include a short pass for that; any difference becomes an open item, not a silent change.
- **Proposed ordering for `/to-tickets`** (each about 150 to 200k tokens): (1) converter project, `LegacyFolder`, config table parsing, the ID function, log-kind map and Exercises; (2) Plan, Phases, Sessions and "Sets × reps" parsing; (3) Guide parsing and the multi-Exercise rule; (4) Badges; (5) history (Log parsing, value rules, Weigh-ins, the `2.3` case, `weights.csv`); (6) the importer proof tests, the opt-in real-folder check, the bundled assets and the report; (7) the A2 assumption pass against `view.js` and the docs.
- Each ticket must fit one agent in roughly 150 to 200k tokens (see `CLAUDE.md`).
