# 09: Week schedule computation and Missed sessions

**What to build:** For any week the app can lay out which Session is on which day (default days, that week's one-off moves, Break days unavailable), with benchmark Sessions only in their interval weeks, and can list the Missed sessions: Sessions whose planned day passed with no Workout made from them. Spec: `docs/specs/a2-core-rules.md` (Outcomes stories 57 to 61, Week schedule stories 69, 70, 75; "Outcomes" and "Week schedule" decisions).

**Blocked by:** 05 (Weeks, counted Workouts, Streak and consistency messages), 06 (Week schedule data in the exchange format)

**Status:** ready-for-agent

- [ ] The Week schedule for a week starts from each Session's default day, applies that week's moves, and marks Break days
- [ ] A benchmark Session appears only in the weeks its interval says and never counts towards the weekly target
- [ ] A Missed session is a scheduled Session on a date before today with no Workout from that Session in the same week; today's Session is not missed
- [ ] A Session made up on a different day in the same week (a Workout made from it) is not missed; Break days and weeks before the Plan start are excluded
- [ ] A Missed session stays missed even if its Exercises were later done in a Custom workout
- [ ] The shape used for the schedule is reusable by rearrangement, Benchmarks and Nudges
- [ ] Tests use explicit "today" and dated scenarios covering each rule above
