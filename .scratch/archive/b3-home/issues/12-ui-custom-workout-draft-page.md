# 12: Ui: Custom workout draft page (Build a Custom workout and Make up)

Read `AGENTS.md` first.

Stage: b3-home   Wave: 2   Blocked by: 05
Branch: b3-home/12-ui-custom-workout-draft-page   Project(s) you may edit: Ui, Ui.Tests

## What to build

A page where the owner builds a Custom workout before starting, used by **Build a Custom workout** and by **Make up a Missed session** (design F-02).

- Routes: `@page "/custom"` (empty draft) and `@page "/custom/makeup/{SessionId}"` (draft pre-filled with that Session's Exercises). Check `src/Ui/Pages` and the router for conflicts first.
- The draft is held in the page's memory only (lost on reload; acceptable).
- Page title "Custom workout" (or "Make up {Session name}" for Make up). A top Back link to Home (`/`).
- Draft content: a list of rows, each the Exercise name with a **Remove** button (44 px, accessible name "Remove {name}"). Empty draft: a quiet line "No Exercises yet. Add some to start."
- **Add Exercise** button opens the shared `ExercisePicker` (C-14) titled "Add Exercise". Choices: build an `ExercisePickerChoices` with no "Similar" group (empty) and `Others` = `CustomWorkoutOffer.Order(plan, schedules, breaks, workouts, library, today)` minus Exercises already in the draft; `ShowSearch` true when the library has more than 20 Exercises (see how the log page builds its choices and follow it). Picking adds the Exercise at the end of the draft.
- **Start** (primary) is disabled while the draft is empty, while saving, or when the app is read-only (`Data.Health.CanWrite`; follow `ReadOnlyMessage` in `WorkoutLog`). Start builds the rows with `CustomWorkoutStart.RowsForMakeUp` (Make up, for the Exercises still in the draft use the Session's rows whose Exercise is still present, in draft order) or `CustomWorkoutStart.RowsForExercises` (otherwise), calls `WorkoutStarter.StartCustomAsync` (ticket 05, merged), reports the commit through `SaveFailureNotice`, and on success navigates to `log/{workout.Id}`. A second tap while saving does nothing. If a Workout is already in progress, `StartCustomAsync` continues it: navigate to that Workout and show nothing else.
- Make up pre-fill: unknown or non-matching `SessionId`, or no active Plan, shows an `EmptyState`/plain message ("That Session isn't in your active Plan.") with a link Back to Home instead of the draft.
- The Missed Session stays Missed (the Workout has no `SessionId`): you do not need extra code for this, but add the page test below.

Follow the code-behind style of `WorkoutLog.razor` / `.razor.cs` where it keeps the markup thin. CSS isolation, tokens only.

## Read first
- `src/Core/Rules/Starting/CustomWorkoutStart.cs` and `WorkoutStarter.cs` (ticket 05, merged)
- `src/Core/Rules/CustomWorkout/CustomWorkoutOffer.cs`
- `src/Ui/Components/ExercisePicker.razor`, `ExercisePickerChoices.cs`
- `src/Ui/Pages/WorkoutLog.razor.cs` (how the picker choices, read-only message and SaveFailureNotice are used; search for `ExercisePickerChoices`, `ReadOnlyMessage`, `Notice.Report`)
- `src/Ui/Pages/Home.razor` (how the placeholder starts a Workout and navigates; do NOT edit it)
- `src/Ui.Tests/Pages/LogFixture.cs` (`Wire`, `OpenSeededAsync`, `Plan`, `Exercises`), `src/Ui.Tests/Pages/HomeTests.cs` (page test style)
- `docs/design/design-document.md` flow F-02 only
- Glossary terms used: Custom workout, Missed session, Session, Exercise library

## Files you may create or edit
- `src/Ui/Pages/CustomWorkoutDraft.razor`, `CustomWorkoutDraft.razor.cs`, `CustomWorkoutDraft.razor.css` (new)
- `src/Ui.Tests/Pages/CustomWorkoutDraftTests.cs` (new)
Anything else is out of scope (including `LogFixture.cs`: use its helpers as they are). If you need more, write a blocked report.

## Acceptance
- [ ] `/custom` shows an empty draft with Start disabled; adding an Exercise enables Start; Remove removes it; an Exercise already in the draft is not offered again
- [ ] The picker offers this week's Missed Sessions' Exercises first (via `CustomWorkoutOffer.Order`)
- [ ] `/custom/makeup/{id}` pre-fills the Session's Exercises, which can be removed and added to; an unknown id shows the message and a Back link
- [ ] Start saves a Workout with no Session id and the draft rows as planned, navigates to `log/{id}`; a second tap while saving starts only one Workout
- [ ] A Missed Session remains in `MissedInWeek` after making it up
- [ ] Read-only disables Start with a plain reason; a failed save leaves the page unchanged and reports through `SaveFailureNotice`
- [ ] Tests written first (red run shown in the report), waiting for visible results after async clicks; full `dotnet build` and `dotnet test` green
- [ ] No Core, Web, shared CSS or token changes

## Do not
- Do not edit `Home.razor`, `LogFixture.cs`, `WorkoutLog.*` or `Program.cs`.
- Do not persist the draft anywhere.
