# 06: Daily Track rules (PR 1)

**Read first:** `.scratch/a4-5-tracks/HANDOFF.md`, `docs/specs/a4-5-tracks.md` ("Daily Track"), `docs/specs/a2-core-rules.md` (Streak, Breaks), `src/Core/Rules/Weeks/WeekRules.cs`, `Rules/Weeks/BreakCalendar.cs`, `Rules/Outcomes/WorkoutOutcomes.cs`.

**Parent:** spec `docs/specs/a4-5-tracks.md`, PR 1.

**What to build:** the rules for a daily Track, in Core, tested through public entry points with injected time.
- **Day done:** a Workout of the Track on that date with at least `DayDonePercent` of its `planned` rows done (`done * 100 >= percent * planned`, integer arithmetic). A tick-only row counts when it has a `done` entry; skipped and swapped-away rows do not count as done; a Workout with no planned rows is not done. Several Workouts of the Track on one date: the day is done if any one is.
- **Daily Streak:** the run of consecutive done days ending today. An open today does not break it; a Break day is neutral; days before the Plan's start date do not count.
- **Best run** and **days done** (total) from the same day set.
- **A daily Session is never Missed**, and there is no Missed Nudge for it; the Week layout rules from ticket 04 already skip it.
- A small result type (current run, best run, days done, today's done state) that Home and Badges (tickets 07 and 08) read. Suggested home: `Core/Rules/Tracks/DailyTrackRules.cs`.

**Blocked by:** 04.

**Status:** resolved
Notes: Implemented `DailyTrackRules` and `DailyTrackStatus` in `src/Core/Rules/Tracks/DailyTrackRules.cs` with `Status`, `IsDayDone`, `IsWorkoutDone`, and `DonePercent`, plus unit tests in `src/Core.Tests/Rules/Tracks/DailyTrackRulesTests.cs`.

- [x] Tests: done at exactly 75%, just below, above, 100%; 1 of 10 rows is not done at 75%; today open does not break the run and today done adds to it; a Break day inside a run is neutral; a missed day ends the run; best run and total days; two Workouts on a date; a Workout from a weekly Track on the same date is ignored; days before the start date ignored; a daily Session is never reported Missed.
- [x] A custom `dayDonePercent` (for example 50) changes the result.
- [x] `dotnet build` 0 warnings; all tests pass.

## Comments
- `DailyTrackRules.IsWorkoutDone` uses integer arithmetic `done * 100 >= dayDonePercent * planned` with `WorkoutOutcomes.Of` counting `RowOutcomeKind.Done` only (so tick-only `Done` rows count while `Skipped`, `Swapped`, `Unrecorded`, and unplanned extras do not, and 0-planned-row Workouts never count).
- `DailyTrackRules.Status` returns `DailyTrackStatus(Current, BestRun, DaysDone, TodayDone)` over `[plan.StartDate .. today]`, treating Break days as neutral for the Streak (`Current` and `BestRun`) while keeping all done days in `DaysDone` and `TodayDone`, matching `WeekRules.Streak` / `CountedWorkoutsByWeek` semantics.
