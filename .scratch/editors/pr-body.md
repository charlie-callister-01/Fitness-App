## Summary

Editors (B4 + B5): the Guide, the Exercise library, Plans and the Plan editor, and the Week view, on top of three shared Ui modules that Home, the Custom workout draft and the log now use too. Spec: `docs/specs/editors-b4-b5.md` (issue #24). Ten tickets in `.scratch/editors/issues/`, then a review fix pass.

```text
Ui/Saving/GuardedSave            one save: busy guard, read-only refusal, "no longer there", failure notice
Ui/Pages/PageContext             CanWrite, ReadOnlyMessage, Today, ActivePlan (one place)
Ui/Components/FormSheet          read-only well, error, Cancel, WriteDisabled (every editor sheet)

callers:   Home, Custom workout draft, log (actions, finish, Logged afterwards), Theme saving,
           Guide page, New Exercise, Plans, Plan editor, Week view
```

```text
src/
├── Core/Rules/
│   ├── Library/       NewExerciseRules, ExercisePickerRules (PickerRequest), LibraryViewBuilder
│   ├── Guides/        GuideEdits, GuideHistoryView
│   ├── PlanEditing/   PlanSettingsRules, SessionRowRules, PlanLifecycleRules, PlanSaveRules,
│   │                  WeekEditRules (+ WeekDraft), PlanProgress, ActivePlan
│   ├── Phases/        PhaseRanges   (one Phase-range rule, shared with the storage validator)
│   └── Schedule/      WeekRows
└── Ui/
    ├── Pages/         Exercises, GuidePage, Plans, PlanEditor (+ tabs, PlanEditState), Week
    └── Components/    NewExerciseSheet, FormSheet, ReadOnlyWell; ExercisePicker now takes a PickerRequest
```

```diff
 Home / draft / log save
-  own busy flag + CanWrite check + try/finally + notice report   (three copies)
+  GuardedSave.CommitAsync(...) -> SaveResult (Saved | Failed | Busy | ReadOnly | Gone | NothingToSave)

 Plan editor Save
+  PlanSaveRules.Changes(plan, saved, library, benchmarkOverrides)
+  one GuardedSave.CommitAsync(changes)     // Plan and Benchmark flags together; Discard undoes both
```

No change to the data model or the exchange format.

### Decisions that are not obvious

- **New Exercise offers 11 log kinds** (all but the two body measurements). The design says nine without naming them.
- **A New Exercise cue is stored as a Guide with that one Cue**, because `Exercise` has no cue field. An Exercise with a cue therefore already has a Guide.
- **No per-row Step increment.** The model only has the Plan-wide weight increment (Settings tab). A per-row one is a data-model change.
- **Phases must start at week 1 with no gap or overlap.** The storage validator already requires it, so the editor enforces the same rule from one shared `PhaseRanges` (the validator's messages are unchanged). Phases are edited by week number with the covered dates shown.
- **Rules beyond the spec, kept on purpose:** a weight increment must be more than 0; `PlanProgress` has "starts d MMM" and "finished after week n" labels.
- **Benchmark toggle in a Plan row** is held in the editor draft and committed with the Plan in one save. The log's Exercise menu still toggles it instantly.
- **Typed numbers in a Plan row:** text that is not a whole number stays as typed, shows "Enter a whole number." and disables Save (it used to become 0).
- **The log's state survives only the round trip to the full Guide.** Any other exit clears it, so a Workout reopened later starts on its first incomplete Exercise.
- **Week view:** the suggested week shows when a Break touches the current week and the rearrangement rule has something to move. A failed Apply keeps the draft and its Undos. Cancel is remembered for the session in memory only (not stored) until a Break or the Week schedule changes. "Make this the default day" also clears that Session's move for the week, in the same save.
- **Read-only wording** lost its `place` argument, so the text says "this page" everywhere. The read-only well is `role="status"` everywhere (two sheets used `alert`). The Partial prompt, Pain check and Logged afterwards now show the well too.
- **`FinishFlowTests`:** the `canWrite` argument is gone from the flow calls, so the old "Skip while read-only" test became "Skip writes nothing".
- **Deferred to Data and settings:** a `DataPage` base component and `PageContext` in DI (the next pages will show if the shape is right). Open items for the owner are in `.scratch/data-and-settings/HANDOFF.md`.

## Evidence

- **Before** (start of the stage, `e7ae75f`): Core 1168, Ui 459, Web 39 tests; Exercises, Plans and Week were placeholders; "Add a new Exercise" was a disabled button.
  **After** (`dotnet build` 0 warnings; `dotnet test`): Core 1336, Ui 611, Web 39, all green. Browser tests were not run.

```text
GuardedSaveTests:  second request while saving      -> SaveStatus.Busy, one record stored
                   read-only window                 -> SaveStatus.ReadOnly, nothing attempted
                   storage full                     -> SaveStatus.Failed, notice says so
PlanEditorTests:   tick Benchmark, then Discard     -> library unchanged
                   tick Benchmark, then Save        -> Plan and Exercise committed once
                   Phase 1-4 and 4-8                -> "Build (weeks 4 to 8) overlaps Foundation (weeks 1 to 4)"
WeekTests:         Apply fails, earlier Undo set    -> draft still on screen, Undo still applied
ExerciseLibraryTests: add Exercise from a failed search -> stored with a permanent id; duplicate name refused
```

Both reviews ran: `/mattpocock-skills:code-review` (Standards and Spec) and `/thermo-nuclear-code-quality-review`; their findings are applied in the "Editors fixes" commits. Manual phone checks are owed: "Editors manual checks (B4 + B5)" in `docs/pwa-manual-checks.md`. The implementers did not check 360 px or both Themes and Modes visually.

## Merge Danger

**Door:** two-way

Nothing is stored in a new shape, so a revert leaves existing data readable. The one thing to know: the Plan editor and Week view now write Plans, Week schedules, Breaks, Exercises and Guides, so data created with them stays after a revert (it is valid under the unchanged format).

**Blast Radius:** Home, the Custom workout draft and the log (all moved onto the shared saves), plus every screen that shows a read-only window

Touching the shared save path means a mistake would show up on screens this PR does not otherwise change (Start, Log weight, confirming a Set, finishing a Workout, Theme). The existing tests for those pass; check Start, a Set, Finish and the Theme toggle on the phone, in a read-only second window too. Phase rules now come from one shared rule that the storage validator also uses, so a mistake there would also affect Import.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
