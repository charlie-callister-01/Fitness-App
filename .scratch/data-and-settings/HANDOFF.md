# Data and settings (B9 + B10): handoff

Point a fresh chat at this file. State as of 22 Nov 2026. Read `CLAUDE.md`, `docs/README.md`, `GLOSSARY.md`, then this, then `docs/build-plan.md` (the Data and settings row).

## Where things are
- The Editors group (B4 + B5) is implemented on `b4-implementation` (spec `docs/specs/editors-b4-b5.md`, issue #24; ten tickets in `.scratch/editors/issues/`, all done). Its reviews (`/mattpocock-skills:code-review`, `/thermo-nuclear-code-quality-review`), fix pass and PR body (`.scratch/editors/pr-body.md`) are owed; see `.scratch/editors/HANDOFF.md`. After that PR, run `/mattpocock-skills:improve-codebase-architecture` again.
- Final counts at the end of the Editors tickets: `dotnet build` 0 warnings; `dotnet test` Core 1309, Ui 597, Web 39. Browser tests were not run. Manual phone checks owed: "Editors manual checks (B4 + B5)" in `docs/pwa-manual-checks.md`.
- Next group: Data and settings: S-10 export and import screens on A4's logic, S-11 Settings, S-12 First run (see the build plan).

## What the Editors group landed (use these, do not copy them)
- **`GuardedSave`** (`Ui/Saving`): the one way a screen saves. `CommitAsync(change or changes)` and `RunAsync(Func<Task<SaveWork>>)` return a `SaveResult` (`Saved`, `Failed`, `Busy`, `ReadOnly`, `Gone`, `NothingToSave`). It owns the busy guard, the read-only refusal, "no longer there" and the failure notice. Make one per page or flow (`new GuardedSave(data, notice)`); `requireWrite: false` is only for live controls with no disabled state (Theme). `WorkoutWriter` and `ExerciseWriter` sit on it.
- **`PageContext`** (`Ui/Pages`): `new PageContext(Data, Clock)` gives `CanWrite`, `ReadOnlyMessage`, `Today`, `LocalNow`, `ActivePlan`. `ReadOnlyWording.For(data)` has no `place` argument. The active Plan is `Core.Rules.PlanEditing.ActivePlan.Of(plans)`.
- **`FormSheet`, `FormSheetState`, `ReadOnlyWell`** (`Ui/Components`): the frame for every editor sheet. Content and actions are `RenderFragment<FormSheetState>`; use `context.WriteDisabled` on write buttons. `ReadOnlyWell` is the one read-only well (`role="status"`).
- **Test fixture:** `Ui.Tests/Pages/PageFixture.cs` (was `LogFixture`).
- **Core rules added:** `Rules/Library` (`NewExerciseRules`, `ExercisePickerRules` with `PickerRequest`, `LibraryViewBuilder`), `Rules/Guides` (`GuideEdits`, `GuideHistoryView`), `Rules/PlanEditing` (`PlanEditRules`, `WeekEditRules`, `WeekDraft`, `PlanProgress`, `ActivePlan`), `Rules/Schedule/WeekRows`, `ExercisePerformance.Recent`.
- **Pages:** `Exercises` (library) with `NewExerciseSheet`; `GuidePage` at `/exercises/{id}/guide`; `Plans`; `PlanEditor` at `/plans/{id}` with `PlanEditState` and the Settings, Phases and Sessions tab components; `Week`. The log keeps its `LogEditState` across a visit to the full Guide (`Ui/Log/LogEditStates`, registered in `Web/Program.cs` and `PageFixture.Wire`).
- **Pattern for a new form page:** a Core rule returns a changed record or a reason; the page holds the draft, builds a `PageContext` and a `GuardedSave`, shows `ReadOnlyWell`, and saves with one `CommitAsync`. Leaving with unsaved changes uses `RegisterLocationChangingHandler` and a "Discard changes?" form sheet (see `GuidePage` and `PlanEditor`).

## Open items for the owner (from Editors)
- **Log kinds offered in New Exercise are 11** (all but the two body measurements). The design says nine without naming them.
- **A New Exercise cue is stored as a Guide with that one Cue** (the Exercise has no cue field), so an Exercise with a cue already has a Guide.
- **No per-row Step increment in the Plan editor.** The model has only a Plan-wide weight increment (Settings tab); adding a per-row one is a data-model change.
- **Phases are edited by week number** with the covered dates shown beside them (storage is in weeks).
- **Phases must start at week 1 with no gap or overlap** (the storage validator requires it, and the editor enforces it).
- **The Benchmark toggle in a Plan row saves at once** to the library Exercise, not with the Plan Save.
- **Discard prompts do not cover refresh or closing the tab.** Blocked pop-ups for Watch a demo are not detected.
- **Import Guides and Import a Plan are not offered** on the no-Guide state and the Plans list; they belong to this group (B9).
- **No deleting Plans, Sessions or Exercises, and no editing a Break after creating it** (out of scope for Editors).
- **The Week draft appears only when a Break touches the current week** and the rearrangement rule has something to move.
- **Make this the default day** also clears that Session's move for this week, in the same save.
- Visual checks (360 px, both Themes and Modes) were not run by the implementers; they are in the manual checks.
