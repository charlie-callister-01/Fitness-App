## Summary

A2: every rule the screens will call, as pure, UI-free, storage-free functions in `Core`. Spec `docs/specs/a2-core-rules.md`; tickets `.scratch/a2-core-rules/issues/` (01 to 13). "Today" is always an explicit argument (no real clock in rules). **Merge order:** this branch contains the A1 review-fixes commits, so merge the A1 PR (`worktree-a1-spec`) first.

```text
src/Core/Rules/
├── Units/            # kg/lb, km/mi conversion and display rounding        (01)
├── Parsing/          # forgiving input per log kind -> ParsedSet           (02)
├── LogKinds/         # ONE per-kind table: ranking key, chart value, step, (review)
│                     # lower-is-better, measurement, expected-input text
├── Performance/      # Best, Max set, PRs, progress points, Last/Best/Target (03, 04)
├── Weeks/            # week calendar, counted Workouts, Streak, consistency (05)
│                     # + BreakCalendar (one Break helper)
├── Outcomes/         # per-row outcome, Partial workout                    (07)
├── Phases/           # Phase position, Phase goals, Override rows          (08)
├── Schedule/         # Week schedule layout, Missed sessions, benchmark weeks (09)
├── Rearrangement/    # proposed re-arranged week (draft only)             (10)
├── CustomWorkout/    # Missed-session Exercises offered first            (story 64)
├── Badges/           # Badge progress from history                         (11)
├── Benchmarks/       # Benchmarks list, pre-fill, next due                 (12)
└── Nudges/           # missed / one short / repeated pain / no Backup      (13)
```

```diff
 Model/Set (WorkoutSet)
+  measurementCm            # optional; body measurements in cm get their own field
 Model/
+  WeekSchedule, SessionMove # optional Week schedule collection (one-week-only Session moves)
 data-spec
+  weekSchedules, measurementCm, Badge metrics, Badge target = any non-zero number
```

Non-obvious decisions:

- **Single seam.** All behaviour is tested through the public rules surface with in-memory data (same idea as A1's exchange-format seam). Each ticket was built by its own agent in its own folder; a review pass then removed the cross-ticket duplication (one Break helper, one week/date helper, one step table, one shared test-data builder, one `LogKindRules` table replacing about six `LogKind` switches).
- **Format changes are additive, no version bump:** `weekSchedules` (validated: Monday key, moves inside the week, known Session, no duplicate week/Session), `measurementCm` (> 0, not combined with weight/distance), and Badge `target` relaxed from `> 0` to non-zero (a negative target means "drop by this much" on a measurement). The `bodyweight` Badge case is a **metric** (latest Weigh-in), not a target value, because a target is numeric; the spec was corrected.
- **Assumptions where the brief was silent** (spec Further Notes 1 to 9): first result is a baseline not a PR; RPE <= 6 applies the step twice; carry resets to 20 m after 40 m; consistency is hit/out of reach/behind/on track from remaining Workouts vs non-Break days; "one short" Nudge from Thursday; a Phase goal is met by any single Set at or above goal weight and reps; Benchmark weeks are Plan weeks 1, 1+N, 1+2N. **These should be checked against the old `view.js`** when it is supplied.
- **Backup Nudge** takes the last Backup time as an input (storage is A3/A4), compares calendar days in the Backup's own offset, and fires when none exists.
- **Known leaks, left on purpose:** Workouts dated earlier in the Plan's start week still count towards that week's Streak; `WeekRules.IsCounted` keeps its own benchmark check rather than reusing `BenchmarkWeeks.IsBenchmark`; public API shapes (`IReadOnlyList` vs `IEnumerable`, argument order) and the `Snapshot` overloads are not unified.
- Reviews: `mattpocock-skills:code-review` (Standards and Spec) and the thermo-nuclear review, both applied. Rounds: story 64 added, `NextDue` honours moves and no longer can hang, `Missed` no longer throws, `!`-forgiveness removed from `TargetRules`/`NudgeRules`, `ParseResult` is a closed Success/Failure pair.

## Evidence

- **Before:** Core.Tests 166 passed, Ui.Tests 9 (end of A1).
  **After:** `dotnet test`: Core.Tests 539 passed, Ui.Tests 9 passed, 0 failed.

```text
# examples now asserted (exact values)
parse "22,5x8" (load, de-DE and en-GB)          -> 22.5 kg x 8, identical under both cultures
parse "bwx8"                                    -> bodyweight, 8 reps
100 kg x 5 then 110 kg x 3                      -> PR by Epley e1RM; equal repeat -> not a PR
Max set 1 x 140 kg exists                       -> best = the Max set, not a higher-rep estimate
12/12 reps at 20 kg, RPE 7, increment 2 kg      -> target 22 kg x 10, reason "Hit 12 of 12 ... add 2 kg"
last RPE 9                                      -> same weight and reps
Plan min 4, week touched by a Break             -> neither breaks nor adds to the Streak
Session A Monday, no Workout by Wednesday       -> Missed; stays Missed after a Custom workout
no Backup for 14 days                           -> Backup Nudge (13 days -> none)
benchmark interval 0 / missing                  -> not a benchmark, no exception
```

## Merge Danger

**Door:** two-way

Additive Core code plus three additive format fields. Nothing is stored, shipped or wired to UI yet, so reverting is a plain revert.

**Blast Radius:** Core rules and exchange format

Later stages build directly on these APIs (A3 storage, A4 export/import, all UI). The exchange document now accepts `weekSchedules`, `measurementCm` and negative Badge targets, so an A1-only reader would warn on the first two and reject a negative target. No such reader has shipped. Rule behaviour rests on the nine named assumptions, so expect small tuning once `view.js` is compared.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
