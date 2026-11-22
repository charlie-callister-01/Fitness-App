# 05: Core: Custom workout creation and start

Read `AGENTS.md` first.

Stage: b3-home   Wave: 1   Blocked by: None
Branch: b3-home/05-core-custom-workout-creation   Project(s) you may edit: Core, Core.Tests

## What to build

Create and save an in-progress **Custom workout** from a list of Exercises, with no Session (so a Missed session stays Missed).

- `CustomWorkoutStart.Create(Plan plan, IReadOnlyList<SessionExercise> rows, DateOnly date, IIdGenerator ids, IClock clock)` returns a Workout with `SessionId` null, `LoggedAfterwards` false, `StartedAt` now, no `EndedAt`, no entries, no Pain check and `Planned` equal to `rows`. Put it in a new file in `Core.Rules.Starting`.
- `CustomWorkoutStart.RowsForExercises(Plan plan, IReadOnlyList<string> exerciseIds, DateOnly today)` builds the rows for Exercises picked from the library: for each id, in the order given, the row of that Exercise in the active Plan (search the Plan's Sessions using `PhaseRules.ExerciseRows(plan, sessionId, today)` so the current Phase's rows apply; first match wins) with its Sets, Reps and RepsMax but **Cue kept**; if the Exercise is in no Session, a plain row `SessionExercise(id, 3, null, null, null)`. Duplicates in the input are dropped (keep the first).
- `CustomWorkoutStart.RowsForMakeUp(Plan plan, Session session, DateOnly today)` returns a copy of the Session's rows as in force that week (`PhaseRules.ExerciseRows(plan, session.Id, today)`).
- Add `WorkoutStarter.StartCustomAsync(Plan plan, IReadOnlyList<SessionExercise> rows, DateOnly today)` returning `StartOutcome` with the same behaviour as `StartAsync`: if a Workout is already in progress return `StartOutcome.Continued(existing)` and save nothing; if `rows` is empty return `new StartOutcome(null, null)` and save nothing; otherwise create and `Change.Put` it and return `StartOutcome.Saved(...)`.

## Read first
- `src/Core/Rules/Starting/WorkoutStart.cs` and `WorkoutStarter.cs` (follow their style exactly)
- `src/Core/Rules/Phases/PhaseRules.cs` (`ExerciseRows`)
- `src/Core/Model/Plan.cs` (`SessionExercise`, `Session`), `src/Core/Model/Workout.cs`
- `src/Core.Tests/Rules/Starting/WorkoutStarterTests.cs` and `WorkoutStartTests.cs` (style, fakes for ids, clock and store)
- `src/Core.Tests/Rules/TestData/PlanFixture.cs`
- Glossary terms used: Custom workout, Missed session, Session

## Files you may create or edit
- `src/Core/Rules/Starting/CustomWorkoutStart.cs` (new)
- `src/Core/Rules/Starting/WorkoutStarter.cs` (add `StartCustomAsync` only)
- `src/Core.Tests/Rules/Starting/CustomWorkoutStartTests.cs` (new)
Anything else is out of scope. If you need it, write a blocked report.

## Acceptance
- [ ] `Create`: no Session, not logged afterwards, started now, no end, no entries, `Planned` equals the rows
- [ ] `RowsForExercises`: order kept; a planned Exercise takes its Plan row including Cue; an unplanned one gets 3 Sets and no reps; duplicates dropped; the current Phase's Override rows are used when present
- [ ] `RowsForMakeUp`: matches the Session's rows as in force that week
- [ ] `StartCustomAsync`: saves a new Workout; continues one in progress without saving; empty rows save nothing; a failed save returns no Workout (as `StartAsync`)
- [ ] A Custom workout made for a Missed Session leaves that Session still Missed (test via `WeekScheduleRules.MissedInWeek`)
- [ ] Tests written first (red run shown in the report), full `dotnet build` and `dotnet test` green
- [ ] No data model, exchange format or storage change

## Do not
- Do not change `Create`, `CreatePastDated`, `StartAsync` or `StartLoggedAfterwardsAsync`.
- Do not mark the Workout with a Session id for Make up.
