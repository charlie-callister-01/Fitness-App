# 08: Phase position, Phase goal readiness and Override row resolution

**What to build:** For any date the app can say which Phase the Plan is in, "week 3 of 11", whether the Phase's goals are met so the owner can move on early, and exactly which Exercise rows a Session has today (the current Phase's Override, else the Plan default). Spec: `docs/specs/a2-core-rules.md` (Phase position stories 65 to 68; "Phase position" decisions; assumption 8).

**Blocked by:** 03 (Best, PRs, Max sets and progress points), 05 (Weeks, counted Workouts, Streak and consistency messages)

**Status:** done

- [x] Current Plan week and current Phase come from the Plan's start date and the Phase week ranges, with explicit states for before the start and after the last Phase
- [x] Week N of M within the Phase is returned
- [x] Phase goal readiness reports which goals are met (any single Set of the Exercise at or above the goal weight and the goal reps) and whether all are met, with a message
- [x] A Session's Exercise rows for a date resolve through the current Phase's Override when one exists, else the Plan default
- [x] Tests cover Phase boundaries, before start and after end, a goal met by a heavier set, and an Override versus default
