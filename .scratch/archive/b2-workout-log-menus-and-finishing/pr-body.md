## Summary

B2 finishes the Workout log: Exercise and Workout menus, Skip, Swap and Add (shared ExercisePicker), Guide sheet, rest timer and Wake Lock, the full Finish flow (Partial prompt, Pain check, summary), and Logged afterwards. It replaces B1's temporary Finish. No data model, exchange format or storage change: every new state maps onto fields A1 already had.

Spec: `docs/specs/b2-workout-log-menus-and-finishing.md`. Tickets and review-fix tickets: `.scratch/b2-workout-log-menus-and-finishing/issues/` and `fixes/`.

**What the owner gets**

```text
Log screen
  Exercise menu ........ Swap · Skip (reason chips) · Add an Exercise · Mark/Unmark benchmark · View Guide
  Workout menu ......... Rest timer Off/60/90/120 · Theme · Mode · Logged afterwards · Finish Workout
  Pager ................ ✓ done · — skipped · ⇄ swapped · + added
  ConfirmBar ........... "Rest 1:12" (clock-based; hidden when logged afterwards)

Finish
  planned Exercise left?  -> "Finish as Partial?"  Keep going / Finish
  commit end time                                   (never leaves an open Workout)
  Pain check: No / Yes / Skip                       (Skip = not answered, not "none")
  Summary: duration, Sets, PRs + previous best, outcomes + RPE, pain, Streak line, Badge unlock
```

**How it is built** (pure Core, thin Ui, as in B1)

```diff
 Core/Rules
   Editing/WorkoutEdits ........ pure edits
+    Skip, Unskip, Swap, UndoSwap, AddExercise, RemoveAdded
+  Editing/ExerciseEdits, SimilarExercises, SkipReasons, ExerciseMenuAvailability
+  Finishing/WorkoutFinishing .. Finish(partial), Pain check, LoggedAfterwards (re-date), EditResult
+  Finishing/FinishCheck, PainNote (side lives in the note text)
+  Summary/WorkoutSummaryBuilder
   Outcomes/WorkoutOutcomes ... the one "has Sets" rule and the one row matcher
+    RowOutcome now carries its entry
   Starting/WorkoutStart ...... + past-dated creation (B3 adds Home entry points)
   Performance ................ Suggested target reason now reads lb for lb users (closes a B1 open item)

 Ui
   Pages/WorkoutLog.razor.cs .. 371 -> 189 lines, wiring only
+  Log/LogActions ............. busy guard + every write; one ApplyAsync(ExerciseAction)
+  Log/LogSession ............. rest countdown, Wake Lock rules, event subscriptions
+  Log/LogStanding ............ skip/swap/add standing, split out of LogViewBuilder
+  Log/WorkoutSavingFlow ...... base for FinishFlow and LoggedAfterwardsFlow
   Saving/WorkoutWriter ....... + EditAsync(edit -> EditResult): a refusal commits nothing
+  Components: Sheet, StateChip, MenuItem/MenuChoices, ExerciseMenu, WorkoutMenu, SkipSheet,
+    ExercisePicker, GuideSheet, PartialPrompt, PainCheckSheet, FinishSummary, LoggedAfterwardsSheet
 Web
+  WakeLock (interface, browser implementation, wake-lock.js), per-device preferences (rest timer)
```

## Evidence

- **Before:** B1 on main: Finish stamped `EndedAt` and went Home; no Skip, Swap, rest timer, Pain check or summary. Tests: Core 939, Ui 210, Web 37.
  **After:** Core 1048, Ui 358, Web 39, all green, Ui suite repeated 3 times in a row. Browser tests not run.

Behaviour that changed after review, with the test that was red first:

```text
Full finish, every planned row done, plus an added Exercise with no Sets
  before: empty added entry saved in the Workout          (spec story 27 broken)
  after:  Finish drops it                                  Full_finish_..._drops_an_empty_added_Exercise

Swap an Exercise, log no Set, finish
  before: counted done, no Partial prompt, "Swapped" row with no Sets
  after:  not done; Finish prompts Partial; original saved as skipped
          An_unlogged_swap_is_not_done_and_prompts_Partial
          An_unlogged_swap_is_not_handled_so_the_last_Exercise_offers_Next_back_to_it_not_Finish

Flaky: Confirming_another_Set_restarts_the_countdown failed 2 of 6 full runs
  cause: the test read the render before the async Confirm had saved
  fix:   wait for the done row (no sleeps or retries); Ui suite 16 runs green
```

Both review axes (Standards and Spec) and the strict quality review were applied in one fix pass (F1 to F4). The old unlogged-swap test was changed to match the new rule (`Next: Romanian deadlift`, Finish only after a Set).

## Merge Danger

**Door:** two-way.

No data model, exchange format, storage or ADR change. Workouts saved with B2 use existing fields (skipped and swapped entries, Pain check, `loggedAfterwards`), so older builds still read them. Reverting the PR leaves valid data behind.

**Blast Radius:** the Workout log.

- A Partial finish now writes explicit skipped entries (no reason) where B1 left planned rows unrecorded. A2's outcomes rule reads both.
- Not verified on a phone: sheet focus and Escape, Wake Lock reacquire on returning to the tab (`wake-lock.js` was reviewed by reading), the rest timer after the phone sleeps, safe areas, 360 px, all four looks, offline. 31 checks are in `docs/pwa-manual-checks.md` ("Workout log manual checks (B2)"), on top of B1's 25 that are still open.
- Suggested target reason text now follows the display unit for every lb user, not just in the log.
- New static files (`Sheet.razor.js`, `wake-lock.js`) are in the service worker's published asset list (checked with a publish and asserted by a Web test). The rest timer choice is stored in `localStorage` on the device.
- Two tokens added in both `tokens.css` copies, `tokens.json` and the design tables: `--sheet-max-height`, `--underline-offset`.

**Decisions to know about**

- **Added Exercise position is approximate.** The data model does not record which row an added Exercise follows, so it is placed after the row owning the nearest earlier entry. A proper fix needs a position field (A-stage change). Open item in `docs/build-plan.md`.
- **Past-dated Workouts have no end time**, so they count as in progress until finished. Undoing Logged afterwards restores today's date but cannot restore the start time.
- **The Pain check's side** (Left, Right, Both) has no field, so it is written as a leading phrase in the note ("Left side: ...") through one Core Compose/Parse pair.
- **The end time is committed before the Pain check opens**, so a killed tab leaves a finished Workout with an unanswered Pain check, never an open one. Reopening the URL shows the summary.
- **Design deviations:** body-area chips show all 11 areas (design lists 5); Mode has three choices (Match phone, Dark, Light); menus are bottom sheets; a backdrop tap on the Pain check counts as Skip and on the Partial prompt as Keep going; the Badge unlock toast is static, with no auto-hide; "Add a new Exercise" in the picker is disabled until B4.
- **`WorkoutLog.razor.cs` is 189 lines**, not the 150 aimed for. About 40 are injected services and comments; it holds no write logic.
- **Still open from B1:** the Distance + time kind, Text Exercises, and `12:30` for Minutes (see `docs/build-plan.md`).

🤖 Generated with [Claude Code](https://claude.com/claude-code)
