# 02: Core: per-Set PR feedback

**What to build:** A pure Core rule used while a Workout is open. Given an Exercise, the history excluding the current Workout, and the current Workout's confirmed Sets so far for that Exercise, it answers: which Set (if any) is this Workout's best so far, whether it strictly beats the previous Best by the Exercise's kind rules, and the previous Best (for "PR! 22 kg × 9 beats 22 × 8" and "was …" text). Reuse `SetRanking`, `LogKindRules` and `ExercisePerformance` (Max set is the source of truth; the first ever result is a baseline and not a PR; lower is better for time). Only the Workout's single best Set carries the marker; a later Set that beats an earlier PR Set moves it. Nothing is stored. Spec: `docs/specs/b1-workout-log-core-input.md` ("per-Set PR feedback"); design §8.3.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] Correct for every log kind that ranks (load, bodyweight load, carry, reps, seconds, minutes, km, m, time)
- [x] Baseline is not a PR; equal is not a PR; strictly better is
- [x] Max set precedence both ways (a Max set in history; a Max set in the current Workout)
- [x] Marker moves to a later better Set; removing or editing the Set changes the answer
- [x] Agrees with `ExercisePerformance.Prs` once the Workout is finished (test)
- [x] Core unit tests in the style of `ExercisePerformanceTests`
