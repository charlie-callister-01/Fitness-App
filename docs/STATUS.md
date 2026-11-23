# Status

Rewritten by `/end-session` from git and platform facts. Keep under ~40 lines. History: `docs/LOG.md`.

**Last updated:** 2026-11-23 (after Data and settings B9+B10 opened in PR #33)

## Where we are
- **Merged:** Track A0–A4, A4.5 PR 1 (PR #27) & PR 2 (PR #31), A5 (PR #32); Track B0–B3, Editors B4+B5 (PR #25), Insight B6–B8 (PR #28).
- **In flight:** **Data and settings (B9+B10)** (Export/Import, Prompt pack, Settings, First run; tickets 01–05 + code-review & thermo-nuclear fixes) open in **PR #33** (`feature/data-and-settings`).
- **Next up:** **Hardening** (B11).
- Tests (at `ef68ffa` on `feature/data-and-settings`): **Core 1,691, Ui 796, Web 40, LegacyConverter 46 green** (0 warnings).

## Branches and PRs (2026-11-23)
- `origin/main` = `97a1e0e` (local `main` is at `97a1e0e`).
- Active branch here: `feature/data-and-settings` (`ef68ffa`).
- Open GitHub PRs:
  - **#33** `feat(data-and-settings): export/import, prompt pack, settings, and first run (B9+B10)` (`feature/data-and-settings`)

## Next action
1. Review/merge **PR #33** (`feature/data-and-settings`), fast-forward local `main`, run `/mattpocock-skills:improve-codebase-architecture`, archive `.scratch/data-and-settings/` to `.scratch/archive/`, then spec and start **Hardening** (B11).

## Caveats
- Manual phone checks in `docs/pwa-manual-checks.md` (including Data and settings) are still owed.
- Keep `.scratch/a5-starter-data/legacy/` git-excluded via `.git/info/exclude` (never commit private vault data); `.scratch/a5-starter-data/out/` is in `.gitignore`.
- Three A2 assumption differences against `view.js` are recorded as open items in `docs/build-plan.md`.
