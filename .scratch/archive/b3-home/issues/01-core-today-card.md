# 01: Core: TodayCard state

Read `AGENTS.md` first.

Stage: b3-home   Wave: 1   Blocked by: None
Branch: b3-home/01-core-today-card   Project(s) you may edit: Core, Core.Tests

## What to build

A pure rule that decides what Home's TodayCard shows, plus the list of this week's undone Sessions. New namespace `Core.Rules.Home`.

- `HomeTodayRules.Compute(DataSnapshot snapshot, DateTimeOffset now, DateOnly today)` returns a `TodayCard` record. Use an enum `TodayKind` { NoPlan, Break, InProgress, DoneToday, PlanFinished, NotStarted, Start, RestDay } and carry the data each kind needs on the record: the Session (Start), the in-progress Workout with `DoneCount`, `PlannedCount` and `Elapsed` (InProgress), the done summary (DoneToday: duration, Set count, PR count, whether Partial), the next Session and its date (RestDay), the Plan start date (NotStarted). You choose the exact members; keep them plain and immutable.
- `HomeTodayRules.UndoneSessions(DataSnapshot snapshot, DateOnly today)` returns this week's scheduled Sessions (as `ScheduledSession`) that have no Workout from them this week, in date order. Missed ones are included.

**Precedence, first match wins:**
1. No active Plan (`plan.IsActive`) gives NoPlan.
2. Today is a Break day (`BreakCalendar.IsBreakDay`) gives Break.
3. A Workout in progress (`WorkoutStart.InProgress`, any date, including a past-dated one) gives InProgress. `DoneCount` is the rows counted Done or Swapped by `WorkoutOutcomes.Of`; `PlannedCount` is `Planned.Count`; `Elapsed` is `now - StartedAt`, null when the Workout has no start time (Logged afterwards).
4. A finished Workout dated today (`EndedAt` not null, `Date == today`; if several, the latest) gives DoneToday. Take duration, Set count, PR count and Partial from `WorkoutSummaryBuilder.Build(workout, BadgeHistories.For(snapshot, workout), snapshot.Badges, today)`.
5. `PhaseRules.Position(plan, today).State == PhaseState.AfterEnd` gives PlanFinished.
6. `PhaseState.BeforeStart` gives NotStarted. (`NoPhase` is treated as a normal day.)
7. A Session scheduled today in this week's layout (`WeekScheduleRules.Layout`; a Session moved to today counts) with no Workout from it this week gives Start for that Session. If two are due, the first in layout order.
8. Otherwise RestDay. The next Session is the first undone Session with a date after today this week; when none is left, the first Session (by date) of next week's layout (`Layout` for the next Monday). `NextDate` is that date. If the Plan has no Sessions at all, RestDay with no next Session.

"A Workout from a Session this week" means a Workout whose `SessionId` matches and whose date is in the same Monday-to-Sunday week as `today` (`WeekCalendar`).

## Read first
- `src/Core/Rules/Starting/WorkoutStart.cs`: `InProgress`, `NextToStart` (do NOT reuse `NextToStart`; it offers Missed Sessions)
- `src/Core/Rules/Schedule/WeekScheduleRules.cs` and `WeekLayout.cs`: `Layout`, `ScheduledSession`
- `src/Core/Rules/Phases/PhaseRules.cs`: `Position`, `PhaseState`
- `src/Core/Rules/Weeks/WeekCalendar.cs` and `BreakCalendar.cs`
- `src/Core/Rules/Summary/WorkoutSummaryBuilder.cs` and `WorkoutSummary.cs`
- `src/Core/Rules/Outcomes/` (`WorkoutOutcomes.Of`), `src/Core/Rules/Badges/BadgeHistories.cs`
- `src/Core/Storage/DataSnapshot.cs`
- `src/Core.Tests/Rules/TestData/PlanFixture.cs`, `WorkoutBuilder.cs`, `Dates.cs`; one existing test for style: `src/Core.Tests/Rules/Starting/WorkoutStartTests.cs`
- Glossary terms used: Plan, Session, Workout, Break, Partial workout, Missed session

## Files you may create or edit
- `src/Core/Rules/Home/HomeTodayRules.cs` (new), `src/Core/Rules/Home/TodayCard.cs` (new)
- `src/Core.Tests/Rules/Home/HomeTodayRulesTests.cs` (new)
Anything else is out of scope. If you need it, write a blocked report.

## Acceptance
- [ ] Each of the 8 kinds is produced by a test, in the precedence order above
- [ ] Conflict tests: Break plus in progress gives Break; in progress plus finished today gives InProgress; finished today plus another Session due gives DoneToday; Plan finished plus in progress gives InProgress
- [ ] A Session moved to today gives Start; a Session already done this week is not Start
- [ ] Rest day: next Session later this week; when none is left, first Session of next week; Plan with no Sessions
- [ ] A past-dated in-progress Workout gives InProgress with `Elapsed` null
- [ ] `UndoneSessions` returns undone and Missed Sessions in date order and excludes done ones
- [ ] Tests written first (red run shown in the report), full `dotnet build` and `dotnet test` green
- [ ] No data model, exchange format or storage change

## Do not
- Do not change `WorkoutStart.NextToStart` or any existing rule.
- Do not read the clock: `now` and `today` are parameters.
- Do not add a dependency on `Ui` or `Web`.
