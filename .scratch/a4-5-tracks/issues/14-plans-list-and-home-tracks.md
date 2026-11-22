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

**Status:** ready-for-agent

- [ ] bUnit tests: Plans headline with Phases, without Phases and with two Tracks; Home shows a daily Track card in each state with its Streak; Start on the card creates today's Workout of the daily Session; an extra weekly Track week line renders and starts a Session; a one-Track Plan's Home is unchanged (existing `HomeTests` pass); read-only and failure cases as in B3.
- [ ] Browser tests are not added; the manual checks go in ticket 15.
- [ ] `dotnet build` 0 warnings; Ui tests green.
