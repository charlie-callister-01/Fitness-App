# 07: Core: Home view builder

Read `AGENTS.md` first.

Stage: b3-home   Wave: 2   Blocked by: 01, 02, 03, 04
Branch: b3-home/07-core-home-view-builder   Project(s) you may edit: Core, Core.Tests

## What to build

The one seam for the Home page: a pure builder that composes the rules from tickets 01 to 04 into a single description of the screen.

`HomeViewBuilder.Build(DataSnapshot snapshot, DateTimeOffset now, DateOnly today, int localHour, IReadOnlySet<string> dismissedNudgeKeys, DateTimeOffset? lastBackup)` returns a `HomeView` record:
- `Header`: `DateText` source (`Date` as `today`), `PhaseLabel` (for example "Foundation week 5 of 8"; empty when there is no Phase position), `Greeting` ("Good morning" for hours 0 to 11, "Good afternoon" 12 to 17, "Good evening" 18 to 23).
- `Today`: the `TodayCard` from `HomeTodayRules.Compute` (ticket 01).
- `UndoneSessions`: from `HomeTodayRules.UndoneSessions`.
- `Strip` and `Line`: from `HomeWeekRules` (ticket 02). `Orbs`: from `HomeOrbs.Compute`.
- `Nudges`: `NudgeRules.Compute(plan, schedules, breaks, workouts, lastBackup, today)` passed through `HomeNudges.Select(computed, dismissedNudgeKeys, today)` (ticket 03). Also `NudgeKeys` = `HomeNudges.KeysFor(computed, today)` so the Ui can look up which are dismissed. Empty when there is no active Plan.
- `RecentPrs`: `RecentPrs.Latest(snapshot, today)`. `Weight`: `WeightPanelRules.Compute(snapshot.WeighIns, today)`.
- Visibility flags, all derived here so the page only reads them:
  - `ShowAlternatives` (Do a different Session, Build a Custom workout): true only when there is an active Plan and `Today.Kind` is not Break, InProgress, NoPlan.
  - `ShowLogPastWorkout`: the same rule as `ShowAlternatives`.
  - `ShowNudges`: false when `Today.Kind` is Break or NoPlan; otherwise true (the list may still be empty).
  - `ShowLogWeight`: always true.
  - `Mode`: a small enum `HomeMode { NoPlan, Normal }` is not needed; use `Today.Kind`.
- `Build` calls each rule once and does no extra computation of its own beyond the greeting, the phase label and the flags. It must not throw for an empty snapshot (no Plan): return a `HomeView` with `Today.Kind == NoPlan`, empty lists, null orbs, a panel with all nulls, and flags false except `ShowLogWeight`.

The rules from tickets 01 to 04 are already merged when you start. Read their source for exact member names; do not change them. If a member you need is missing or wrong, write a blocked report naming it.

## Read first
- `src/Core/Rules/Home/HomeTodayRules.cs`, `TodayCard.cs` (ticket 01, merged)
- `src/Core/Rules/Home/HomeWeekRules.cs`, `WeekTile.cs`, `HomeOrbs.cs` (ticket 02, merged)
- `src/Core/Rules/Home/HomeNudges.cs`, `RecentPrs.cs` (ticket 03, merged)
- `src/Core/Rules/Home/WeightPanelRules.cs` (ticket 04, merged)
- `src/Core/Rules/Nudges/NudgeRules.cs`, `src/Core/Rules/Phases/PhaseRules.cs`
- `src/Core/Storage/DataSnapshot.cs`
- `src/Core.Tests/Rules/TestData/PlanFixture.cs`, `WorkoutBuilder.cs`, `HistoryBuilder.cs`, `Dates.cs`
- Glossary terms used: Plan, Phase, Nudge, Break, Workout

## Files you may create or edit
- `src/Core/Rules/Home/HomeViewBuilder.cs`, `src/Core/Rules/Home/HomeView.cs` (new)
- `src/Core.Tests/Rules/Home/HomeViewBuilderTests.cs` (new)
Anything else is out of scope. If you need it, write a blocked report.

## Acceptance
- [ ] Greeting at hours 0, 11, 12, 17, 18, 23
- [ ] Empty snapshot: NoPlan, no exceptions, flags as described
- [ ] Break day: `ShowAlternatives`, `ShowLogPastWorkout` and `ShowNudges` false, Nudges empty, Streak orb `OnBreak`, `ShowLogWeight` true
- [ ] In progress: Today is InProgress, alternatives and past-Workout hidden, Nudges still shown
- [ ] Normal week: Today, strip, line, orbs, Nudges (capped at two), Recent PRs and Weight all populated from one fixture and consistent with each other (for example the Missed code in the line equals the Missed Nudge's Session)
- [ ] A dismissed key removes that Nudge
- [ ] `lastBackup` null gives the "No Backup yet" Nudge; a recent one does not
- [ ] Tests written first (red run shown in the report), full `dotnet build` and `dotnet test` green
- [ ] No data model, exchange format or storage change

## Do not
- Do not re-implement any rule from tickets 01 to 04 here, and do not edit those files.
- Do not read the clock or time zone: `now`, `today` and `localHour` are parameters.
