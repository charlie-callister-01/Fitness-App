# 14: Plans list headline and Home Tracks (PR 2)

**Read first:** `.scratch/a4-5-tracks/HANDOFF.md`, `docs/specs/a4-5-tracks.md` ("Screens (PR 2)"), `docs/specs/b3-home.md`, ticket 08's Home view data, `src/Ui/Pages/Home.razor(.cs,.css)`, `Plans.razor(.cs)`, `src/Ui/Components/TodayCard*`, design S-01, C-08, C-09, C-24.

**Parent:** spec `docs/specs/a4-5-tracks.md`, PR 2, user stories 19 and 20.

**What to build:**
- **Plans list:** "Active · week n of m" already reads the main Track's Phases (ticket 05); check the list and the Plan editor header show it, and that a Plan whose main Track has no Phases shows "Active · week n". A Plan row shows its Track count when more than one ("2 Tracks").
- **Home:** the Today card, week strip, week line and orbs stay the main Track's. Render the Core data from ticket 08:
  - one **card per daily Track**: Track name, today's Session, state (not started, in progress, done), the daily Streak ("12 days"), Start or Continue, and a Break state. A done day shows the done state, not a button. Uses existing components and tokens (a compact variant of the Today card; no new visual language).
  - one **week line per extra weekly Track**: its Sessions this week with their states and progress against its target; tapping a Session starts it with the same guards as the main week line.
  - Nudges from ticket 07 name the Track when needed; per-week dismissal still works.
- Order on Home: Today card, daily Track cards, week line(s), orbs, Nudges, as the design places them; adjust only if the design says otherwise.
- Both Themes and Modes, 360 px and desktop, tokens only.

**Blocked by:** 13.

**Status:** resolved

- [x] bUnit tests: Plans headline with Phases, without Phases and with two Tracks; Home shows a daily Track card in each state with its Streak; Start on the card creates today's Workout of the daily Session; an extra weekly Track week line renders and starts a Session; a one-Track Plan's Home is unchanged (existing `HomeTests` pass); read-only and failure cases as in B3.
- [x] Browser tests are not added; the manual checks go in ticket 15.
- [x] `dotnet build` 0 warnings; Ui tests green.

## Comments
- **Plans list & Plan editor header (`Plans.razor.cs`, `PlanEditor.razor`, `PlanEditor.razor.css`):**
  - Updated `Plans.Summary(Plan plan)` to prefix `"{plan.Tracks.Count} Tracks"` when `plan.Tracks.Count > 1`, keeping a 1-Track Plan's summary unchanged.
  - Added `<p class="fa-label fa-planeditor__active">@PlanProgress.ActiveLabel(_state.Saved, Page.Today)</p>` in `PlanEditor.razor` when `_state.Saved.IsActive` is true, reading the main Track's Phases via `PlanProgress.ActiveLabel`.
- **Home (`Home.razor`, `Home.razor.cs`, `Home.razor.css`):**
  - Kept the Today card, alternatives, week strip, main week line and orbs on the main Track.
  - Rendered one compact card per daily Track (`_view.DailyTracks`) under the Today card/alternatives showing the Track name, daily Streak (`"n day"` / `"n days"`), today's Session name, Break state (`"On a Break"`), or state (`Not started` with `Start` button, `In progress · p% done` with `Continue` button, or `Done` `StateChip` with `p% done` and no button), wired to `StartAsync` / `OnContinue` with `GuardedSave` and read-only guards.
  - Rendered one week line per extra weekly Track (`_view.ExtraWeekLines`) below the main week line showing its summary (`"{TrackName}: {Done} of {Scheduled} · target {Target} · Streak {Streak} week(s)"` plus `Missed` codes when present) and tappable Session buttons (`ExtraWeekSession`) with today outline and state words, starting the Session via `StartAsync` with the same read-only and busy guards.
- **Tests:** Added bUnit tests in `PlansTests`, `PlanEditorTests`, and `HomeTests` covering active headlines with/without main Track Phases, multi-Track summary counts, daily Track cards in `NotStarted`, `InProgress`, `Done`, and `OnBreak` states, extra weekly Track week lines and starting Sessions, Track-qualified Nudges and dismissal, single-Track invariance, read-only mode, and failed saves.
