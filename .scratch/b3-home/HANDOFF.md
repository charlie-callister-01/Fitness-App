# Handoff: B3 (Home)

Point a fresh chat at this file. State as of 19 Nov 2026. Read `docs/README.md`, then `GLOSSARY.md`, then this.

## Where things are
- **On main:** A0 to A4 and B0, B1 (PR #20). **B2 is implemented on `b2-implementation`** but its code review, quality review, fix pass and PR body are not done yet (see "Before B3 starts"). Do those first (CLAUDE.md: don't start the next stage until the current stage's PR is written).
- Tests on `b2-implementation`: Core 1020, Ui 335, Web 39 (`dotnet test` from the repo root). Browser tests (`dotnet test src/Web.BrowserTests -p:RunBrowserTests=true`) were not run for B1 or B2.
- Stale local branches and worktrees exist (`b1-*`, `b2-ticket-NN`, `b2-handoff`, older a*/b0 branches). Safe to delete once B2 is merged.

## Workflow (CLAUDE.md, don't skip steps)
`/mattpocock-skills:to-spec` -> `/mattpocock-skills:to-tickets` -> `/mattpocock-skills:implement-spec` (integration branch, one worktree implementer per ticket, merge each, run the suite) -> `/mattpocock-skills:code-review` -> `/thermo-nuclear-code-quality-review` -> one fix pass -> `/mattpocock-skills:pr`. Specs in `docs/specs/`, tickets in `.scratch/<stage>/issues/`, PR body in `.scratch/<stage>/pr-body.md`. Ticket size: one agent, about 150-200k tokens. Use the glossary terms exactly (Session vs Workout, Set capital S in UI prose, Theme vs Mode, Logged afterwards, Partial, Streak, Break).

## Before B3 starts
1. Run `/mattpocock-skills:code-review` on the B2 changes (since the B1 merge, commit `8c8affe`), then `/thermo-nuclear-code-quality-review`, apply the fixes, then write `.scratch/b2-workout-log-menus-and-finishing/pr-body.md` with `/mattpocock-skills:pr`.
2. Points the B2 reviewers should look at: `WorkoutLog.razor.cs` size (it wires `FinishFlow`, `LoggedAfterwardsFlow`, `RestCountdown`, `WorkoutWriter` and must stay thin); the same-shaped `FinishFlow` and `LoggedAfterwardsFlow` (a shared base may or may not pay); `ExerciseMenu` taking many parameters; the added-Exercise position approximation in Core (`WorkoutEdits.AddExercise`); the history rule "on or before the Workout's date" in `LogViewBuilder.FinishedHistory` and `WorkoutSummaryBuilder.Prs`.

## B3 scope (from `docs/build-plan.md`)
S-01 in all states (TodayCard C-09, StatusOrb Streak and race countdown, WeekStrip C-24, in-progress, done, rest day, Break, Plan finished); Nudge C-08 (at most two, Home only) wired to the A2 Nudge rules; Do a different Session (F-03), Build a Custom workout, Make up a Missed session (F-02); the Log weight sheet entry point. Design refs: `docs/design/design-document.md` (C-08, C-09, C-24, S-01, F-02, F-03) and the `S-01-*` mockups in `docs/design/assets/mockups/`.

B2 spec lines that name B3's share: "Home entry points for Logged afterwards, Do a different Session, Build a Custom workout, Make up a Missed session, the real Home", "Nudges that consume the Pain check (A2 rules exist; B3 wires them to Home)", and "the Log weight sheet and body-weight screens: B3 and B6".

## What B2 left for B3 (what exists, what to call)
- **Starting a Workout:** `WorkoutStarter` (Core/Rules/Starting): `StartAsync(plan, session, today)` (creates and saves, or continues the one in progress) and `StartLoggedAfterwardsAsync(plan?, session?, date, today)` (a past-dated Workout with the flag set and no start time; plan or session null for a Custom workout). Both return a `StartOutcome` carrying the `CommitResult`. Home still has the **temporary Start/Continue button** from B1 (`src/Ui/Pages/Home.razor`); B3 replaces the whole page. Report save failures through `SaveFailureNotice`.
- **Custom workout:** `CustomWorkoutOffer` (Core/Rules/CustomWorkout) is the pure offer rule; the Exercise picker for building one is the shared `ExercisePicker` (C-14, `src/Ui/Components`), whose "Add a new Exercise" link is disabled until B4.
- **Logged afterwards entry points on Home** must use `StartLoggedAfterwardsAsync` with a date from a native date input (the log's own sheet is `LoggedAfterwardsSheet`; reuse its pattern, and `LoggedAfterwardsFlow.BannerText` for the banner wording). A future date is refused by Core (`WorkoutFinishing.LoggedAfterwards` / the starter).
- **A past-dated Workout is in progress until it is finished** (it has no end time), so Home's in-progress state and Continue apply to it. State this on Home if it confuses (the owner may want a different rule: open item in `docs/build-plan.md`).
- **In progress** means `Workout.EndedAt == null`; at most one. `WorkoutStart.InProgress(workouts)` finds it.
- **Finishing:** `WorkoutFinishing` (Core/Rules/Finishing), `FinishCheck`, `WorkoutSummaryBuilder` (Core/Rules/Summary) and `FinishFlow` (Ui/Log). A finished Workout's `/log/{id}` shows the summary (`FinishSummary`). A Pain check left unanswered stays "not answered"; Nudge rules that read the Pain check treat unanswered and "none" differently.
- **Streak and Weeks:** `WeekRules` (Core/Rules/Weeks), `WeekScheduleRules`, `WeekRearrangement` (Make up a Missed session), `BadgeHistories.For(snapshot, workout)`, `NudgeRules` (Core/Rules/Nudges). All pure; Home reads them with the clock's today.
- **Shared Ui pieces:** `Sheet` (modal shell with focus handling in `Sheet.razor.js`), `MenuItem`, `MenuChoices`, `StateChip`, `Button`, `Icon`, `StatusOrb`, `EmptyState`, `SaveFailureNotice` (banner in all three layouts), `WorkoutWriter` for every Workout write (merges into the snapshot's latest copy and reports failures).
- **Read-only and failed-save pattern:** disable the action with the plain message (`Data.Health.CanWrite`, `AnotherWindowOpen`, `Startup.State`; see `ReadOnlyMessage` in the log page) and leave the screen unchanged with the banner on a failed save. Tests use `FaultInjectingDataStore` (Core.Tests) and `WriterLockResult.HeldElsewhere`.

## CSS and tokens rules (B0)
Plain CSS, `fa-*` classes, design tokens only. A raw hex or px in component CSS is a defect. New tokens go in `src/Ui/wwwroot/tokens.css` **and** `docs/design/assets/tokens.css` (plus `tokens.json` and the design document tables) in the same change. Tap targets: `--tap-min` (44 px) at least. Reduced motion is one global rule in `src/Ui/wwwroot/ui.css` (it turns off all animation and transition); the app has no keyframes, so new motion must be justified and covered by it.

## Owner to do by hand (phone)
`docs/pwa-manual-checks.md`: "Workout log manual checks (B1)" (25 checks) and "Workout log manual checks (B2)" (31 checks: rest timer, Wake Lock, every sheet, Pain check, summary, Logged afterwards, kill the tab mid-finish, offline). Still open from earlier stages: B0, A0 and A3 manual checks. B3 should add its own section (Home states, Nudges, entry points, Logged afterwards from Home, Continue after a past-dated start).

## Open items carried from B1 and B2
See "Open items for the owner" in `docs/build-plan.md`. Those that touch B3: past-dated Workout is in progress until finished; undoing Logged afterwards cannot restore the start time; added-Exercise position is approximate (needs a model field); "Add a new Exercise" disabled until B4; no Distance + time kind; Text Exercises cannot be logged.

## Gotchas
- Don't pass multi-line commit messages or scripts through bash heredocs with unbalanced quotes; write files with the Write tool. There is no Python on this machine; use sed, PowerShell or the Edit tool. Worktree-isolated agents may refuse long compound shell commands: keep them plain and separate.
- `gh` works from bash. LF->CRLF warnings are harmless.
- Agent worktrees: base them on the integration branch and check `git log` first (they sometimes start from an older commit).
- Implementers sometimes write code before tests; require the red run in the ticket report.
- bUnit: a `Click()` returns before an async handler's save has finished, so a test must wait for a visible result (`WaitForState` / `WaitForAssertion`) before asserting on what the save changes. B2's flaky rest-countdown test was this (fixed in ticket 09).
- Merge conflicts are likely in `WorkoutLog.razor(.cs)`, `ConfirmBar`, `SetRow` and `components.css` when tickets run in parallel; order tickets so shared files are touched by one ticket at a time where possible.
- Commit attribution lines come from the session reminder; follow it.
