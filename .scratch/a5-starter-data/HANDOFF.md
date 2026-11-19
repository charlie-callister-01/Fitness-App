# Handoff: write the A5 (Starter data) spec

> **Superseded (19 Nov 2026).** The A5 spec is written (`docs/specs/a5-starter-data.md`) and depends on the new A4.5 (Tracks) stage. Use `.scratch/a4-5-tracks/HANDOFF.md` instead. The notes below are kept for the inputs list and background.

Point a fresh chat at this file to start the A5 `/mattpocock-skills:to-spec`. File lives at `.scratch/a5-starter-data/HANDOFF.md`.

**Task: run `/mattpocock-skills:to-spec` for stage A5 of the Fitness App.** Stop after the spec is written and pushed. Then follow the workflow in `CLAUDE.md` (`/to-tickets`, implement, `/mattpocock-skills:code-review`, the thermo-nuclear review, fixes, `/mattpocock-skills:pr`). Don't start the next stage until A5's PR is written.

## State of the repo
- A0 to A2 are merged. **A3 (Storage) is draft PR #14** (`a3-storage`). **A4 (Export, Import and prompt pack) is draft PR #15** (`a4-implementation`, based on `a3-storage`; 804 Core, 9 Ui, 32 Web tests pass; browser tests not run for A4). The owner confirmed all A4 spec defaults on 17 Nov 2026.
- **Before starting A5:** check whether #14 and #15 have merged (`gh` is at `C:\Program Files\GitHub CLI\gh.exe`, works from PowerShell, not bash). If not, base A5 on `a4-implementation`. If #14 merged and #15 was retargeted to `main`, base on `main`.
- Work in a new worktree and branch (for example `a5-spec`). Don't reuse old ones. The session's git operations must stay inside its worktree. Specs and tickets are local files: `docs/specs/<stage>.md` and `.scratch/<stage>/issues/NN-*.md`.

## Read first, in order
`CLAUDE.md`, `docs/README.md`, `GLOSSARY.md`, `docs/product-brief.md` ("Starting data (via Import)" and "Starting Plan"), `docs/build-plan.md` (A5 entry, "Inputs still needed from the owner"), `docs/data-spec.md`, `docs/specs/a4-export-import.md` (what Import does, in particular Plan, Merge and Guides kinds, stable IDs and "nothing to change" on a re-import), then `src/Core/Exchange/` (`ImportInput`, `ImportPreviewer`, `ImportApplier`, `PromptPack`). Copy the layout of `docs/specs/a3-storage.md` and `a4-export-import.md`: front matter `labels: ready-for-agent`, `stage: A5`, then the to-spec template sections.

## What A5 is
Build plan: "The starting Plan converted from `Config.md`, the 27 Guides, and the weight history, all delivered through Import." The product brief says the starting Plan arrives through the normal Import, converted from `Config.md`; the 27 Guides as starter Guides; weight history from 1 Feb 2026 (start 113 kg) from the old daily notes; no Workout history exists to migrate. Starting Plan: Sessions A Lower+core (Mon), B Upper+shoulders (Tue), C Engine+core (Wed), D Full body (Thu), E Long session (Sat), Sunday weigh-in, benchmark every 4 weeks; Phases Foundation (weeks 1-8), Build (9-19), HYROX prep (20-27); start 2026-10-19, race 2027-04-24; 5 Sessions a week, Streak minimum 4, increment 2 kg; about 40 Exercises. No UI in this stage.

## What A4 gives A5
- The importer: `ImportInput.Inspect(text)` then `ImportPreviewer.Preview(appData, inspected, kind, decisions, now)` then `ImportApplier.ApplyAsync(appData, preview, backupSink, now)`. Kinds: Plan (new Plan, first Plan in an empty library becomes active), Guides (keyed by Exercise ID; no existing Guide: added), Merge (Weigh-ins, Exercises, etc. by key), Restore.
- **IDs must be stable and hand-written** (`^[A-Za-z0-9_-]+$`, such as `goblet-squat`, `w-2026-02-01`) so re-importing the same files says "nothing to change" and a re-run never duplicates. A4's tests prove that for Plan and Guides.
- Matching: by ID, then exact name, then near-match (needs the owner's answer, blocks `IsReady`), else new.
- A5 decides **how starter data is delivered**: the converted files as JSON the owner pastes or opens through the Import screen (Track B), bundled as app assets, or a first-run offer. Propose a default and flag it; the screens are Track B, so A5 probably produces the converted data files plus conversion/validation tests, not a UI.
- The owner's Backup flow needs a pre-Import Backup sink (`IBackupSink`); a first run has no readable data, so A4 skips the automatic Backup then.

## Inputs still needed from the owner (the main risk for this stage)
None of these are in the repo yet. Check which exist before speccing, and ask the owner to supply the rest:
- `Fitness/Config.md` (current plan, kinds, badges), from the Obsidian vault folder `Beta/99 Life Planning/Exercise/`.
- The 27 files in `Fitness/Exercises/*.md` (starter Guides).
- Weight history from the old daily notes (`session::`/`weight` data from 1 Feb 2026) and `Plan/Tracking/Weight.md`.
- `Fitness/_scripts/fitness/view.js` and `view.css` (the reference for rules; the A2 spec's nine assumptions still need checking against it), `Fitness/How to use.md`, `Homepage.md`.
- The pre-plan is at `C:\Users\calli\Downloads\Fitness App - Pre-plan.md`.

If the vault files are not reachable, say so plainly in the spec's Further Notes and write the conversion rules and the tests against a small fixture, with the real conversion left as a ticket the owner unblocks.

## Decisions A5 will probably need to make (propose defaults, flag them, don't interview at length)
- The conversion approach: a one-off script (what language? not C# in `Core`) versus a tested C# converter in a tools project, and whether the converted JSON is committed to the repo (it is the owner's own data: a private repo question).
- Exercise IDs and names for the ~40 Exercises, and their log kinds and tags (the old system's kinds map to A1's `LogKind`).
- How the old Badges map to A1 Badges (editable, with a starter set) and whether they ship as part of the starter data (Backup-style/merge) or separately.
- How the weight history imports (Weigh-ins by stable date IDs through Merge) and the start-weight/goal-weight Settings.
- How "benchmark every 4 weeks" and the Phase structure are expressed in the Plan JSON.
- The seam for tests: a golden fixture per input type run through the real importer (`Preview` is ready, no open choices, expected counts) is the natural single seam.

## Known sharp edges (also in `docs/build-plan.md`)
- Only active Plan quarantined means `CannotRead`; restore is the only way out.
- No real layout or format upgrade has been exercised (both versions are still 1).
- The `Changed` event fires twice for an Import that stamps a Backup (A4, pinned by a test).
- The A4 prompt pack wording is a first draft awaiting the owner's review.
- The storage manual checks and the PWA phone checks still need doing by hand (`docs/pwa-manual-checks.md`).

## Housekeeping to remember
- Update `docs/build-plan.md` and `docs/README.md` status as part of the A5 spec commit.
- Use the glossary's terms exactly (Plan, Phase, Session, Exercise, Guide, Weigh-in, Import).
- Ticket sizing: each about 150-200k tokens. The repo's `.claude/skills/thermo-nuclear-code-quality-review/SKILL.md` is the strict review (user-invoked only; apply from the file via an agent).

## Deliverable
A pushed branch with `docs/specs/a5-starter-data.md`, a short summary of the proposed seam and key defaults, which of the owner's input files were found (and which are missing), and the open questions that need the owner's answer before tickets.
