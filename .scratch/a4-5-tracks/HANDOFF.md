# Handoff: Track A, A4.5 (Tracks) then A5 (Starter data)

Point a fresh chat at this file. It is the single handover for the remaining Track A work. File lives at `.scratch/a4-5-tracks/HANDOFF.md`.

**Task: implement A4.5 (Tracks), then A5 (Starter data), following the workflow in `CLAUDE.md`.** Specs and tickets are written. Start at ticket 01 of A4.5. Don't start A5's tickets until A4.5's PR is written, and don't start B3 (Home) until A4.5 has merged.

## State (19 Nov 2026)
- A0 to A4 merged. B0 and B1 merged. B2 (Workout log, menus and finishing) has a handoff at `.scratch/b2-workout-log-menus-and-finishing/HANDOFF.md` and can run in parallel with Track A, since neither touches the other's code; B2 should read Plans through the existing API, which A4.5 keeps compatible.
- **A4.5 and A5 specs are written, A4.5 tickets are written. A5 tickets are not.** Both are on branch `worktree-a5-spec` (a worktree off `main`). Check whether it has been pushed or merged (`gh` is at `C:\Program Files\GitHub CLI\gh.exe`, PowerShell only) and base new work on `main` once it has; otherwise on that branch.
- Work in a new worktree and branch per stage (for example `a4-5-implementation`). Don't reuse old ones.

## Read first, in order
`CLAUDE.md`, `docs/README.md`, `GLOSSARY.md`, then `docs/specs/a4-5-tracks.md`, `docs/data-spec.md`, `docs/specs/a2-core-rules.md` and `docs/specs/a4-export-import.md` for what A4.5 changes, then the tickets in `.scratch/a4-5-tracks/issues/`.

## A4.5 tickets (each about 150 to 200k tokens; blocking edges are in each file)
1. Track model, reader/writer and validation. (no blockers)
2. Weekly Tracks and Phases per Track. (01)
3. Daily Track rules. (02)
4. Track Badge metrics and Nudges. (02, 03)
5. Exchange: Import, Export and prompt pack. (01; can run in parallel with 02 to 04)
6. Whole-loop test, regression sweep and docs. (04, 05)

Then the stage's reviews (`/mattpocock-skills:code-review`, then the thermo-nuclear review from `.claude/skills/thermo-nuclear-code-quality-review/SKILL.md`, applied as C#), the fix pass, and `/mattpocock-skills:pr`.

Key invariant: **a Plan with no Tracks behaves exactly as before.** Every A1 to A4 test must pass unchanged; one that does not is a finding to report, not a test to loosen.

## Decisions already made (owner, 19 Nov 2026)
- One active Plan with Tracks inside it (several active Plans rejected). Each Track has its own cadence (weekly or daily), target, Streak and optional Phases; a Track without Phases uses the Plan's.
- Defaults confirmed: gym and cardio combined on the main track, `formatVersion` stays 1, main track named "Main", Custom workouts count for the main track only, no daily-Track Nudge yet, benchmark stays on the main track. The owner's current app data is only an example, so no migration is needed.
- A5: benchmark Session on Friday, Guides copied onto each covered Exercise (to be replaced by full per-Exercise Guides later), personal "Watch-outs for you" via a converter flag (or an in-app setup choice later), `Bodyweight deadlift` Badge dropped, `2.3` side plank means two 30 s Sets, `12×10` is one Set, all past data `loggedAfterwards: true`.

## A5 (after A4.5 merges)
1. Read `docs/specs/a5-starter-data.md` (it now includes mobility as a daily Track), then run `/mattpocock-skills:to-tickets` for A5. The spec's "Proposed ordering" has seven tickets.
2. **The legacy inputs are not in the repo and must not be committed (the repo is public).** They are in the main checkout at `.scratch/a5-starter-data/legacy/` (git-excluded through `.git/info/exclude`, so they are absent from a fresh worktree; copy them in or point at that path). `phone-2026-11-19/` is the newest (Config.md, 33 Guides, 13 Logs, view.js); the rest is the older vault copy. If that folder is gone, the originals are `Fitness (2).zip` in Downloads and the vault at `Documents\Obsidian Brain\Second Brain\Beta\99 Life Planning\`.
3. **Missing data:** weigh-ins from 1 Feb to 25 Sep 2026. Only 113 kg (1 Feb, start), 85.5 kg (26 Sep) and 86.5 kg (18 Oct) exist. The converter reads extra weigh-ins from an optional `weights.csv`.
4. Only synthetic fixtures are committed. The real-folder test is opt-in through an environment variable.
5. Update `.scratch/a5-starter-data/HANDOFF.md`'s first lines to say the spec is done (it still says to write it).

## Known sharp edges (also in `docs/build-plan.md`)
- Only active Plan quarantined means `CannotRead`; restore is the only way out. A4.5 does not change that.
- The A4 prompt pack wording is a first draft awaiting the owner's review.
- A2's nine assumptions still need checking against `view.js` (A5 ticket 7).
- The storage manual checks and the PWA phone checks are still owed (`docs/pwa-manual-checks.md`).
- The push of `worktree-a5-spec` was blocked once by the permission check; if you cannot push, ask the owner to run `git push -u origin worktree-a5-spec` from the worktree.

## Deliverable per stage
A pushed branch, passing tests, the PR body in `.scratch/<stage>/pr-body.md`, and `docs/build-plan.md` plus `docs/README.md` status updated.
