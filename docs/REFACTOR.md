# Refactor backlog

Findings from `/thermo-nuclear-code-quality-review` and architecture passes. Open items only; strike through or note when resolved.

## From A4.5 PR 1 thermo-nuclear review (22 Nov 2026)

1. **Unify `ParseFindings` (`src/Core/Serialisation/ReadContext.cs`):** `ParseFindings` holds 4 separate reference-identity `HashSet<T>` sets (`_phasesWithUnreadableWeeks`, `_plansWithMissingTracks`, `_tracksWithUnreadableCadence`, `_sessionsWithUnreadableDefaultDay`) and 8 mark/check methods across four disjoint record types (`Phase`, `Plan`, `Track`, `Session`). Collapse into a single `HashSet<object> _unreadable = new(ReferenceEqualityComparer.Instance)` with `MarkUnreadable` / `IsUnreadable`.
2. **Unify Plan ID-graph remapping (`src/Core/Rules/PlanEditing/PlanLifecycleRules.cs` and `src/Core/Exchange/PlanImport.cs`):** `PlanLifecycleRules.Duplicate` and `PlanImport.Rebuild` both walk a `Plan` to mint new Track, Session and Phase IDs while rewiring `Session.TrackId` and `Phase.SessionOverrides[*].SessionId`. Extract a shared graph-remapping helper parameterized by the ID-allocation function.
3. **Consolidate Session `TileState` and `doneThisWeek` queries (`src/Core/Rules/Home/HomeTrackRules.cs`, `HomeWeekRules.cs`, `HomeTodayRules.cs`, `WorkoutStart.cs`, `WeekScheduleRules.cs`, `WeekRearrangement.cs`):** `HomeTrackRules.StateOf` duplicates the finished-outcome / break / missed / upcoming tile-state decision tree in `HomeWeekRules.Strip`, and `doneThisWeek` (`workouts.Where(w => WeekCalendar.InWeek(monday, w.Date)).Select(w => w.SessionId).OfType<string>().ToHashSet()`) is rebuilt in five places.
4. **Extract shared Streak run counter (`src/Core/Rules/Weeks/WeekRules.cs` and `src/Core/Rules/Tracks/DailyTrackRules.cs`):** `WeekRules.Runs` and `DailyTrackRules.Status` run the same current/best streak state machine over break-filtered periods where the open current period (`today` or `thisMonday`) can extend a run without breaking it.
5. **Split `DocumentValidator` before it crosses 500 lines (`src/Core/Serialisation/DocumentValidator.cs`, currently 455 lines):** Extract Plan/Track/Phase/Session validation into `DocumentValidator.Plans.cs` (partial class) before the next schema addition pushes the file over 500 lines.

## From A4.5 PR 2 thermo-nuclear review (22 Nov 2026)

6. **Extract shared `GuardedWorkoutStart` helper in `Ui` (`src/Ui/Pages/Home.razor.cs` and `src/Ui/Pages/Week.razor.cs`):** Both `Home.StartAndOpenAsync` and `Week.OnStartDailyAsync` duplicate the `GuardedSave.RunAsync` + `WorkoutStarter.StartAsync` + `Navigation.NavigateTo($"log/{workout.Id}")` ceremony. Move onto `PageContext` or a small `Ui.Saving` helper.
7. ~~**Consolidate `PlanEditState.Read` cross-Track problem discovery (`src/Ui/Pages/PlanEditState.cs`)**~~ *(resolved in A4.5 PR 2 review fix pass)*
8. **Split `PlanEditorTests.cs` (`src/Ui.Tests/Pages/PlanEditorTests.cs`):** `PlanEditorTests.cs` has grown past 500 lines with the Track selector, split Settings, and per-Track Phases/Sessions suites; split into `PlanEditorSettingsAndTracksTests.cs`, `PlanEditorPhasesTests.cs`, and `PlanEditorSessionsTests.cs`.

## From Data and settings (B9 + B10) thermo-nuclear review (23 Nov 2026)

9. ~~**Extract shared `BackupFlow` helper (`src/Ui/Pages/SettingsPage.razor.cs` and `src/Ui/Components/ExportTab.razor.cs`)**~~ *(resolved on `feature/data-and-settings`: `src/Ui/Saving/BackupFlow.cs`)*
10. ~~**Parallelize `Home.RefreshAsync` / `SettingsPage.RefreshDataStatusAsync` and drop redundant `Rebuild()` (`src/Ui/Pages/Home.razor.cs` and `src/Ui/Pages/SettingsPage.razor.cs`)**~~ *(resolved on `feature/data-and-settings`)*
11. ~~**Collapse `SettingsPage` field-change boilerplate (`src/Ui/Pages/SettingsPage.razor.cs`)**~~ *(resolved on `feature/data-and-settings`)*
12. ~~**Unify Keep/Replace toggle branch in `ImportPreview.razor` (`src/Ui/Components/ImportPreview.razor`)**~~ *(resolved on `feature/data-and-settings`)*
13. ~~**Split `ExportImportTests.cs` (`src/Ui.Tests/Pages/ExportImportTests.cs`, currently 551 lines)**~~ *(resolved on `feature/data-and-settings`: split into `ExportTabTests.cs`, `ImportTabTests.cs`, `PromptsTabTests.cs`)*

## From Hardening (B11) ticket 03 axe scans (23 Nov 2026)

14. **axe moderate and minor findings:** none in the first full scan (every screen and tab x Theme x Mode x empty/fixture, plus the data-only sheets). Re-check the `AXE-BACKLOG` lines in the browser-test output when screens change.
15. ~~**State colours used as chip text (`src/Ui/wwwroot/components.css`):** `.fa-chip--*` colours the word with its 3:1 state token. Skipped now uses `text-muted` for the word; Swapped in Keep light (`state-swapped` on `surface` 4.0) would also fail text contrast once a Swapped chip is on a scanned screen. Give every chip word a text token (or raise the state tokens to 4.5:1) and add a Swapped Workout to the fixture.~~ *(resolved on `fix/hardening-review`: the Swapped word takes `text-muted` like Skipped, glyph and outline keep the state colour; the fixture has a Swapped Workout, scanned by axe)*
