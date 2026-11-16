# 03: Best, PRs, Max sets and progress points

**What to build:** For any Exercise the app can say what the best Set is, whether a Workout set a PR, what "Last" and "Best" were for the log screen, and what the progress points are for its chart. All of it is worked out from history in date order and never stored. Spec: `docs/specs/a2-core-rules.md` (Best, PRs and Max sets stories 19 to 31; "Exercise performance" decisions).

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Best Set ordering per log kind: load by Epley estimated 1RM; bodyweight sets rank below any loaded set; carry by weight then distance; time lowest; seconds, minutes, reps and distance highest; body measurements, text and tick-only have no best
- [ ] When a Max set exists it is the source of truth for the best (the best Max set)
- [ ] A PR is a Workout's best Set that strictly beats the best of all earlier Workouts; an equal repeat is not a PR; the first ever result is a baseline, not a PR
- [ ] PRs are recomputed in chronological order from all history, so a back-dated Workout slots in correctly
- [ ] Only Sets from done or swapped entries count, attributed to the Exercise actually done (a Swapped exercise's Sets do not count for the planned original)
- [ ] Progress points give one point per day (the day's best Set) and a series is only offered for an Exercise logged on two or more days
- [ ] Body measurements report change since the start and never produce PRs
- [ ] Last, Best and (later) Suggested target are returned together as one per-Exercise snapshot shape for the log screen; Last and Best are filled by this ticket
- [ ] Tests build short dated histories and assert the exact best, PR flags and points, including the edge cases listed in the spec
