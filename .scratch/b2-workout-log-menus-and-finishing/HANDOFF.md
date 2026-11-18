# Handoff: B2 (Workout log, menus and finishing)

Point a fresh chat at this file. State as of 18 Nov 2026. Read `docs/README.md`, then `GLOSSARY.md`, then this.

## Where things are
- **On main:** A0 to A4 and B0, B1 (PR #20, merged). B1 is not yet deployed-verified on a phone (see "Owner to do by hand").
- Tests on main: Core 939, Ui 210, Web 37 (`dotnet test` from the repo root). Browser tests (`dotnet test src/Web.BrowserTests -p:RunBrowserTests=true`, needs Playwright chromium) were not run for B1.
- Stale local branches and worktrees exist (`b1-ticket-NN`, `b1-review-fixes`, `a1-spec`, `a2-spec` worktrees, older a*/b0 branches). Safe to delete.

## Workflow (CLAUDE.md, don't skip steps)
`/mattpocock-skills:to-spec` -> `/mattpocock-skills:to-tickets` -> `/mattpocock-skills:implement-spec` (integration branch, one worktree implementer per ticket, merge each, run the suite) -> `/mattpocock-skills:code-review` -> `/thermo-nuclear-code-quality-review` -> one fix pass -> `/mattpocock-skills:pr`. Specs go in `docs/specs/`, tickets in `.scratch/<stage>/issues/`, PR body in `.scratch/<stage>/pr-body.md`. Ticket size: one agent, about 150-200k tokens. The owner never answered the to-tickets granularity quiz in B1, so ask or state your assumption. Use the glossary terms exactly (Session vs Workout, Set capital S in UI prose, Theme vs Mode).

## B2 scope (from `docs/build-plan.md`)
Workout and Exercise menus (design §8.4): skip sheet with reason chips, swap picker (C-14 ExercisePicker, shared with B4/B5), add Exercise, mark/unmark benchmark, View Guide as a sheet, rest timer and Wake Lock. PainCheckSheet C-13. Finish, Partial and summary (§8.5). Logged afterwards (§8.6). StateChips C-07. Error states. Design refs: `docs/design/design-document.md` (C-07 line 354, C-13 436, C-14 443, S-02 527, §8.4-8.6 771-780), mockups in `docs/design/assets/mockups/`, prototype `docs/design/assets/prototype-log.html`.

B2 **replaces the temporary Finish** from B1 (see below). If it exceeds one agent's budget at ticket time, split: menus + skip/swap/add, rest timer + Wake Lock, Pain check + Finish/Partial/summary, Logged afterwards.

## How B1 is built (what B2 extends)
Pure Core, thin Ui. Call tree is in `.scratch/b1-workout-log/pr-body.md`.
- **In progress** means `Workout.EndedAt == null`; at most one. `WorkoutStarter` (Core/Rules/Starting) creates and saves it; Home has a temporary Start/Continue (B3 replaces it).
- **`WorkoutWriter`** (Ui/Saving) merges a change into the snapshot's latest copy of the Workout and commits through `AppData.CommitAsync`, reporting failures to the shared **`SaveFailureNotice`** banner (in all three layouts). Use it for every B2 write.
- **`WorkoutEdits`** (Core/Rules/Editing) holds pure Workout mutation (`WithSet`, `WithMax`, `WithRpe`, `FindDoneEntry`). Add skip, swap, add Exercise, Pain check and finish as pure functions here with Core tests, not in the page.
- **`LogEditState`** (Ui/Log) holds unsaved UI state (typed text, reopened row, extra rows, error, focus). **`LogViewBuilder`** turns snapshot + state into views (`LogViews`). **`RowSpec`** (Core/Rules/LogKinds) is the one per-kind table; a new kind is one entry.
- **`WorkoutLog.razor.cs`** is 236 lines and only wires events. Keep it that way: put new menu/finish logic in new classes or components, not the page. `RunExclusive` guards against double saves.
- Components in `src/Ui/Components`: SetRow, ExerciseHeader, ExercisePager, RpeSelector, ConfirmBar, PrMarker.
- PR feedback is derived on every render, never stored. Validation runs only on Confirm.

## Things B1 left for B2 to replace or decide
- **Temporary Finish** (`FinishAsync` in `WorkoutLog.razor.cs`): stamps `EndedAt` and goes Home. Replace it with the Partial prompt, Pain check and summary. Finish currently appears only when every loggable Exercise is done; otherwise the button reads "Next: <first unlogged Exercise>". Partial finish must record the rest as skipped.
- **Skipped exercise** and **Swapped** have no UI yet. Check the data model/Core for how a Skip with reason and a Swap are stored (A1/A2) before specifying; the pager already needs the done/skipped/swapped glyphs (✓ — ⇄).
- **Tapping RPE before any Set** creates a Done entry with no Sets. Decide whether Skip/Done semantics should change this.
- **Unconfirmed rows** from Add Set are not persisted (B1 accepted); revisit only if Skip/Swap make it matter.
- **Rest timer** starts on Confirm and shows "Rest 1:12" in the ConfirmBar (off by default; 60/90/120 s). Wake Lock needs a JS interop; B0/A3 interop pattern is in `src/Web`. Not browser-testable by bUnit, so add manual checks.
- **Logged afterwards**: no timers, no duration recorded, banner "Logged afterwards · Sun 15 Nov. No timers." Check how a past-dated Workout is created (B3 owns the entry points, B2 owns the log behaviour).
- **Shared pieces land where first needed:** ExercisePicker C-14 is built in B2 (reused by B4/B5). The Log weight sheet is B3.
- Known accepted gaps from B1 (full list in `docs/build-plan.md`, "Open items for the owner (from B1)"): no Distance + time kind; Text Exercises cannot be logged; Suggested target reason always says kg for lb users (Core change, could be folded into B2 if touching Core anyway); Minutes does not accept `12:30`; no steppers for Reps, Minutes or body measurements; the Time (lower is better) PR path has no page-level test (cheap to add).

## CSS and tokens rules (B0)
Plain CSS, `fa-*` classes, design tokens only. A raw hex or px in component CSS is a defect. New tokens go in `src/Ui/wwwroot/tokens.css` **and** `docs/design/assets/tokens.css` (plus `tokens.json` and the design document tables) in the same change. B1 added `--column-log` and `--outline-offset`. `docs/design/assets/components.css` was already out of sync with the app copy before B1; leave it unless the stage touches it. Tap targets: `--tap-min` (44 px) at least.

## Owner to do by hand (phone)
`docs/pwa-manual-checks.md`: the 25 "Workout log manual checks (B1)" (keypad per kind, select-all on focus, safe-area bottom bar, 360 px, all four looks, Back during a Workout, kill the tab and resume, PR in each Theme, airplane-mode logging). Also verify the review-fix visual drift: steppers now 64 px wide (were 60), Nudge button 44 px tall (was 36). Still open from earlier stages: B0, A0 and A3 manual checks. B2 should add its own section (rest timer, Wake Lock, sheets, Pain check, summary).

## Gotchas
- Don't pass multi-line commit messages or scripts through bash heredocs with unbalanced quotes; write files with the Write tool. There is no Python on this machine; use sed, PowerShell or the Edit tool.
- `gh` works from bash. LF->CRLF warnings are harmless.
- Agent worktrees: base them on the integration branch and check `git log` first (they sometimes start from an older commit).
- Implementers sometimes write code before tests; require the red run in the ticket report.
- Merge conflicts are likely in `WorkoutLog.razor(.cs)`, `ConfirmBar`, `SetRow` and `components.css` when tickets run in parallel; order tickets so shared files are touched by one ticket at a time where possible.
- Commit attribution lines come from the session reminder; follow it.
