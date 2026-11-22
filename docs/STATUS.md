# Status

Rewritten by `/end-session` from git and platform facts. Keep under ~40 lines. History: `docs/LOG.md`.

**Last updated:** 2026-11-22 (after Insight merged in PR #28)

## Where we are
- **Merged:** Track A0–A4 (Core, Storage, Export/Import); Track B0–B3 (Shell, Workout log, Home); Editors B4+B5 (PR #25); A4.5 Tracks spec & ADR 0007 (PR #26); Workflow refit + **A4.5 PR 1** (PR #27); **Insight B6–B8** (PR #28: Progress, Weight, Trophies, Weekly review; main Track only).
- **Next up (one stage at a time):**
  1. **A5** (`docs/specs/a5-starter-data.md`) or **Data and settings** (B9+B10), one after the other. B9 also gives Weekly review's "Export for the LLM" button its target (hidden until then) and reuses `DateRangePicker`.
  2. **A4.5 PR 2** (`docs/specs/a4-5-tracks.md`, tickets 11–15 in `.scratch/a4-5-tracks/issues/`, 0/5 done), then **Hardening** (B11).
- **Shelved Insight follow-ups** (owner decided, not built): (a) Badge art choice: add an optional `art` field to Badge in the format once A4.5 is fully merged; (b) pain Nudge action "See easier variations" (C-08 wants one action). Spec or ticket them when planning the next stage.
- Tests (at `origin/main` `c11c8f4`): **Core 1670, Ui 713, Web 39 green** (0 warnings); CI build-and-test and browser-tests passed on PR #28.

## Branches and PRs (2026-11-22)
- `origin/main` = `c11c8f4`. No open GitHub PRs. Merged branches still on the remote: `feature/a4-5-pr1`, `feature/insight`, `chore/workflow-refit` (sweep at phase end).

## Next action
1. Pick **A5** or **Data and settings** (B9+B10); run `/mattpocock-skills:to-spec` if its spec is missing (`docs/specs/` has A5 only), then tickets.

## Caveats
- Insight was never looked at on a device: Theme/Mode/360 px/900 px checks are in `docs/pwa-manual-checks.md` (Insight section), along with older owed checks (A0/A3/B0/B1/B2/B3/Editors).
- The Insight stage skipped `/thermo-nuclear-code-quality-review` (owner's call); `docs/REFACTOR.md` has no Insight findings.
- Owner-confirmed Insight rules: 14-day chart gap, Best of the week = biggest relative jump (first-ever results are baselines), Load Badge targets stored as estimated 1RM, benchmark target from the first Badge naming the Exercise.
- Keep `.scratch/a5-starter-data/legacy/` git-excluded via `.git/info/exclude` (never commit private vault data).
