# 05: Weeks, counted Workouts, Streak and consistency messages

**What to build:** The dashboard can say how many counted Workouts this week has, what the Streak is, the best week and the best-ever run, and a consistency message ("on track", "behind", "target out of reach", "target hit"), all from history, the active Plan's rules and Breaks. This also creates the shared Monday-to-Sunday week helper that later tickets use. Spec: `docs/specs/a2-core-rules.md` (Weeks, Streak and consistency stories 44 to 56; "Weeks, Streak, consistency" decisions; assumptions 5 and 6).

**Blocked by:** None (can start immediately)

**Status:** done

- [x] One shared week helper (week identified by its Monday, ISO weeks, crossing month and year boundaries) that other rules reuse
- [x] A counted Workout has at least one completed Exercise, excludes benchmark test Sessions and weigh-ins, and includes Custom workouts only when the Plan's rule says so
- [x] Streak uses the active Plan's minimum, ignores weeks before the Plan start, never breaks on the current week while in progress, and includes the current week only if it already qualifies
- [x] A week touched by a Break neither breaks nor adds to the Streak
- [x] Best week count and best-ever run are exposed for Badges
- [x] Consistency message follows the spec's rule from counted Workouts, weekly target and non-Break days remaining
- [x] Plan week number is available for a given date
- [x] Tests use dated scenarios with explicit "today", including a week crossing a year boundary
