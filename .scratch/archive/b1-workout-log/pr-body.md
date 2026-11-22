## Summary

B1: the Workout log, core input. The owner taps Start on Home, logs Sets for every log kind the data model can store, sees PRs as they happen, and can leave and resume at any point. Menus, Skip, Swap, rest timer, Pain check and the finish summary are B2.

Spec: `docs/specs/b1-workout-log-core-input.md`. Tickets: `.scratch/b1-workout-log/issues/` (01 to 11). Review findings and what was done about them: `.scratch/b1-workout-log/review-findings.md`.

```text
Home (placeholder, temporary Start / Continue)
  WorkoutStarter                      Core: create + save an in-progress Workout (EndedAt null)
  navigate /log/{workoutId}

WorkoutLog page (full-screen layout)        236 lines: events and rendering only
  LogEditState                        typed text, reopened row, extra rows, error and focus
  LogViewBuilder                      snapshot + edit state -> views (Last, Best, target, PR)
    ExercisePerformance.Snapshot      Core, reused
    SetPrFeedbackRule                 Core, new: per-Set PR while the Workout is open
    RowSpec                           Core, new: the one per-kind table
  <ExercisePager> <ExerciseHeader> <SetRow>* <RpeSelector> <ConfirmBar>
  WorkoutWriter                       merge change into latest Workout -> AppData.CommitAsync
    SaveFailureNotice                 shared plain-language banner (all layouts)

Confirm
  RowInput.Check                      join fields -> InputParser, once; says which field is bad
  WorkoutEdits.WithSet                Core, pure
  commit, then PR toast if SetPrFeedbackRule says so
```

```diff
 src/
 ├── Core/Rules/
+│   ├── Starting/         WorkoutStart, WorkoutStarter
+│   ├── Editing/          WorkoutEdits (pure Workout changes)
+│   ├── LogKinds/         RowSpec (per-kind table), StepRules folded in
 │   ├── Parsing/
+│   │   ├── RowInput      two-field join and validation
+│   │   └── RowPrefill    pre-fill order
 │   └── Performance/
+│       └── SetPrFeedback
 └── Ui/
+    ├── Log/              LogViewBuilder, LogEditState, LogViews
+    ├── Saving/           SaveFailureNotice, banner, WorkoutWriter
     ├── Components/       + SetRow, ExerciseHeader, ExercisePager, RpeSelector, ConfirmBar, PrMarker
     └── Pages/            + WorkoutLog, Home gets Start / Continue
```

### Non-obvious decisions

- **In progress means `EndedAt` is null.** No new field and no format change. At most one Workout is in progress; Start refuses a second and offers Continue.
- **Failed saves** now report through one shared `SaveFailureNotice` (B0 had a local note in the Theme service). Core and storage are unchanged.
- **PR feedback is derived, never stored.** History passed to the rule is every other finished Workout. Reopening a Set removes it from the PR calculation until it is confirmed again.
- **The review pass was structural.** The first version put 640 lines in the page code-behind and repeated a per-kind switch in seven files. The fix pass moved Workout mutation into Core, view building into `LogViewBuilder`, and every per-kind fact into one `RowSpec`, so adding a log kind is one entry.
- **Temporary Finish.** Finish stamps `EndedAt` and returns Home so no Workout is stranded. B2 replaces it with Partial prompt, Pain check and summary. Finish only appears when every loggable Exercise is done; before that the button on the last Exercise reads "Next: <first unlogged Exercise>".
- **Tokens added** (`--column-log`, `--outline-offset`) in both `src/Ui/wwwroot/tokens.css` and `docs/design/assets/tokens.css`, per the B0 rule that the copies change together. Some padding and the stepper width moved to tokens: the steppers are now 64 px wide (were 60), and the Nudge button is 44 px tall (was 36). Worth a look on the phone.
- **Known and accepted gaps**, all listed in `docs/build-plan.md` ("Open items for the owner (from B1)"):
  - no Distance + time kind in the data model
  - Text Exercises cannot be logged (a Set holds no text)
  - the Suggested target reason always says kg, even for lb users (a Core change)
  - Minutes does not accept `12:30`
  - rows added with Add Set are not saved until confirmed
  - tapping RPE before any Set creates a Done entry with no Sets
  - no steppers for Reps, Minutes or body measurements
  - the Time (lower is better) PR path has no page-level test

## Evidence

- **Before:** no way to start or log a Workout. Core 814 / Ui 103 / Web 37 tests on `main` before this stage.
  **After:** `dotnet test` from the repo root: Core 939, Ui 210, Web 37, 0 failures. Browser tests not run for this stage.

```text
test: reopen a Set, jump to another Exercise and back
  before review fix: row shows Done with the abandoned typed text
  after:             row shows the stored Set

test: stepper with a later row the owner typed in
  before review fix: later row overwritten with the stepped value
  after:             typed row keeps its value; untouched rows take the step

test: jump to the last chip with earlier Exercises unlogged
  before review fix: button reads Finish
  after:             button reads "Next: <first unlogged Exercise>"
```

Not checked in a browser: visible focus, the 44 / 64 px targets, the safe-area bottom bar and the 360 px layout. They are in `docs/pwa-manual-checks.md` ("Workout log manual checks (B1)", 25 checks) for the owner to run on a phone.

## Merge Danger

**Door:** two-way. No stored data shape, exchange format or storage code changed.

A Workout started in this stage is an ordinary Workout record, so rolling back leaves it in the owner's data. A Workout left in progress (no `EndedAt`) would sit there until B2 or until finished with the temporary Finish.

**Blast Radius:** Home and Log screens, plus shared CSS tokens.

- Home placeholder changed (Start / Continue). B3 replaces it.
- `ThemeService` lost its local failure note and now reports through the shared banner. A failed Theme save on More shows at the top of the page.
- `tokens.css` and `components.css` changed (the Nudge button and the Chip sizes), so every later screen inherits them.
- `docs/design/assets/components.css` was already out of sync with the app's copy before this stage and is untouched.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_016NV3mrbPnEsidtPTUeRyTp
