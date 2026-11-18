---
labels: ready-for-agent
stage: B1
---

# B1: Workout log, core input

Parent docs: [`../build-plan.md`](../build-plan.md) (Track B), [`../design/design-document.md`](../design/design-document.md) (§5 C-02 to C-06, §6 S-02, §8.1 to 8.3, F-01 main path), [`../design/assets/prototype-log.html`](../design/assets/prototype-log.html) (reference behaviour), [`../data-spec.md`](../data-spec.md) (Workout, Set), [A2 spec](a2-core-rules.md) (rules), [A3 spec](a3-storage.md) (storage), [B0 spec](b0-design-system-and-shell.md) (shell, components), [ADR 0005](../adr/0005-single-writer-quarantine-and-two-storage-versions.md) (single writer), `../../GLOSSARY.md` (Workout, Session, Set, Max set, PR, Suggested target, RPE, Skipped exercise).

## Problem Statement

The app can store a Workout and Core can rank Sets, spot PRs and propose a Suggested target, but there is nowhere to actually log one. The owner still logs at the gym in the old Obsidian system. The log is the screen used mid-set, one-handed, with sweaty thumbs and a few seconds between efforts, so it has to be faster than the old system or the app has failed. Today a Workout cannot be started, a Set cannot be entered, and nothing survives a reload or a phone call that kills the browser tab.

## Solution

B1 builds the Workout log's main path, end to end on the real store and Core rules:

1. **Start or Continue.** The Home placeholder gets a plain, temporary Start button for the next Session of the active Plan (and Continue when a Workout is in progress). B3 replaces it with the real Home.
2. **The log screen (S-02), full-screen, no tab bar.** A top bar, an Exercise pager, the current Exercise's header with Last, Best and the Suggested target with its reason, one Set row per planned Set prefilled from last time, Add Set and Mark as Max set, an optional RPE selector, and a ConfirmBar with the one primary action and weight steppers.
3. **Every log kind the data model can store**, using the same row grid: weight × reps, bodyweight × reps, carry, seconds, minutes, reps only, time, distance (km or m), tick.
4. **Fast, forgiving input.** Rows are pre-filled and muted until confirmed; confirming a pending row means "same as last". Typed text is checked on Confirm with a plain-language message under the row. Commas work as decimal points.
5. **PR feedback.** A Set that strictly beats the previous Best shows the PR marker, a short toast ("PR! 22 kg × 9 beats 22 × 8") and a count in the header.
6. **Nothing is lost.** The Workout is saved after every change and the log reopens exactly where the owner left off with a "Resumed where you left off" note.

The owner sees this as: tap Start on Home, the log opens on Exercise 1 with every Set already filled in from last time, tap Confirm Set once per Set (or tweak a number first), see a PR when they earn one, tap Next: through the Exercises, leave and come back without losing a thing. Menus, skipping, swapping, the rest timer, the Pain check and the finish summary come in B2.

## User Stories

### Starting and resuming
1. As the owner, I want a Start button for today's Session on Home, so that I can begin logging from the app's front door.
2. As the owner, I want Start to pick the next unlogged Session of the active Plan for this week, so that I do not have to choose.
3. As the owner, I want starting a Workout to copy the planned Exercise rows as they are now, so that later Plan edits never change what I did.
4. As the owner, I want the Phase's Session rows (with any override) used for the current week, so that the log matches my current block.
5. As the owner, I want the log to open on the first Exercise with its Sets already filled in, so that I can start lifting immediately.
6. As the owner, I want Home to show Continue instead of Start when a Workout is in progress, so that I never begin a second one by accident.
7. As the owner, I want to leave the log with Back and have the Workout stay in progress, so that a phone call or a trip to the water fountain loses nothing.
8. As the owner, I want reopening a Workout in progress to land on the Exercise I was on with a "Resumed where you left off" note, so that I pick up where I stopped.
9. As the owner, I want my Sets saved after every confirm, edit and RPE tap, so that a killed browser tab or dead battery loses at most the field I was typing.
10. As the owner, I want a Start with no active Plan or a Plan with no Sessions to say so plainly, so that I understand why nothing starts.
11. As the owner, I want a deep link or refresh on a log URL to restore the log, so that reloading never throws me out of a Workout.
12. As the owner, I want a log URL for a Workout that does not exist (or is already finished) to show a plain message with a way back to Home, so that a stale link is not a dead end.

### Layout and navigation
13. As the owner, I want the log to be full screen with no tab bar, so that a stray tap cannot leave the Workout.
14. As the owner, I want a top bar with Back, the Session name and "x of n done · PRs", so that I always know where I am and how it is going.
15. As the owner, I want a pager of numbered Exercise chips with a tick on the done ones, so that I can see progress and jump to another Exercise.
16. As the owner, I want jumping to another Exercise to lose nothing on the one I left, so that I can do things out of order when the rack is taken.
17. As the owner, I want the primary action fixed at the bottom in my thumb zone and above the phone's gesture bar, so that I can hit it one-handed.
18. As the owner, I want an "Up next" strip in the bottom bar, so that I know what comes after this Exercise.
19. As the owner on a 360 px phone, I want nothing clipped or scrolling sideways, so that small phones work.
20. As the owner on a desktop or tablet, I want the log in a centred readable column, so that it still looks right on a big window.
21. As the owner, I want the log to look right in all four Theme and Mode combinations, with Keep nearly plain on this screen, so that nothing fancy slows me down.

### The Exercise header
22. As the owner, I want the Exercise name large, with the planned Sets × reps and the Plan's cue, so that I remember what I am meant to do.
23. As the owner, I want Last and Best on one line ("Last 22 × 8 · Best 22 × 9"), so that I know what I am trying to beat.
24. As the owner, I want the Suggested target in a sunken well with its one-line reason ("Hit 8 of 8 reps: add 2 kg, drop reps"), so that I trust why it is suggested.
25. As the owner, I want no Last, Best or target shown (and no empty labels) the first time I do an Exercise, so that a new Exercise does not look broken.
26. As the owner, I want the header to read in the unit I chose (kg or lb), so that numbers match my plates.

### Set rows and input
27. As the owner, I want one row per planned Set, numbered, with two large inputs and a state cell, so that each Set is obvious.
28. As the owner, I want rows pre-filled from last time's matching Set (or the Suggested target, or the plan when I have no history), muted until I confirm or edit, so that "same as last" is one tap.
29. As the owner, I want tapping into a field to select all its text, so that typing replaces the value.
30. As the owner, I want number fields to open the numeric keypad (decimal for weight and distance, whole numbers for reps), so that I never fight a letter keyboard.
31. As the owner, I want Enter to move from the first field to the second and to confirm from the last field, so that I can log from the keypad alone.
32. As the owner, I want a comma accepted as a decimal point, so that my phone's keypad never blocks me.
33. As the owner, I want a time typed as `1:48`, `108` or `1:04:10` accepted, so that I can type it the quickest way.
34. As the owner, I want a seconds or minutes Exercise to take `45`, `45s` or `3min`, so that suffixes are optional.
35. As the owner, I want bodyweight Exercises to show a fixed "bw" tag with just reps, and a way to add weight, so that I do not type "bw" every time.
36. As the owner, I want carries entered as kg per hand × metres, so that they match how I think of them.
37. As the owner, I want a tick-only Exercise to be one big tick button, so that a stretch or mobility item is one tap.
38. As the owner, I want a row's input checked only when I tap Confirm, with the message under that row in plain language ("Weight should be a number, like 22 or 22.5"), so that typing is never interrupted.
39. As the owner, I want a failed Confirm to mark the field invalid, return focus to it and keep what I typed, so that I fix it without retyping.
40. As the owner, I want a confirmed Set to show a green tick, and tapping that tick to reopen it for editing, so that fixing a wrong number is easy.
41. As the owner, I want the row I am working on outlined, later rows dimmed, so that my place is obvious.
42. As the owner, I want to add an extra Set to an Exercise, so that I can do more than planned.
43. As the owner, I want to remove an extra Set I added by mistake before confirming it, so that the Exercise does not show a stray row.
44. As the owner, I want to mark a Set as a Max set, so that the attempt is the source of truth for Best.
45. As the owner, I want a Max set clearly labelled on the row, and to be able to unmark it, so that a misclick is not permanent.

### Steppers and the primary action
46. As the owner, I want − and + steppers beside Confirm that change the current Set's weight by the Plan's increment (2 kg by default, in lb when I use lb), so that I can adjust load without the keypad.
47. As the owner, I want a stepped value carried down to the later unconfirmed Sets of that Exercise, so that I only adjust once.
48. As the owner, I want seconds Exercises to step by 5 s and bodyweight Exercises by 1 rep, so that steppers fit the Exercise.
49. As the owner, I want the steppers hidden for time and distance Exercises, so that I am not offered meaningless buttons.
50. As the owner, I want the primary button to read Confirm Set while Sets remain, **Next: <Exercise>** when this Exercise is complete, and Finish after the last Exercise, so that one button always does the obvious thing.
51. As the owner, I want finishing the last Exercise to end the Workout and take me back to Home, so that no Workout is left hanging (B2 replaces this with the full finish flow).

### PRs
52. As the owner, I want a Set that strictly beats my previous Best to show the PR marker (shield in Keep, "PR!" in Clean), so that I feel it.
53. As the owner, I want a short toast "PR! 22 kg × 9 beats 22 × 8" and the header count to update, so that it is celebrated and counted.
54. As the owner, I want the PR rule to follow the Exercise's kind (estimated 1RM for lifts, more reps, longer, lower time, more weight at the same distance for carries), so that PRs mean the same as in the rest of the app.
55. As the owner, I want a Max set to count as the source of truth for Best, so that a heavy single is not drowned out by volume.
56. As the owner, I want the first time I ever do an Exercise never to be called a PR (it is a baseline), so that PRs are earned.
57. As the owner, I want unmarking or editing a PR Set to update the marker and count immediately, so that the screen never lies.
58. As the owner using a screen reader, I want the PR announced in words (", PR" on the reps input label), so that it is not only a picture.

### RPE
59. As the owner, I want an optional effort selector of 6 to 10 per Exercise, so that the next Suggested target can use it.
60. As the owner, I want tapping the selected value to clear it, so that I can take it back.
61. As the owner, I want RPE never to block Confirm or Next, so that it never slows me down.
62. As the owner, I want a Suggested target that holds at RPE 9 to 10 and jumps at 6 or less (A2's rules), so that my effort rating has an effect next time.

### Safety and states
63. As the owner, I want a failed save to say so plainly while keeping my entry on screen, so that a storage problem never silently loses Sets.
64. As the owner, I want a clear read-only message when another window of the app holds the writer lock, so that I know why nothing saves.
65. As the owner, I want the same plain message when storage cannot be read, so that I am not left tapping a dead button.
66. As the owner, I want the log never to crash on an Exercise that was deleted or has an unsupported kind, and to say what it cannot show, so that one bad record does not block a Workout.
67. As the owner, I want a Text Exercise (which has no place in a stored Set) shown as a note that it cannot be logged yet, so that I am not offered a field whose value would be lost.
68. As the owner using a keyboard or screen reader, I want visible focus, labelled fields ("Weight, set 2", "Reps, set 2") and `aria-invalid` on bad fields, so that the log is usable without sight or a touch screen.
69. As the owner, I want reduced-motion settings respected, so that toasts and presses do not animate when my phone asks for none.

## Implementation Decisions

### In progress means no end time (owner's decision)
- A Workout with `EndedAt` null is **in progress**. B1 creates a Workout with `StartedAt` set, `LoggedAfterwards` false and `EndedAt` null, and saves after every change. There is no new field and no format change. At most one Workout is in progress at a time; Start refuses to create a second and offers Continue instead.
- A Workout in the dataset is an ordinary record put through the existing atomic commit path, once per change (the whole Workout record each time, as the store already does). Nothing about the storage layer changes.
- B2 later sets `EndedAt` through the full Finish flow and creates logged-afterwards Workouts with an end time at creation. Both must treat "no end time" exactly as B1 defines it.

### Core: starting a Workout from a Session (new rule)
- A new pure rule creates a Workout for a given Session on a given date: a fresh ID from the existing ID generator, the date, the Session ID, the current time as `StartedAt`, and `Planned` as a copy of the Session's Exercise rows **as in force that week** (a Phase's override for that Session when the Phase covers the week, otherwise the Plan default). It uses the existing Phase and Week rules rather than re-deriving which Phase or week applies. No entries yet: an entry is created when the first Set of that Exercise is confirmed.
- Which Session "Start" offers is decided by existing Week schedule rules (the Session due today or the next undone one this week). If those rules do not already expose a "next undone Session for this week" answer, the ticket adds the thinnest query over them; it does not add new scheduling logic. Make-up and "Do a different Session" belong to B3.

### Core: per-Set PR feedback (new rule)
- A's `ExercisePerformance` finds PRs per Workout, using finished history. The log needs the answer **while the Workout is open**. A new pure rule takes an Exercise, the history without the current Workout, and the current Workout's confirmed Sets so far for that Exercise, and answers: which Set (if any) is this Workout's best so far, whether it strictly beats the previous Best by the Exercise's kind rules, and the previous Best for the toast text. Ranking, Max-set precedence and the baseline rule (first ever result is not a PR) are reused from `SetRanking`, `LogKindRules` and `ExercisePerformance`, not re-implemented.
- Only the Workout's single best Set for an Exercise carries the marker (matching the finish summary's "one PR per Exercise"). When a later Set beats an earlier PR Set in the same Workout, the marker moves to the later Set.
- The marker and the "was …" text are derived on every render from history plus the current Workout. Nothing about PRs is stored.

### Core: building rows and checking input (reuse, one small addition)
- Typed text becomes a Set through the existing input parser (kinds, commas, `m:ss`, suffixes, unit conversion to canonical, plain-language failure message). B1 does not add parsing rules. The log screen shows the parser's message verbatim under the row.
- **Two-field rows.** The design shows two inputs (weight and reps) while the parser takes one string such as `20x10`. The log joins the two fields into the parser's own pair format for the kind (`weight x reps`, `bw x reps`, `kg x metres`) rather than the parser growing a second entry point; for single-field kinds it passes the one field. The join is one small function in Core's rules next to the parser so it is tested once. A blank field produces the parser's own failure message, not a new one.
- **Pre-fill source, in order:** the same-numbered Set of the Exercise's Last Workout; else the Suggested target; else (no history) empty fields with the planned reps as a hint. Pre-fill rows are not part of the stored Workout until confirmed.
- **Display units.** Values shown and typed are in the Settings' display units; the stored value is canonical (kg, km, m, seconds). Steppers use the Plan's `WeightIncrementKg` and show lb when units are lb. The unit converter already exists.

### Ui: the log screen and its parts
- One routed page for the log, using the full-screen layout from B0, addressed by the Workout's ID (so refresh and deep links resume). It reads `AppData`'s snapshot, commits through `AppData`, and re-renders from `Changed`. It holds only transient UI state itself (which Exercise is showing, which field has focus, unconfirmed typed text, the open toast).
- **Components (one Razor component per design C-number, built where first needed):** SetRow (C-02) with its kind variants, PR marker (C-03), ExerciseHeader (C-04), RpeSelector (C-05), ConfirmBar (C-06). They are driven by plain parameters and callbacks, not by `AppData`, so the page owns all saving and a component never decides a rule. The B0 Button, Icon and EmptyState components are reused. Nothing else from the design is built. Skin classes (`fa-*`) from the design's `components.css` are reused or extended there; a raw hex value in component CSS is a defect (B0 rule).
- **Log kinds in the row.** The row component picks its layout from the Exercise's log kind through one switch: two-input (load, bodyweight load, carry), one wide input (reps, seconds, minutes, time, km, m), and the tick button. Input mode and fields per kind follow design §8.1. Body-measurement kinds reuse the one-wide-input row. Text-kind Exercises render the "cannot be logged yet" note (story 67) because a Set has nowhere to keep text.
- **Validation on Confirm only,** never while typing (design §8.2). On failure: message under the row, `aria-invalid`, focus back to the bad field, input preserved.
- **Pager and Next.** Numbered chips show a tick when the Exercise has all its Sets confirmed. The primary button's three labels (Confirm Set, Next: X, Finish) follow the page's state. Skipped and swapped chip marks arrive with B2.
- **Finish in B1 is minimal and temporary:** confirming the last Set of the last Exercise changes the primary to Finish, and Finish sets `EndedAt` to the current time and returns to Home. There is no Partial prompt, Pain check or summary (B2). This exists so a Workout cannot be stranded in progress; B2 replaces it wholesale.
- **Resumed note.** Reopening a Workout that already has a confirmed Set (or whose StartedAt is earlier than this page load) shows "Resumed where you left off" once per page load, and lands on the first Exercise that is not complete.
- **Entry point (owner's decision):** the existing Home placeholder gains a plain Start button (the Session name from the rule above) or Continue (when a Workout is in progress). It is explicitly temporary, with a code note naming B3 as its replacement. It is the only change outside the log itself.

### Saving and failure reporting (decided here, as B0 deferred it)
- B0 left failed-save reporting as a local note in the Theme service because `AppData.Health` has no failed-commit channel. B1 is the first screen that must report a failed save, so it settles this: a small **save-failure notice service in Ui** that any screen reports a failed `CommitResult` to, and that renders one shared, plain-language banner (storage failed, blocked, writer-lock held, rejected by validation), which the Theme service also adopts in place of its local note. Core's `AppData` and the storage layer are unchanged.
- On a failed commit the typed value stays on screen and in the page's state, the Set is shown as not confirmed, and the banner explains; the next Confirm retries. The page never throws into the UI. A rejected commit (format issues) shows the same banner with the first issue's message.
- When the startup state is `CannotRead` or `StorageFailed`, or the writer lock is held, the log opens read-only with the same plain message and Confirm disabled (stories 64 and 65).

### Design assets and mockups
- The log's visual targets are `design/assets/mockups/S-02-log-*.png` for default, 360 px, desktop, PR, format error, Exercise done and each log kind, in both Themes and both Modes. Visual fit is checked by eye against these (manual check), not by tests. The prototype `prototype-log.html` is the reference for behaviour where prose is silent; B1 ports its logic into Core rules or Ui components (it does not ship the prototype).

### Not changed
- Core data model (no new fields), the exchange format and `data-spec.md`, the storage interfaces, the service worker's cache list (no new static file types are expected; if one is added the B0 offline-cache coverage test already catches it), the shell and routing structure, and A2 rules (reused, not edited, apart from the additive new rules above in their own files).

## Testing Decisions

- **What makes a good test here:** it drives the log the way the owner does (type, tap Confirm, leave, come back) and asserts what they can see or what was saved (text on screen, which tab or label is present, `aria-invalid`, the saved Workout's Sets, the in-progress state), not component internals, private state or CSS class names beyond the documented `fa-*` contract.
- **Main seam: bUnit on the log page in `Ui.Tests`,** over a real `AppData` on Core's in-memory store, with the fixed clock and sequential ID generator, seeded with a Plan, Exercises and history through the existing test-data builders. Each log kind gets one scenario (type into the right fields, Confirm, assert the stored Set in canonical units). Cross-cutting scenarios: pre-fill and "same as last", validation message and focus on a bad value, steppers carrying values down, Max set, Add Set, Next and Finish labels, a PR toast and marker (including the baseline not being a PR and the marker moving), RPE set and clear, leaving and reopening restoring the Exercise and showing the Resumed note, read-only states. Use the existing fault-injecting store for a failed save (entry stays, banner shown, retry succeeds) and for writer-lock held.
- **Core seam: unit tests for the new pure rules only** (Start-a-Workout-from-a-Session including Phase override and week logic; per-Set PR feedback for every log kind's ranking, Max set precedence, baseline, strictly-better edge, lower-is-better; the two-field join for the parser; "next Session to start" if added). Follow the style of `ExercisePerformanceTests`, `WorkoutOutcomesTests` and `InputParserTests`, reusing `WorkoutBuilder`, `HistoryBuilder` and `PlanFixture`.
- **Home placeholder:** extend `HomeTests` for Start and Continue (with and without a Plan; with a Workout already in progress).
- **Failure reporting:** a small test for the save-failure notice service and a test that `ThemeService` now reports through it (its existing failure test is adapted, not dropped).
- **Component tests** (bUnit, same project) only for the rules later stages most easily break: the Icon-style accessible-name rule for any icon-only button in the log, the PR input's `aria-label`, `aria-pressed` on RPE and its toggle-off, and 44 px (64 px primary) target classes. Other component behaviour is covered through the page.
- **No new browser test** (owner's decision). The numeric keypad (`inputmode`) and select-all-on-focus are asserted as attributes and handlers in bUnit; the real phone keyboard behaviour, the safe-area bottom bar, 360 px layout, all four looks against the mockups, Back during a Workout and killing the tab mid-Workout are added to `docs/pwa-manual-checks.md` as manual phone checks.
- **Not unit tested:** how the screen looks (checked against the mockups by eye) and contrast (B0's table; B11 re-checks).

## Out of Scope

- The Exercise menu and Workout menu (Swap, Skip with reason, Add Exercise, Mark as benchmark, View Guide, Theme and Mode, Logged afterwards), the swap picker (C-14), the rest timer and the Wake Lock: B2.
- The full Finish flow: Partial prompt, Pain check (C-13), finish summary, the Streak line, the unlock toast: B2. B1's temporary Finish only stamps the end time and returns Home.
- Skipped and swapped chip marks (StateChips C-07) and any Workout with skipped, swapped or added Exercises: B2.
- The "Logged afterwards" Workout (past date, banner, no timers): B2.
- The real Home (TodayCard, Orbs, WeekStrip, Nudges), Do a different Session, Build a Custom workout, Make up a Missed session: B3. B1 starts only the next Session of the active Plan.
- Opening the Guide from the Exercise name: B4 (the name is plain text in B1).
- The Distance + time row with pace (`1 km in 4:35`) in the design's §8.1 table: the data model has no log kind that stores both together (distance and time are separate kinds), so it cannot be saved. Recorded as an open design/data question for the owner; not built.
- Free-text Exercises: a Set cannot store text, so B1 shows the "cannot be logged yet" note rather than losing the value.
- Weight (body) entry, the Log weight sheet and body-measurement history screens: B3 and B6.
- Any change to Core's data model, the exchange format, storage or the service worker.
- Charts, Badge unlocking and the PR list on Home.

## Further Notes

- **Seam check (settled with the owner):** bUnit on the log page over real `AppData` is the main seam; Core unit tests only for the new pure rules; no new Playwright test.
- **Decisions recorded with the owner:** a temporary Start/Continue on the Home placeholder as the entry point; "no `EndedAt` means in progress" with no new field.
- **Decision made in this spec (veto at ticket time if wrong):** B1 includes a *minimal* Finish (stamp end time, back to Home) so a Workout is never stranded in progress before B2. Without it, Home would show Continue forever after the first real Workout.
- **Sizing for `/to-tickets`:** the build plan expects B1 may need splitting by log kind group. Suggested cuts, each within one agent's budget: (1) Core rules: Start-a-Workout-from-a-Session, per-Set PR feedback, the two-field join, and the "next Session" query, with unit tests; (2) the save-failure notice service and the ThemeService adoption; (3) the row components (SetRow with every kind layout, PR marker) with their component tests; (4) ExerciseHeader, RpeSelector and ConfirmBar with steppers and their tests; (5) the log page for weight × reps and bodyweight × reps, end to end: routing, pre-fill, validation, Confirm, save and resume, pager, Next and the temporary Finish, PR feedback, read-only states; (6) the remaining kinds on the same page (carry, seconds and minutes, reps only, time, km and m, tick, Text note); (7) the Home placeholder Start and Continue, the manual phone checks and docs. Tickets 1 and 2 are independent and can run in parallel; 3 and 4 need nothing but B0 and can start in parallel with 1.
- **Known sharp edge to watch:** the stored Workout is replaced as a whole on each change, and the log page has to merge the change into the snapshot's latest copy of the Workout (not a stale local copy), because B2's menus and the write queue can both change it.
- **For the owner when it lands:** B1 on its own is usable but incomplete as a daily tool (no Skip, Swap, rest timer or summary); the sensible time to start logging real Workouts in the app is after B2.
