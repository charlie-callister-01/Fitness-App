# 03: Core: Nudge selection and Recent PRs

Read `AGENTS.md` first.

Stage: b3-home   Wave: 1   Blocked by: None
Branch: b3-home/03-core-nudge-selection-and-recent-prs   Project(s) you may edit: Core, Core.Tests

## What to build

Two pure rules in `Core.Rules.Home`.

**1. Nudge selection.** `HomeNudges.Select(IReadOnlyList<Nudge> computed, IReadOnlySet<string> dismissedKeys, DateOnly today)` returns at most two `HomeNudge`s.
- `computed` is the output of the existing `NudgeRules.Compute` (already most important first; keep that order).
- `HomeNudge(Nudge Nudge, string Key, NudgeAction Action)`.
- **Key** identifies a Nudge for the week it was shown in: `"{Monday:yyyy-MM-dd}|{Kind}|{subject}"`, where Monday is `WeekCalendar.MondayOf(today)` and subject is the Nudge's `SessionId`, else its `Area` as text, else empty. Same Nudge, same week gives the same key; next week gives a different key (so a dismissed Nudge returns next week if it still applies).
- Remove any Nudge whose key is in `dismissedKeys`, then take the first two.
- **Action** by kind (`enum NudgeAction { None, MakeUp, DifferentSession, BackUp }`): MissedSession gives MakeUp; OneShortOfTarget gives DifferentSession; NoRecentBackup gives BackUp; RepeatedPain gives None (Dismiss only in this stage).
- Also expose `HomeNudges.KeysFor(IReadOnlyList<Nudge> computed, DateOnly today)` returning each computed Nudge's key, so the Ui can ask the device store which are dismissed.

**2. Recent PRs.** `RecentPrs.Latest(DataSnapshot snapshot, DateOnly today, int count = 5)` returns up to `count` `RecentPr(DateOnly WorkoutDate, string ExerciseId, string ExerciseName, WorkoutSet Set, BestResult? PreviousBest)`, newest first.
- Only finished Workouts (`EndedAt` not null). Order by Workout date descending, then by `EndedAt` descending.
- For each Workout in that order take the PRs from `WorkoutSummaryBuilder.Build(workout, BadgeHistories.For(snapshot, workout), snapshot.Badges, today).Prs` and stop once `count` are collected. The summary rule already judges a Workout against history on or before its own date, so reuse it as is.
- `ExerciseName` comes from `snapshot.Exercises` by id; if the Exercise is missing use the id.

## Read first
- `src/Core/Rules/Nudges/NudgeRules.cs`
- `src/Core/Rules/Summary/WorkoutSummaryBuilder.cs`, `WorkoutSummary.cs` (`SummaryPr`)
- `src/Core/Rules/Badges/BadgeHistories.cs`
- `src/Core/Rules/Performance/PerformanceTypes.cs` (`BestResult`)
- `src/Core/Rules/Weeks/WeekCalendar.cs`
- `src/Core/Storage/DataSnapshot.cs`
- `src/Core.Tests/Rules/Nudges/NudgeRulesTests.cs` (style), `src/Core.Tests/Rules/TestData/HistoryBuilder.cs`, `WorkoutBuilder.cs`, `Dates.cs`
- Glossary terms used: Nudge, PR, Workout, Missed session

## Files you may create or edit
- `src/Core/Rules/Home/HomeNudges.cs`, `src/Core/Rules/Home/RecentPrs.cs` (new)
- `src/Core.Tests/Rules/Home/HomeNudgesTests.cs`, `src/Core.Tests/Rules/Home/RecentPrsTests.cs` (new)
Anything else is out of scope. If you need it, write a blocked report.

## Acceptance
- [ ] Nudges: order kept; capped at two; a dismissed key is removed and the next Nudge takes its place; key differs between weeks and between subjects; Action mapping for all four kinds
- [ ] Empty input gives an empty list
- [ ] Recent PRs: newest Workout first; capped at 5 by default and by `count`; previous best carried; unfinished Workouts ignored; no PRs gives an empty list; a missing Exercise falls back to the id
- [ ] Tests written first (red run shown in the report), full `dotnet build` and `dotnet test` green
- [ ] No data model, exchange format or storage change

## Do not
- Do not change `NudgeRules` or the PR rules. Do not store anything.
- Do not call `NudgeRules.Compute` inside `Select`: it receives the computed list.
