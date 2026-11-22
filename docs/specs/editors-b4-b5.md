# Editors (B4 + B5): Guides, Exercise library, Plans and Week view

Covers design screens S-03, S-04, S-05, S-06, components C-20 (FormPatterns), flows F-04, F-08 and F-09. First wave is a Ui prefactor from the 22 Nov 2026 architecture review (see `docs/build-plan.md`, "Editors starts with a Ui prefactor").

## Problem Statement

I can run my training (Home, the log) but I cannot change anything about it on my phone. The Guide is read-only inside the log, the Exercise library page is a placeholder, so "Add a new Exercise" in the picker is disabled, and the Plan and the Week schedule can only change through Import. Every new form I add would also copy the same saving, read-only and sheet code that is already duplicated across Home, the Custom workout draft and the log.

## Solution

Four editing surfaces plus the shared plumbing under them:
- A **Guide page** per Exercise: read view, section-by-section edit, a "No Guide yet" state, a video link with an offline state, and personal notes.
- An **Exercise library** with search, tag filter, groups by Session, and New Exercise (also reachable from the picker, where "Add a new Exercise" becomes live).
- A **Plans list and Plan editor**: make a Plan active, create a Plan from scratch or by copying, and edit a Plan's Settings, Phases and Sessions with form rows and reorder buttons.
- A **Week view**: this week's Sessions on their days, move a Session (this week only or as the new default day), add a Break, and accept or undo a suggested re-arranged week.

Before any of these, a prefactor wave adds three shared Ui modules (guarded save, page context, form sheet) and moves the existing pages onto them, so the new editors are callers of one module instead of copies.

## User Stories

Guide (S-03, F-09)
1. As the owner, I want to open an Exercise's Guide from the library, the log, or the Plan editor, so that I can read how to do it.
2. As the owner, I want the Guide to show its sections in the design's order (What it works, Why it's in your plan, Setup, How to do it, Cues, Common mistakes, Easier / harder variations, Watch-outs), so that it reads the same everywhere.
3. As the owner, I want the Guide header to show where the Exercise sits in the Plan and its Best, so that I see the context at a glance.
4. As the owner, I want to see my last three results with Personal records marked, so that I do not have to open the log to remember.
5. As the owner, I want to tap Watch a demo and open the video link, so that I can see the movement.
6. As the owner, I want a clear offline message when the video cannot open, so that I know the rest of the app still works.
7. As the owner, I want to edit each Guide section in its own text area and Save or Cancel, so that I can fix a Guide on the phone.
8. As the owner, I want a "Discard changes?" prompt when I leave an edit with unsaved changes, so that I never lose text by accident.
9. As the owner, I want to add and edit personal notes separately from the Guide sections, so that a note like "Left knee: keep the box high" is quick to save.
10. As the owner, I want an Exercise with no Guide to show "No Guide yet" with Write a Guide, so that I can start one.
11. As the owner, I want an invalid video link to be refused with a plain message, so that Watch a demo always opens something real.
12. As the owner, I want coming back from the Guide to the log to put me on the same Set with nothing lost, so that checking a Guide mid-Workout is safe.
13. As the owner, I want editing a Guide to be disabled with a plain reason when the app is read-only, so that I am never confused by a dead Save button.

Exercise library (S-04)
14. As the owner, I want to browse every Exercise grouped by Session and then "Not in a Session", so that I find the ones I use first.
15. As the owner, I want to search by name and filter by tag chips, so that I can find an Exercise fast.
16. As the owner, I want each row to show its log kind, tags and whether it has a Guide, so that I can see gaps.
17. As the owner, I want a no-match search to offer "Add '<text>' as a new Exercise", so that adding is one tap from a failed search.
18. As the owner, I want to add an Exercise with a name, how it is logged, tags and an optional cue, so that it can be planned and logged.
19. As the owner, I want a duplicate or empty name to be refused with a plain message naming the existing Exercise, so that the library stays clean.
20. As the owner, I want "Write a Guide now" on the New Exercise form to go straight to the Guide editor after Add, so that I can finish the job in one go.
21. As the owner, I want a new Exercise to keep its identity if I rename it later, so that history is never lost (existing rule).
22. As the owner, I want "Add a new Exercise" in the Swap, Add and Custom workout picker to open the same form and return me to the picker with the new Exercise chosen, so that I do not leave the log.
23. As the owner, I want the library page to have an empty state when there are no Exercises, so that it is never blank.

Plans list (S-05)
24. As the owner, I want a list of Plans with the active one outlined as "Active · week n of m", so that I know which Plan is running.
25. As the owner, I want Make active on another Plan, so that I can switch Plans; the previous Plan stops being active in the same save, so there is never zero or two active Plans.
26. As the owner, I want New Plan > From scratch and Copy this Plan, so that I can start a new programme or a variation.
27. As the owner, I want a copied Plan to get fresh identities and start inactive, so that it never collides with the original.

Plan editor (S-05, F-04, C-20)
28. As the owner, I want tabs Settings, Phases and Sessions, so that each kind of edit has a place.
29. As the owner, I want to edit start date, goal and goal date, weekly target, Streak minimum, counting of Custom workouts and the weight increment, so that Plan-level rules are mine to set.
30. As the owner, I want Phase rows with focus and the weeks they cover (shown as dates), so that I can see and change the shape of the Plan.
31. As the owner, I want overlapping or reversed Phases refused with a message naming both Phases, so that the Plan always has a clear current Phase.
32. As the owner, I want an optional Phase goal per Phase, so that I can say when I am ready to move on.
33. As the owner, I want each Session's Exercises as editable rows with Sets, Reps (and a max), a Step increment where it applies, Cue and the Benchmark toggle, so that I can tune the work.
34. As the owner, I want Move up, Move down and Remove on every row, so that reordering works without dragging.
35. As the owner, I want Add Exercise from library on a Session, using the picker, so that I can extend a Session.
36. As the owner, I want to edit a Session's name, default day and type, and add a new Session, so that the week can change shape.
37. As the owner, I want invalid sets or reps to show an inline message and disable Save until fixed, so that a bad Plan can never be saved.
38. As the owner, I want a banner "Edits apply going forward. Past Workouts are unchanged.", so that I do not worry about my history.
39. As the owner, I want leaving the Plan editor with unsaved changes to ask "Discard changes?", so that nothing is lost by accident.
40. As the owner, I want editing the active Plan to update Home and the Week view immediately after Save, so that I see the result.

Week view (S-06, F-08)
41. As the owner, I want this week's Sessions on their days with Done, Missed and Today chips, so that I see where I am.
42. As the owner, I want a Move button per Session with a day picker and the choice "This week only" or "Make this the default day", so that I can handle a busy week or a permanent change.
43. As the owner, I want a moved Session to stay moved for that week only when I choose "This week only", so that next week is unchanged.
44. As the owner, I want to add a Break with a date range and reason, so that Home goes quiet and the Streak is protected.
45. As the owner, I want a Break that overlaps an existing Break refused with a plain message, so that Breaks stay unambiguous.
46. As the owner, I want a suggested re-arranged week when a Break ends, shown as a draft with changed rows highlighted and one summary line, so that I can accept it knowingly.
47. As the owner, I want per-row Undo and Cancel or Apply on the draft, so that nothing changes until I say so; Cancel keeps the old week.
48. As the owner, I want outstanding Sessions to become Missed only after their day has passed, so that a draft never creates a Missed session early (existing rule).

Shared and cross-cutting
49. As the owner, I want every save in these screens to refuse politely when the app is read-only and say why, so that a disabled screen is never a mystery.
50. As the owner, I want a failed save to show the same failure notice as everywhere else and keep my edits on screen, so that I can retry.
51. As the owner, I want a double tap on Save to save once, so that I never create duplicates.
52. As the owner, I want every editor sheet to look and behave the same (read-only well, disabled rule, error message), so that I learn it once.
53. As a maintainer, I want one guarded-save module, one page context and one form-sheet module, so that each new form is a caller, not another copy.
54. As a maintainer, I want the choice rules for the picker (matching, ordering, exclusion, search threshold) in Core behind one interface, so that they are tested without rendering.
55. As a maintainer, I want the active Plan decided in one Core rule, so that Ui never re-derives it.
56. As a maintainer, I want existing pages (Home, the Custom workout draft, the log) moved onto the shared modules in the first wave, so that the old copies are deleted, not left beside the new ones.

## Implementation Decisions

Prefactor wave (no new screens; order: guarded save, page context, form sheet)
- **Guarded save module.** One Ui module owns the busy guard (a second tap while saving does nothing), the CanWrite check, the commit, the "no longer there" outcome for a record that disappeared, and reporting a failure to the existing failure notice. Callers pass a change and read a result (saved, refused because read-only, refused because busy, failed). It replaces the copies in Home, the Custom workout draft, the log actions, the log's saving base, and the direct commits in Theme saving. Workout and Exercise writers become callers of it.
- **Page context.** One module gives a page `CanWrite`, the read-only message, today's date and the active Plan. The active-Plan rule ("only one Plan is active") moves to a Core rule that Ui calls. The `place` argument of the read-only wording and the `canWrite` argument threaded through the finish flow go away. May be merged into the guarded save module if that is simpler at ticket time.
- **Form sheet module.** One Ui module owns open state, the read-only well (one ARIA role), the disabled rule (saving or read-only), the error message and close. The six existing sheets (Log weight, Log a past Workout, Do a different Session, Pain check, Logged afterwards, and the Today card's read-only well) move onto it. Their public behaviour does not change.
- **Existing pages move in the same wave.** Home, the Custom workout draft and the log use the shared modules before any Editors page lands. Their tests keep passing unchanged except where they asserted copied internals.
- **Neutral page fixture.** The log's test fixture is renamed and kept as the shared fixture for page tests. No behaviour change.

Picker (inside Editors, not the prefactor)
- Matching, ordering, exclusion and the search threshold move behind one Core interface that takes the context (Swap, Add, Custom workout, Plan editor) and the query and returns the groups. The picker renders them. The "Add a new Exercise" button becomes live and opens the New Exercise form; on Add the picker returns with the new Exercise selected. Empty search offers "Add '<text>' as a new Exercise".

Exercise and Guide rules (Core)
- **New Exercise rule:** validates name (non-empty, not a duplicate ignoring case and surrounding spaces), log kind, optional tags and cue; assigns a permanent id through the existing id generator; returns the Exercise or a reason. Log kinds offered: every kind in the model except the two body measurements, which belong to Weight and Body (design says nine kinds; confirm at ticket time).
- **Guide edit rules:** sections are plain text and lists; saving a Guide creates it if none exists; personal notes save independently of the sections; the video link must be an http or https address, or empty. An empty section is stored as absent.
- **Guide history view (Core):** last three results for the Exercise with Personal records marked, and where it sits in the Plan (Sessions and Phase). Reuses the existing performance rules.

Plan rules (Core)
- **Plan edit rules:** validation (Sets at least 1, Reps at least 1, max not below Reps, weekly target and Streak minimum at least 1, goal date not before start), Phase overlap and ordering (naming both Phases in the message), row move up and down, remove, add from library (appended at the end), add Session, copy Plan (fresh ids, inactive, same content), make active (returns both Plan changes for one commit).
- Phases are stored in weeks (as in the data model); the editor shows them as dates derived from the Plan start date and the week numbers.
- "Edits apply going forward": no past Workout is touched, because a Workout already holds its own planned rows.
- Deleting a Plan or a Session is out of scope (Workouts reference both).

Week rules (Core)
- **Move a Session:** "This week only" writes a move into that week's Week schedule; "Make this the default day" changes the Session's default day in the Plan. Both validate that the target day is in the Plan's week and that two Sessions on the same day are allowed (the existing layout rule).
- **Break creation:** rejects an end before the start and an overlap with an existing Break.
- **Suggested week:** uses the existing re-arrangement rule; the draft is a proposal held in the page until Apply. Apply writes the Week schedule in one commit; Cancel writes nothing.

Ui pages
- **Guide page** at a Guide route per Exercise, reachable from the library, the log's View Guide, and the Plan editor. The log's View Guide sheet stays a sheet; the page and the sheet share the same Guide content and read view.
- **Exercise library page** replaces the placeholder at the existing Exercises route; **New Exercise** is a form sheet.
- **Plans page** replaces the placeholder; **Plan editor** is a page with three tabs (Settings, Phases, Sessions) using the C-20 row patterns (expand to edit, Move up, Move down, Remove, toggle, banner). No drag and drop in this stage; the buttons are the accessible alternative the design requires.
- **Week page** replaces the placeholder: rows, Move sheet, Add a Break sheet, draft view with Undo, Cancel and Apply.
- Every save goes through the guarded save module; every sheet is a form sheet; every page reads CanWrite, the read-only message, today and the active Plan from the page context.
- Leaving an editor with unsaved changes asks "Discard changes?" (Keep editing or Discard) for the Guide editor and the Plan editor.
- Theme and Mode behave as everywhere; tokens only, no raw colours or pixels.

## Testing Decisions

- A good test drives external behaviour: what a page shows after a tap and what is stored. It never reads private fields or CSS-structure beyond a stable selector.
- Three seams, no more:
  1. **Core rules** in Core.Tests: New Exercise rule, Guide edit rules, Plan edit rules (validation, overlap, reorder, copy, make active), Break creation, Session move, picker choice rules, the active-Plan rule. Prior art: `HomeViewBuilderTests`, `HomeTodayRulesTests`, `WeekRearrangement` tests.
  2. **The guarded save module** tested once at its own interface over the in-memory store and the fault-injecting store: busy guard, read-only refusal, "no longer there", failure notice. Prior art: `LogActions` tests and the `HomeTests` failure cases. Per-page tests then only prove a page uses it, not the guard itself.
  3. **Each page in bUnit** over the in-memory store: Guide, Library with New Exercise, Plans and Plan editor, Week with Move, Break and the draft; the picker's new "Add a new Exercise" path; existing pages after the prefactor. Prior art: `HomeTests`, `HomeActionsTests`, `CustomWorkoutDraftTests`, `WorkoutLog*Tests`.
- Assertions after a click that starts async work use `WaitForAssertion`.
- No new browser tests. The 360 px and desktop layouts and the offline video behaviour are manual checks added to `docs/pwa-manual-checks.md`.
- Shared page fixture replaces the log-named one for these tests.

## Out of Scope

- The Badges tab of the Plan editor (Insight, B7) and Import a Plan (Data and settings, B9).
- Deleting a Plan, a Session or an Exercise, and renaming Plans beyond the Settings tab.
- Drag and drop reordering (buttons only).
- Editing or ending a Break after it is created, beyond adding one and the draft that follows it.
- Settings, Backup, Import and Export screens.
- Progress, charts, Trophies, Weekly review.
- Changes to the data model or exchange format (the Guide, Exercise, Plan, Week schedule and Break records are used as they are).
- Browser (Playwright) tests.

## Further Notes

- The design says "9 log kinds" in the New Exercise form; the model has 13 values. This spec offers all except the two body measurements; adjust at ticket time if the design intends a different nine.
- The design shows Phases as dates; the model stores weeks. The editor converts for display and storage keeps weeks.
- Ticket waves: wave 1 is the prefactor (guarded save, page context, form sheet, existing pages moved); wave 2 is the Core rules (Exercise, Guide, Plan, Week, picker choices); wave 3 is the pages (Guide, Library, Plans and editor, Week), which can run in parallel because they touch different files; the router and shell edits are owned by one ticket at a time.
- After the group's PR, run the architecture review again (see `docs/build-plan.md`).
