# Handoff: A4.5 (explicit Tracks)

Point a fresh chat at this file. Read `CLAUDE.md`, `docs/README.md` and `GLOSSARY.md` first, then `docs/specs/a4-5-tracks.md`, `docs/adr/0007-explicit-tracks-and-format-version-2.md` and the ticket you are working on. File lives at `.scratch/a4-5-tracks/HANDOFF.md`.

## State (22 Nov 2026)
- The spec, ADR 0007, fifteen tickets and the docs are written. **Nothing is implemented; no ticket is done.** The spec replaces an earlier "implicit main track" version (never implemented).
- Base: `main` after the Editors PR (#25) merged (Core 1309, Ui 597, Web 39 at the end of Editors; `dotnet build` must stay at 0 warnings). Work in a new worktree and branch per PR (for example `a4-5-pr1`, `a4-5-pr2`). Do not reuse old ones. Leave the untracked `Workflow/` folder in the repo root alone.
- **One implementer at a time.** The owner is usage-constrained, so tickets are run sequentially in the order below and stages are not run in parallel (A5 and Data and settings are queued behind PR 1, not beside it).

## Order and blocking
PR 1 (Core, exchange, upgrader and migration, Editors Core rules made Track-aware; screens still show the main Track only):
1. 01 Track model and the mechanical ripple (no blockers)
2. 02 Format 2 reader, writer and validation (01)
3. 03 Stored-data migration (02)
4. 04 Weekly Tracks, Phases and schedule per Track (01)
5. 05 Editors Core rules made Track-aware, and Track edit rules (01)
6. 06 Daily Track rules (04)
7. 07 Track Badge metrics and Nudges (04, 06)
8. 08 Home view data per Track (04, 06, 07)
9. 09 Exchange: Import, Export, Markdown, prompt pack (02, 03)
10. 10 PR 1 whole-loop test, regression sweep and docs, then reviews, fixes and the PR body (all above)

Then, one at a time, either A5 (`docs/specs/a5-starter-data.md`, needs PR 1 only) or Data and settings (B9 + B10). Then PR 2 (screens):
11. 11 Plan editor Track selector and split Settings (PR 1 written)
12. 12 Plan editor Phases and Sessions per Track (11)
13. 13 Week view daily row (12)
14. 14 Plans list and Home Tracks (13)
15. 15 Guide and Library Track context, manual checks, docs, reviews and the PR body (14)

Commit after each ticket as `A4.5: ticket NN <title>` with a green build and tests. Within a PR, do not start the next PR until the current PR's body is written.

## After each PR's tickets (CLAUDE.md steps 4 to 7)
`/mattpocock-skills:code-review` against the commit before the PR's first ticket, `/thermo-nuclear-code-quality-review` (read `.claude/skills/thermo-nuclear-code-quality-review/SKILL.md` if it cannot be run as a skill; apply it as C#: nullable abuse, `object`, casts), apply the fixes, `/mattpocock-skills:pr`. PR bodies: `.scratch/a4-5-tracks/pr-body-1.md` and `pr-body-2.md`.

## Conventions and gotchas (from `.scratch/editors/HANDOFF.md`)
- .NET 10, Blazor WebAssembly. Projects: Core, Core.Tests, Ui, Ui.Tests, Web, Web.Tests, Web.BrowserTests. Nullable on, `TreatWarningsAsErrors`; `dotnet build` 0 warnings, `dotnet test` all green.
- Use glossary terms exactly: Session vs Workout, Custom workout, Missed session, Weigh-in, Logged afterwards, Guide, Exercise library, Week schedule, Phase, Phase goal, Benchmark exercise, and now Track, Main track, Daily Track, Cadence. User-facing copy uses them too.
- Core is pure rules (no clock, no storage); Ui pages stay thin and read a Core view where one exists. Page logic lives in `*.razor.cs`; pure rules go in Core with Core.Tests. Every edit rule returns `PlanEditResult` (a changed value or a plain reason).
- CSS: plain CSS with design tokens, Blazor CSS isolation (`Foo.razor.css`). No raw hex or px except where an existing token file already does; tap targets use `--tap-min`. Both Themes (Keep, Clean) and both Modes; 360 px.
- Design reference: `docs/design/design-document.md` (S-01, S-05, S-06, C-08, C-09, C-20, C-24, F-04, F-08), mockups in `docs/design/assets/mockups/`.
- Shared Ui pieces: `GuardedSave` (the one way a screen saves), `PageContext`, `FormSheet`/`FormSheetState`/`ReadOnlyWell`, `Sheet`, `Button`, `StatusOrb`, `EmptyState`, `ExercisePicker`, `GuideContent`, `SaveFailureNotice`, `AppData`, `NudgeDismissals`. Plan editor: `PlanEditState` (draft, `Compose()`, `IsDirty`), `PlanSettingsTab`, `PlanPhasesTab`, `PlanSessionsTab`.
- bUnit: assert inside `cut.WaitForAssertion(...)` after any async click; sheets need `JSInterop.Mode = JSRuntimeMode.Loose` and `SetupModule("./_content/Ui/Components/Sheet.razor.js")`; use `PageFixture` (`Wire`, `OpenSeededAsync`, `TestClock`, fixed now Mon 16 Nov 2026 09:00Z); a read-only app uses `WriterLockResult.HeldElsewhere`; failures use `FaultInjectingDataStore`.
- Prior art: `HomeViewBuilderTests`, `HomeTests`, `PlanEditingRulesTests`, `DocumentValidatorTests`, `PlanTestData`, the A4 whole-loop test, `AppData` migration tests.
- Shell tooling: the Bash tool breaks on heredocs with apostrophes, so write files with the editor tools. No Python on the machine (perl exists). `gh` is at `C:\Program Files\GitHub CLI\gh.exe` and works from PowerShell.
- Key invariant: **a one-Track Plan behaves exactly as the old one-rhythm Plan.** Existing tests pass with only mechanical fixture changes; one that needs more is a finding to report in the ticket, not a test to loosen.

## Decisions already made (owner, 22 Nov 2026; do not re-ask)
- Explicit Tracks, not an implicit main track. `Plan` keeps plan-level fields, `tracks` (required, at least one) and a flat `sessions`. Weekly Track: `weeklyTarget`, `streakMinWorkoutsPerWeek`, `streakCountsCustomWorkouts`. Daily Track: `dayDonePercent` 1 to 100, default 75. Every Track has its own optional `phases`. `Session.trackId` required; `Session.type` gains `mobility`; `Session.defaultDay` gains `daily` (required on daily Tracks, forbidden on weekly). Phase IDs unique document-wide; the week-range rule runs per Track; a Phase override names a Session of that Track.
- Main Track is the first Track and must be weekly; no `isMain` flag. Custom workouts, the benchmark and Home's Today card attach to it. Tracks can be reordered (Move up, Move down), added and edited, never removed. Cadence fixed at creation; Track IDs permanent, names editable.
- `formatVersion` 2 with a Core v1 upgrader (one Track named "Main"); the importer keeps reading v1; a Core startup migration rewrites stored Plans (ADR 0005); only the upgrader and migration know the old shape. ADR 0007 records this.
- Daily rules: a day is done at `dayDonePercent` of planned rows done; the daily Streak is consecutive done days (open today does not break it; a Break day is neutral); a daily Session is never Missed and cannot be moved (refused via `PlanEditResult`). No daily-Track Nudge for now. Custom workouts and the benchmark are main Track only.
- Badges: unqualified `streak`, `bestweek`, `sessions` mean the main Track; `streak:<id>`, `days:<id>`, `bestweek:<id>` name a Track; unknown Track ID is "not measurable"; the Badge editor will later offer a Track picker (Insight).
- Exchange: Plan Import remaps colliding Track IDs (and `trackId`, Phase overrides, Track Badge metrics follow); Export JSON and Markdown carry Tracks; the prompt pack example has two Tracks including a daily mobility Track; Merge treats Tracks as part of the Plan.
- Two PRs, in this order: PR 1, then A5 or Data and settings, then PR 2. A5 is repurposed as an example and test dataset (see its spec).

## Calls made when writing the tickets (flag if the owner objects, otherwise keep)
- Upgraded Track ID is `<planId>-main` (deterministic and unique across Plans; Track IDs are document-wide).
- Unqualified `sessions` now counts the main Track's Workouts only (the old meaning was all counted Workouts).
- `days:<id>` on a weekly Track is that Track's total counted Workouts.
- A day with several Workouts of a daily Track is done if any one is; days before the Plan start do not count.
- The reader requires `dayDonePercent`; the editor and prompt pack default it to 75.
- New Track defaults: weekly target 3, Streak minimum 2, Custom workouts off.
- `streakCountsCustomWorkouts` is stored on every weekly Track but only the main Track's value matters.
- Starting a Session of an extra weekly Track from Home is through its own week line; "Do a different Session" and "Make up a Missed session" stay main Track only.
- Ticket 01 keeps writing format 1 (from the main Track) until ticket 02, so the suite stays green in between.
- ADR 0005 says the format upgrade has never run; ticket 03 is its first real use.

## What NOT to re-ask
Anything in "Decisions already made", the per-ticket scope, and the two-PR split. If a ticket and the spec disagree, the spec wins; note it in the ticket.
