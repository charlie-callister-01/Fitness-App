# F1: Core: one "done" rule, one row matcher, Core homes for pain note and skip reasons

Source: code review findings of stage B2 (Spec findings 1 and 2; thermo review items 1, 2 and 6). Spec: `docs/specs/b2-workout-log-menus-and-finishing.md` (story 27: an added Exercise never logged is dropped on Finish, not recorded as skipped; story 53; decisions 1 to 3).

**What to build:** remove the duplicated notions of "done" and "which entry belongs to which planned row", in Core, keeping behaviour except where noted.

1. **One "has Sets / is logged" rule** for an entry, in Core, used by every site that currently decides this separately: `WorkoutEdits.IsLogged`, `WorkoutFinishing.IsEmptyDone`, `FinishCheck`, and the Ui's `LogViewBuilder.IsHandled` and `ProgressOf` (the Ui sites are switched in F2/F3; here expose the rule and switch the Core sites). A Swapped entry with no Sets is NOT done (settled here): it is neither "handled" for Finish gating nor counted done; on a Partial finish an empty swapped row becomes a skipped entry for the planned original (no reason); a full finish cannot happen while one exists (it prompts Partial). Keep the pager/primary-button behaviour in the Ui as is until F2/F3 (document the one remaining discrepancy in the report if the Ui still treats an unlogged swap as handled).
2. **`Finish` always drops added Exercises that have no Sets** (and an RPE-only added entry), whether or not the finish is Partial. `partial` then controls only whether unrecorded planned rows become skipped entries.
3. **`RowOutcome` carries the entry it matched** (null for Unrecorded). `WorkoutFinishing`, `WorkoutSummaryBuilder` (RPE, skip reason, swapped-from) and `WorkoutEdits` (`StandsFor`) use `WorkoutOutcomes` instead of their own matchers; delete the private copies. `WorkoutOutcomes` behaviour is unchanged.
4. **Pain note Compose/Parse in Core** next to `WorkoutFinishing`: one place that writes the optional side ("Left side", "Right side", "Both sides") into the note ("Left side: note") and one that reads it back; the side list lives there once. Switch `WorkoutSummaryBuilder` to Parse and `FinishFlow.NoteOf` and `PainCheckSheet`'s side list to the Core pieces (minimal Ui edits only; F2/F3 restructure the Ui).
5. **Skip reasons list in Core** (Short on time, Pain, No equipment, Other); `SkipSheet` reads it from Core (minimal Ui edit).

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Finish with every planned row done and an empty added Exercise saves no empty added entry (test; this was a bug)
- [ ] Unlogged swap: not counted done by `FinishCheck`, prompts Partial, becomes a skipped entry for the original on Partial finish (tests)
- [ ] `RowOutcome` exposes its entry; Finishing, Summary and Edits have no private matcher; `WorkoutOutcomes` tests unchanged and green
- [ ] Pain note Compose/Parse round-trips side and note, with and without either; summary text unchanged for existing tests
- [ ] Skip reasons come from Core
- [ ] Core tests written first (show the red run in the report); the full suite stays green (adapt a Ui test only where it asserted the old unlogged-swap behaviour, and say so)
- [ ] No data model, exchange format or storage change
