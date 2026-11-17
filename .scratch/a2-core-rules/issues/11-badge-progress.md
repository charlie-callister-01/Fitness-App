# 11: Badge progress

**What to build:** Every Badge shows value, target, percentage and reached-or-not, always worked out from history and never stored. Spec: `docs/specs/a2-core-rules.md` (Badges and Benchmarks stories 76 to 80; "Badges and Benchmarks" decisions).

**Blocked by:** 03 (Best, PRs, Max sets and progress points), 05 (Weeks, counted Workouts, Streak and consistency messages)

**Status:** done

- [x] Supported metrics: sessions (counted Workouts), streak, best week, prs, or an Exercise (its best value in its own measure)
- [x] A target of `bodyweight` means the latest Weigh-in; a negative target on a measurement means a drop of that size from the start
- [x] An unknown metric returns a "not measurable" result carrying the metric name and never throws
- [x] Progress percentage is capped sensibly and reached is true at or past the target
- [x] Tests cover each metric, the bodyweight target, a negative target and an unknown metric
