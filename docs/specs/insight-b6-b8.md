---
labels: ready-for-agent
stage: B6-B8
---

# Insight (B6 + B7 + B8)

Parent docs: [`../build-plan.md`](../build-plan.md) (Track B, row "Insight"), [`../design/design-document.md`](../design/design-document.md) (C-08, C-10, C-15, C-17, C-18, C-19, C-22, C-23, S-07, S-08, S-09, S-13, F-07), [`../../GLOSSARY.md`](../../GLOSSARY.md). Depends on Editors and A2 only (both merged). Independent of A4.5 (see "Tracks").

## Problem Statement

The owner logs Workouts and Weigh-ins but cannot see where they are heading. Progress, Weight, Trophies and Weekly review are placeholder screens ("arrives in a later stage"). There is no chart of an Exercise over time, no Benchmark comparison, no whole-Plan calendar, no weight trend, no list of Badges or personal Bests, no way to create or edit a Badge, and no Sunday review that says whether the week is on track. The Core rules for most of the numbers already exist (Badge progress, Benchmarks, Streak, consistency message, PRs, weight panel, Nudges) but nothing presents them.

## Solution

Insight turns the four placeholders into real screens, built from Core views that already have the numbers behind them, plus a small number of new pure Core views:

1. **Progress (S-07):** three tabs. **Progress** (a LineChart per Exercise logged twice or more, grouped by chips, one-entry Exercises listed as "needs 2 for a chart"). **Benchmarks** (latest test against previous per benchmark Exercise, with the next test date). **Calendar** (a CalendarHeatmap of the whole Plan with a Phase strip).
2. **Weight (S-08):** three stats (now, change since start, this week's average), a weight LineChart with the 7-entry rolling average and an optional goal line, a weekly averages table, and the existing Log weight sheet.
3. **Trophies (S-13):** tabs **Badges**, **Bests**, **Calendar**. A BadgeTile grid (unlocked first, then closest to unlocking), a "Badges unlocked: 3 / 20" line, Badge detail, and the **BadgeEditor** (create, edit, delete). Bests is a personal-bests table plus "Best of the week".
4. **Weekly review (S-09, F-07):** a verdict card, rows for Missed, PRs, weight average against last week and Pain checks, PRs this week, and **Export for the LLM** (last 7 days, Markdown, health data off).
5. **Shared pieces:** LineChart (C-17), CalendarHeatmap (C-18), ProgressBar (C-19), DateRangePicker (C-15, reused by Calendar and Progress ranges), BadgeTile (C-10), BadgeEditor (C-22), and a real **UnlockToast (C-23)** on the finish summary, replacing the static "Badge unlocked" line.
6. **Nudge copy and dismissal across screens** (S-09 note): Nudges stay Home-only per C-08; Insight reviews the copy of every existing Nudge kind and makes the Weekly review reachable from the Sunday Today card and the Streak orb.

The owner sees this as: tap the Streak orb and see the Calendar; tap an Exercise chip and see the line go up; on Sunday log the weigh-in, open the review and read "On track: 1 to go".

## User Stories

### Progress charts
1. As the owner, I want a chart per Exercise I have logged at least twice, so that I can see whether I am getting stronger.
2. As the owner, I want strength charts to show an estimated 1RM, so that sets of different weights and reps compare fairly.
3. As the owner, I want non-strength Exercises charted in their own measure (reps, time, distance, carry load), so that a plank is not shown as a weight.
4. As the owner, I want the change since the first entry and the best point labelled on each chart ("Best 33.3 · +7.3 since W1"), so that I get the headline without reading the axis.
5. As the owner, I want PR points drawn larger and in the PR colour, so that I can spot records at a glance.
6. As the owner, I want gaps in logging to show as breaks in the line, never an interpolated line, so that the chart does not invent data.
7. As the owner, I want Phase bands behind the plot, so that I can tell which Phase a gain happened in.
8. As the owner, I want to tap a point to see its date and value, so that I can read exact numbers.
9. As a screen-reader user, I want a "Table" toggle that shows the same data as a list, so that I get the information without the SVG.
10. As the owner, I want group chips (Strength, Carries, Engine, Bodyweight, Body) that filter the charts, so that I can find an Exercise quickly.
11. As the owner, I want Exercises with a single entry listed as "1 entry · needs 2 for a chart", so that nothing seems missing.
12. As the owner, I want a clear empty state before I have any history, so that the screen is never blank.
13. As the owner, I want body measurements (weight kg, cm) charted from their own readings, so that Body shows my measurements.
14. As the owner, I want to pick a date range for the charts (last 4 weeks, last 12 weeks, whole Plan, custom), so that I can zoom to a Phase.
15. As the owner, I want weights shown in my chosen units (kg or lb), so that the chart matches what I log.

### Benchmarks
16. As the owner, I want each benchmark Exercise to show its latest test against the previous, with a ▲ when better, so that I see improvement.
17. As the owner, I want lower-is-better measures (times) to count a drop as better, so that ▲ is correct for a 1 km run.
18. As the owner, I want a progress bar per benchmark toward its target where a target exists, so that I see how far is left.
19. As the owner, I want the next benchmark test date, so that I know when to re-test.
20. As the owner, I want an Exercise never tested to say so, so that I know to do the first test.

### Calendar
21. As the owner, I want the whole Plan from start to race day as a heatmap of weeks and days, so that I see consistency at once.
22. As the owner, I want done days coloured by Session type, Partial days half-filled, Missed days dashed, Break days hatched, so that each state is distinct without relying on colour alone.
23. As the owner, I want a PR triangle on days with a PR, so that big days stand out.
24. As the owner, I want today outlined and future days empty, so that the grid orients me.
25. As the owner, I want a Phase strip above the weeks with name and length, so that I see the plan shape.
26. As the owner, I want to scroll sideways to race day, so that long Plans fit on a phone.
27. As the owner, I want to tap a day to open its Workout, so that I can look at what I did.
28. As a screen-reader user, I want each day labelled ("Wed 18 Nov: Session C, done, 1 PR"), so that the grid is accessible.
29. As the owner, I want a legend, so that I can decode the cell styles.

### Weight
30. As the owner, I want to see my latest weight, change since the start and this week's average, so that I get the picture in three numbers.
31. As the owner, I want every Weigh-in as a small dot and the 7-entry rolling average as the main line, so that daily noise does not hide the trend.
32. As the owner, I want the lowest Weigh-in ringed, so that I can see my best.
33. As the owner, I want an optional dashed goal line from a Badge target, so that I see the gap to the goal.
34. As the owner, I want a weekly averages table (Monday to Sunday), so that I can compare weeks.
35. As the owner, I want to log a Weigh-in from this screen with the same sheet Home uses, so that logging is one habit.
36. As the owner, I want the sheet prefilled with the last value and "Last: 86.3 on Sun 15 Nov", so that entry is a nudge not a retype.
37. As the owner, I want a plain empty state with a Log weight button when I have no Weigh-ins, so that the first entry is easy.
38. As the owner, I want a Weigh-in never to count toward the Streak, so that weight and training stay separate.

### Badges and Bests
39. As the owner, I want a grid of Badge tiles with art, name and "x / y", so that I see my achievements like a stats panel.
40. As the owner, I want unlocked Badges first and the rest ordered closest-to-unlocking first, so that the next goal is at the top.
41. As the owner, I want unlocked, in-progress and locked tiles to look different and not only by colour, so that state is accessible.
42. As the owner, I want a "Badges unlocked: 3 / 20" summary and progress bars for the closest few, so that I know what is next.
43. As the owner, I want a Badge detail with large art, what it needs, progress and the events that contributed, so that I know why it is where it is.
44. As the owner, I want to create a Badge by choosing art, a name and a metric (Workouts, Streak, Best week, PR count, Exercise best, Body weight), so that I can set my own goals.
45. As the owner, I want metric-specific fields (an Exercise and a target such as "24 × 8"; a weight target or "drop by" kg), so that every metric is expressible.
46. As the owner, I want a live line in the editor ("Now: 22 × 9. Unlocks when…"), so that I can sanity-check a target before saving.
47. As the owner, I want a metric that cannot be measured to say "not measurable" instead of failing, so that a bad Badge is understandable.
48. As the owner, I want to edit or delete a Badge, with a confirm on delete, so that I can fix mistakes safely.
49. As the owner, I want my Badges saved with my Plan data and included in Backup and export, so that they are not lost.
50. As the owner, I want a Bests tab with each Exercise's personal best and its date, so that I have one list of my records.
51. As the owner, I want a "Best of the week" row, so that the week's best moment is celebrated.
52. As the owner, I want a calm UnlockToast on the finish summary only, auto-hiding after 4 s, so that a celebration never interrupts logging.
53. As the owner, I want the toast to appear instantly with reduced motion on, and to open the Badge when tapped, so that it respects accessibility and is useful.
54. As the owner, I want an empty state on Trophies before there is history, so that the screen invites the first Workout.

### Weekly review
55. As the owner, I want a verdict ("On track", "Behind", "Target out of reach", "Target hit") with Workouts against target and my Streak, so that I know where I stand.
56. As the owner, I want a row for Missed sessions this week, so that I see what to make up.
57. As the owner, I want PRs this week listed, so that I end the week on the good news.
58. As the owner, I want my average weight against last week's, so that the trend is in the same place.
59. As the owner, I want Pain checks flagged that week, so that I notice repeated pain.
60. As the owner, I want an **Export for the LLM** button preset to the last 7 days, Markdown, health data off, so that sharing the review is one tap.
61. As the owner, I want the review for a past week as well as this one, so that I can look back.
62. As the owner, I want a Break week to say so and not call the week Behind, so that excused weeks are not penalised.

### Navigation, Nudges and general
63. As the owner, I want the Streak orb on Home to open the Calendar and the Phase orb to open Progress, so that Home leads into Insight.
64. As the owner, I want the Sunday Today card to lead to the Weekly review after the weigh-in, so that F-07 flows.
65. As the owner, I want Nudge copy to be neutral and specific, with at most two shown, so that Home stays calm.
66. As the owner, I want every Insight screen to work in both Themes and both Modes at 360 px, so that it is usable on my phone.
67. As the owner, I want every screen to work offline, so that nothing needs a connection.
68. As the owner, I want a read-only app (another tab holds the writer lock) to show charts and lists but disable Badge saves and Weigh-in logging with the usual message, so that data stays safe.
69. As the owner, I want a failed save to show the same notice as elsewhere and keep my Badge draft, so that I lose nothing.
70. As the owner, I want a tablet or desktop layout to use the width (charts side by side from 900 px), so that larger screens are not wasted.

## Implementation Decisions

### Tracks
- Insight is built on today's one-rhythm Plan. **Unqualified metrics and views mean the main Track** (the only one that exists until A4.5). Every new Core view takes the Plan, Workouts, Breaks, Weigh-ins and Exercises as plain inputs (as `BadgeHistory` does) and does not read the Plan's rhythm fields in a new way, so A4.5 PR 2 can add per-Track variants (daily-Track heatmap rows, a Track picker in the BadgeEditor, `streak:<id>`) without reworking Insight. The BadgeEditor in this stage offers no Track picker. Where A4.5 PR 1 changes a rule Insight calls (the Streak or counted Workouts), Insight picks up the change by calling the same Core rule, never by reimplementing it.

### New Core views (pure, no clock, no storage; `today` passed in)
- **Progress series:** for an Exercise, an ordered list of points (date, value, whether it is a PR, the Phase the date falls in) plus the best point and the change since the first entry. Strength (`Load`) Exercises plot an estimated 1RM; every other log kind plots its own measure through the existing progress-point rule. Gaps are represented by absence of a point, so the view has no interpolation. The existing progress points and PR rules are reused.
- **Exercise groups:** derived from the log kind (Load to Strength, Carry to Carries, distance, time and minutes to Engine, reps, seconds and bodyweight-load to Bodyweight, the two body measurements to Body). Text and tick-only Exercises are never charted. No new stored field.
- **Progress list:** the Exercises per group with their series, split into "chartable" (two or more entries) and "needs 2 for a chart" (one entry).
- **Benchmark view:** per benchmark Exercise, latest and previous results, whether better (respecting lower-is-better), the change, a target-progress percentage where a target exists, and the next test date, from the existing Benchmark rules.
- **Calendar view:** for the whole Plan from start to race day, a grid of weeks by days. Each day has a state (empty, future, done, Partial, Missed, Break), the Session type for colour, a PR flag, whether it is today, and an accessible label. A Phase strip lists Phases with name and week length. Uses the existing week, Break and Phase rules. A day's Workout reference is included so a tap can open it.
- **Weight view:** the rolling 7-entry average series, the lowest Weigh-in, now, change since start, the current-week average, and a Monday-to-Sunday weekly averages table. The Weigh-ins' own data is the only input; the goal line value comes from the first weight Badge whose target is a weight, or is absent. The existing weight panel rules provide the three headline numbers so Home and Weight can never disagree.
- **Trophy view:** each Badge's progress (existing Badge rules), ordered unlocked first then by percent descending (ties by name), the unlocked count over the total, the "closest few" list, and a per-Badge detail including the contributing events (for example the Workouts that raised a count, the PR dates, the Weigh-ins). Progress is never stored.
- **Bests view:** per Exercise with a Best, the best result and date, plus the single "Best of the week" (the strongest PR in the current Monday-to-Sunday week, or none).
- **Weekly review view:** verdict kind and message (from the existing consistency rule, with "Target hit" added as a verdict when the counted Workouts reach the target, and a Break week reported as On a Break), Workouts against target, Streak, Missed Sessions, PRs this week, weight average against last week's, Pain checks that week. A week is selected by its Monday; the default is the current week.
- **Badge editing rules:** pure functions in the same shape as the Editors' rules, returning the same plain-reason result type as other edit rules: validate a draft (name required and unique-friendly, metric one of the supported ones, target a positive number, or a negative "drop by" for body weight, an Exercise required for the Exercise-best metric), a live "Now / Unlocks when" line from current progress, and add, replace and delete on the Badge list with a new permanent ID on add. Metrics offered map to the supported metric names (Workouts to `sessions`, Streak to `streak`, Best week to `bestweek`, PR count to `prs`, Body weight to `bodyweight`, Exercise best to an Exercise ID).
- **Newly unlocked Badges** after finishing a Workout continue to come from the existing Badge rules comparing before and after; the finish summary shows them through the UnlockToast.

### UI components
- **LineChart (C-17):** plain inline SVG exactly to the C-17 spec (viewBox, insets, line, point sizes, PR ring, best label, Phase bands, weight variant with dots, rolling line, goal line and lowest ring), tap-for-tooltip and a Table toggle. It takes a prepared series, not Workouts.
- **CalendarHeatmap (C-18):** a scrolling panel of cells to the C-18 spec; every state is distinguishable without colour (fill pattern, dashes, hatching, triangle); one accessible label per day; tap opens the Workout.
- **ProgressBar (C-19):** label, value always shown, state colour.
- **DateRangePicker (C-15):** presets plus Custom with native date inputs; start must not be after end, otherwise an inline message. Built here; B9/B10 (Export) reuse it later.
- **BadgeTile (C-10)** and **BadgeEditor (C-22)** as specified. Art is chosen from the starter art family already in the design assets.
- **UnlockToast (C-23):** on the finish summary only; 4 s auto-hide; tap opens the Badge; instant under reduced motion; no sound or haptics.
- Pages stay thin: logic in `*.razor.cs`, rules in Core, Core views read by the pages. Plain CSS tokens with Blazor CSS isolation; no raw hex or px where a token exists; tap targets use the existing tap-min token; both Themes and both Modes at 360 px; one column on phones and two from 900 px.
- The Log weight sheet is reused unchanged by the Weight screen and always adds a new Weigh-in.
- Saving Badges goes through the one guarded-save path used everywhere; a failed save keeps the draft; a read-only app disables the editor.

### Storage and exchange
- Badges already exist in the data model and in the exchange document, so no schema or format change is needed. Badge create, edit and delete write the Badge list through the existing storage path and are included in Backup and export. No new stored data for Progress, Calendar, Weight or Weekly review: everything is derived.

### Navigation
- Home's Streak orb opens Trophies' Calendar tab (or Progress' Calendar, whichever the existing orb routes already name; keep the existing route, add the tab), the Phase orb opens Progress, the Sunday Today card links to Weekly review, and the Weekly review's Export button opens the existing Export with the preset. If the Export screen is not yet built, the button is hidden and a plain note is shown until B9.

### Nudges
- No new Nudge kinds. Nudges stay Home-only. This stage reviews the existing copy against C-08 (neutral, specific, one action, one dismiss) and fixes any that miss it.

## Testing Decisions

- A good test checks external behaviour: given stored data and a date, what the screen or view shows. No test asserts on private structure, CSS or call order.
- **Seam 1: Core view builders** (Core.Tests): Progress series and groups, Benchmark view, Calendar view, Weight view (rolling average edges: fewer than 7 entries, gaps, one entry), Trophy ordering and detail, Bests and Best of the week, Weekly review verdicts (including Target hit and Break week), Badge draft validation and the live line, and unit conversion. Prior art: `HomeViewBuilderTests`, `PlanEditingRulesTests`, the existing Badge, Benchmark, Nudge and Week rule tests, `PlanTestData` for fixtures.
- **Seam 2: bUnit page tests** (Ui.Tests) through `PageFixture` for Progress, Weight, Trophies and Weekly review: empty states, populated states, chip filtering, the Table toggle, tap tooltip, Badge create, edit and delete, the save-failure notice (`FaultInjectingDataStore`), the read-only app (`WriterLockResult.HeldElsewhere`), the UnlockToast on the finish summary. Assert inside `WaitForAssertion` after async clicks; sheets need Loose JS interop and the Sheet module set up. Fixed now is Mon 16 Nov 2026 09:00Z. Prior art: `HomeTests`, the Editors page tests.
- Charts and the heatmap are tested through the page (labels, text and table content), not by SVG geometry. A few component tests cover state mapping (cell state to label and style class).
- Both Themes and Modes, 360 px and tablet layout are checked manually; entries are added to `docs/pwa-manual-checks.md`.
- No new Playwright seam. `dotnet build` stays at 0 warnings and all existing tests stay green.

## Out of Scope

- Per-Track views, daily-Track heatmap rows, `streak:<id>` metrics and the Track picker in the BadgeEditor (A4.5 PR 2 and a later Insight follow-up).
- The Export, Import, Backup and Restore screens, the full Export picker and the prompt pack (Data and settings, B9). The Weekly review's export button only passes the preset.
- Settings screens (Streak rules, units, Theme) and First run (B10).
- Accessibility pass beyond each component's own requirements, performance tuning and device checks (Hardening, B11).
- Push notifications, sound and haptics, social features, sharing images of charts.
- Weight goal setting as its own setting (the goal line comes only from a weight Badge).
- Importing or converting the old Obsidian data (A5).

## Further Notes

- Suggested ticket order: (1) shared chart components LineChart, ProgressBar, DateRangePicker; (2) Progress series, groups, Progress tab; (3) Benchmarks tab; (4) Calendar view and CalendarHeatmap; (5) Weight view and screen; (6) Trophy view, BadgeTile and Badges tab; (7) BadgeEditor and Badge editing rules; (8) Bests and UnlockToast; (9) Weekly review view and screen; (10) navigation, Nudge copy review, manual checks, docs and the PR body. Each ticket should fit about 150 to 200k tokens.
- Branch: `feature/insight` from `main`. Work one implementer at a time.
- Open question to settle in ticket 1, not asked now: the exact estimated-1RM formula (Epley is the usual default). Record the choice in the glossary under Progress.
