# A2 handoff (paused 2026-11-17)

Branch `worktree-a2-spec` (pushed). All 13 tickets are implemented and merged on it; 525 Core + 9 Ui tests pass. It already contains the A1 review fixes (A1 branch `worktree-a1-spec`, PR body in `.scratch/a1-review-fixes/pr-body.md`; not yet opened as a PR).

## Where the workflow stands (CLAUDE.md steps)
- Steps 1 to 3 done: spec `docs/specs/a2-core-rules.md`, tickets here, implemented.
- Step 4 (`mattpocock-skills:code-review`) done; results below.
- Step 6 (apply fixes) PARTLY done. Step 5 (thermo-nuclear review) and step 7 (`/pr`) NOT done.
- Do not start A3 until the A2 PR body is written.

## Review fixes already applied
- A1 `measurementCm` optional Set field (model, reader, validator, data spec, tests); cm kinds now read from it.
- A2 Badge `target` is any non-zero number (negative = drop).
- A3 (partial) spec text corrected for Badge `bodyweight` metric, Phase goal (any single Set), Benchmark week rule.

## Still to do (fix list)
- A3 leftover: align the text of tickets 08 and 11 in `.scratch/a2-core-rules/issues/` with the code.
- B: story 64, Custom workout offer order (Missed-session Exercises first, then library). New rule in `src/Core/Rules/CustomWorkout/`, tests, and update the spec's coverage note.
- C: bugs. `BenchmarkRules.NextDue` ignores Week schedule moves; `BenchmarkWeeks.IsScheduledInWeek` modulo throws on interval 0 (guard it); `NudgeRules.Missed` uses a throwing `First` (carry the Session name on `MissedSession`); Backup Nudge day comparison is not offset-correct (make it so and document).
- D: de-duplication. One shared Break helper (used by WeekRules, WeekScheduleRules, NudgeRules); `WeekCalendar.InWeek` and `DefaultDate` replacing six inline Monday..Sunday filters; one per-kind step table in `TargetRules`; the `"best week"` alias in `BadgeRules` (story 77 says `best week`, data spec says `bestweek`; decide first).
- E: consolidate the three test-history builders (`History`, `WeekHistory`, `WorkoutBuilder`) and copied helpers into one shared builder under `src/Core.Tests/Rules/`; deterministic Set IDs (no `Guid.NewGuid()`).
- Deferred on purpose: unifying `IReadOnlyList` vs `IEnumerable` and argument order across Rules APIs; `ExercisePerformance.Snapshot` overloads.
- Known data gap not addressed: Phase goal uses "any single Set" (assumption 8); Streak counts Workouts dated before the Plan start inside the start week.

## Then
1. Thermo-nuclear review of the A2 diff (`git diff b4735c1...HEAD`): read `.claude/skills/thermo-nuclear-code-quality-review/SKILL.md` and apply it (C# intent).
2. Apply its fixes; run `dotnet test`.
3. `/pr` for A2 (and open the A1 PR from `worktree-a1-spec` first; A2 branch contains A1's commits, so merge order is A1 then A2).
4. Nothing has been merged to `main` and no PR is open for either stage (`gh` was not installed on the first machine).

## Open assumptions to confirm against the old `view.js` (not in the repo)
See "Named assumptions" in `docs/specs/a2-core-rules.md` Further Notes.
