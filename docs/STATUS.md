# Status

Rewritten by `/end-session` from git and platform facts. Keep under ~40 lines. History: `docs/LOG.md`.

**Last updated:** 2026-11-23 (Hardening B11 PR #34 merged)

## Where we are
- **Merged:** Track A0–A4, A4.5 PR 1 (PR #27) & PR 2 (PR #31), A5 (PR #32); Track B0–B3, Editors B4+B5 (PR #25), Insight B6–B8 (PR #28), Data and settings B9+B10 (PR #33), **Hardening B11 tickets 01–07 (PR #34)**.
- **In flight:** **Hardening (B11)** post-merge (tickets 08 performance on the deployed build, 09 combined manual phone pass in `.scratch/hardening-b11/issues/`, both `ready-for-human`); any fixes in a small follow-up PR.
- Tests (at `bfde41d` on `main`): **Core 1,691, Ui 914, Web 40, LegacyConverter 46, Web.BrowserTests 318 green** (0 warnings).

## Branches and PRs (2026-11-23)
- `origin/main` = `bfde41d` (local `main` is at `bfde41d`).
- Active branch here: `chore/hardening-post-merge` (from `bfde41d`).
- Open GitHub PRs: none.

## Next action
1. Ticket 08 (Lighthouse ≥ 90, warm start < 3 s on the deployed build) and ticket 09 (combined manual phone pass in `docs/pwa-manual-checks.md`).
2. Then: archive `.scratch/hardening-b11/` to `.scratch/archive/`, run `/mattpocock-skills:improve-codebase-architecture` (still owed since Data and settings), and do the end-of-build branch sweep.

## Caveats
- Manual phone checks in `docs/pwa-manual-checks.md` (A0, A3, B0–B3, Editors, A4.5 PR 2, Data and settings) are still owed; ticket 09 clears them.
- Local leftovers: merged worktrees under `.claude/worktrees/` (some locked by the agent harness) and merged `ticket/hardening-*`, `fix/hardening-review`, `refactor/hardening-thermo` branches; safe to delete.
- Keep `.scratch/a5-starter-data/legacy/` git-excluded via `.git/info/exclude` (never commit private vault data); `.scratch/a5-starter-data/out/` is in `.gitignore`.
- Three A2 assumption differences against `view.js` are recorded as open items in `docs/build-plan.md`.
