# Status

Rewritten by `/end-session` from git and platform facts. Keep under ~40 lines. History: `docs/LOG.md`.

**Last updated:** 2026-11-22 (workflow refit on `chore/workflow-refit`)

## Where we are
- **Merged:** Track A0–A4 (Core, Storage, Export/Import); Track B0–B3 (Shell, Workout log, Home); Editors B4+B5 (PR #25); A4.5 Tracks spec & ADR 0007 (PR #26).
- **Next up (sequential, one implementer at a time):**
  1. **A4.5 PR 1** (`docs/specs/a4-5-tracks.md`, tickets 01–10 in `.scratch/a4-5-tracks/issues/`, 0 done).
  2. **A5** (`docs/specs/a5-starter-data.md`) or **Data and settings** (B9+B10), one after the other.
  3. **A4.5 PR 2** (tickets 11–15), then **Insight** (B6–B8), then **Hardening** (B11).
- Tests: **Core 1336, Ui 611, Web 39 green** (0 warnings).

## Branches and PRs (2026-11-22)
- `main` = `4fc7e46`. Active: `chore/workflow-refit` (workflow refit + beta `AGENTS.md` setup). No open GitHub PRs yet.

## Next action
1. Finish/merge `chore/workflow-refit`, then start **A4.5 PR 1** (`01-track-model-and-mechanical-ripple.md`).

## Caveats
- Owner is usage-constrained: run one implementer at a time, never in parallel.
- Manual phone checks in `docs/pwa-manual-checks.md` (A0/A3/B0/B1/B2/B3/Editors) are still owed.
- Keep `.scratch/a5-starter-data/legacy/` git-excluded via `.git/info/exclude` (never commit private vault data).
