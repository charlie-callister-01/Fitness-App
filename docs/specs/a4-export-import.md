---
labels: ready-for-agent
stage: A4
---

# A4: Export, Import and prompt pack

Parent docs: [`../product-brief.md`](../product-brief.md) ("Moving data in and out"), [`../technical-brief.md`](../technical-brief.md) ("Data and exchange format"), [`../data-spec.md`](../data-spec.md), [`../adr/0001-schema-first-with-permanent-ids-and-versioned-exchange-format.md`](../adr/0001-schema-first-with-permanent-ids-and-versioned-exchange-format.md), [`../adr/0005-single-writer-quarantine-and-two-storage-versions.md`](../adr/0005-single-writer-quarantine-and-two-storage-versions.md), [`../../GLOSSARY.md`](../../GLOSSARY.md). Builds on the A1 exchange format, the A2 rules (the Backup Nudge) and the A3 `AppData` API. A3 is a draft PR (#14) at the time of writing; A4 branches from `a3-storage` and rebases onto `main` once it merges.

## Problem Statement

The data now survives restarts, but it can only leave the device, or arrive from outside, through code that does not exist yet. That blocks the three things the owner asked for in the grilling: a Backup that protects years of history from the browser clearing its storage, an Export the owner can paste into a chat tool to have an LLM analyse training, weight and pain, and an Import so an LLM (or the old Obsidian data, in A5) can write a Plan, Guides or small corrections that land in the app without retyping. LLM-written data can be wrong, so none of this is safe unless Import shows plainly what it will change, saves a Backup first, names the exact bad field when it refuses, and never merges an Exercise it is only guessing about. Until these exist as tested, UI-free logic, the Export, Import and Backup screens (Track B) have nothing to call and A5's starter data has no way in.

## Solution

Stage A4 adds one `Core` namespace, `Core.Exchange`, with three parts. Everything is pure text-in, text-out logic over `DataSnapshot` plus `AppData` for applying; there is no UI, no file I/O and no browser code.

1. **Export.** An `ExportRequest` (date range, which kinds of data, JSON or Markdown) and a snapshot give an `ExportResult`: the text, a suggested file name, a plain summary of what is in it, and whether it counts as a Backup. JSON is the exchange format with its `readMe` block; Markdown is a read-only rendering for chats.
2. **Import.** Pasted text goes through three pure steps, then one write: *inspect* (clean the paste, read it with the versioned reader, suggest an Import kind), *preview* (match Exercises, work out every change, validate the result, write the plain-language list), then *apply* (save an automatic Backup through a host-supplied sink, then commit atomically). Restore a Backup, add a Plan as a new Plan, merge small changes and import Guides are four rule sets over the same document.
3. **Prompt pack.** Three ready-made prompts (analyse my training, write me a Plan, write Guides) whose text carries the format reference, a worked example and the current Exercise library.

The owner sees this as: tick what to share, copy it; paste what an LLM returned, read a list of what will change, confirm, done; and a clear message naming the field whenever a file is not acceptable.

## User Stories

### Export
1. As the owner, I want an Export of the last 12 weeks by default, so that "analyse my training" is one tap.
2. As the owner, I want to choose any date range, so that I can export a block, a month or everything.
3. As the owner, I want tick boxes for Workouts, Plan, Weigh-ins, Pain checks, Guides and Exercises, so that I choose what leaves the app.
4. As the owner, I want Guides off by default and the rest on, so that a quick Export is useful but nothing odd leaves by accident.
5. As the owner, I want unticking Pain checks to remove them from every Workout in the Export, so that I can leave health data out before pasting into a chat tool.
6. As the owner, I want an Export that names Exercises by ID to always carry those Exercises' names, so that the file is readable and importable on its own.
7. As the owner, I want "export everything" to produce a Backup that holds every kind of data (including Settings, Breaks, Badges, all Plans, Week schedules and personal notes), so that restoring it gives me the whole app back.
8. As the owner, I want a JSON Export that starts with a "how to read this" block, so that an LLM given the bare file understands it.
9. As the owner, I want a Markdown Export that reads like a training log, so that I can paste it into a chat and get a sensible answer.
10. As the owner, I want a suggested file name that says what it is and when, so that my downloads folder makes sense.
11. As the owner, I want a one-line summary of what is in an Export (counts and the date range) before I copy it, so that I can check it.
12. As the owner, I want making a Backup to reset the 14-day Backup Nudge, so that the Nudge reflects what I did.

### Import: reading what was pasted
13. As the owner, I want to paste an LLM's reply as it came, including a markdown code fence or a sentence before and after the JSON, and have the app find the JSON, so that I do not have to trim it by hand on a phone.
14. As the owner, I want a message that says I pasted extra text and it was ignored, so that I know what the app used.
15. As the owner, I want a refused file to name the exact field and say what is wrong in plain language, so that I can fix it or ask the LLM to.
16. As the owner, I want a long list of problems cut to the first few with a count of the rest, so that the message fits a phone screen.
17. As the owner, I want a file from a newer app version refused with "update the app", and a file from an older version read as normal, so that format changes never strand a Backup.
18. As the owner, I want the app to suggest which kind of Import a file is (Backup, Plan, Guides or merge) and let me change it, so that a wrong guess costs me nothing.

### Import: preview and safety
19. As the owner, I want a plain-language preview ("adds 3 Exercises, changes the target on 2, creates a new Plan called X, touches no Workouts") before anything changes, so that I know what I am agreeing to.
20. As the owner, I want nothing changed until I confirm, so that opening an Import is always safe.
21. As the owner, I want a Backup saved automatically just before an Import changes anything, so that a bad Import can be undone by restoring it.
22. As the owner, I want the Import to stop, changing nothing, if that Backup could not be saved, so that I am never changed without a safety copy.
23. As the owner, I want an Import that would break my data (a Plan that drops a Session my Workouts used, an unknown Exercise) refused with the reason, so that bad data never gets in.
24. As the owner, I want an Import whose data changed since I looked at the preview to ask me to look again, so that I never apply a stale preview.
25. As the owner, I want importing the same file twice to say "nothing to change" the second time, so that re-pasting is harmless.

### Import: Exercise matching
26. As the owner, I want an Exercise in the file with the same ID as one in my library to be the same Exercise, so that IDs mean what they say.
27. As the owner, I want an Exercise whose name matches one in my library exactly (ignoring case and spacing) to be treated as that Exercise, so that an LLM that forgot the ID still lines up.
28. As the owner, I want a near-match ("Goblet squats" for "Goblet squat") offered as a question, with "add as a new Exercise" as the alternative, and never merged without my answer, so that history is never joined to the wrong movement.
29. As the owner, I want an Exercise nothing matches offered as new, so that new movements come in cleanly.
30. As the owner, I want every reference in the file (Plan rows, Phase goals, Workouts, Guides, Badges) to follow my choice, so that matching one Exercise fixes it everywhere.
31. As the owner, I want an Import never to change an existing Exercise's log kind, so that history is not reinterpreted.

### Import: the four kinds
32. As the owner, I want restoring a Backup to replace everything in one step and to say how much current data will be lost, so that I never restore by accident over newer work.
33. As the owner, I want a file that is not a complete Backup refused as a restore, naming what is missing, so that a partial file cannot wipe my data.
34. As the owner, I want a Plan Import to add the Plan as a new inactive Plan I can select later (or active if it is my first), so that an imported Plan never silently replaces the one I am following.
35. As the owner, I want a Plan Import to give the new Plan, Phases and Sessions fresh IDs where mine collide, so that nothing is overwritten.
36. As the owner, I want a merge to add new records and update existing ones by ID (new Exercises, edited targets, back-filled Workouts, Weigh-ins, Breaks, Badges), so that small corrections are easy.
37. As the owner, I want a merge never to delete anything, so that a partial file cannot remove data.
38. As the owner, I want an existing Workout with the same ID but different content kept as it is unless I choose to replace it, so that history is not rewritten by accident.
39. As the owner, I want a warning when a back-filled Workout lands on a date and Session I already have a Workout for, so that I do not log it twice by mistake.
40. As the owner, I want a Guides Import to add Guides for Exercises that have none and to let me choose keep or replace for each existing one, so that I control what an LLM overwrites.
41. As the owner, I want my personal notes never overwritten by an Import, so that my own words are safe.
42. As the owner, I want anything in the file that the chosen kind does not import (Workouts in a Guides file, Settings in a merge) listed as ignored, so that I know it was not used.

### Prompt pack
43. As the owner, I want an "Analyse my training" prompt that carries an Export, so that I can paste one block into a chat.
44. As the owner, I want a "Write me a Plan" prompt that carries the Plan format and my Exercise library, so that the LLM reuses my Exercise IDs and the result imports cleanly.
45. As the owner, I want a "Write Guides" prompt for chosen Exercises (by default those without a Guide), so that I can fill the knowledge base.
46. As the owner, I want each prompt to carry a worked example that the app itself can read, so that what the LLM is shown is what the importer accepts.
47. As the owner, I want the prompts to tell the LLM to reply with the JSON only, so that the paste box gets what it expects.

### Restore and the Backup date
48. As a developer, I want restoring a Backup and recording its time to be one atomic write, so that a failure between them cannot leave the data restored with a stale Backup date.

### Quality
49. As a developer, I want every Export to read back through the real reader and validate, and every example the prompts show to pass the real Import, so that the three documents (spec, prompts, code) cannot drift.
50. As a developer, I want the whole stage testable through one public surface with no browser, no file system and no real clock.

## Implementation Decisions

Items marked **(default)** are proposals for the owner to confirm (see Further Notes); each can be changed in one place.

### Shape
- **One namespace, `Core.Exchange`, inside `Core`.** No new project, no new dependency. `Ui` and `Web` only call it. Delivery (download, copy, share-sheet, the file picker) is a host concern: Core returns text and a suggested file name and takes text.
- **Public surface (the one test seam):** `Exporter.Export(snapshot, request, exportedFrom, now)`; `ImportInput.Inspect(text)`; `ImportPreviewer.Preview(snapshot, inspected, kind, decisions)`; `ImportApplier.ApplyAsync(appData, preview, backupSink, now)`; `PromptPack.Analyse/WritePlan/WriteGuides(...)`. Matching, remapping, Markdown rendering and the summary wording are internal.
- **Pure first, one write last.** Export, inspect, preview and the prompts are pure functions (data in, value out, injected time). Only `ApplyAsync` writes, and only through `AppData`. Nothing here reads a clock or touches storage directly.
- **The preview is the truth.** `ImportPreviewer` builds the `Change` list, applies it to a copy of the snapshot and runs the A1 validator over the result. Validator issues become blocking items in plain words. `ApplyAsync` commits that same change list, and `AppData` validates again, so the preview cannot promise what the commit would reject.
- **Records are compared by their stored JSON** (the descriptor's `ToStored`), not by record equality, because the records hold lists and do not compare structurally. Equal means "nothing to change".

### Export
- **`ExportRequest`:** an inclusive date range (or none, meaning all time), the six ticks (Workouts, Plan, Weigh-ins, Pain checks, Guides, Exercises) and the format (JSON or Markdown). `ExportRequest.Default(today)` is the last 12 weeks (the 84 days ending today), Guides off, the other five on **(default)**. `ExportRequest.Backup` is the "everything" request: all time, all data, JSON. `PastDays(today, n)` gives the Weekly review's "past week".
- **What each tick means:**
  - *Workouts:* Workouts dated in range, with their `planned` snapshot, Sets, RPE and skip/swap data. Pain checks are inside them (below).
  - *Pain checks:* when unticked, `painCheck` is removed from every exported Workout. When ticked without Workouts, the Export holds Workouts reduced to date, Session and Pain check (no entries), so health data can be shared alone.
  - *Plan:* the active Plan only, whole (Plans are never date-filtered), plus the Week schedules and Breaks that overlap the range, because they explain the weeks. **(default)** Settings, Badges and the inactive Plans are Backup-only.
  - *Weigh-ins:* Weigh-ins dated in range.
  - *Guides:* the Guides of every exported Exercise, **without personal notes** **(default)**; a Backup keeps them.
  - *Exercises:* the whole library. When unticked, the Exercises referenced by the other exported data are included anyway (an Export that names IDs without names is useless to an LLM and cannot be imported), and the summary says "only the Exercises used".
- **Backup:** a request is a Backup only when it is all time with every kind ticked, built through `ExportRequest.Backup`; the document then holds every collection (empty ones as `[]`, never absent; `weekSchedules` included) and Settings. The result's `CountsAsBackup` says so. `Exporter` does not stamp the Backup date, because only the host knows the file was delivered; `ExportResult` plus `BackupRecorder.RecordAsync(appData, result, now)` stamps it, and does nothing for a non-Backup Export.
- **JSON:** `ExchangeFormat.Write(document, indented: true)` with `exportedAt` (the injected time) and `exportedFrom` (a build string the host passes in). The `readMe` block is unchanged (it is the same in every document); the slice is described by the absent collections and, for an LLM, by the prompt (which states the range).
- **Markdown:** a read-only rendering, never imported. Fixed section order: a short "about this export" header (range, what is and is not included, units), the Plan (Phases and Sessions with their rows), Workouts by date (Session name, each Exercise with its Sets in readable text, RPE, skips and swaps, the Pain check), Weigh-ins as a table with the weekly averages left to the reader, the Exercise library as a table (ID, name, log kind, tags), then Guides. Weights are in kg, distances in km or m, durations as `m:ss`, always labelled; the display-unit Setting is ignored so that a chat tool never has to guess. Set text reuses A2's formatting where it exists.
- **Summary:** `ExportResult.Summary` is a short list of plain lines ("48 Workouts, 12 Weigh-ins, 6 Pain checks, 27 Exercises", "26 Aug to 17 Nov 2026"). **File names (default):** `fitness-backup-2026-11-17.json`, `fitness-export-2026-08-26-to-2026-11-17.json` (or `.md`).

### Import: reading the paste
- **`ImportInput.Inspect(text)`** cleans the paste, reads it with `ExchangeFormat.Read` and returns an `Inspected` value or a rejection. Cleaning removes a leading BOM, then, if the text does not start with `{`, takes the content of the first markdown code fence, or failing that the span from the first `{` to the last `}`. When anything was removed the result carries a notice ("Ignored text around the JSON"). It never guesses beyond that.
- **Rejections** are lists of `FormatIssue` (path, code, message) from the reader, shown as `path: message`, first 10 and then "and N more" **(default)**. `unsupported_version` keeps A1's "update the app" wording. Reader warnings (`unknown_field`) are kept as notes ("ignored: `plans[0].colour`"), never errors.
- **Suggested kind:** a complete Backup (every collection present except `weekSchedules`, plus `settings`) suggests Restore; a document with only `guides` (and optionally `exercises`) suggests Guides; only `plans` (and optionally `exercises`) suggests Plan; anything else suggests merge. The owner can pick any kind; a kind a file cannot satisfy (Restore on a partial file) is refused with `not_a_backup` listing what is missing.
- **Deferred checks run here:** the A1 reader leaves references to Exercises and Sessions unchecked when the document lacks `exercises` or `plans`. Preview resolves them against the library and the document's own records and reports `unknown_exercise` and `unknown_session` at the exact path.

### Import: Exercise matching
- **Order:** (1) same ID as a library Exercise: it is that Exercise, whatever the names say; (2) same name after normalising (trim, collapse spaces, ignore case): matched by name, and the file's ID is mapped to the library's; (3) near-match: suggested for the owner to confirm; (4) otherwise new.
- **Near-match:** over normalised names, a character-bigram (Dice) similarity of at least 0.75, or the same set of words after dropping a trailing `s` from each, with the same log kind preferred when scores tie; up to three suggestions, best first **(default threshold, tuned against fixtures)**. A near-match is `NeedsDecision` until the owner picks one suggestion or "add as new" in `ImportDecisions`; **an Import with an open decision cannot be applied** **(default)**.
- **New Exercises** keep their file ID (it cannot collide: a collision would have matched by ID). Their log kind and tags come from the file.
- **Matched Exercises** keep the library's name and log kind. A different name is noted ("file calls it X"), changed tags and `isBenchmark` are applied as a change **(default)**, and a different log kind is ignored with a warning (story 31).
- **Remapping:** one map from file ID to library ID is applied to every reference: Plan rows, Phase goals, overrides, Workout `planned`, entries, `swappedFromExerciseId` and Pain checks, Guides, and a Badge `metric` that equals an Exercise ID.
- **Result type:** `ExerciseMatch(IncomingId, IncomingName, Outcome, Candidates, Chosen)` is part of the preview, so a UI can render the question with no logic of its own.

### Import: the four kinds
Each kind decides what it reads from the file and what it ignores; everything it ignores is listed in the preview.
- **Restore a Backup.** Requires a complete Backup. Replaces everything through `AppData.ReplaceAllAsync`. Settings come from the file. The preview says what is lost: counts of the current Workouts, Weigh-ins and Plans that the file does not have, how many current Workouts are dated after the file's `exportedAt`, and how many set-aside (quarantined) records will be removed. Works from any startup state, including `CannotRead` (the only way out of `no_active_plan`).
- **Plan.** Reads `plans` and `exercises`. Each Plan is added as a new Plan, inactive, unless the library has no Plan, in which case the first is active (the format needs exactly one active). Plan, Phase and Session IDs that collide with existing ones (Session and Phase IDs are unique across the whole document) get fresh IDs from the injected generator, and references inside the imported Plan (overrides) follow. Exercises are matched as above. Everything else in the file is ignored.
- **Merge.** Reads `exercises`, `plans`, `weighIns`, `workouts`, `breaks`, `badges` and `weekSchedules`; ignores `settings` and `guides` (Guides have their own Import). By key: a record not in the library is added; an identical one is "nothing to change"; one that differs replaces the existing one and is listed as a change. Exceptions: a Plan keeps its existing `isActive` (a new Plan is added inactive); a **Workout that differs is kept as it is by default** and replaced only by the owner's choice **(default)**, because history is not rewritten by accident. Never deletes (story 37). Workouts on a date and Session that already have a Workout, under a different ID, add a warning (story 39). A change that would leave a Workout pointing at a removed Session (a merged Plan that drops it) is a blocking validator item.
- **Guides.** Reads `guides` and `exercises` (for matching); Guides are keyed by the matched Exercise ID. No existing Guide: added. Existing Guide with no content: replaced. Existing Guide with content: **kept by default**, replaced by the owner's choice per Guide **(default)**. Replacing swaps all content fields and always keeps the existing `personalNotes`; the file's `personalNotes` are used only when the library has none.
- **Preview shape:** `ImportPreview` holds the kind, a short list of plain-language summary lines, structured items (add, change, keep, ignore, warning, blocker; each with record kind, key and text), the Exercise matches, and the choices still open (`IsReady` is true only with no blockers and no open choices). Summary wording follows the product brief ("adds 3 Exercises, changes the target on 2, creates a new Plan called X, touches no Workouts"); wording is covered by tests so it does not drift.
- **Decisions:** `ImportDecisions` holds the owner's answers (Exercise choice by file ID, Guide keep or replace by Exercise ID, Workout keep or replace by ID). The UI calls `Preview` again with updated decisions; there is no hidden state.

### Applying
- **`ImportApplier.ApplyAsync(appData, preview, backupSink, now)`** refuses a preview that is not ready, or that was built from a snapshot that is no longer current (`PreviewOutdated`: the snapshot reference differs), changing nothing.
- **Automatic Backup first.** It builds a Backup document from the current snapshot and hands it to `IBackupSink` (a small Core interface: `Task<bool> DeliverAsync(BackupFile)`, where `BackupFile` is text, a suggested name and the time). If the sink returns false the Import stops, nothing changed (`BackupNotSaved`). If true, `RecordBackupAsync(now)` is called, then the commit. So the Backup date reflects a Backup that was actually handed over, and a failed commit does not undo it. Auto-backup is skipped, and the preview says so, only when there is no readable data (a restore from `CannotRead` or `StorageFailed`, or a snapshot with no records at all).
- **Commit.** Restore is `ReplaceAllAsync`; the other three kinds are one `CommitAsync(changes)` batch, so they are atomic and validated again. The outcome is `Applied(summary)`, `Rejected(issues)`, `StoreFailed(failure)` or `Blocked(state)`, the same shapes as A3.
- **Small A3 addition (default):** `AppData.ReplaceAllAsync(document, lastBackupAt)` writes the Backup date in the same atomic call as the replace, so the A3 sharp edge ("restore is two writes") goes away. A restore stamps the file's `exportedAt` if present, otherwise `now`: the data is exactly as safe as that file, no safer.
- **The sink's delivery is the host's job.** The browser implementation (a file download) is Track B; A4 supplies the interface and a recording fake for tests.

### Prompt pack
- **Three builders, text only:** `PromptPack.Analyse(exportResult, range)`, `PromptPack.WritePlan(snapshot)` and `PromptPack.WriteGuides(snapshot, exerciseIds)` (by default the Exercises with no Guide content). Each returns plain text ready to copy.
- **Contents of every prompt:** a one-paragraph role and task; the exact reply rules ("reply with one JSON object and nothing else", "use these Exercise IDs", "to add an Exercise put it in `exercises` with a new lowercase-hyphen ID", "do not invent personal notes"); the format reference for just the parts that prompt needs (Plan, Phase, Session and Exercise row for the Plan prompt; Guide for the Guides prompt; none for Analyse, which reads an Export); a worked example; and the Exercise library as compact JSON (ID, name, log kind, tags) so IDs and names line up. Analyse states the range and units and embeds the Export (the owner's chosen format) after the instructions. Blanks the owner fills in (goal, days a week, equipment, injuries) are marked in square brackets.
- **No drift:** enum value lists in the format reference are generated from the C# enums, and every worked example is a constant that tests read with the real reader, validate, and run through `ImportPreviewer` against a fixture library (so the example is also a valid Plan or Guides Import). The exact wording is a ticket deliverable the owner reviews; this spec fixes what each prompt must contain, not its sentences.

## Testing Decisions

- **One seam: the `Core.Exchange` public surface, with an in-memory `AppData`.** Tests build a snapshot (the A1/A3 whole-dataset builder), call Export, Inspect, Preview, Apply or a prompt builder, and assert only what a caller can observe: the text, the preview lines and items, and the `AppData` snapshot and the sink's recorded Backup afterwards. No assertions on matchers, remappers or renderers directly. "Restart" is a new `AppData` over the same store. Time is the injected clock; the sink is a recording fake that can be told to fail.
- **Export cases:** default range is 84 days ending today and inclusive at both ends; each tick includes and excludes exactly its data; unticking Pain checks strips them from every Workout, ticking them alone gives date-and-pain-only Workouts; an unticked Exercises tick still carries the Exercises that are referenced; a Backup holds every collection including empty ones and personal notes, a non-Backup does not; the JSON reads back through `ExchangeFormat.Read` with no errors and (apart from the slice) equals the source records; Markdown contains the expected sections in order, labelled units and no IDs outside the library table; file names; summary lines; `CountsAsBackup` and the stamp only for a Backup.
- **Inspect cases:** a code-fenced reply, a reply with a sentence before and after, a BOM, empty text, not JSON, a top-level array; each rejection names the exact path and code; more than 10 issues are cut with a count; `unsupported_version` says to update the app; unknown fields become notes; suggested kind for a Backup, a Plan-only file, a Guides-only file and a mixed file.
- **Matching cases:** ID wins over a different name; exact name after normalising maps the ID; near-match produces candidates and blocks `IsReady` until decided, with "add as new" and "use X" both producing the right data; no match is new; remapping reaches Plan rows, Phase goals, overrides, Workout entries and `swappedFromExerciseId`, Pain checks, Guides and a Badge metric; a log-kind difference is ignored with a warning; a file with an Exercise reference that is nowhere is `unknown_exercise` at the exact path.
- **Kind cases:** *Restore:* a partial file is `not_a_backup`; the preview counts what will be lost, including Workouts newer than the file and set-aside records; applying replaces everything and stamps the Backup date atomically; works from a `CannotRead` start. *Plan:* added inactive, first Plan active, colliding Plan/Phase/Session IDs get fresh IDs and overrides follow, other collections listed as ignored, a second identical import reports nothing to change only if the IDs are stable. *Merge:* add, change and unchanged by key, nothing deleted, a differing Workout kept until chosen, the same-date-and-Session warning, a Plan keeps `isActive`, a merged Plan that drops a used Session is a blocker, Settings and Guides ignored. *Guides:* none, empty and non-empty existing Guides, per-Guide keep and replace, personal notes never overwritten.
- **Apply cases:** a not-ready preview and a stale preview change nothing; the Backup is delivered before the commit and contains the pre-Import data; a sink that fails stops the Import with nothing changed and no Backup date; a rejected or failed commit leaves the data and snapshot unchanged and the Backup date as stamped; `Changed` fires once per applied Import; importing the same file twice gives "nothing to change".
- **Prompt cases:** each prompt contains its instruction block, the library with every ID and name, the right range and units; the generated enum lists equal the C# enums; each worked example reads, validates and previews cleanly; Analyse embeds the Export text verbatim; WriteGuides defaults to Exercises without a Guide.
- **Whole-loop check:** one test exports a Backup from a full dataset, wipes the store, restores it and asserts the snapshot equals the original, so the format, the reader and the restore are proved consistent end to end.
- **Prior art:** `Core.Tests/Serialisation` (builders, exact path-and-code assertions, whole-dataset round trip) and `Core.Tests/Storage` (the in-memory `AppData`, fault-injecting wrapper). No new test infrastructure beyond the recording sink.
- Fast and deterministic: no sleeping, no real clock, no browser. bUnit for the Import preview component belongs to Track B.

## Out of Scope

- Any UI: the Export picker, paste box, file picker, preview screen, Exercise-match questions and Backup screens (Track B). A4 fixes the facts and wording they show.
- Delivery: download, copy to clipboard, the share sheet, the file picker, and the browser implementation of `IBackupSink`.
- Starter data (the starting Plan, the 27 Guides, the weight history) and converting the old Obsidian formats: A5, which uses this Import.
- Importing Markdown, or any format other than the exchange JSON. Reading an LLM reply beyond cutting out the JSON.
- Deleting data through an Import, merging Settings, and merging two Plans' Phases or Sessions into one.
- A saved copy of the data inside the app for "undo last Import", cloud storage, sync, and encrypting Backups.
- Keeping set-aside (quarantined) raw records in a Backup: they cannot be written in the exchange format. A restore removes them, and the preview says how many.
- A stats-driven "similar Exercises" matcher: matching here is name similarity only.

## Further Notes

- **Seam and defaults to confirm.** The proposed single seam is the `Core.Exchange` surface over an in-memory `AppData`. Items marked **(default)** are recommendations, not yet confirmed by the owner. The ones that change behaviour most, for the owner to confirm or change before `/to-tickets`:
  1. **Near-matches block applying** until answered (alternative: default to "add as new" with the question shown). Recommended: block, as the brief says near-matches "need confirmation".
  2. **The automatic Backup goes through a host-supplied sink** (a download on the phone), and the Import stops if it is not delivered. Alternative: also keep a pre-Import copy inside the app (a new storage item) so "undo last Import" needs no file. Recommended: sink only now; revisit with the screens.
  3. **A Workout that differs from the stored one with the same ID is kept** unless chosen otherwise. Recommended for history safety.
  4. **A restore stamps the Backup date as the file's `exportedAt`**, not "now", so a restore of an old file still nudges for a new Backup.
  5. **Weigh-ins and Pain checks are ticked by default** in the 12-week Export (the owner asked for pain and weight analysis; unticking is one tap). Personal notes in Guides are left out of every non-Backup Export.
  6. **Guides default to keep** when the Exercise already has Guide content.
  7. **A restore removes set-aside records** with a count in the preview and no way to keep them.
  8. **Prompt wording** is written during implementation and reviewed by the owner.
- **Housekeeping.** `docs/build-plan.md` and `docs/README.md` now describe A3 as implemented with PR #14 open, and A4 as specified. The A2 spec's nine assumptions still need checking against the old `view.js`, which is not in the repo yet. Layout version is still 1 and no real format upgrade has been exercised.
- **Proposed ordering for `/to-tickets`** (each about 150 to 200k tokens): (1) Export request, slicing, JSON, file names, summary, Backup recording; (2) Markdown rendering; (3) Import input cleaning, Inspect, rejection summaries, suggested kind; (4) Exercise matcher and remapper; (5) Previewer for Plan, Merge and Guides with decisions, the validated candidate and the wording; (6) Restore preview, `IBackupSink`, `ImportApplier` and the atomic `ReplaceAllAsync` addition; (7) Prompt pack and its example tests; (8) the whole-loop test and the docs. Tickets 4 and 5 are the likeliest to need splitting.
- Each ticket must fit one agent in roughly 150 to 200k tokens (see `CLAUDE.md`).
