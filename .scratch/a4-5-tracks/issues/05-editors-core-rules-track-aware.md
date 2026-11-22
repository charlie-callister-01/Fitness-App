# 05: Editors Core rules made Track-aware, and Track edit rules (PR 1)

**Read first:** `.scratch/a4-5-tracks/HANDOFF.md`, `docs/specs/a4-5-tracks.md` ("Edit rules"), `docs/specs/editors-b4-b5.md` ("Plan rules", "Week rules"), `.scratch/editors/issues/04-core-plan-and-week-rules.md`, `src/Core/Rules/PlanEditing/*` (`PlanSettingsRules`, `PlanPhaseRules`, `SessionRowRules`, `PlanLifecycleRules`, `PlanSaveRules`, `PlanProgress`, `WeekEditRules`, `PlanEditResult`).

**Parent:** spec `docs/specs/a4-5-tracks.md`, PR 1.

**What to build:** Core and Core.Tests only; no new screens (the Plan editor keeps editing the main Track through the new rules, minimal Ui changes to compile).
- `PlanSettings` and `PlanSettingsRules` split: **Plan settings** (start date, goal, goal date, weight increment; the existing validation) and **Track settings** (weekly Track: weekly target, Streak minimum, Custom workouts; daily Track: day-done percentage 1 to 100; refuse values out of range with a plain message). `Plan` and `Track` are returned changed; Workouts are never touched.
- `PlanPhaseRules` takes a Track: validate and replace that Track's Phases with the shared `PhaseRanges.Check`; Phase ID uniqueness is checked document-wide across Tracks.
- `SessionRowRules` takes a Track: add Session on a daily Track gives `defaultDay: daily`; `daily` on a weekly Track and a day change on a daily Session are refused; the benchmark interval only on the main Track; Phase overrides may name only that Track's Sessions.
- `PlanLifecycleRules`: Copy remaps every Track ID (plus Phase and Session IDs) and rewrites `trackId` and any Phase override that follows; From scratch creates one weekly Track named "Main" (weekly target 3, Streak minimum 2, Custom workouts off, as today's defaults).
- New **Track rules**, all returning `PlanEditResult`: add a Track (name, cadence, fresh ID; defaults weekly target 3, Streak minimum 2, Custom workouts off, day-done 75; blank or duplicate name refused with a plain message), rename and edit a Track, move a Track up or down (refused when it would put a daily Track first or move past an end). No remove and no cadence change exist.
- `PlanSaveRules.IsDifferent` compares Tracks (and each Track's Phases) by sequence so reordering enables Save.
- `WeekEditRules` refuses moving a daily Session this week and refuses "Make this the default day" for it, with a plain message.
- `PlanProgress` ("Active · week n of m") reads the main Track's Phases.
- Every new or changed edit rule returns `PlanEditResult` (a reason on refusal, nothing changes).

**Blocked by:** 01.

**Status:** ready-for-agent

- [ ] Each rule has Core tests, including: Track settings range refusals, Phase overlap naming both Phases inside one Track, duplicate Phase ID across Tracks, daily Session day change refused, `daily` on a weekly Track refused, benchmark off the main Track refused, Copy gives new Track, Phase and Session IDs with `trackId`s following, add Track defaults, reorder refused when a daily Track would be first, reordering makes `IsDifferent` true, moving a daily Session refused, `PlanProgress` from the main Track.
- [ ] Editing never touches past Workouts (tested).
- [ ] Existing Editors tests pass with only mechanical changes; the Plan editor still works on the main Track.
- [ ] `dotnet build` 0 warnings; all tests pass.
