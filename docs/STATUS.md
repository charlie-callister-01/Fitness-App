# Status

Rewritten by `/end-session` from git and platform facts. Keep under ~40 lines. History: `docs/LOG.md`.

**Last updated:** 2026-11-22 (after A4.5 PR 1 merged in PR #27)

## Where we are
- **Merged:** Track A0–A4 (Core, Storage, Export/Import); Track B0–B3 (Shell, Workout log, Home); Editors B4+B5 (PR #25); A4.5 Tracks spec & ADR 0007 (PR #26); Workflow refit + **A4.5 PR 1** tickets 01–10 (PR #27).
- **In flight / Next up:**
  1. **Insight (B6–B8):** open in **PR #28** (`feature/insight`).
  2. **A5** (`docs/specs/a5-starter-data.md`) or **Data and settings** (B9+B10), one after the other.
  3. **A4.5 PR 2** (`docs/specs/a4-5-tracks.md`, tickets 11–15 in `.scratch/a4-5-tracks/issues/`, 0/5 done), then **Hardening** (B11).
- Tests (at `c3997d7` on `origin/main`): **Core 1479, Ui 611, Web 39 green** (0 warnings).

## Branches and PRs (2026-11-22)
- `origin/main` = `c3997d7` (local `main` is at `4fc7e46`, behind `origin/main` by PR #27).
- Active branch here: `feature/a4-5-pr1` (`eb760fd`, merged into `origin/main` by PR #27).
- Open GitHub PRs: **#28** `feat(insight): Progress, Weight, Trophies and Weekly review screens` (`feature/insight`).

## Next action
1. Fast-forward local `main` to `origin/main` (`c3997d7`), review/merge **PR #28** (`feature/insight`), then start **A5** (`docs/specs/a5-starter-data.md`), **Data and settings** (B9+B10), or **A4.5 PR 2** (`11-plan-editor-track-selector-and-settings.md`).

## Caveats
- Manual phone checks in `docs/pwa-manual-checks.md` (A0/A3/B0/B1/B2/B3/Editors) are still owed.
- Keep `.scratch/a5-starter-data/legacy/` git-excluded via `.git/info/exclude` (never commit private vault data).
