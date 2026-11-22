# 10: PR 1 whole-loop test, regression sweep and docs

**Read first:** `.scratch/a4-5-tracks/HANDOFF.md`, `docs/specs/a4-5-tracks.md`, the whole-loop test in `src/Core.Tests` (search for the A4 loop that exports then restores a dataset), `docs/build-plan.md`.

**Parent:** spec `docs/specs/a4-5-tracks.md`, PR 1, "Further Notes".

**What to build:** prove PR 1 as a whole, then close it out.
- Extend the whole-loop test: a two-Track dataset (a weekly main Track with Phases, a daily mobility Track with its own Phases, Workouts on both, a Break, a Badge `streak:<id>`, a Week schedule) is Exported, Restored into a fresh `AppData`, and every rule result (Streaks, daily run, week counts, Phases, Home view data, Badge progress, Nudges) is identical before and after. Add the same loop starting from a v1 Backup and from a stored v1 dataset (ticket 03).
- Regression sweep: run the whole suite and write any test that needed more than a mechanical change under Notes; confirm the data-spec JSON blocks are all parsed by the conformance test.
- Docs: `docs/data-spec.md` matches the code; `docs/build-plan.md` status for A4.5 (PR 1 delivered, tests counts, the open items below) and `docs/README.md`; the `.scratch/a4-5-tracks/HANDOFF.md` state section; `docs/specs/editors-b4-b5.md` gets a one-line pointer that the Plan, Week and Home behaviour is Track-aware from A4.5.
- Then the stage steps from `CLAUDE.md` for PR 1: `/mattpocock-skills:code-review` against the commit before ticket 01, the thermo-nuclear review (applied as C#), the fix pass, and `/mattpocock-skills:pr` (commit the body as `.scratch/a4-5-tracks/pr-body-1.md`).

**Blocked by:** 03, 04, 05, 06, 07, 08, 09.

**Status:** resolved
Notes: Extended `ApplyTests` with three whole-loop tests (`A_v2_backup_with_two_tracks_round_trips_through_export_and_restore`, `A_v1_backup_restores_exports_and_restores_with_identical_rule_results`, `A_stored_v1_dataset_migrates_exports_and_restores_with_identical_rule_results`) verifying identical Streaks, daily runs, week counts, Phases, Home view data, Badge progress and Nudges before and after Export/Restore. Updated `docs/build-plan.md`, `docs/README.md`, `docs/specs/editors-b4-b5.md`, `.scratch/a4-5-tracks/HANDOFF.md`, `docs/REFACTOR.md`, and `.scratch/a4-5-tracks/pr-body-1.md`. Completed two-axis `/code-review` and `/thermo-nuclear-code-quality-review` and applied all PR 1 fixes (`TrackRules.PhasesOf`/`IsDaily`, `PlanPhaseRules`/`TrackEditRules`benchmark guard on reorder, `GuideHistoryView` per-Track Phase resolution, single-owner empty-`tracks` validation in `DocumentValidator`, `PlanSaveRules.TracksDiffer` reuse in `PlanEditState`, `PlanPhasesTab.razor.cs` extraction, and open-vs-finished alignment between `DailyTrackRules` and `HomeTrackRules`).

- [x] The extended whole-loop test passes for the v2, v1 Backup and stored v1 starts.
- [x] `dotnet build` 0 warnings; `dotnet test` all green (state the new Core, Ui and Web counts in the PR body).
- [x] No test was loosened; every changed expectation is listed with the reason.
- [x] Docs, handoff and PR body written; both reviews and their fixes done.

## Comments

### Changed expectations across existing tests (no test loosened)
1. **Format version 1 -> 2 (`ExchangeFormatErrorTests`, `ParseThenValidateTests`, `WholeDatasetTests`, `ReadMeAndCultureTests`, `DataSpecConformanceTests`):** Format 2 is now the current version (`ExchangeFormat.CurrentVersion == 2`), `formatVersion: 3` is the unsupported-future version test (was `2`), `readMe` includes `tracks`, and `docs/data-spec.md` examples use the two-Track format 2 Plan shape.
2. **Stored-data migration (`AppDataMigrationTests`):** Replaced the pre-A4.5 synthetic `CurrentVersion + 1` placeholder tests with 12 real format 1 -> format 2 stored-record migration tests.
3. **Markdown Export (`ExportMarkdownTests.The_Plan_lists_its_Phases_and_Sessions_with_their_rows`):** Session headings under a Plan moved from `####` to `#####` because `#### <Track Name> ([main, ]<cadence>)` now renders each Track heading under `### <Plan Name>`.
4. **Prompt pack (`PromptPackTests`):** Updated assertions to match the two-Track `docs/data-spec.md` Plan example (`HYROX London` with `Main` weekly Track and `Morning mobility` daily Track).
5. **Document-wide Track ID uniqueness (`Ui.Tests/Pages/PlansTests.cs`):** `Other()` helper in `PlansTests` previously cloned `PageFixture.Plan()` without changing its Track ID (`plan-main`), which collided with the active Plan's Track ID once format 2 persisted Track IDs; updated `Other()` fixture to give the second Plan its own Track ID (`$"{id}-main"`).
