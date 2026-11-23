# Status

Rewritten by `/end-session` from git and platform facts. Keep under ~40 lines. History: `docs/LOG.md`.

**Last updated:** 2026-11-23 (PR #33 merged; Hardening B11 specced)

## Where we are
- **Merged:** Track A0–A4, A4.5 PR 1 (PR #27) & PR 2 (PR #31), A5 (PR #32); Track B0–B3, Editors B4+B5 (PR #25), Insight B6–B8 (PR #28), Data and settings B9+B10 (PR #33).
- **In flight:** **Hardening (B11)**: spec `docs/specs/hardening-b11.md` written on `feature/hardening`; tickets next.
- **Next up:** the 7 Hardening tickets, one at a time.
- Tests (at `ef68ffa`, now in `main`): **Core 1,691, Ui 796, Web 40, LegacyConverter 46 green** (0 warnings).

## Branches and PRs (2026-11-23)
- `origin/main` = `c8dcac4` (local `main` is at `c8dcac4`).
- Active branch here: `feature/hardening` (from `c8dcac4`).
- Open GitHub PRs: none.

## Next action
1. `/mattpocock-skills:to-tickets` for Hardening (`.scratch/hardening-b11/issues/`, 7 tickets in the spec's Delivery order). Still owed from the Data and settings close-out: `/mattpocock-skills:improve-codebase-architecture` and archiving `.scratch/data-and-settings/` to `.scratch/archive/`.

## Caveats
- Manual phone checks in `docs/pwa-manual-checks.md` (including Data and settings) are still owed.
- Keep `.scratch/a5-starter-data/legacy/` git-excluded via `.git/info/exclude` (never commit private vault data); `.scratch/a5-starter-data/out/` is in `.gitignore`.
- Three A2 assumption differences against `view.js` are recorded as open items in `docs/build-plan.md`.
