---
labels: ready-for-agent
stage: A2
---

# A2: Core rules

Parent docs: [`../product-brief.md`](../product-brief.md) ("Rules carried over from the old system"), [`../data-spec.md`](../data-spec.md), [`../technical-brief.md`](../technical-brief.md), [`../../GLOSSARY.md`](../../GLOSSARY.md). Builds on the A1 model and the in-memory `ExchangeDocument`.

## Problem Statement

The owner's old Obsidian system keeps its training rules (how a Set is typed, what counts as a PR, what to lift next time, what keeps a Streak alive, which Sessions were missed) inside one large `view.js`, tangled with display code and untested. The A1 data model now stores everything, but nothing in `Core` can yet answer the questions the app exists to answer: "Is this a PR?", "What should I lift next time and why?", "Is my Streak alive?", "Which Sessions did I miss and how do I make them up?", "How far is each Badge?". Until these rules exist as tested, UI-free code, no screen can be built without inventing rules in the UI (which the architecture forbids).

## Solution

Stage A2 delivers, entirely inside `Core` with no UI, no storage and no JavaScript, **every rule the screens will call**, as pure functions over the in-memory data model and an injected "today":

1. **Input parsing** for every log kind, forgiving of how the owner types.
2. **Unit conversion** between canonical units and the display units (kg/lb, km/mi).
3. **Best, PRs and Max sets**, **Suggested target** (with RPE and a one-line reason), and per-day **progress points**.
4. **Weeks, Streak and consistency messages**, honouring Breaks, Plan rules and the counted-Workout rules.
5. **Workout outcomes**: Missed session, Partial workout, Skipped and Swapped handling.
6. **Phase position** (which Phase and week are current, Phase goal readiness).
7. **Week schedule** (per-week Session placement) and **week-rearrangement suggestions**.
8. **Badges** and **Benchmarks** progress, worked out from history.
9. **Nudges**, as a plain list of in-app messages computed from the data.

Rules are written to match the product brief's carried-over rules exactly. Where the brief is silent, this spec records the chosen behaviour as a named assumption (see Further Notes) so it can be corrected in one place.

## User Stories

### Input parsing
1. As the owner, I want to type `20x10`, `20 x 10`, `20×10` or `20*10` for a loaded Set, so that I can log with whichever symbol my phone keyboard makes easiest.
2. As the owner, I want `22,5x8` to read as 22.5 kg for 8 reps, so that a comma decimal keyboard works.
3. As the owner, I want `bwx8` to log 8 bodyweight reps, so that bodyweight work is as quick as loaded work.
4. As the owner, I want a bodyweight Exercise with extra weight (a weighted pull-up) to take added kg and reps, so that progress with a belt is captured.
5. As the owner, I want a carry typed as `32x40` to mean 32 kg per hand for 40 m, so that carries need no special syntax.
6. As the owner, I want a time typed as `4:32` (or `1:04:10`) to be read as a duration, so that runs and timed efforts are logged naturally.
7. As the owner, I want `45` for a seconds Exercise, `3` for a minutes Exercise and `5` for a km Exercise to read in that Exercise's unit, so that I don't type units.
8. As the owner, I want an explicit unit suffix (`45s`, `3min`, `5km`, `400m`) accepted where it is unambiguous, so that I can be explicit when I want.
9. As the owner, I want spaces ignored anywhere in the input, so that stray spaces never cause an error.
10. As the owner, I want a body measurement (kg or cm) logged as one number, so that weigh-ins and tape measures are quick.
11. As the owner, I want a tick-only Exercise to log as done with no number, and a text Exercise to log free text, so that every log kind has a way to be recorded.
12. As the owner, I want input I type in my display units (lb, miles) converted to canonical units before it is stored, so that the switcher never corrupts data.
13. As the owner, I want unparseable input to produce a plain-language reason naming what was expected for that Exercise (for example "Expected weight x reps, like 20x10"), so that I can fix it immediately instead of losing the entry.
14. As a developer, I want parsing to be deterministic and culture-independent, so that a device locale cannot change what a Set means.

### Units
15. As the owner, I want weights and distances shown in my chosen display units (kg or lb, km or miles) with sensible rounding, so that numbers look natural.
16. As the owner, I want some screens able to show both units side by side, so that I can see kg and lb together.
17. As the owner, I want conversion to round-trip without drift (display then re-enter does not change the stored value beyond display rounding), so that repeated edits don't erode data.
18. As the owner, I want the weight step for targets to stay in kg internally even when I display lb, so that suggested targets are consistent.

### Best, PRs and Max sets
19. As the owner, I want the best Set for a loaded Exercise to be the one with the highest estimated 1RM (Epley: kg x (1 + reps/30)), so that 100 kg x 5 and 110 kg x 3 are compared fairly.
20. As the owner, I want bodyweight-only Sets to rank below any loaded Set and be ranked among themselves by reps, so that adding weight always counts as progress.
21. As the owner, I want a carry's best to be the heavier weight and then the longer distance, so that loading a carry beats just walking further.
22. As the owner, I want a time Exercise's best to be the lowest time, and every other measured kind's best to be the highest value, so that "better" always points the right way.
23. As the owner, I want a PR only when a Workout's best Set strictly beats the previous best, so that a repeat of a previous best is never celebrated.
24. As the owner, I want a Max set to be the source of truth for an Exercise's best when one exists, so that a deliberate 1RM attempt isn't overridden by a high-rep estimate.
25. As the owner, I want my first ever log of an Exercise to set a baseline, not count as a PR, so that PR highlights aren't noise.
26. As the owner, I want PRs worked out in date order from all history, so that back-filling an old Workout recalculates correctly.
27. As the owner, I want the same-day best used for each Exercise's progress chart (one point per day), so that charts are readable.
28. As the owner, I want an Exercise's progress points to cover only Exercises logged on two or more days, so that the chart rule in the product brief is a single tested function.
29. As the owner, I want body measurements never to be PRs but to report change since the start, so that weight and tape measures aren't gamified.
30. As the owner, I want skipped Sets-less entries and tick-only entries never to produce PRs, so that nothing meaningless is celebrated.
31. As the owner, I want a Swapped exercise's Sets to count as history for the Exercise actually done, not the planned original, so that PRs and targets stay truthful.

### Suggested target
32. As the owner, I want a load Exercise's next target to add one rep until the top of the planned rep range, so that I progress by reps first.
33. As the owner, I want a load target at the top of the range to add the weight increment and drop reps by 2, so that progression is steady.
34. As the owner, I want a carry target to add 10 m up to 40 m and then add weight, so that carries progress safely.
35. As the owner, I want a time Exercise's target to be to beat my best, so that the target always has a direction.
36. As the owner, I want other numeric Exercises' targets to be the best plus a step, so that every Exercise gets a target.
37. As the owner, I want RPE 9 or 10 on the last Workout to hold the same weight and reps, so that a maxed-out effort isn't punished with a harder target.
38. As the owner, I want RPE 6 or below to suggest a bigger jump, so that easy work leads to faster progression.
39. As the owner, I want every Suggested target to come with a one-line reason ("Hit 12 of 12 reps at RPE 7: add 2 kg, back to 10 reps"), so that I trust it.
40. As the owner, I want the first time I do an Exercise to have no Suggested target but still show a prefill when there is one, so that the log screen never shows a made-up number.
41. As the owner, I want the weight increment for targets to come from the active Plan, so that I can tune it.
42. As the owner, I want the "Last" and "Best" figures for an Exercise available alongside the target, so that the log screen shows all three from one place.
43. As the owner, I want the target to use only the last Workout where the Exercise was actually done (not skipped), so that a skip doesn't reset progress.

### Weeks, Streak and consistency
44. As the owner, I want weeks to run Monday to Sunday (ISO), so that they match how I plan.
45. As the owner, I want a week's count to include any Workout with at least one completed Exercise, so that a Partial workout still counts.
46. As the owner, I want strength and cardio Workouts to count but weigh-ins and benchmark test Sessions not to, so that the weekly target measures training.
47. As the owner, I want Custom workouts to count towards the Streak only when my Streak setting says so, so that the rule is mine to edit.
48. As the owner, I want a Streak week to need at least the minimum number of counted Workouts from the active Plan, so that the target is the Plan's.
49. As the owner, I want the current week never to break the Streak while it is still in progress, so that a Monday does not show a broken Streak.
50. As the owner, I want Streak to be the count of consecutive qualifying weeks ending at the latest completed week (plus the current week if it already qualifies), so that it grows the moment I hit the target.
51. As the owner, I want a week touched by a Break to neither break nor add to my Streak, so that illness or a holiday is excused.
52. As the owner, I want Weeks before the Plan's start date ignored, so that a new Plan doesn't start with a broken Streak.
53. As the owner, I want my best-ever Streak week count available for Badges, so that "best week" and "streak" metrics have one source.
54. As the owner, I want a consistency message ("on track", "behind", "target out of reach", "target hit") worked out from the day of the week, so that the dashboard tells me how the week is going.
55. As the owner, I want Break days to be excluded from the days available when working out consistency, so that a Break midweek doesn't mislabel me as behind.
56. As the owner, I want the Plan's week number and the Phase's "week 3 of 11" progress available, so that the dashboard can show where I am.

### Outcomes: Missed, Partial, Skipped, Swapped
57. As the owner, I want a Session to count as missed only once its planned day has passed with no Workout made from it, so that today's Session is never "missed" early.
58. As the owner, I want a Missed session to stay recorded as missed even if its Exercises are later done in a Custom workout, so that history is honest.
59. As the owner, I want a Session made up on a different day in the same week (a Workout made from that Session) not to be missed, so that moving a Session is fine.
60. As the owner, I want Sessions on Break days not to be counted as missed, so that excused days are excused.
61. As the owner, I want a benchmark Session to appear only in the weeks its interval says, and never to count towards the weekly target, so that retests schedule themselves.
62. As the owner, I want a Workout's planned Exercises I didn't record to be treated as skipped, so that a Partial workout is complete in the data.
63. As the owner, I want each planned row's outcome (done, skipped with reason, swapped with replacement) answered from one function, so that every screen reports it the same way.
64. As the owner, I want Missed-session Exercises offered first when building a Custom workout, so that making up work is one tap.

### Phase position
65. As the owner, I want the current Phase worked out from the date and the Plan's start date, so that Phases switch automatically.
66. As the owner, I want to see the week number inside the Phase and its length, so that "week 3 of 11" is available.
67. As the owner, I want a Phase goal's readiness (best set meeting the weight and reps) reported with a message, so that the app can say I'm ready to move on early.
68. As the owner, I want the Exercise rows for a Session to resolve through the current Phase's Override (else the Plan default), so that the planned work for today is a single call.

### Week schedule and rearrangement
69. As the owner, I want each Session placed on its default day in the Week schedule, so that the week is laid out without any setup.
70. As the owner, I want to move a Session to another day for one week only, so that a busy day doesn't change the Plan.
71. As the owner, I want a one-week move to be storable in the data and survive Backup and restore, so that a rearranged week isn't lost.
72. As the owner, I want a suggested rearranged week after a Break or pile-up, with outstanding Sessions on coming days in Plan order, so that I can get back on track quickly.
73. As the owner, I want the suggestion to avoid back-to-back days that load the same muscle group (using tags), so that I don't train the same muscles twice in a row.
74. As the owner, I want the suggestion to always be a draft I can accept or edit and never applied automatically, so that I stay in control.
75. As the owner, I want a clear "nothing to rearrange" result when the week is fine, so that the UI can stay quiet.

### Badges and Benchmarks
76. As the owner, I want Badge progress (value, target, percentage, reached or not) worked out from history, so that it can never get out of sync.
77. As the owner, I want the metrics `sessions`, `streak`, `best week`, `prs` and "any Exercise" supported, so that the starter Badges work.
78. As the owner, I want a Badge target of `bodyweight` (as the Exercise-name-free special case) to mean my latest Weigh-in, so that a weight-goal Badge works.
79. As the owner, I want a negative target on a measurement to mean "drop by this much from the start", so that weight-loss Badges work.
80. As the owner, I want an unknown Badge metric to show as "not measurable" with its name, never crash, so that a Badge edited oddly doesn't break the Trophies screen.
81. As the owner, I want benchmark Exercises' current best, previous test result and change listed together, so that the Benchmarks screen is one call.
82. As the owner, I want a benchmark Session's pre-fill to come from Benchmark exercises, with each Exercise's previous result shown beside it, so that I can compare while testing.
83. As the owner, I want the next benchmark due date worked out from the interval and the last benchmark Workout, so that the dashboard can say when it is.

### Nudges
84. As the owner, I want a Nudge for each Missed session this week offering to make it up, so that missed work is visible.
85. As the owner, I want a Nudge when I am exactly one Workout short of the weekly target late in the week, so that I get one last push.
86. As the owner, I want a Nudge when the same body area has been reported twice in the same week, so that repeated pain prompts me to ease off.
87. As the owner, I want a Nudge when there has been no Backup in 14 days, so that I don't lose data.
88. As the owner, I want Nudges to go quiet during a Break, so that excused days aren't nagged.
89. As the owner, I want each Nudge to carry a stable kind and the data it refers to (Session, area), so that the UI can show it and link to the right action.
90. As the owner, I want Nudges ordered with the most important first and nothing repeated, so that the dashboard shows at most a few.

### Quality
91. As a developer, I want every rule to take "today" from an injected clock or an explicit date, so that tests never depend on the real date.
92. As a developer, I want all rules to be pure functions of the model (no storage, no UI), so that they are unit-testable in memory.
93. As a developer, I want rule results to be plain values with reasons, so that screens only render and never decide.
94. As a developer, I want new rules to be addable without editing screens, so that Core stays the single home of behaviour.

## Implementation Decisions

### Shape
- **Everything is in `Core`.** No new project, no dependencies. Rules are stateless and take their inputs as arguments: the data model (an `ExchangeDocument` or its parts), Plan/Settings values and a "today" `DateOnly` obtained from the existing clock abstraction at the call site. Rules never read a clock themselves.
- **One rules entry per area**, each a small deep module with a simple public surface and its own internals hidden: *input parsing*, *unit conversion*, *exercise performance* (best, PR, progress points, last/best/target), *week and Streak*, *outcomes* (Missed session, Partial workout, per-row outcome), *Phase position*, *week schedule and rearrangement*, *Badges and Benchmarks*, *Nudges*. Nudges and the dashboard-level answers are composed from the lower modules, not re-implemented.
- Results are **immutable records** that include the answer and, where the brief demands it, a one-line human reason (Suggested target, consistency message, Nudge text, parse failure). Screens render these; they decide nothing.
- Exercises are resolved by ID through the Exercise library; a log kind is always read from the Exercise, never guessed from a Set. A row that references an unknown Exercise is ignored by rules rather than throwing (the validator already reports it).
- If an Exercise appears in several Sessions, the first row (in Plan order) defines the kind and rep range used for targets.

### Input parsing and units
- Parsing is a pure function from (text, log kind) to either a Set value in canonical units or a failure with a plain-language expected-format message. It never throws.
- Accepted separators for load and carry: `x`, `X`, `×`, `*`. Whitespace is ignored everywhere. Both `.` and `,` are decimal marks (the first of two ambiguous marks is not a thousands separator; a number is a single decimal number). Parsing is culture-invariant.
- Per kind: `load` = weight x reps; `bodyweightLoad` = `bwxN` (bodyweight reps) or `kgxN` (added weight and reps); `carry` = weight x metres; `reps` = integer; `seconds` and `minutes` = number in that unit (suffixes accepted); `time` = `m:ss` or `h:mm:ss` or plain seconds; `distanceKm`, `distanceM` = number (suffixes accepted); body measurements = number; `tickOnly` = no value; `text` = any non-empty text.
- **Unit conversion:** a small module with fixed factors (kg to lb, km to mi), conversion to display and from display, and display rounding rules (kg and lb to a sensible increment, km/mi to two decimals). The user's typed input in display units is converted to canonical before anything is stored. Stored values are never rounded by display rules.

### Exercise performance
- **Best Set ordering per kind:** `load` by Epley estimated 1RM; `bodyweightLoad` ranks unweighted Sets below any weighted Set, then by added weight then reps (weighted by 1RM estimate as for load); `carry` by weight then distance; `time` lowest; `seconds`, `minutes`, `reps`, `distanceKm`, `distanceM` highest. Body measurements, `text` and `tickOnly` have no best and no PR.
- **Best for an Exercise:** the best Set across history, except that when any Max set exists the best is the best Max set. Only Sets from `done` or `swapped` entries count, attributed to the Exercise actually done.
- **PR:** evaluated in chronological order of Workout date (ties by start time, then ID). A Workout's best Set for an Exercise is a PR if it strictly beats the best of all earlier Workouts; the first ever record is a baseline, not a PR. Because the order is by date, back-dated Workouts are slotted into history and PRs are recomputed from scratch each time, never stored.
- **Progress points:** per Exercise, one point per day (the day's best Set). An Exercise needs points on two or more days before a series is offered.
- **Suggested target:** from the last Workout where the Exercise was done. The base is that Workout's best Set. Progression by kind follows the brief (load: +1 rep up to the top of the planned rep range, then +increment kg and −2 reps; carry: +10 m to 40 m, then add kg; time: beat best; other numbers: best plus a step). RPE adjusts it: 9 or 10 holds weight and reps; 6 or below makes a bigger jump; no RPE means normal progression. Output is a target value plus a one-line reason. No history means no target.
- **Last / Best / Target** are returned together as one "exercise snapshot" for the log screen.

### Weeks, Streak, consistency
- A week is identified by its Monday date. Week arithmetic is one shared helper used everywhere.
- **Counted Workout:** has at least one `done` or `swapped` entry with a completed Set (or a tick-only/text entry done), is not a benchmark test Session's Workout, and is a Custom workout only when the Plan's `streakCountsCustomWorkouts` is true. Weigh-ins are separate records and never count.
- **Streak** uses the active Plan's `streakMinWorkoutsPerWeek`, counted from the Plan's first week, walking back from the latest completed week. The current week is included only if it already qualifies and never breaks the Streak while in progress. A week with any Break day is excused (neither adds nor breaks). Weeks before the Plan start are ignored. The result also exposes best-ever consecutive run and best week count (for Badges).
- **Consistency message** is one of on track, behind, target out of reach, target hit, from the counted Workouts so far this week, the weekly target and the number of non-Break days remaining.

### Outcomes
- A **Missed session** is a Session scheduled in the Week schedule for a date before today, for which no Workout with that `sessionId` exists in the same week. Break days and weeks before Plan start are excluded; benchmark Sessions are only scheduled in their interval weeks. It remains missed even if its Exercises were done in a Custom workout.
- **Per-row outcome** compares a Workout's `planned` snapshot with its entries: done, skipped (reason), swapped (replacement) or, for a planned row with no entry, skipped with no reason. An entry with no matching planned row is an extra Exercise.

### Phase position
- The current week number is derived from the Plan's `startDate` and today; the current Phase from the Phase week ranges; "week N of M" within the Phase; before the start or after the last Phase are explicit states.
- **Phase goal readiness:** met when the Exercise's best loaded Set reaches the goal weight at or above the goal reps (a heavier Set with at least those reps also meets it); the result lists which goals are met.
- A Session's Exercise rows for a date resolve through the current Phase's Override, else the Plan default.

### Week schedule and rearrangement
- **New data (small additive format change):** the exchange document gains an optional collection of **Week schedules**: one per week (keyed by the Monday date), listing Session moves (Session ID to a date in that week). It is added to the model, writer, reader, validator and data spec as an optional field, so it does not bump the format version. A Session with no move is on its default day. "Make this the new default" is an edit to the Session's default day and is a Plan edit, not part of the Week schedule.
- **Schedule computation:** default days, then that week's moves, then Break days marked unavailable.
- **Rearrangement suggestion:** given outstanding Sessions (missed this week plus not yet reached), the coming non-Break days of the week and the Plan order, produce a proposed placement as a draft. It prefers Plan order, never places two Sessions on a day already holding one unless unavoidable, and avoids placing consecutive-day Sessions that share a muscle-group tag. It returns "nothing to rearrange" when the week is already workable. The result is only ever a proposal.

### Badges and Benchmarks
- Badge progress is a pure function of the Badge definition and the full history. Supported metrics: `sessions` (counted Workouts), `streak`, `bestweek`, `prs`, or an Exercise (its best value in the Exercise's measure). A target of `bodyweight` means the latest Weigh-in; a negative target on a measurement means a drop of that size from the first Weigh-in/measurement. The metric value is a plain string in the data (A1 left it open); A2 defines the supported values and returns "not measurable" for anything else.
- **Benchmarks:** the list of Benchmark exercises with current best, previous benchmark-test result and change; the next benchmark due date from the interval and the last benchmark Workout; and the Benchmark pre-fill rows for a benchmark Session.

### Nudges
- Nudges are a computed list, never stored. Kinds: missed session, one short of target, repeated pain area, no Backup in 14 days. The Backup Nudge takes the last Backup timestamp as an input value supplied by the caller (its storage is A3 and A4). All quiet during a Break. Result ordering and de-duplication are deterministic.

## Testing Decisions

- **One seam:** the Core rules public surface, driven by in-memory data (the existing model records and `ExchangeDocument`) plus an explicit "today". There is no storage, UI or JSON in these tests. This is the same "highest public seam" principle as A1, which tested through the exchange-format reader.
- **A good test** states a scenario in domain terms (a short history, a date) and asserts the externally visible answer: the best Set, whether a PR is flagged, the exact target and reason text, the Streak number, the list of Missed sessions, the Nudge kinds. Tests never assert on private helpers, internal types or call order.
- **Tables for rules, scenarios for composition.** Parsing and the target rules are table-driven (input to expected value, one row each, including the awkward inputs: comma decimals, `×`, `bwx8`, `4:32`, `1:04:10`, unit suffixes, rejects). Streak, Missed session and Nudges use short dated scenarios built with a small fluent history builder.
- **Edge cases that must have a test:** the first ever log is not a PR; an equal repeat is not a PR; a Max set overrides a higher estimated 1RM; a back-dated Workout changes later PRs; RPE 9/10 holds, RPE ≤6 jumps; target at the top of the rep range; carry at 40 m; time beats best; the current week never breaks Streak; a Break week is excused; a missed Session stays missed after a Custom workout; a today's Session is not missed; Sessions on a Break are not missed; unknown Badge metric; negative Badge target; rearrangement avoiding a shared muscle group; Backup Nudge at exactly 14 days.
- **Determinism and culture:** a test runs the parsers and formatters under a comma-decimal culture (for example de-DE) to prove invariance. All date tests pass "today" explicitly, with weeks crossing a month and a year boundary.
- **Week schedule format change:** round-trip, validation errors (exact path and code) and conformance with the data-spec example, through the existing exchange-format seam.
- **Prior art:** `Core.Tests/Serialisation` (builders such as `PlanTestData` and `WorkoutTestData`, exact path-and-code assertions, the whole-dataset round trip) and `Core.Tests/Time` (the clock). The new tests live in a `Core.Tests` folder per rules area and reuse the same builders where possible.
- The test suite must stay green and fast; no sleeping, no real clock.

## Out of Scope

- Storage, IndexedDB, persistence of anything (A3). The last-Backup timestamp is only an input here.
- Export, Import, Exercise name matching, the Markdown rendering and the prompt pack (A4). Exchange-format work in A2 is limited to the optional Week schedule collection.
- The starting Plan, Guides and weight history (A5).
- Any UI, formatting for display beyond number rounding, the rest timer, Wake Lock, charts themselves, and the UI/UX design (Track B).
- The stats-driven recommendation engine, XP and levels, per-Phase progression rules, and similar-Exercise recommendations beyond using tags for rearrangement (parked in the product brief).
- A declarative rules DSL or user-editable rule scripting. The rules are code; only the values the brief calls editable (Streak rules, increment, Badge definitions) are data.

## Further Notes

- **Reference not yet in the repo:** the old `view.js` and `Config.md` are listed as inputs still needed in the build plan. The product brief's "Rules carried over from the old system" is the authority for this stage. When the owner supplies `view.js`, each assumption below should be checked against it and any difference recorded in an ADR or the spec, not silently changed.
- **Named assumptions (the brief is silent; chosen so the stage can proceed, easy to change in one place each):**
  1. The first logged result for an Exercise is a baseline, not a PR.
  2. "Bigger jump" at RPE 6 or below = apply the progression step twice (for load: two reps, or the increment twice at the top of the range).
  3. After a carry reaches 40 m and weight is added, distance resets to 20 m.
  4. Time target = best minus a small fixed step; "best plus a step" for other kinds uses a per-kind step table (reps +1, seconds +5, minutes +1, km +0.5, m +10).
  5. A week containing any Break day is excused from Streak entirely rather than having its minimum scaled.
  6. Consistency: with R workouts remaining and D non-Break days left (including today), "target hit" if R is 0; "target out of reach" if R > D; "behind" if R = D (no spare day); otherwise "on track".
  7. "One short of target" Nudge shows from Thursday onwards.
  8. A Set counts towards a Phase goal if its weight is at least the goal weight and its reps at least the goal reps.
  9. Rearrangement muscle-group clash uses the Session Exercise rows' Exercise tags; any shared `muscleGroups` value counts as a clash.
- **Ordering inside the stage** (proposed for `/to-tickets`): units and parsing first (no dependencies), exercise performance next (best, PR, target), then week/Streak/outcomes/Phase, then Week schedule (includes the small format addition) and rearrangement, then Badges/Benchmarks, then Nudges (depends on the rest). Several of these are independent and can run in parallel after the shared week helper exists.
- Each ticket must fit one agent in roughly 150 to 200k tokens (see `CLAUDE.md`); the exercise-performance and Week-schedule areas are the likeliest to need splitting.
- Seams were not confirmed with the owner because this session was running unattended; the single-seam choice above follows A1 and can be revisited at `/to-tickets` time.
