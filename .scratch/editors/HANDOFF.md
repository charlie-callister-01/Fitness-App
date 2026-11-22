# Editors (B4 + B5): implementer handoff

Read this after `CLAUDE.md`, `docs/README.md` and `GLOSSARY.md`. Then read `docs/specs/editors-b4-b5.md` (also GitHub issue #24) and the ticket you are working on.

## State
- Branch: `b4-implementation` (from main after B3 merged). Spec and ten tickets are committed. Nothing is implemented yet.
- Tickets: `.scratch/editors/issues/01..10-*.md`. Do not edit the spec; if a ticket and the spec disagree, the spec wins and you note it in the ticket.
- Untracked `Workflow/` folder in the repo root is not yours; leave it alone.
- The agent workflow trial was reverted. There is no AGENTS.md, no RUN.md and no reports folder: implement the tickets yourself, in this order.

## Order and blocking
- Start: 01, 03, 04 (independent; 03 and 04 are Core only).
- Then 02 (needs 01).
- Then pages 05, 06, 07, 09 (05 and 06 need 02 and 03; 07 and 09 need 02 and 04).
- Then 08 (needs 07 and 05), then 10 (docs).
- One ticket at a time unless you are deliberately parallelising; commit after each ticket as `Editors: ticket NN <title>` with a green build and tests.

## After the tickets (CLAUDE.md workflow steps 4 to 7)
`/mattpocock-skills:code-review` against the commit before ticket 01, then `/thermo-nuclear-code-quality-review` (user-invoked), apply the fixes, then `/mattpocock-skills:pr`; commit the PR body as `.scratch/editors/pr-body.md`. Re-run `/mattpocock-skills:improve-codebase-architecture` after the PR (user-invoked).

## Conventions and gotchas (learned in B1 to B3)
- .NET 10, Blazor WebAssembly. Projects: Core, Core.Tests, Ui, Ui.Tests, Web, Web.Tests, Web.BrowserTests. Nullable on, `TreatWarningsAsErrors`; `dotnet build` must have 0 warnings, `dotnet test` all green (baseline: Core 1168, Ui 459, Web 39).
- Use glossary terms exactly: Session vs Workout, Custom workout, Missed session, Weigh-in, Logged afterwards, Guide, Exercise library, Week schedule, Phase, Phase goal, Benchmark exercise. User-facing copy uses them too.
- Core is pure rules (no clock, no storage); Ui pages stay thin and read a Core view where one exists. Page logic lives in `*.razor.cs`; pure rules go in Core with Core.Tests.
- CSS: plain CSS with design tokens, Blazor CSS isolation (`Foo.razor.css`). No raw hex or px except where an existing token file already does; tap targets use `--tap-min`. Both Themes (Keep, Clean) and both Modes must look right; screens must work at 360 px.
- Design reference: `docs/design/design-document.md` (S-03 to S-06, C-20, F-04, F-08, F-09), mockups in `docs/design/assets/mockups/`.
- Shared Ui pieces that exist: `Sheet` (with `Sheet.razor.js`), `Button`, `StatusOrb`, `EmptyState`, `ExercisePicker` and `ExercisePickerChoices`, `GuideContent`, `GuideSheet`, `SaveFailureNotice`, `AppData` (`Snapshot`, `Health.CanWrite`, `CommitAsync`, `Changed`), `ReadOnlyWording`, `WorkoutStarter`, `NudgeDismissals`.
- bUnit tests:
  - After a click that starts async work, assert inside `cut.WaitForAssertion(...)`, never straight after the click.
  - Sheets need `JSInterop.Mode = JSRuntimeMode.Loose` and `SetupModule("./_content/Ui/Components/Sheet.razor.js")`.
  - Use the page fixture (`LogFixture` today, renamed by ticket 02): `Wire(...)`, `OpenSeededAsync(...)`, `Plan()`, `Finished`, `InProgress`, `FakeDevicePreferences`, `FixedClock`/`TestClock` (fixed now is Mon 16 Nov 2026 09:00Z). Services cannot be registered after the first resolve; advance the registered `TestClock` instead.
  - A read-only app is simulated with an `AppData` opened with a `WriterLock = WriterLockResult.HeldElsewhere` environment (see `HomeTests`).
  - Failures are simulated with `FaultInjectingDataStore` and `FailAllWrites(StoreFailureKind...)`.
- Prior art to copy: `HomeTests`, `HomeActionsTests`, `CustomWorkoutDraftTests`, `WorkoutLog*Tests`, `HomeViewBuilderTests`.
- Shell tooling: the Bash tool breaks on heredocs containing apostrophes, so write files with the editor tools. There is no Python on the machine.
- Data model facts: `Plan` has Phases stored in weeks (`StartWeek`, `EndWeek`), Sessions with `DefaultDay`, `Type` and `Exercises` rows (`SessionExercise`: Sets, Reps, RepsMax, Cue). `Exercise` has `LogKind` (13 values), optional `ExerciseTags`, `IsBenchmark`. `Guide` is keyed by `ExerciseId`. `WeekSchedule` holds moves for one week. `Break` has start, end and reason. No model or exchange-format changes in this stage.
- Existing Core rules to reuse: `WeekScheduleRules` (layout, missed), `WeekRearrangement.Suggest`, `PhaseRules`, `ExercisePerformance`, `CustomWorkoutOffer`, `SimilarExercises`, `HomeViewBuilder`.
- Existing placeholders to replace (routes already exist): Exercises, Plans, Week pages in `src/Ui/Pages`.

## Decisions already made (do not re-ask)
- Groups: Editors now; Data and settings, then Insight, then Hardening later.
- Plan editor has Settings, Phases and Sessions tabs only; no Badges tab and no Import a Plan here.
- Reordering uses Move up and Move down buttons only; no drag and drop.
- No deleting Plans, Sessions or Exercises; no editing a Break after creation.
- Log kinds offered in New Exercise: all except the two body measurements (adjust only if the design clearly says otherwise).
- Tests: Core rules in Core.Tests, the guarded save once at its own interface, each page in bUnit. No new browser tests.
