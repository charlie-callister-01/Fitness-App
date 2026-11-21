# B3 review fixes

Follow-up to `b3-home.md`. Covers what the code review (Standards and Spec axes) and the thermo-nuclear review found that is not fixed yet.

## Problem Statement

Home works and its tests pass, but the reviews left things the owner would notice or that break the repo's own rules: some Home and Custom workout tests assert straight after an async click, the Phase orb caption reads oddly, the Home actions are not all disabled when the app is read-only, the two-column desktop layout never appears, and a few spec points are ambiguous.

## Solution

A small fix stage on the B3 branch, before the B3 PR: close the rule breaks, correct the wording and read-only gaps, and record decisions for the ambiguous spec points instead of guessing.

## User Stories

1. As the owner, I want the Phase orb to read "week 5 of 8", so that I know where I am in the Plan at a glance.
2. As the owner, I want Do a different Session, Log a past Workout and Log weight to say why they cannot be used when the app is read-only, so that a disabled screen is never a mystery.
3. As the owner, I want the Done today card to open the summary of the Workout it describes, so that the summary never differs from the card.
4. As the owner, I want a Logged afterwards Workout finished today for an earlier day to be handled predictably on the Today card, so that Home never shows a misleading state.
5. As the owner, I want the week line to make clear what "of N" counts, so that "1 of 5 · target 5" is not misread.
6. As the owner, I want Home to use two columns on a wide screen, so that the desktop layout from the design is real.
7. As a maintainer, I want every Home and Custom workout test to wait for visible results after async clicks, so that tests do not pass by timing luck.
8. As a maintainer, I want unused alias members on the Home records removed, so that each fact has one name.
9. As a maintainer, I want the workflow trial notes filled in, so that the planner and implementer split can be judged.

## Implementation Decisions

- Phase orb caption: change to "week {n} of {m}" with the value unchanged; the accessible name stays as is.
- Read-only on Home: the buttons that open a sheet stay enabled only if the sheet explains and disables its actions (already true); decide per button whether a disabled button with the reason is clearer. Recommended: disable Do a different Session and Log a past Workout when read-only and show the plain reason once under the alternatives. Log weight keeps opening its sheet, which already shows the reason.
- Done today: the Core `TodayCard` carries the finished Workout's id so the page navigates from the card, not by searching again. This is an additive change to the Core record.
- Done today for a Logged afterwards Workout: the rule currently matches on the Workout's date. Decide: keep (a Workout only counts as done today when it is dated today). Recommended: keep and document in the spec.
- Week line: keep "This week {done} of {scheduled} · target {target}". Done follows the Streak counting setting, so it can differ from all finished Workouts. Needs the owner's choice between this and "of {target}".
- Desktop layout: widen the content column at 900 px and above in the shell layouts so Home's grid shows. Affects every page, so check each page at 900 px; if that is too wide for other pages, scope it to Home with a layout class.
- Remove aliases on `TodayCard`, `WeekTile` and `HomeOrbs` that duplicate a primary member, updating the Core tests that use them.
- Filling the trial notes in `docs/agents/workflow-change-log.md`: record that implementer output was lost to a usage cutoff and that tickets 06 to 14 were done by the planner.

## Testing Decisions

- A good test drives external behaviour only: what the page shows and what is stored, never private members.
- Highest seams stay as they are: `HomeViewBuilder` in Core for rules and the Home page in bUnit over the in-memory store for behaviour. No new seams.
- Every assertion after a click that starts async work uses `WaitForAssertion`.
- Prior art: `HomeTests`, `HomeActionsTests`, `CustomWorkoutDraftTests`, `HomeViewBuilderTests`.
- New or changed tests: Phase caption text; read-only disables the two alternative buttons with a reason; Done today summary opens the card's Workout; Home wide layout is CSS only, so it is a manual check.

## Out of Scope

- Any data-model, exchange-format or storage change.
- New Home features (Weekly review, Progress changes).
- Moving Streak or Phase rules.
- The Week page behind a strip tile tap.

## Further Notes

- The Streak orb "On a Break" caption was reported missing but is correct: `StatusOrb` swaps its caption when greyed.
- Open choices for the owner: week line wording, and whether to widen the shell column for every page or only Home.
- Not published to GitHub Issues yet; the file is the spec until the owner says otherwise.
