# B1 review findings (code-review Standards + Spec, thermo-nuclear)

Branch under review: `b1-implementation`, since `c5bb699`. One fix pass applies all of this. Behaviour must not change except where a finding says it is wrong. Every existing test must still pass (rewrite tests only where they pin internals that move).

## Must fix: correctness (Spec axis)
1. **Abandoned reopen leaves a stale draft on a confirmed row.** `Reopen` writes a draft for the Done row; `JumpTo` / reopening another row / tapping the tick again clears `_editingRow` but not the draft; the row then shows Done with the abandoned text instead of the stored Set. Stopping an edit must drop that row's draft. Add tests (reopen, jump away and back; reopen then tap the tick again).
2. **Steppers overwrite later rows the owner typed.** Spec story 47: carry down to later *unconfirmed* Sets only. A later row the owner typed in themselves must keep its typed value; only untouched (pending/prefilled or already-carried) rows take the stepped value. Add a test.
3. **Finish after jumping to the last chip with earlier Exercises unlogged.** Finish label should depend on the shown Exercise being the last AND all Exercises complete; otherwise show Next-style navigation or disable until complete. Keep the temporary Finish otherwise (B2 replaces it). Decide the least surprising behaviour and test it.
4. **A commit against a missing Workout is silent** (`CommitAsync` returns null without `Notice.Report`). Report a plain message through the notice service (e.g. the Workout is no longer open).
5. Bodyweight rows: story 35 asks for "a way to add weight". Currently the owner overtypes "bw" in the first field. Keep it, but make sure the field's label, placeholder and a test make this discoverable; do not add a new control.

## Must fix: structure (thermo-nuclear)
A. **Decompose `WorkoutLog.razor.cs` (640 lines).** Move pure Workout mutation (`WithSet`, `WithMax`, `WithRpe`, any entry lookup) into one Core type (e.g. `WorkoutEdits`) with Core unit tests; one `FindDoneEntry` helper replaces the repeated `FindIndex`/`FirstOrDefault` lookups. Extract view-model building (the `ExerciseViewOf` / `ViewOf` family, header text, PR derivation) out of the page class into a plain Ui class (e.g. `LogViewBuilder`) that takes the snapshot, state and units and returns views. The page keeps only event handlers and rendering. Goal: page code-behind well under 300 lines, no behaviour change.
B. **Replace the parallel mutable fields with one explicit state model.** `_drafts`, `_extraRows`, `_editingRow`, `_errorRow`, `_errorMessage`, `_invalidField`, `_focusRow`, `_focusField`, `_focusSequence` become a small `LogEditState` / per-row state (text, carried vs typed, max flag, error) so that findings 1 and 2 fall out of the design. Use an enum for the focused field instead of 0/1/2 magic numbers.
C. **Parse once.** `ConfirmAsync` parses three times then casts `(ParseResult.Success)`. Pattern-match one parse result. Which field is invalid should come from `RowInput` (Core, tested) rather than re-parsing with a stub `"1"`.
D. **One per-kind table, not seven switches.** Put what is per-kind (which Set fields it fills, its unit, text format, input mode, step, hint) in one place beside `LogKindRules` or a Ui `RowLayout` that the others read: `RowLayout`, `StepRules`, `SetText`, `RowPrefill.Fields`/`Hint`, `RowInput.Join`, `FirstHintOf`. Adding a log kind should touch one file. Delete the duplicated `Number` / `WeightText` helpers and the repeated `"0.##"` formatting and `unit == Kg ? "kg" : "lb"` logic. Only do this as far as behaviour and tests stay identical.
E. **Data clump:** `(Units, weightUnit, distanceUnit)` is passed into every `SetText` call; bundle it (a `DisplayUnits` value or give the converter a formatter).
F. **`RowView` (11 params) and `ExerciseView` (13 params):** split into focused records (the stepper / Max / Remove tools are one record).
G. **Nullable abuse:** remove `RowLayout.For(LogKind.Load)!` in `SetRow` and `.Key(set)!` in `SetPrFeedback` by making the contract explicit.
H. **Duplication:** merge `EditFirst`/`EditSecond`; one `RunExclusive` helper for the `_busy` try/finally (4 places); one `FinishedHistory` for the "other finished Workouts" filter (`SnapshotOf`, `PrOf`).
I. **Wrong layer:** move `Home.StartAsync` orchestration (create, commit, navigate) into a small service next to `WorkoutStart`; Home just calls it.
J. **Middle men:** collapse `GoNext` / `JumpTo` / `PrimaryAsync` thin chain; `JumpTo` must not return `Task.CompletedTask` without a reason.

## Must fix: standards
K. Raw px in new CSS instead of tokens: `ExercisePager.razor.css` (44px, 2px outline), `components.css` (10/12/18/60/32/36px; the 36px control must be at least `--tap-min`), `ConfirmBar.razor.css` (10px gap), `WorkoutLog.razor.css` (430px; outline offsets). Use existing tokens (`--tap-min`, `--border-thick`, space tokens); where no token exists add one to `tokens.css` AND `docs/design/assets/tokens.css` together (B0 rule: copies change in the same PR) and say so in the commit. Add an outline-offset token for the repeated 2px.
L. User-facing text: glossary term is "Set" (capital S) in prose: "Edit set N", ", set N", "Mark set N done". Fix wording and the tests that assert it. Keep accessible-name tests consistent.

## Known and accepted (do not change)
Distance + time kind absent; Text kind not loggable; temporary Finish until B2; kg in the Suggested target reason for lb users (Core change); Minutes does not accept 12:30; unconfirmed extra rows not persisted; RPE before any Set creates an empty Done entry (consider only noting it for B2); no steppers for Reps/Minutes/body measurements. Listed in docs/build-plan.md "Open items for the owner (from B1)".

## Not part of this pass
No new features. No change to the exchange format, storage or A2 rules beyond the small Core additions above.
