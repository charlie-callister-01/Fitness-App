# 14: Ui: Home wiring (sheets, Make up, Nudge actions, Log weight)

Read `AGENTS.md` first.

Stage: b3-home   Wave: 4   Blocked by: 13
Branch: b3-home/14-ui-home-wiring   Project(s) you may edit: Ui, Ui.Tests

## What to build

Fill in the six empty handlers ticket 13 left in `Home.razor.cs`, and add the sheets and their state to `Home.razor`. Everything below is behaviour; follow the existing patterns in `WorkoutLog` for the sheet open/close state, saving flag, read-only message and `SaveFailureNotice`.

1. **Do a different Session** (`OnDifferentSessionAsync`): opens `DifferentSessionSheet` (ticket 06) with `Sessions` = `view.UndoneSessions` as Sessions. Picking a Session calls `WorkoutStarter.StartAsync(plan, session, today)`, reports the commit, closes the sheet and navigates to `log/{id}` on success; ignore a second pick while saving. **Build a Custom workout** from the sheet closes it and navigates to `custom`.
2. **Build a Custom workout** (`OnBuildCustom`): navigates to `custom`.
3. **Log a past Workout** (`OnLogPastWorkout`): opens `LogPastWorkoutSheet` with `Today`, `Sessions` = the active Plan's Sessions. `OnStart(date, session)` calls `WorkoutStarter.StartLoggedAfterwardsAsync(plan, session, date, today)`; on a null Workout and null Commit (a future date) show "That day hasn't happened yet." through the sheet's `Message`; on success close the sheet and navigate to `log/{id}`; report commits through `SaveFailureNotice`.
4. **Make up** (`OnNudgeAction` for `NudgeAction.MakeUp`): navigate to `custom/makeup/{Nudge.SessionId}`. `DifferentSession` opens the sheet from item 1. `BackUp` navigates to `export-import` (use the existing More/Export route; read `src/Ui/MoreDestinations.cs` and the router for its real path). `None` does nothing.
5. **Dismiss** (`OnNudgeDismiss`): `NudgeDismissals.DismissAsync(item.Key)`, then rebuild so the next Nudge, if any, takes its place. It persists across a re-render and is keyed by week.
6. **Log weight** (`OnLogWeight`, from the header button and the Weight panel's empty state): opens `LogWeightSheet` (ticket 11) with `InitialText` = the latest Weigh-in in the display unit (one decimal; empty when none), `LastText` = "Last: {value} {unit} on {ddd d MMM}" (empty when none), `UnitLabel` from `UnitConverter`. `OnSave(text)`: `WeighInRules.Create(text, converter, today, Ids)` (ticket 04); if Invalid show its message through the sheet's `Message` and save nothing; if Valid commit `Change.Put(weighIn)` via `Data.CommitAsync`, report the result through `SaveFailureNotice`, and close the sheet only when the commit succeeded. Always adds a new Weigh-in (never replaces). Log weight stays available in a Break and while a Workout is in progress.
7. **Read-only and saving:** pass `CanWrite`, `ReadOnlyMessage` and `Saving` to every sheet; when read-only the save actions are disabled with the reason and nothing is written. A failed save leaves Home unchanged and shows the shared banner.
8. When data changes (a Weigh-in saved, a Workout started) the page rebuilds without a reload.

## Read first
- `src/Ui/Pages/Home.razor`, `Home.razor.cs` (ticket 13, merged)
- `src/Ui/Components/DifferentSessionSheet.razor`, `LogPastWorkoutSheet.razor`, `LogWeightSheet.razor`, `NudgeCard.razor` (parameters)
- `src/Core/Rules/Home/HomeNudges.cs` (`NudgeAction`), `WeighInRules.cs`, `WeightPanelRules.cs`, `HomeView.cs`
- `src/Core/Rules/Starting/WorkoutStarter.cs`
- `src/Ui/Pages/WorkoutLog.razor.cs` and `src/Ui/Log/` (open/close and saving patterns; search for `LoggedAfterwardsSheet`)
- `src/Ui/MoreDestinations.cs`
- `src/Ui.Tests/Pages/HomeTests.cs`, `LogFixture.cs` (`FakeDevicePreferences`, `Wire`), `src/Core.Tests/Storage/` fault-injecting store (`FaultInjectingDataStore`)
- Glossary terms used: Session, Custom workout, Logged afterwards, Missed session, Nudge, Weigh-in, Backup

## Files you may create or edit
- `src/Ui/Pages/Home.razor`, `Home.razor.cs`, `Home.razor.css` (extend only; keep the page thin)
- `src/Ui.Tests/Pages/HomeActionsTests.cs` (new)
- `src/Ui.Tests/Pages/HomeTests.cs` (only if a ticket 13 test must change)
Anything else is out of scope. If you need it, write a blocked report.

## Acceptance
- [ ] Different Session: lists undone Sessions (incl. Missed), picking starts that Session's Workout for today and navigates; the original Session stays outstanding; empty list offers Build a Custom workout
- [ ] Build a Custom workout navigates to `custom`; Make up navigates to `custom/makeup/{id}`
- [ ] Log a past Workout: starts a Logged afterwards Workout for the chosen date and Session or Custom; a future date is refused with the message and saves nothing
- [ ] Nudge actions: MakeUp, DifferentSession, BackUp navigate or open as above; Dismiss hides the Nudge, persists via the fake preferences, the next Nudge fills in, and a new week brings it back
- [ ] Log weight: prefilled with the last value, saves a new Weigh-in in kg when entered in lb, invalid text shows the message and saves nothing, a failed save keeps the sheet open and shows the banner, and a second Weigh-in on the same day is added not replaced
- [ ] Read-only disables saves with a reason; the sheets report `SaveFailureNotice` on failure (use `FaultInjectingDataStore` and `WriterLockResult.HeldElsewhere`)
- [ ] Tests written first (red run shown in the report), waiting for visible results after async clicks; full `dotnet build` and `dotnet test` green
- [ ] No Core, Web, shared CSS or token changes

## Do not
- Do not change the components from earlier tickets: if one is wrong, write a blocked report naming it.
- Do not store anything outside `AppData` and the device preferences.
