# Status

Rewritten by `/end-session` from git and platform facts. Keep under ~40 lines. History: `docs/LOG.md`.

**Last updated:** 2026-11-22 (after A5 opened in PR #32)

## Where we are
- **Merged:** Track A0–A4; Track B0–B3; Editors B4+B5 (PR #25); A4.5 Tracks spec & ADR 0007 (PR #26); **A4.5 PR 1** (PR #27); **Insight (B6–B8)** (PR #28); **A4.5 PR 2** (PR #31).
- **In flight:** **A5** (`tools/LegacyConverter`, tickets 01–07 + code-review fixes) open in **PR #32** (`feature/a5-starter-data`).
- **Next up:**
  1. **Data and settings** (B9+B10).
  2. **Hardening** (B11).
- Tests (at `23a304b` on `feature/a5-starter-data`): **LegacyConverter 46, Core 1,675, Ui 754, Web 39 green** (0 warnings).

## Branches and PRs (2026-11-22)
- `origin/main` = `8a4aa65` (local `main` is at `8a4aa65`).
- Active branch here: `feature/a5-starter-data` (`23a304b`).
- Open GitHub PRs:
  - **#32** `feat(a5): legacy Obsidian converter and example dataset proof` (`feature/a5-starter-data`)

## Next action
1. Review/merge **PR #32** (`feature/a5-starter-data`), fast-forward local `main`, archive `.scratch/a4-5-tracks/` and `.scratch/a5-starter-data/` issues/handoff to `.scratch/archive/`, then spec and start **Data and settings** (B9+B10).

## Caveats
- Manual phone checks in `docs/pwa-manual-checks.md` (A0/A3/B0–B3/Editors/A4.5 PR 2) are still owed.
- Keep `.scratch/a5-starter-data/legacy/` git-excluded via `.git/info/exclude` (never commit private vault data); `.scratch/a5-starter-data/out/` is in `.gitignore`.
- Three A2 assumption differences against `view.js` are recorded as open items in `docs/build-plan.md`.
