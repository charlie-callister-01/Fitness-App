# 08: Home view data per Track (PR 1)

**Read first:** `.scratch/a4-5-tracks/HANDOFF.md`, `docs/specs/a4-5-tracks.md` ("Home view data"), `docs/specs/b3-home.md`, `src/Core/Rules/Home/*` (`HomeViewBuilder`, `HomeView`, `HomeOrbs`, `HomeTodayRules`, `HomeWeekRules`, `TodayCard`), `src/Ui/Pages/Home.razor.cs`.

**Parent:** spec `docs/specs/a4-5-tracks.md`, PR 1.

**What to build:** the Core Home seam knows about Tracks; Home's screen does not change yet.
- `HomeViewBuilder`, `HomeOrbs`, `HomeTodayRules`, `HomeWeekRules` take the main Track for the Today card, the week strip, the week line and the Streak and Phase orbs (the race orb is Plan-level). Their output for a one-Track Plan is identical to before.
- The Home view model gains data for PR 2 to render: one **daily Track card** per daily Track (Track ID and name, today's Session, today's state: not started, in progress, done, with the done percentage reached, the daily Streak from ticket 06, the Break state) and one **week line** per extra weekly Track (its Sessions this week with done, Missed and today states, and its progress against its weekly target and Streak). Empty lists for a one-Track Plan.
- The Nudges the view carries come from ticket 07. The "Do a different Session" offer and "Make up a Missed session" stay main Track only in the data; a Session of an extra weekly Track is started from its own week line (data only here).
- Ui: `Home.razor.cs` and any Ui caller are adjusted only to compile and to keep showing the main Track. No new markup.

**Blocked by:** 04, 06, 07.

**Status:** ready-for-agent

- [ ] Tests (`HomeViewBuilderTests` style): a two-Track Plan with a daily Track gives the main Track's Today card and orbs unchanged by the other Track; the daily card is not started, in progress and done at the right states; a Break marks it; an extra weekly Track gives its own week line; a one-Track Plan has empty lists and every existing Home test passes unchanged.
- [ ] Today's card never offers a daily Session, and a Missed daily Session never appears.
- [ ] `dotnet build` 0 warnings; Core and Ui tests green.
