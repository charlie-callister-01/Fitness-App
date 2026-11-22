# Handoff: A5 (example and test dataset)

Point a fresh chat at this file. Read `CLAUDE.md`, `docs/README.md` and `GLOSSARY.md`, then `docs/specs/a5-starter-data.md` (the spec, rewritten 22 Nov 2026), `docs/specs/a4-5-tracks.md` and `.scratch/a4-5-tracks/HANDOFF.md` (conventions). File lives at `.scratch/a5-starter-data/HANDOFF.md`.

## State (22 Nov 2026)
- **A5 is repurposed.** The owner will wipe all current data. The old Obsidian data, converted, becomes an **example and test dataset**: one complete Backup-style file (format 2) restored through Restore to exercise and test the app. New real data comes later through the prompt pack and an LLM. The earlier plan (public starter files bundled for First run, a private history file, three Imports) is dropped.
- The spec is written. **The tickets are not**: run `/mattpocock-skills:to-tickets` for A5 when it is this stage's turn. The spec's "Proposed tickets" gives a sequential chain of seven.
- **Depends on A4.5 PR 1 only** (the Track model, format 2, the v1 upgrader and the importer). It does not need the screens. Do not start it before PR 1 is merged (or base on its branch).
- **Run one implementer at a time.** Never in parallel with A4.5, Data and settings or anything else: the owner is usage-constrained. Order: A4.5 PR 1, then A5 or Data and settings, one after the other, then A4.5 PR 2. The A5 tickets are a chain (each blocked by the one before), so they can be queued one by one.

## Inputs
- **The legacy inputs are not in the repo and must never be committed (the repo is public).** They are in the main checkout at `.scratch/a5-starter-data/legacy/` (git-excluded through `.git/info/exclude`, so absent from a fresh worktree; copy them in or point at that path). `phone-2026-11-19/` is the newest (`Config.md`, 33 Guides, 13 Logs, `view.js`); the rest is the older vault copy. If the folder is gone: `Fitness (2).zip` in Downloads, and the vault at `Documents\Obsidian Brain\Second Brain\Beta\99 Life Planning\`.
- **Missing data:** weigh-ins from 1 Feb to 25 Sep 2026. Only 113 kg (1 Feb), 85.5 kg (26 Sep) and 86.5 kg (18 Oct) exist. The converter reads extras from an optional `weights.csv`. Not a blocker for example data.
- The output (`example-dataset.json`, `conversion-report.md`) goes to `.scratch/a5-starter-data/out/`, git-ignored (add it to `.gitignore`). Only synthetic fixtures are committed. The real-folder test is opt-in through an environment variable.

## Decisions already made (do not re-ask)
- One output document, Backup-style, format 2, through the real writer; restored through Restore (B9, or the test harness until then). Nothing bundled, nothing public.
- The Plan uses explicit Tracks: a weekly **Main** Track (A to E and the benchmark, target 5, Streak minimum 4, three Phases) and a daily **Mobility** Track (`dayDonePercent` 75, no Phases) with the Session `m-mobility`. Gym and cardio stay combined on Main.
- Owner answers of 19 Nov 2026: benchmark on Friday; Guides copied onto each covered Exercise; `Bodyweight deadlift` Badge dropped; side plank `2.3` is two 30 s Sets; `12×10` is one Set; all past data `loggedAfterwards: true`.
- Mobility Badges use the Track metrics (`streak:mobility`, `days:mobility`).

## To confirm with the owner when convenient
- The whole dataset (including Guides and Badges) is private and not bundled, and so B10 First run has no bundled starter Plan; decide its content when Data and settings is specced.

## Known sharp edges (also in `docs/build-plan.md`)
- Only active Plan quarantined means `CannotRead`; restore is the only way out. (Which makes Restore of this file the reset path.)
- The A4 prompt pack wording is a first draft awaiting the owner's review.
- A2's nine assumptions still need checking against `view.js` (last A5 ticket).
- The storage manual checks and the PWA phone checks are still owed (`docs/pwa-manual-checks.md`).

## Deliverable
A pushed branch, passing tests, the PR body in `.scratch/a5-starter-data/pr-body.md`, and `docs/build-plan.md` plus `docs/README.md` status updated. Follow `CLAUDE.md` steps 3 to 7 (reviews and fixes) before the PR.
