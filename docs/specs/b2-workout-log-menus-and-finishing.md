---
labels: ready-for-agent
stage: B2
---

# B2: Workout log, menus and finishing

Parent docs: [`../build-plan.md`](../build-plan.md) (Track B), [`../design/design-document.md`](../design/design-document.md) (C-07 StateChips, C-13 PainCheckSheet, C-14 ExercisePicker, S-02 states, §8.4 to 8.6, F-01), [`../design/assets/prototype-log.html`](../design/assets/prototype-log.html) (reference behaviour), [B1 spec](b1-workout-log-core-input.md) (what B2 extends), [`../data-spec.md`](../data-spec.md) (Workout, Entry, Pain check), [A2 spec](a2-core-rules.md) (WorkoutOutcomes, Streak, Badges), [A3 spec](a3-storage.md), [ADR 0005](../adr/0005-single-writer-quarantine-and-two-storage-versions.md), `../../GLOSSARY.md` (Skipped exercise, Swapped exercise, Partial workout, Pain check, Benchmark exercise, Streak, Badge).

## Problem Statement

B1 made the log usable for the happy path only. The owner cannot skip an Exercise when the rack is taken or a joint hurts, cannot swap in an alternative, cannot add an Exercise, cannot look at a Guide mid-Workout, and has no rest timer. Finishing just stamps an end time and drops the owner on Home: no Partial prompt (so unfinished Exercises are silently "unrecorded"), no Pain check (so Nudges have no data), no summary (so no PRs recap, no Streak line, no sense of closure). A Workout cannot be logged for a past day, so a gym visit with no phone has to be thrown away or faked. Until this is done the owner cannot honestly replace the Obsidian system.

## Solution

B2 completes the Workout log:

1. **Exercise menu** beside the Exercise name: Swap, Skip, Add an Exercise, Mark as / Unmark as benchmark, View Guide.
2. **Workout menu** in the top bar: rest timer (Off / 60 / 90 / 120 s), Theme, Mode, Logged afterwards, Finish Workout.
3. **Skip sheet** with reason chips (Short on time, Pain, No equipment, Other; all optional). Skipped Exercises show a "—" chip in the pager and a Skipped state, and can be un-skipped.
4. **Swap and Add via the shared ExercisePicker (C-14):** Similar Exercises first, then all; Exercises already in the Workout hidden; search when the library has more than 20. Swap keeps the planned sets and records "was X"; Add inserts after the current Exercise marked "+ Added".
5. **View Guide as a sheet** with "Back to the Set" that loses nothing. Until B4 lands the Guide screen, the sheet shows the Guide content that exists (or the no-Guide state).
6. **Rest timer and Wake Lock.** The timer (off by default) starts on Confirm and reads "Rest 1:12" in the ConfirmBar; the screen stays awake while the log is open where the browser supports it.
7. **Finish flow:** Finish with Exercises left asks "Finish as Partial? 5 of 8 done; the rest are recorded as skipped. Partial still counts toward your Streak." (Keep going / Finish); then the optional **Pain check** (No / Yes / Skip); then the **summary** (duration, Sets, PRs with previous best, every Exercise's Done / Skipped / Swapped and RPE, the Pain check result, the Streak line, and a Badge unlock toast if one unlocked).
8. **Logged afterwards:** a Workout for a past date shows the banner "Logged afterwards · Sun 15 Nov. No timers.", hides the rest timer and elapsed time, and records no duration. B2 adds the Core rule that creates it and the Workout-menu item that re-dates the open Workout; B3 adds Home entry points.
9. **StateChips (C-07)** everywhere the log shows state (PR, Done, Partial, Skipped, Swapped, Pain), each with glyph, word and colour.
10. **Error states** for the new surfaces, using B1's shared save-failure banner.

The owner sees this as: a joint twinges, tap the Exercise menu, Skip, Pain; rack taken, Swap to a similar Exercise; done early, Finish, confirm Partial, answer the Pain check in one tap, see the summary. The old Finish is gone.

## User Stories

### Exercise menu
1. As the owner, I want an Exercise menu button beside the Exercise name, so that every per-Exercise action is one tap away.
2. As the owner, I want the menu to list Swap, Skip, Add an Exercise, Mark as benchmark (or Unmark) and View Guide, so that the whole set of actions is in one place.
3. As the owner, I want the menu to close with Escape or a tap outside and to return focus to its button, so that it is usable with a keyboard and never traps me.
4. As the owner using a screen reader, I want the menu button labelled with the Exercise name ("Menu for Goblet squat"), so that it is not an unnamed icon.
5. As the owner, I want every menu item to be a 44 px target, so that I can hit it with sweaty thumbs.

### Skip
6. As the owner, I want Skip to open a sheet with reason chips Short on time, Pain, No equipment and Other, so that I can say why in one tap.
7. As the owner, I want the reason to be optional with a plain **Skip** button, so that skipping is never slowed down.
8. As the owner, I want a skipped Exercise to show a "—" in the pager, a Skipped state with its reason and its Sets hidden, so that I can see what I left.
9. As the owner, I want skipping to move me on to the next Exercise that is not complete, so that I keep flowing.
10. As the owner, I want to un-skip a skipped Exercise from its menu, so that a mistaken tap is not permanent.
11. As the owner, I want skipping an Exercise that already has confirmed Sets to ask first and say the Sets will be removed, so that I never lose work by accident.
12. As the owner, I want choosing Pain as the reason to remain separate from the Pain check, so that skipping and reporting stay independent.

### Swap
13. As the owner, I want Swap to open a picker titled "Swap Goblet squat", so that I know what I am replacing.
14. As the owner, I want a Similar group first (most shared tags first), then All Exercises, so that the likely alternative is at the top.
15. As the owner, I want each row to show an icon by log kind, the name and its tags, so that I can recognise it.
16. As the owner, I want Exercises already in this Workout hidden from the picker, so that I cannot duplicate one.
17. As the owner, I want a search box when the library has more than 20 Exercises, and an empty-search message "No match. Add a new Exercise", so that I can find one quickly.
18. As the owner, I want the swapped Exercise to take the original's place with its planned sets, so that I log it straight away.
19. As the owner, I want the swapped Exercise to show "Swapped · was Pull-up" and a ⇄ in the pager, so that the substitution is visible.
20. As the owner, I want the original recorded as swapped, not skipped, so that history and Partial rules treat it correctly.
21. As the owner, I want the new Exercise's header (Last, Best, Suggested target) to be its own history, not the original's, so that the numbers make sense.
22. As the owner, I want to swap again or undo a swap before logging a Set, so that a wrong pick is fixable.
23. As the owner, I want swapping an Exercise that already has confirmed Sets to ask first, so that I never lose work by accident.

### Add an Exercise
24. As the owner, I want Add an Exercise to open the same picker titled "Add an Exercise", so that I learn one pattern.
25. As the owner, I want the added Exercise inserted after the current one and marked "+ Added", so that it is clearly extra.
26. As the owner, I want an added Exercise to get one empty Set row with the Exercise's own kind, and to be loggable at once, so that it behaves like any other.
27. As the owner, I want an added Exercise that I never log to be dropped on Finish rather than recorded as skipped, so that extras do not make a Partial.
28. As the owner, I want to remove an added Exercise that has no Sets, so that a stray pick does not linger.

### Benchmark
29. As the owner, I want to mark or unmark the current Exercise as a benchmark from its menu, so that I can start tracking it mid-Workout.
30. As the owner, I want the menu label and a small mark to reflect the current state, so that I know what the tap will do.
31. As the owner, I want the change saved at once, and a failed save reported with the shared banner, so that nothing silently fails.

### View Guide
32. As the owner, I want View Guide to open the Exercise's Guide in a sheet, so that I can read the cues without leaving the Workout.
33. As the owner, I want "Back to the Set" to close it with typed text, the open row and the pager exactly as they were, so that nothing is lost.
34. As the owner, I want an Exercise with no Guide to say so plainly in the sheet, so that the sheet never looks broken.
35. As the owner, I want the Guide's video link to open in a new tab and say so, so that I do not lose the log.
36. As the owner, I want the sheet to be a labelled dialog with focus inside it and Escape to close, so that it is accessible.

### Workout menu
37. As the owner, I want a Workout menu in the top bar, so that Workout-level actions are separate from Exercise ones.
38. As the owner, I want rest timer choices Off, 60, 90 and 120 s with Off the default, so that the timer never appears unless I want it.
39. As the owner, I want the Theme and Mode switches in the menu, so that I can change the look without leaving the log.
40. As the owner, I want the chosen rest timer remembered between Workouts, so that I set it once.
41. As the owner, I want Finish Workout in the menu, so that I can end a Partial Workout early.
42. As the owner, I want Logged afterwards in the menu, so that I can say this Workout was for a past day.

### Rest timer and Wake Lock
43. As the owner, I want the rest timer to start when I confirm a Set, so that I do not have to remember to start it.
44. As the owner, I want it shown as "Rest 1:12" in the ConfirmBar, counting down, so that I see it while I stand there.
45. As the owner, I want confirming another Set to restart it, and finishing the countdown to stop and clear it quietly, so that it never nags.
46. As the owner, I want the timer to survive the page re-rendering and to be based on the clock, not a tick count, so that it stays right if the phone sleeps.
47. As the owner, I want the screen to stay awake while the log is open, so that I do not unlock my phone between Sets.
48. As the owner, I want the Wake Lock to be reacquired when I return to the tab and released when I leave the log, so that the battery is not drained elsewhere.
49. As the owner, I want a browser without Wake Lock support to work normally, so that the lack is silent.
50. As the owner, I want no timer and no elapsed time on a Logged afterwards Workout, so that the screen never shows meaningless numbers.

### Finishing
51. As the owner, I want Finish to ask "Finish as Partial?" with the count done and the words "the rest are recorded as skipped", so that I know what will be saved.
52. As the owner, I want Keep going to close the prompt and change nothing, so that an accidental Finish is harmless.
53. As the owner, I want a Partial finish to record each unlogged planned Exercise as skipped with no reason, and any empty Done entry as skipped too, so that history never has an ambiguous gap.
54. As the owner, I want a fully done Workout to skip the Partial prompt and go straight to the Pain check, so that the normal path is short.
55. As the owner, I want Finish to be unavailable until at least one Set is confirmed, with a plain message, so that I cannot save an empty Workout by mistake.
56. As the owner, I want a second tap on Finish while it saves to do nothing, so that the end time is stamped once.
57. As the owner, I want a failed finish save to leave the Workout open with the banner shown, so that nothing is half-finished.

### Pain check
58. As the owner, I want a bottom sheet "Any pain today?" with No, Yes and Skip, so that the answer is one tap.
59. As the owner, I want No to be the primary, focused button, so that the usual answer is fastest.
60. As the owner, I want Skip to record "not answered", different from No, so that the data says what I actually did.
61. As the owner, I want Yes to expand in place with body area chips (multi-select), severity chips (single), an optional Exercise select listing this Workout's Exercises, an optional side (Left, Right, Both) and an optional note, so that I can describe it quickly.
62. As the owner, I want Save to need at least one area and one severity, with a plain message when missing, so that a "reported" check is always meaningful.
63. As the owner, I want the chips to be toggle buttons announced as pressed, and Escape to mean Skip, so that the sheet is accessible.
64. As the owner, I want my answer saved with the Workout and the Pain check shown in the summary ("Yes: Left knee, mild (during Goblet squat)"), so that I can see what I recorded.
65. As the owner, I want the side recorded as part of the note text, so that the data model stays unchanged.

### Summary
66. As the owner, I want a summary of duration, Sets and PRs, so that I see how it went.
67. As the owner, I want each PR listed once per Exercise with the previous best, so that it matches Home and the toast.
68. As the owner, I want every Exercise listed with a Done, Skipped or Swapped chip, its reason or "was X", and its RPE, so that the Workout is explained.
69. As the owner, I want a Partial workout labelled Partial with the half-filled chip, so that it is honest.
70. As the owner, I want a Streak line from the Streak rule ("Week 5 of your Streak"), so that the effort registers.
71. As the owner, I want a toast when a Badge unlocked, so that I am told.
72. As the owner, I want a Done button that takes me Home, and reopening a finished Workout's log URL to show the summary rather than a "not found", so that a refresh does not lose it.
73. As the owner, I want the duration shown as "—" for a Logged afterwards Workout, so that nothing misleading appears.

### Logged afterwards
74. As the owner, I want the Workout menu to let me pick the past date this Workout happened, so that a gym visit with no phone can be logged.
75. As the owner, I want a date after today to be refused with a plain message, and no limit on how far back, so that mistakes are caught but history is free.
76. As the owner, I want the banner "Logged afterwards · Sun 15 Nov. No timers.", so that the screen says what it is.
77. As the owner, I want a Logged afterwards Workout to record no duration, so that the data is truthful.
78. As the owner, I want its Sets to be compared with history as of that date, so that PRs are judged fairly.
79. As the owner, I want to undo Logged afterwards from the same menu item while I am still logging, so that a mistaken tap is fixable.

### States, errors and housekeeping
80. As the owner, I want all new state shown with a glyph, a word and a colour, so that nothing relies on colour alone.
81. As the owner, I want a failed save from any menu action to show the shared banner and leave the screen unchanged, so that a storage problem never loses work.
82. As the owner, I want the read-only message from B1 to disable every new action, so that no button is dead without explanation.
83. As the owner, I want the Suggested target reason to say lb when I use lb, so that the numbers match my plates.
84. As the owner, I want the log to survive every new action across reload (skip, swap, add, rest setting), so that a killed tab loses nothing.
85. As the owner using a keyboard or screen reader, I want every sheet and menu to be a labelled dialog or menu with visible focus, so that nothing needs a touch screen.
86. As the owner, I want reduced-motion respected for sheets and toasts, so that nothing animates when my phone asks for none.

## Implementation Decisions

### Data model: unchanged
- **No new fields, no format change.** Every B2 concept maps onto what A1 stores:
  - Skipped: an entry with status skipped, an optional reason and no Sets.
  - Swapped: an entry for the Exercise actually done with status swapped and the planned original named; the Workout's planned snapshot is untouched.
  - Added: an entry whose Exercise matches no planned row. A2's outcomes rule already reports these as extras, so "+ Added" is derived, not stored.
  - Pain check: the Workout's Pain check record (none or reported). Absent means unanswered (Skip).
  - Logged afterwards: the existing flag, with the date set to the past day and no start or end time.
- **Side of the body** has no field, so the optional side is written as a leading phrase in the Pain check note ("Left side. ..."). The summary reads it back from the note as plain text.
- **Benchmark** is a property of the library Exercise, not the Workout, so marking it is a separate commit of the Exercise record (not through the Workout writer).

### Core: pure Workout edits (extend the existing edit rules)
All are pure functions returning a new Workout, in the same place as B1's edits, each with Core tests:
- **Skip** an Exercise with an optional reason (removes any Sets and RPE). **Un-skip** returns it to not-yet-done.
- **Swap** a planned Exercise for another (keeps the position; the entry is a swapped entry carrying the Sets so far, if the owner confirmed the swap). **Undo swap** restores the original.
- **Add** an Exercise after a given position; **remove** an added Exercise that has no Sets.
- **Pain check**: set none, set reported (areas, severity, optional Exercise and note), clear.
- **Finish**: stamps the end time. With a Partial choice it first records each planned row that has no entry (or only an empty Done entry) as a skipped entry with no reason, and drops added Exercises with no Sets. A Workout with no confirmed Set cannot be finished.
- **Re-date as Logged afterwards**: sets the flag and a past date, clears the start time and ensures no duration is stored; undoing restores today's date and the flag false.
- **The "Tapping RPE before any Set" question from B1 is settled here:** an empty Done entry holding only an RPE stays until Finish; Finish treats it as not done (it becomes a skipped entry and the RPE is dropped, because a skipped entry records no effort). The pager shows it as not complete, as today.

### Core: new rules
- **Similar Exercises.** A pure ranking of library Exercises against a given Exercise by shared tags (muscle group, equipment, movement pattern), highest first, stable by name, excluding Exercises already in the Workout. Exercises with no tags rank last in All. This is also what B4 and B5 reuse.
- **Workout summary builder.** A pure rule giving, for a finished Workout and history: counts (Sets, PRs, done of planned), one PR per Exercise with previous best (reusing the existing per-Workout PR rules, history excluding that Workout), each Exercise's outcome from A2's outcomes rule with RPE, the Pain check result as text, the Partial flag, the Streak line (from A2's Streak rule), and any newly unlocked Badge (compare Badge progress with and without this Workout, via A2's Badge rules). It performs no I/O.
- **Duration** is end minus start, and absent when logged afterwards or when a start or end is missing.
- **Logged afterwards creation.** A pure rule creating a past-dated Workout for a Session or a Custom workout with the flag set and no timers, saved through the existing atomic commit. B3 supplies the Home entry points; B2's Workout-menu item uses the re-date edit above on the open Workout.
- **Suggested target reason in the display unit.** The reason text takes the user's unit so lb users read "add 5 lb" (closes the B1 open item).
- A2 rules are reused, not edited, apart from the unit change above.

### Ui: menus, sheets and the finish flow
- **Menus and sheets are components driven by plain parameters and callbacks** (the B1 pattern): ExerciseMenu, WorkoutMenu, SkipSheet, ExercisePicker (C-14), GuideSheet, PainCheckSheet (C-13), PartialPrompt, FinishSummary, StateChip (C-07). The page owns all saving; no component decides a rule. B0's Button, Icon and EmptyState are reused. A generic Sheet/dialog shell is built once and shared by all sheets (focus trap, Escape, `aria-modal`, return focus).
- **The log page stays a thin wiring class.** New logic goes in new classes (for example the menu/finish orchestration and view building), not in the page, which must not grow materially. The view builder gains skipped, swapped, added and outcome views; `RowSpec` and the unsaved-state class are extended only where needed.
- **Writes** all go through the existing Workout writer (merging into the snapshot's latest Workout copy) and report failures to the shared notice. The Exercise benchmark commit goes through the same failure reporting.
- **Pager** shows ✓ done, — skipped, ⇄ swapped and "+" added, and the ConfirmBar's primary keeps its three labels, with Finish now opening the finish flow.
- **Finish flow state machine:** Finish → (Partial prompt if any planned Exercise is not done) → write the finish → Pain check sheet → write the answer → summary. The end time is committed before the Pain check opens, so killing the tab at that point leaves a finished Workout with an unanswered Pain check, never an open one. Reopening a finished Workout's log URL shows the summary (the Pain check, if unanswered, is not re-asked).
- **Logged afterwards in the UI:** banner text from the Workout's date; rest timer, elapsed time and Wake Lock indicator hidden; summary duration "—".
- **Guide sheet:** shows the Guide content that exists today for the Exercise (read from the snapshot) or the no-Guide message. B4 will provide the Guide page; B2's sheet must be built so B4 can swap its content in without changing the sheet.
- **Theme and Mode switches** in the Workout menu reuse B0's Theme service.
- **Rest timer setting** (Off/60/90/120) persists as a per-device preference (not part of exported data or the Workout), using the same mechanism B0 uses for Theme and Mode. Countdown is computed from a start timestamp and the clock, not a tick counter, and is cancelled when the page is disposed.
- **Wake Lock** is behind a small interface (acquire, release, supported) with a browser implementation in the Web project following the existing interop pattern (a small JS module), and a no-op fake for tests and unsupported browsers. It is acquired when the log opens a Workout, reacquired on visibility return, and released on leaving. Failures are swallowed silently (design: silent no-op).
- **Tokens and CSS:** plain CSS, design tokens only, `fa-*` classes. New tokens (for example for sheet backdrop, sheet radius, chip states) go in both `tokens.css` copies plus `tokens.json` and the design document tables in the same change. Tap targets at least `--tap-min`.
- **Service worker:** a new JS file is added to the offline cache list; the existing coverage test must stay green.

### Not changed
- The data model, exchange format, `data-spec.md`, storage layer, ADRs, and routing structure (the finished-Workout summary reuses the log's route).

## Testing Decisions

- **What makes a good test:** it drives the log as the owner does (open a menu, tap Skip, choose a chip, Finish, answer the Pain check) and asserts what they see or what was saved (text, chips, `aria-*`, the saved Workout's entries, end time, Pain check), not private state, component internals or CSS class names beyond the documented `fa-*` contract.
- **Main seam (settled with the owner): bUnit on the log page in `Ui.Tests`,** over real `AppData` on Core's in-memory store, with the fixed clock, sequential ID generator and existing test-data builders. Scenarios: Skip with and without reason, skip with Sets (confirm first), un-skip; Swap (similar order, hidden duplicates, search over 20, kept planned sets, "was X", own header); Add (position, "+ Added", dropped on Finish when empty, remove); benchmark toggle; Guide sheet open and "Back to the Set" preserves typed text; Workout menu items; rest timer on Confirm, restart, expiry, hidden when logged afterwards (fake clock); Wake Lock acquire/release through the fake; Finish fully done vs Partial prompt, Keep going, Partial writes skipped entries, empty Workout refused; Pain check No / Skip / Yes validation and saved record; summary content (PRs with previous best, outcomes, Streak line, Badge toast, "—" duration); reopening a finished Workout shows the summary; Logged afterwards re-date, future date refused, banner; read-only disables every new action; failed save leaves the screen unchanged and shows the banner (existing fault-injecting store).
- **Core seam: unit tests for the new pure rules only:** each new edit (skip, un-skip, swap, undo swap, add, remove, Pain check, finish incl. Partial, re-date), the similar-Exercise ranking, the summary builder (PR per Exercise, previous best, Streak line, Badge unlock, duration rules), the past-dated creation rule, and the unit-aware Suggested target reason. Follow the style of `WorkoutEdits`, `WorkoutOutcomes`, `ExercisePerformance` and `SuggestedTarget` tests, reusing `WorkoutBuilder`, `HistoryBuilder` and `PlanFixture`. Include the empty-RPE-entry-at-Finish case.
- **Component tests** (bUnit) only for rules later stages most easily break: the shared Sheet's focus start, Escape and return-focus behaviour, `aria-pressed` on Pain chips, labelled icon-only buttons, StateChip glyph plus word, 44 px target classes. Everything else is covered through the page.
- **Wake Lock and rest timer:** the interface is faked in bUnit; **no new browser test** (owner's decision). A new section is added to `docs/pwa-manual-checks.md`: rest timer in the ConfirmBar, Wake Lock (screen stays on, reacquires after switching tabs, released when leaving), each sheet on a phone, Pain check, summary, Logged afterwards, kill the tab mid-finish, all four looks, 360 px, offline.
- **Web project:** a small test that the new JS file is in the offline cache list (the existing coverage test).
- **Not unit tested:** how it looks (checked by eye against `S-02-log-*` mockups) and contrast (B0's table; B11 re-checks).

## Out of Scope

- Home entry points for Logged afterwards, Do a different Session, Build a Custom workout, Make up a Missed session, the real Home: B3. B2 builds only the rule and the in-log behaviour.
- The Guide page itself (read and edit), the Exercise library screen and creating a new Exercise from the picker's empty-search link: B4 (the link goes to the library in B2 or is disabled until B4, whichever is cheaper, with a note).
- The Log weight sheet and body-weight screens: B3 and B6.
- Nudges that consume the Pain check (A2 rules exist; B3 wires them to Home).
- The calendar, Week view and history markings of Partial, Skipped and Swapped (C-07 chips are built here and reused there).
- The Distance + time row, Text Exercises, minutes input of `12:30`, extra steppers (open items from B1, unchanged), and the "(prototype only) Discard" item.
- Any data model, exchange format, storage or ADR change.
- Streak or Badge logic changes: the summary only reads A2's rules.

## Further Notes

- **Decisions recorded with the owner:** seams as above (bUnit on the page, Core tests for new rules only, no new browser test); Logged afterwards in B2 is the Core rule plus in-log behaviour, with B3 owning Home entry points.
- **Decisions made in this spec (veto at ticket time if wrong):**
  1. A Partial finish writes explicit skipped entries (no reason) rather than relying on A2's "unrecorded" reading, so history is unambiguous. A2's reading still handles old data.
  2. Added Exercises with no Sets are dropped at Finish rather than recorded as skipped (extras never make a Partial).
  3. An RPE-only empty Done entry is treated as not done at Finish (becomes skipped; RPE dropped).
  4. The end time is committed before the Pain check opens, so a killed tab never strands an open Workout.
  5. The Pain check's side lives in the note text (no new field).
  6. The rest timer preference is a per-device setting, not exported data.
  7. Finish is refused for a Workout with no confirmed Set.
- **Sizing for `/to-tickets`:** the handoff expects B2 may exceed one agent's budget. Suggested cuts, each within one agent's budget: (1) Core: skip/un-skip, swap/undo, add/remove, benchmark helpers and similar-Exercise ranking, with unit tests; (2) Core: Pain check, finish (incl. Partial), re-date and past-dated creation, summary builder, unit-aware Suggested target reason, with unit tests; (3) shared Sheet shell, StateChip, ExerciseMenu, WorkoutMenu and the log page wiring for menus (Theme, Mode, benchmark); (4) SkipSheet and ExercisePicker with Skip, Swap, Add end to end through the page and view builder (pager marks, "Swapped · was", "+ Added"); (5) GuideSheet, rest timer and Wake Lock (interface, JS module, cache list, ConfirmBar readout); (6) Finish flow: PartialPrompt, PainCheckSheet, FinishSummary, reopen-shows-summary, replacing the temporary Finish; (7) Logged afterwards UI behaviour, error and read-only states, manual checks and docs. Tickets 1 and 2 are independent and can run in parallel; 3 needs only B1. Order 4, 5 and 6 so only one touches the log page, `ConfirmBar` and `components.css` at a time, as B1's merge conflicts showed.
- **For the owner when it lands:** after B2 the log is a complete daily tool; the sensible time to start logging real Workouts in the app is after B2 and the B1 phone checks.
