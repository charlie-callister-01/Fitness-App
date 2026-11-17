# 11: Badge progress

**What to build:** Every Badge shows value, target, percentage and reached-or-not, always worked out from history and never stored. Spec: `docs/specs/a2-core-rules.md` (Badges and Benchmarks stories 76 to 80; "Badges and Benchmarks" decisions).

**Blocked by:** 03 (Best, PRs, Max sets and progress points), 05 (Weeks, counted Workouts, Streak and consistency messages)

**Status:** done

- [x] Supported metrics: sessions (counted Workouts), streak, bestweek (best week), prs, bodyweight (Weigh-ins), or an Exercise (its best value in its own measure)
- [x] `bodyweight` is a metric (the latest Weigh-in), not a target value; a target is any non-zero number, and a negative target on a measurement means a drop of that size from the start
- [x] An unknown metric returns a "not measurable" result carrying the metric name and never throws
- [x] Progress percentage is capped sensibly and reached is true at or past the target
- [x] Tests cover each metric, the bodyweight target, a negative target and an unknown metric
