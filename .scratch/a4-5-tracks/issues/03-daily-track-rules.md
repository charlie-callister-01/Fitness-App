# 03: Daily Track rules

**What to build:** A daily Track (the morning mobility routine) works end to end in Core. A day is done when enough of that day's planned rows are done; consecutive done days form a daily Streak with a best run and a total of days done; Break days are excused; today being open never breaks the run; a daily Session is never Missed, so a missed day only ends the run. Spec: "Rules (A2 change)", "Daily Track".

**Blocked by:** 02.

**Status:** ready-for-agent

- [ ] A day is done at exactly `dayDonePercent` of the Workout's own `planned` rows being `done` (default 75), not done below it; tick-only rows count when they have a `done` entry.
- [ ] The daily Streak is the run of consecutive done days ending today; today still open does not break it; a Break day neither adds to nor breaks it.
- [ ] Best run and total days done are derived from history the same way.
- [ ] A daily Session is never Missed and has no make-up offer; the week view data shows it as not done, not as outstanding.
- [ ] Workouts with no `planned` snapshot, or a Track with no done rows, give sensible zero results rather than errors.
- [ ] Time is injected; tests cover the boundary percent, an open today, a Break day inside a run, a gap day ending a run, and a back-dated (`loggedAfterwards`) done day.
