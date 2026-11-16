# 12: Benchmarks

**What to build:** The Benchmarks screen and the benchmark Session can get everything from one place: each Benchmark exercise's current best, previous benchmark-test result and change, the pre-filled rows for a benchmark Session with the previous result beside each, and when the next benchmark is due. Spec: `docs/specs/a2-core-rules.md` (Badges and Benchmarks stories 81 to 83).

**Blocked by:** 03 (Best, PRs, Max sets and progress points), 09 (Week schedule computation and Missed sessions)

**Status:** ready-for-agent

- [ ] List Benchmark exercises with current best, previous benchmark-test result and the change between them
- [ ] Pre-fill rows for a benchmark Session come from the Benchmark exercises, each with its previous result
- [ ] Next benchmark due date comes from the Session's interval weeks and the last benchmark Workout, using the same benchmark-week scheduling as ticket 09
- [ ] A benchmark Workout never counts towards the weekly target or Streak (consistent with ticket 05)
- [ ] Tests cover a first benchmark with no previous result, a repeat with improvement and with regression, and the due date
