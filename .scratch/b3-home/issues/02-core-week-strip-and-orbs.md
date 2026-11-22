# 02: Core: week strip, week line and orbs

Read `AGENTS.md` first.

Stage: b3-home   Wave: 1   Blocked by: None
Branch: b3-home/02-core-week-strip-and-orbs   Project(s) you may edit: Core, Core.Tests

## What to build

Pure rules for the Home week strip, the week line and the three status orbs. Namespace `Core.Rules.Home`. All take the active Plan data and an explicit `today`.

- `HomeWeekRules.Strip(DataSnapshot snapshot, DateOnly today)` returns seven `WeekTile`s, Monday to Sunday of today's week. A tile has: the date, a weekday label, `IsToday`, the Session scheduled that day (null on a rest day; if two, the first in layout order), its **code**, its `SessionType`, and a `TileState`: `Done`, `Partial`, `Missed`, `BreakDay`, `Upcoming`, `Rest`.
  - A Session is Done (or Partial when `WorkoutOutcomes.Of(workout).IsPartial`) when a Workout with that `SessionId` exists in the week. A scheduled date before today with no Workout is Missed (use `WeekScheduleRules.MissedInWeek`). A Break day (`BreakCalendar`) is BreakDay and is never Missed. A scheduled Session today or later with no Workout is Upcoming. No Session that day is Rest.
  - **Session code:** the text of the Session name before the first colon when that text is 1 to 3 characters long; otherwise the first letter of the name, upper-cased. ("A: Lower + core" gives "A"; "Full body" gives "F".)
- `HomeWeekRules.Line(DataSnapshot snapshot, DateOnly today)` returns `WeekLine(Done, Scheduled, Target, MissedCodes)`: `Done` from `WeekRules.CountedWorkouts` for this week, `Scheduled` the number of Sessions in the week's layout, `Target` is `plan.WeeklyTarget`, `MissedCodes` the codes of Missed Sessions in date order. The page renders "This week 1 of 5 · target 5 · Missed: A".
- `HomeOrbs.Compute(DataSnapshot snapshot, DateOnly today)` returns `HomeOrbs(StreakOrb, PhaseOrb?, RaceOrb?)`:
  - `StreakOrb(Current, ThisWeekCount, Minimum, OnBreak)`: `Current` from `WeekRules.Streak(...).Current`, `ThisWeekCount` from `WeekRules.CountedWorkouts` for this week, `Minimum` is `plan.StreakMinWorkoutsPerWeek`, `OnBreak` true when today is a Break day. The ring fraction is `min(ThisWeekCount / Minimum, 1)` (0 when Minimum is 0).
  - `PhaseOrb(WeekInPhase, PhaseLengthWeeks)`: from `PhaseRules.Position`; null unless the state is InPhase.
  - `RaceOrb(DaysToGo, ElapsedShare)`: null when the Plan has no `GoalDate`. `DaysToGo` is the days from today to `GoalDate`, never below 0. `ElapsedShare` is `(today - StartDate) / (GoalDate - StartDate)` clamped to 0..1 (0 when the span is zero or negative).
- All return values are plain immutable records. If there is no active Plan, return empty or null results (no tiles, null orbs): the page shows its own empty state.

## Read first
- `src/Core/Rules/Schedule/WeekScheduleRules.cs`, `WeekLayout.cs`
- `src/Core/Rules/Weeks/WeekRules.cs`, `WeekCalendar.cs`, `BreakCalendar.cs`
- `src/Core/Rules/Phases/PhaseRules.cs`
- `src/Core/Rules/Outcomes/` (`WorkoutOutcomes.Of`, `IsPartial`)
- `src/Core/Model/Plan.cs` (Session, SessionType, Plan fields), `src/Core/Storage/DataSnapshot.cs`
- `src/Core.Tests/Rules/TestData/PlanFixture.cs`, `WorkoutBuilder.cs`, `Dates.cs`; style reference `src/Core.Tests/Rules/Weeks/WeekRulesTests.cs`
- Glossary terms used: Session, Workout, Missed session, Partial workout, Break, Streak, Phase

## Files you may create or edit
- `src/Core/Rules/Home/HomeWeekRules.cs`, `src/Core/Rules/Home/WeekTile.cs`, `src/Core/Rules/Home/HomeOrbs.cs` (new)
- `src/Core.Tests/Rules/Home/HomeWeekRulesTests.cs`, `src/Core.Tests/Rules/Home/HomeOrbsTests.cs` (new)
Anything else is out of scope. If you need it, write a blocked report.

## Acceptance
- [ ] Strip: seven tiles Monday to Sunday; Done, Partial, Missed, BreakDay, Upcoming, Rest each covered; a Break day is never Missed; today flagged; a moved Session shows on its moved day
- [ ] Session code: "A: Lower + core" is "A", "Full body" is "F", "AB: x" is "AB", "Long name: x" is "L"
- [ ] Line: counts and Missed codes in date order; zero Missed gives an empty list
- [ ] Streak orb: current, ring fraction, minimum 0, OnBreak true on a Break day (number kept)
- [ ] Phase orb: week x of y inside a Phase; null before the start, after the end and in a gap
- [ ] Race orb: null without a goal date; days never negative; elapsed share clamped; zero-length span
- [ ] Tests written first (red run shown in the report), full `dotnet build` and `dotnet test` green
- [ ] No data model, exchange format or storage change

## Do not
- Do not edit existing rules; call them.
- Do not read the clock.
- Do not format display text for the Ui beyond the code and weekday label.
