# 13: Week view daily row (PR 2)

**Read first:** `.scratch/a4-5-tracks/HANDOFF.md`, `docs/specs/a4-5-tracks.md` ("Screens (PR 2)"), `.scratch/editors/issues/09-week-view.md`, `src/Ui/Pages/Week.razor(.cs)`, `src/Core/Rules/Schedule/WeekRows.cs`, `Rules/PlanEditing/WeekEditRules.cs`, design S-06, F-08.

**Parent:** spec `docs/specs/a4-5-tracks.md`, PR 2, user story 18.

**What to build:**
- Core (small, with tests): `WeekRows` returns the daily Sessions separately from the day rows: one entry per daily Session with today's done state (done, in progress, not started) and no Missed chip ever.
- Week view: above the week's days, each daily Session is one row "Every day" with its Track name and today's state. No Move button, and no "Make this the default day". It opens or starts today's Workout of it as a weekly row does (Start or Continue; a done one opens the summary).
- Extra weekly Tracks' Sessions appear on their days like the main Track's, with the Track name shown when the Plan has more than one weekly Track. Move, Missed and the Break draft work per weekly Track (rules from PR 1).
- The suggested week draft never lists a daily Session.
- Both Themes and Modes, 360 px, tokens only.

**Blocked by:** 12 (sequential order; it needs only PR 1 and ticket 11's shared pieces, but run one ticket at a time).

**Status:** resolved

- [x] Tests: a daily Session shows once as "Every day" above the week with today's state; never a Missed chip even after days pass without a Workout; no Move control; the Move sheet's Session list and the draft exclude it; extra weekly Track Sessions placed on their days with the Track name; a one-Track Plan's Week view is unchanged (existing tests pass).
- [x] Core test for the new `WeekRows` data.
- [x] `dotnet build` 0 warnings; Core and Ui tests green.

## Comments
- **Core (`WeekRows` & `WeekEditRules`):**
  - Added `DailyRowState` (`NotStarted`, `InProgress`, `Done`) and `DailyWeekRow` (`Session`, `TrackId`, `TrackName`, `State`, `InProgressWorkout`, `DoneWorkout`), built by `WeekRows.BuildDaily(plan, workouts, today)` over the Plan's daily Tracks using `DailyTrackRules.IsOpen` and `DailyTrackRules.IsWorkoutDone`. Daily rows never produce a Missed state even after days pass without a Workout.
  - Updated `WeekRows.Build` to lay out all weekly Tracks in date, Track, and Session order with per-Track `MissedInWeek` and `Done` chips, attaching `TrackName` and `ShowTrackName` (true when the Plan has more than one weekly Track; false for a one-weekly-Track Plan so its rows are unchanged).
  - Updated `WeekEditRules.SuggestDraft` to iterate across all weekly Tracks when proposing a re-arranged week after a Break, never including daily Sessions.
- **Ui (`Week.razor` & `Week.razor.cs`):**
  - Rendered `<ul class="fa-week__rows" aria-label="Every day">` above the weekly days list when `_dailyRows` has entries. Each row shows `"Every day · {TrackName}"`, the Session name, today's state (`"Not started"`, `"In progress"`, or `"Done"` with a `Done` `StateChip`), and its action button (`Start`, `Continue`, or `View summary`), wired through `GuardedSave` and `WorkoutStarter` / `NavigationManager` with no Move control and no Missed chip.
  - Day rows show `"{DayText} · {TrackName}"` when the Plan has more than one weekly Track, and plain `{DayText}` when there is only one weekly Track.
- **Tests:** Added unit tests in `WeekRowsTests` and `WeekEditRulesTests`, and bUnit tests in `WeekTests`.
