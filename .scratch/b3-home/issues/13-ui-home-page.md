# 13: Ui: Home page (render every state, layout, visibility rules)

Read `AGENTS.md` first.

Stage: b3-home   Wave: 3   Blocked by: 06, 07, 08, 09, 10, 11, 12
Branch: b3-home/13-ui-home-page   Project(s) you may edit: Ui, Ui.Tests

## What to build

Replace the B1 placeholder `Home.razor` with the real Home (S-01) built from `HomeViewBuilder` (ticket 07) and the components from tickets 06 to 12, all merged. This ticket **renders** everything and wires only Start, Continue, the Done summary and the navigation links. Ticket 14 wires the sheets, Nudge actions and Log weight, so leave those handlers as empty, clearly named methods in the code-behind (`OnDifferentSessionAsync`, `OnBuildCustom`, `OnLogPastWorkout`, `OnLogWeight`, `OnNudgeAction`, `OnNudgeDismiss`) that ticket 14 will fill.

**Structure.** `Home.razor` (markup only) plus `Home.razor.cs` (state) plus `Home.razor.css`. Keep the page thin; the builder holds the logic. The page subscribes to `AppData.Changed` and disposes it, as the placeholder does.

**Inputs to `HomeViewBuilder.Build`:** `Data.Snapshot`, `Clock.UtcNow`, today = `DateOnly.FromDateTime(Clock.UtcNow.ToLocalTime().DateTime)` (as the placeholder), `localHour` = `Clock.UtcNow.ToLocalTime().Hour`, the dismissed-key set from `new NudgeDismissals(preferences).LoadAsync(view.NudgeKeys)` (load it after the first build; rebuild when it arrives), and `Data.LastBackupAt`. Inject `IDevicePreferences`.

**Layout, top to bottom on a phone** (design S-01; the `S-01-home-*.png` mockups are the reference):
1. Header: the date ("Wed 18 Nov") with the Phase label, the greeting, and a secondary **Log weight** button (always shown).
2. `TodayCard` (ticket 08). Start and Continue work as follows: Start calls `WorkoutStarter.StartAsync(plan, session, today)`, reports the commit through `SaveFailureNotice`, navigates to `log/{id}` on success, ignores a second tap while saving; Continue navigates to `log/{id}`; View summary navigates to `log/{id}` (it shows the summary).
3. When `ShowAlternatives`: the buttons **Do a different Session** and **Build a Custom workout** directly under the card (secondary), and when `ShowLogPastWorkout` a small text-style link **Log a past Workout** under them. All three call the empty handlers for now.
4. `WeekStrip` (tap navigates to `week`), `WeekLine`, `OrbRow` (tap navigates to `progress`). Hidden when there is no Plan.
5. When `ShowNudges`: up to two `NudgeCard`s (handlers empty for now).
6. `RecentPrsPanel` and `WeightPanel` (converted with `new UnitConverter(Data.Snapshot.Settings)`); the panel's Log weight button calls the empty handler.

**From 900 px wide** use two columns as in `S-01-home-desktop.png`: left column TodayCard, alternatives, WeekStrip and line; right column orbs, Nudges, PRs, Weight. Use CSS grid in `Home.razor.css` with the existing breakpoint token/convention in the shell layouts (read `src/Ui/Layouts`). Tokens only; no raw hex or px.

**Read-only.** `CanWrite` = `Data.Health.CanWrite`; pass it and a plain `ReadOnlyMessage` to the TodayCard (copy the wording the old placeholder or `WorkoutLog` uses for read-only). The page must not break when `Data.Snapshot` is empty (NoPlan).

**Update `HomeTests.cs`** to match the new page: keep the tests that still apply (no build id or temporary links; no active Plan shows the NoPlan state with no Start; Start creates a Workout and navigates; Continue navigates; a Workout in progress shows Continue) and change what must change. Add tests for: each TodayCard state appears for its data (Start, rest day, done today, Break, Plan finished); the alternatives and Log a past Workout are hidden in Break and when in progress and shown otherwise; Nudges hidden in Break; the strip, line and three orbs render; Log weight is always present; two-column layout is CSS only (no test).

## Read first
- `src/Ui/Pages/Home.razor` (current placeholder; you are replacing it)
- `src/Core/Rules/Home/HomeViewBuilder.cs`, `HomeView.cs` (ticket 07, merged)
- `src/Ui/Components/TodayCard.razor`, `WeekStrip.razor`, `WeekLine.razor`, `OrbRow.razor`, `NudgeCard.razor`, `RecentPrsPanel.razor`, `WeightPanel.razor` (parameters only)
- `src/Ui/Preferences/NudgeDismissals.cs`
- `src/Ui/Pages/WorkoutLog.razor.cs` (code-behind, `ReadOnlyMessage`, `SaveFailureNotice` usage)
- `src/Ui.Tests/Pages/HomeTests.cs` and `LogFixture.cs`
- Glossary terms used: Plan, Session, Workout, Custom workout, Break, Nudge

## Files you may create or edit
- `src/Ui/Pages/Home.razor` (replace), `src/Ui/Pages/Home.razor.cs` (new), `src/Ui/Pages/Home.razor.css` (new)
- `src/Ui.Tests/Pages/HomeTests.cs`
Anything else is out of scope. If you need it, write a blocked report.

## Acceptance
- [ ] Every TodayCard state renders on the page from real data; Start and Continue behave as above; a second Start tap creates one Workout
- [ ] Visibility rules: alternatives and Log a past Workout hidden in Break and in progress; Nudges hidden in Break and with no Plan; Log weight always present
- [ ] Strip, line, orbs, panels render; strip and orb taps navigate (`week`, `progress`)
- [ ] The page updates when data changes; no exception with an empty snapshot
- [ ] Read-only disables Start with a reason; a failed save reports through `SaveFailureNotice` and the page stays unchanged
- [ ] The six empty handler methods exist and are the only unwired actions
- [ ] No raw hex or px in `Home.razor.css`
- [ ] Tests written first (red run shown in the report), waiting for visible results after async clicks; full `dotnet build` and `dotnet test` green
- [ ] No Core, Web, shared CSS or token changes

## Do not
- Do not wire the sheets, Nudge actions or Log weight (ticket 14 does). Do not edit the components from earlier tickets: if one is wrong, write a blocked report naming it.
- Do not reuse `WorkoutStart.NextToStart` in the page.
