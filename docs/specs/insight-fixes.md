---
labels: ready-for-agent
stage: B6-B8 review fixes
---

# Insight review fixes

Parent docs: [`insight-b6-b8.md`](insight-b6-b8.md) (the stage spec), tickets in `.scratch/archive/insight/issues/`, [`../../GLOSSARY.md`](../../GLOSSARY.md). Source: the two-axis `/mattpocock-skills:code-review` of `feature/insight` against `origin/main` (24 commits, 98 files). The thermo-nuclear review (`/thermo-nuclear-code-quality-review`) has not run yet; its findings may add to this spec's tickets before they start.

## Problem Statement

Insight is built and green, but the review found things that make parts of it not do what the owner asked, and duplication that will cost later. On a tablet or desktop the Insight screens stay one column, so the "two columns from 900 px" the owner asked for never shows. With a date range chosen, PR rings and "since W1" are worked out from only the Workouts in range, so the first point in range can show a PR it never was. A "drop by" weight Badge never draws a goal line on the Weight chart. An Exercise Badge's target is stored as an estimated 1RM, but the Badge detail says "Reach 100 on Squat" and editing a "24 × 8" Badge reopens it as one rep, so the owner's reps look lost. Separately, the same date and number formats, weight display, week grouping and the Epley constant are written out in many places, and a few new styles use raw pixel values.

## Solution

One fix pass, in three parts, without changing the data format:

1. **Behaviour fixes:** make the two-column layouts actually appear from 900 px; keep PR flags and Phases computed from the whole history while the chart shows only the chosen range; give a "drop by" weight Badge a goal line; say what an Exercise Badge target really is (an estimated 1RM) in its detail and in the editor; add the tests the review found missing.
2. **Consolidation:** one shared display-format helper, one inverse of the Epley estimate beside the original, one unrounded weight and distance option on the unit converter, one "Weigh-ins by week" rule used by Weight, Weekly review and Trophies, a shared layout rule for the 900 px breakpoint, an enum for the calendar day state, and the parsing helper shared with the Plan editor.
3. **Tidy:** replace raw pixel values with tokens where a token exists, record the heatmap cell size as the one documented exception, remove two pointless indirections, and split Badge editing rules and the trophy view so formatting text lives apart from rules.

The owner sees this as: on a tablet the Weight chart sits beside its table and Weekly review sections go two across; zooming Progress to "Last 4 weeks" never invents a PR; a "drop by 5 kg" Badge shows a dashed goal line; the Badge detail reads "Estimated 1RM of 100 kg on Squat".

## User Stories

1. As the owner on a tablet or desktop, I want Progress, Weight, Trophies and Weekly review to use the extra width from 900 px, so that charts and tables sit side by side.
2. As the owner on a phone, I want the layout to stay one column at 360 px, so that nothing about the phone view changes.
3. As the owner, I want Home's existing wide layout unchanged, so that the fix to the column cap does not move Home.
4. As the owner, I want a PR ring on a chart to mean "a record at the time", so that zooming the range never changes which points are records.
5. As the owner, I want the first point in a chosen range never to be called a PR just because earlier history is hidden, so that the chart does not invent records.
6. As the owner, I want "Best" and "change" on a ranged chart to describe the points I can see, so that the headline matches the line.
7. As the owner, I want the "since W1" wording to name the first point I can see when I have zoomed in, so that it is true of the visible range.
8. As the owner, I want the Phase bands on a ranged chart to stay correct, so that I can still tell which Phase a gain was in.
9. As the owner with a "drop by 5 kg" body weight Badge, I want a dashed goal line at my first weight minus 5 kg, so that I can see the gap to the goal.
10. As the owner with a plain "reach 80 kg" Badge, I want the goal line at 80 kg as before, so that nothing regresses.
11. As the owner with several weight Badges, I want the first weight Badge to supply the goal, so that the rule is predictable.
12. As the owner with no Weigh-ins yet, I want no goal line for a "drop by" Badge, so that no line appears from nothing.
13. As the owner, I want an Exercise Badge on a Load Exercise to say "Estimated 1RM of 100 kg on Squat" in its detail, so that I know the target is an estimated 1RM.
14. As the owner, I want editing a "24 × 8" Badge to tell me the stored target is an estimated 1RM and show the equivalent single rep, so that I do not think my reps were lost.
15. As the owner, I want saving an unchanged Badge to keep the same target exactly, so that opening and saving never drifts it.
16. As the owner, I want a Break week that also hit its target to say "Target hit", and a Break week with a short count to say "On a Break", so that the verdict order is predictable and tested.
17. As the owner, I want a Badge I edit to survive Backup and Restore, with a test proving the import side as well as the export side, so that I trust my Badges are saved.
18. As the owner, I want dates written the same way across Insight (a weekday and day, or a day month and year), so that screens look consistent.
19. As the owner, I want numbers shown with the same decimals for the same kind of value, so that a weight never shows 86.3 on one screen and 86.30 on another.
20. As the owner, I want my units applied the same everywhere on Insight, so that a number never flips between kg and lb.
21. As the owner, I want Weight, Weekly review and Trophies to agree on a week's average weight, so that the same week never shows two averages.
22. As the owner, I want the heatmap cells to stay the specified 26 px, with each day still reachable by keyboard and labelled, so that the design is kept and still accessible.
23. As the owner, I want the Badge editor's number fields to accept a comma or a dot, the same as the Plan editor, so that typing "86,5" works in both places.
24. As a future maintainer, I want one place that holds the Epley constant, so that a change cannot desync the editor and the progress rules.
25. As a future maintainer, I want the calendar day state to be a type, not a string, so that a new state cannot be mistyped.
26. As a future maintainer, I want one shared rule for the 900 px two-column layout, so that I change a breakpoint in one place.
27. As a future maintainer, I want formatting text kept out of the rules that decide things, so that Badge editing rules and the trophy view each change for one reason.
28. As a future maintainer, I want the existing test suites to stay green and unloosened, so that the fixes are provably behaviour-safe.

## Implementation Decisions

### Behaviour fixes
- **Layout cap.** The shell's single column is capped at 560 px, which defeats every page-level 900 px rule. The shell's wide-layout switch (today applied only to Home) is extended to the Insight pages (Progress, Weight, Trophies, Weekly review) through the same mechanism Home uses, so those pages get the full width from 900 px and stay capped below it. Home's behaviour is unchanged. The per-page two-column rules then take effect; any that were never visible get a first look in the manual checks.
- **Ranged series.** The Progress series is built from the whole history, so PR flags, Phase assignment and the Plan-week numbering are correct, and the range then selects which points are shown. Best point and change since the first visible point are computed from the visible points. The best-point label says "since W{n}" for the first visible point's Plan week, otherwise "since first entry". An empty range still shows the plain message.
- **Weight goal.** The goal line comes from the first weight Badge. A positive target is the goal weight. A negative target (a drop) is the first Weigh-in minus the drop size, and no goal line is drawn while there are no Weigh-ins. Goal values follow the owner's units.
- **Exercise Badge wording.** For a Load Exercise the Badge "needs" text and the contributing-events text say "Estimated 1RM of N on X" in the owner's units, not "Reach N on X". Other log kinds keep the raw wording. In the editor, a Load Exercise Badge shows a plain line that the target is an estimated 1RM and its equivalent weight for one rep. Opening and saving without changes keeps the stored target exactly. No stored field is added; this is wording and one helper line.
- **Weekly verdict order.** Kept as "Target hit" first, then "On a Break", then the existing verdicts; the order is now pinned by tests.
- **Backup round trip.** If the existing Badge round-trip test asserts only the export half, it is extended to import the result and compare the Badge list.

### Consolidation
- **Epley inverse.** A single inverse of the estimated 1RM rule sits beside the original in the ranking rules and is used by Badge editing. The constant 30 appears once.
- **Unit converter.** The unrounded weight and distance conversions become an option on the existing display conversions instead of two parallel copies of the branching. Display rounding to 0.25 kg or 0.5 lb stays the default.
- **Display formats.** One small helper holds the date and number formats Insight uses (weekday and day, day and month, day month and year, the ISO date for native inputs, and the one-decimal and trimmed-decimal number formats) and every Insight page, component and view uses it. Existing screens outside Insight are not migrated in this spec.
- **Weigh-ins by week.** One rule groups Weigh-ins by their Monday and gives each week's average. Weight, Weekly review and Trophies call it; the existing weight-panel average stays the single definition of "this week's average" and the rule reuses it.
- **Calendar day state.** The day state passed into label building becomes the existing state enum rather than a string.
- **Number parsing.** The comma-to-dot decimal parse used by Badge editing is the same function the Plan editor uses, moved to one shared place.
- **Breakpoint.** The repeated `min-width: 900px` rules for the Insight pages are replaced by one shared two-column layout rule in the shared stylesheet that pages opt into. The other uses (tab bar, main layout, Home) are left alone.
- **Indirections removed.** The progress series label helper that returns the group's name and the Streak orb's fallback to the Progress callback are removed.
- **Split.** Badge editing rules keep drafting, validation and add, replace and delete; the "Now / Unlocks when" text and number text move to a formatting module beside it. The trophy view keeps ordering and counts; event text for the Badge detail moves to its own module. No behaviour change.

### Tidy
- Raw pixel sizes, font sizes and borders in the new Insight CSS become existing tokens where a token exists, otherwise stay with a short comment. The 26 px heatmap cell stays as C-18 specifies; it is recorded in the design notes as the one intended exception to the tap-target token, with the day still keyboard-reachable and labelled.
- The invariant-culture reference in the Nudge rules becomes a using, with no behaviour change.

### Decisions already made (do not re-ask)
- No format, schema or storage change. Badge art stays derived (the owner's art choice is a separate decision).
- Main Track only, as in the stage spec.
- The repeated-pain Nudge keeps Dismiss only; its action is a separate owner decision.

## Testing Decisions

- A good test checks what the owner would see: given stored data and a date, what the view or page shows. No test asserts on private structure or CSS text beyond what already exists.
- **Seam 1: Core.Tests** for the views and rules: ranged series (PR flags and Phases from full history, best and change from visible points, label wording), weight goal for positive and negative targets and no Weigh-ins, Epley inverse and round trip of editing, week grouping and average agreeing with the weight panel, verdict order at the Break boundary, Badge round trip with import, the unrounded unit option, and format helper outputs.
- **Seam 2: bUnit page tests** (Ui.Tests, `PageFixture`) for Progress with a range (no false PR ring), Weight with a "drop by" Badge, Trophies detail wording, Badge editor line and unchanged-save, Weekly review verdicts. Layout at 900 px is a manual check, not a unit test, and the manual checks list gains an item for it.
- Prior art: the Insight view tests and page tests from this stage, `HomeTests`, `PlanEditingRulesTests`.
- Existing tests are not loosened. Moving code is checked by the existing tests staying green.

## Out of Scope

- Badge art selection (needs a model change), the repeated-pain Nudge action, the Export button on Weekly review (B9), per-Track views, and migrating non-Insight screens to the new format helper.
- Changing the 14-day chart gap rule or the Best of the week rule (owner confirmations are listed in the manual checks).
- Anything the thermo-nuclear review adds; that gets its own tickets or an addendum.

## Further Notes

- Suggested tickets (about 150k tokens each or less): (1) layout cap and shared two-column rule, with the manual-check item; (2) ranged series PR and label fix and the Weekly verdict and round-trip tests; (3) weight goal for a drop and Exercise Badge wording and editor line; (4) shared format helper, unit converter option and number parsing, applied across Insight; (5) Epley inverse, Weigh-ins by week, calendar state enum and the two removed indirections; (6) the Badge editing and trophy view splits, CSS tokens and the nudge using; (7) the thermo-nuclear findings, the reviews of this fix pass and the PR body.
- Tickets 2 and 3 and 4 touch different files from 1 and can run in parallel; 5 and 6 should follow 4 because they use the shared helpers.
- Branch: `feature/insight`. Commit the fixes on it before `/ship`.
