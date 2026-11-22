# 10: Navigation, Nudge copy, regression and docs

Spec: `docs/specs/insight-b6-b8.md` (Navigation, Nudges, Testing). Stories 63-67, 70.

**Status:** ready-for-agent

**Blocked by:** 02, 04, 07, 08, 09.

**What to build:** Home leads into Insight: the Streak orb opens the Calendar, the Phase orb opens Progress, and the Sunday Today card leads to the Weekly review after the weigh-in (keep existing routes, add the tabs). The copy of every existing Nudge kind is reviewed against C-08 (neutral, specific, one action, one dismiss, at most two) and any that miss it are fixed; no new Nudge kinds. Two-column layouts from 900 px on the Insight screens. A whole-stage test walks: log Workouts and Weigh-ins, see charts, Calendar, Weight, an unlocked Badge, and the review. Regression sweep: existing tests pass with at most mechanical changes.

Then: add manual device checks for Insight to `docs/pwa-manual-checks.md`; update `docs/STATUS.md`, `docs/LOG.md`, `docs/build-plan.md` and `GLOSSARY.md`; run `/mattpocock-skills:code-review`, `/thermo-nuclear-code-quality-review` (findings to `docs/REFACTOR.md`), apply fixes, then the size check from `AGENTS.md`; write the PR body at `.scratch/insight/pr-body.md`.

- [ ] Orb and Sunday-card navigation tested in Home bUnit tests
- [ ] Nudge copy review recorded in the ticket with any copy changes and their tests
- [ ] Whole-stage test passes; no existing test loosened
- [ ] Manual checks and docs updated
- [ ] Reviews done, fixes applied, PR body written
- [ ] `dotnet build` 0 warnings, `dotnet test` green; commit `Insight: ticket 10 Navigation, Nudge copy, regression and docs`
