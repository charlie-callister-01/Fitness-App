# 10: Navigation, Nudge copy, regression and docs

Spec: `docs/specs/insight-b6-b8.md` (Navigation, Nudges, Testing). Stories 63-67, 70.

**Status:** resolved

**Blocked by:** 02, 04, 07, 08, 09.

**What to build:** Home leads into Insight: the Streak orb opens the Calendar, the Phase orb opens Progress, and the Sunday Today card leads to the Weekly review after the weigh-in (keep existing routes, add the tabs). The copy of every existing Nudge kind is reviewed against C-08 (neutral, specific, one action, one dismiss, at most two) and any that miss it are fixed; no new Nudge kinds. Two-column layouts from 900 px on the Insight screens. A whole-stage test walks: log Workouts and Weigh-ins, see charts, Calendar, Weight, an unlocked Badge, and the review. Regression sweep: existing tests pass with at most mechanical changes.

Then: add manual device checks for Insight to `docs/pwa-manual-checks.md`; update `docs/STATUS.md`, `docs/LOG.md`, `docs/build-plan.md` and `GLOSSARY.md`; run `/mattpocock-skills:code-review`, `/thermo-nuclear-code-quality-review` (findings to `docs/REFACTOR.md`), apply fixes, then the size check from `AGENTS.md`; write the PR body at `.scratch/insight/pr-body.md`.

- [x] Orb and Sunday-card navigation tested in Home bUnit tests
- [x] Nudge copy review recorded in the ticket with any copy changes and their tests
- [x] Whole-stage test passes; no existing test loosened
- [x] Manual checks and docs updated
- [ ] Reviews done, fixes applied, PR body written
- [x] `dotnet build` 0 warnings, `dotnet test` green; commit `Insight: ticket 10 Navigation, Nudge copy, regression and docs`

## Nudge copy review (C-08: neutral, specific, one action, one dismiss, at most two)

- Missed session: "You missed Lower on Monday. Make it up this week?" Passes (names Session and day; action Make up). Day name now uses the invariant culture so it cannot change with the device language.
- One short of target: "One workout short of your target of 3 this week." Passes (numbers; action Pick a Session).
- No Backup: "No Backup in 14 days. Make one so you don't lose your data." Passes (action Back up).
- Repeated pain: was "Knee pain reported twice this week. Consider easing off." Vague; changed to "... Consider an easier variation." (design wording). Still no action button (no flow exists); Dismiss only. Owner to confirm.
- At most two: already enforced by `HomeNudges.Select`. Test: `Every_Nudge_kind_has_neutral_specific_copy_...` in `NudgeRulesTests`.

## Notes

- Orb routes: Streak opens `progress?tab=calendar`, Phase and Race open `progress`; `Progress` and `Trophies` read `?tab=`. Sunday card button is shown when today is Sunday and today's Weigh-in exists.
- `docs/STATUS.md` and `docs/LOG.md` do not exist on this branch (they live on the A4.5 branch); not edited here, to be updated by the orchestrator at merge.
- Left unticked on purpose: "Reviews done, fixes applied, PR body written" (the orchestrator does these).
