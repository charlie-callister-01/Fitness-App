# 11: Plan editor Track selector and split Settings (PR 2)

**Read first:** `.scratch/a4-5-tracks/HANDOFF.md`, `docs/specs/a4-5-tracks.md` ("Screens (PR 2)"), `.scratch/editors/HANDOFF.md` (conventions and bUnit gotchas), `.scratch/editors/issues/07-plans-list-and-plan-editor-settings-phases.md` (its implementation notes), `src/Ui/Pages/PlanEditor.razor(.cs)`, `PlanEditState.cs`, `PlanSettingsTab.razor`, `PlanSettingsForm.cs`, design S-05, C-20.

**Parent:** spec `docs/specs/a4-5-tracks.md`, PR 2, user stories 15 to 17.

**What to build:** the Plan editor selects a Track, and Settings splits.
- A **Track selector** row above the tabs: chips for each Track in list order (the first labelled "Main" in a small hint, the name otherwise), and + Add Track. The selected Track drives the Settings (Track part), Phases and Sessions tabs (those two are ticket 12; here they keep showing the main Track).
- **Move up** and **Move down** for the selected Track (rules from ticket 05; a refusal such as a daily Track first shows its plain message). The selector order is the Track order; there is no Remove and no cadence control after creation.
- **Add Track** is a `FormSheet`: name and cadence (weekly or daily), with the rule's defaults; refused names show the rule's message. New Track becomes selected.
- **Settings tab** splits into two groups: **Plan settings** (start date, goal, goal date, weight increment) and **Track settings** for the selected Track: weekly shows weekly target, Streak minimum, Custom workouts counting (only offered on the main Track, with a hint that Custom workouts belong to the main Track); daily shows day-done percentage and a note "A day is done when this share of the routine is ticked". Track name is editable here.
- `PlanEditState` holds the draft Plan with its Tracks and the selected Track ID; one Save, one dirty check (`PlanSaveRules.IsDifferent`), one "Discard changes?", guarded save as before. Home and the Week view update after Save.
- Both Themes and Modes, 360 px, tokens only, tap targets `--tap-min`, read-only well with reason.

**Blocked by:** 10 (PR 1 written). Do not start before PR 1 is merged or its branch is the base.

**Status:** resolved

- [x] bUnit tests: selector lists Tracks in order; selecting a Track shows its settings by cadence; Add Track (weekly and daily) appears, selected, dirty; Move up/down reorders and enables Save; a daily Track cannot be moved first (message shown); no Remove and no cadence control; Plan settings and Track settings edit and persist after Save; invalid values disable Save with the rule's message; discard prompt; read-only disables everything with the reason; failure keeps edits.
- [x] After Save, the Plans list and Home reflect an edited weekly target of the main Track.
- [x] `dotnet build` 0 warnings; Ui tests green; no new browser tests.

## Comments
- Added the Track selector row (`role="group" aria-label="Tracks"`) above the Plan editor tabs in `PlanEditor.razor(.cs,.css)`: renders a chip button per Track in `Draft.Tracks` order with a `"Main"` hint alongside the first Track (`index == 0`), `+ Add Track` opening an `"Add Track"` `FormSheet` (Name and Cadence `Weekly` / `Daily` using `TrackEditRules.AddTrack`), and `Move up` / `Move down` buttons when the Plan has more than one Track (using `TrackEditRules.MoveUp` / `MoveDown` and showing any refusal message in a `role="alert"` well).
- Extended `PlanEditState` and `PlanSettingsForm` (`TrackSettingsForm` per Track ID) so `SelectedTrackId`, `SelectedTrack`, `SelectedTrackIndex`, `IsMainTrackSelected` and `SelectedTrackForm` are exposed for the tabs (ready for ticket 12), per-Track typed settings survive tab and Track switches, valid Track renames sync to `Draft.Tracks`, and `PlanSettingsForm.ApplyTo` validates the selected Track first and then all Tracks in the Plan.
- Split `PlanSettingsTab.razor` into `Plan settings` and `Track settings` sections (`@key="State.SelectedTrackId"`): Track name, weekly fields (weekly target, Streak minimum, Custom workouts switch on the main Track only + hint `"Custom workouts belong to the main Track."`), and daily fields (day-done percentage + note `"A day is done when this share of the routine is ticked."`).
- Added 15 new bUnit test cases in `PlanEditorTests.cs` covering all ticket 11 scenarios.
