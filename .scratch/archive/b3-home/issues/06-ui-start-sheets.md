# 06: Ui: Do a different Session sheet and Log a past Workout sheet

Read `AGENTS.md` first.

Stage: b3-home   Wave: 1   Blocked by: None
Branch: b3-home/06-ui-start-sheets   Project(s) you may edit: Ui, Ui.Tests

## What to build

Two presentational sheets on the shared `Sheet`. They hold no starting logic: they raise events and the Home page (a later ticket) starts the Workout.

**1. `DifferentSessionSheet`** (design F-03). Parameters: `Open`, `Sessions` (`IReadOnlyList<Session>`, this week's undone Sessions, already ordered), `Saving`, `CanWrite`, `ReadOnlyMessage`, callbacks `OnPick(Session)`, `OnBuildCustom`, `OnClose`. Title "Do a different Session". Each row is a button (44 px tap target) showing the Session code (text before the first colon when 1 to 3 characters, else the first letter), the name, the type (Strength, Cardio, Body, Test) and the default day ("Mon"). Tapping a row raises `OnPick`. When `Sessions` is empty show "No Sessions left this week." and a **Build a Custom workout** button raising `OnBuildCustom`. Rows are disabled while `Saving` or when `!CanWrite` (then show `ReadOnlyMessage` in plain words).

**2. `LogPastWorkoutSheet`**. Parameters: `Open`, `Today` (`DateOnly`), `Sessions` (all Sessions of the active Plan), `Saving`, `CanWrite`, `ReadOnlyMessage`, `Message` (a refusal to show), callbacks `OnStart(DateOnly date, Session? session)` (null session means a Custom workout), `OnClose`. Title "Log a past Workout". A native date input (`max` = today, default yesterday) like `LoggedAfterwardsSheet`, then a list of choices: each Session, and "Custom workout". Tapping a choice with a valid date raises `OnStart`. A blank or unparsable date shows "Pick the day it happened." inside the sheet and raises nothing. A date after today is not offered by the input; if one arrives anyway show "That day hasn't happened yet." and raise nothing.

Both: Escape and backdrop tap raise `OnClose` (the `Sheet` does this). Plain CSS isolation (`.razor.css`), design tokens only (a raw hex or px in component CSS is a defect), `fa-*` classes like the existing sheets. Reduced motion needs nothing extra.

## Read first
- `src/Ui/Components/Sheet.razor` and `Sheet.razor.css`
- `src/Ui/Components/LoggedAfterwardsSheet.razor` and `.razor.css` (date field pattern to copy)
- `src/Ui/Components/SkipSheet.razor` and `MenuChoices.razor` (choice rows pattern)
- `src/Ui/Components/Button.razor`
- `src/Ui.Tests/Components/SheetTests.cs` and `SheetButtonsReadOnlyTests.cs` (bUnit style)
- `src/Core/Model/Plan.cs` (`Session`, `SessionType`)
- Glossary terms used: Session, Custom workout, Logged afterwards

## Files you may create or edit
- `src/Ui/Components/DifferentSessionSheet.razor` and `.razor.css` (new)
- `src/Ui/Components/LogPastWorkoutSheet.razor` and `.razor.css` (new)
- `src/Ui.Tests/Components/DifferentSessionSheetTests.cs`, `LogPastWorkoutSheetTests.cs` (new)
Anything else is out of scope. If you need it, write a blocked report.

## Acceptance
- [ ] Different Session: rows show code, name, type and day; tapping raises `OnPick` with that Session; empty list shows the message and the Build a Custom workout button; saving or read-only disables rows and shows the reason
- [ ] Past Workout: date defaults to yesterday and cannot exceed today; picking a Session raises `OnStart(date, session)`; Custom raises `OnStart(date, null)`; blank date shows the message and raises nothing; a future date is refused with the message
- [ ] Both sheets close through `OnClose` and are labelled dialogs (via `Sheet`)
- [ ] bUnit tests wait for visible results before asserting where a handler is async
- [ ] Tests written first (red run shown in the report), full `dotnet build` and `dotnet test` green
- [ ] No Core, Web or token changes

## Do not
- Do not call `WorkoutStarter`, `AppData` or navigation from these components.
- Do not edit `components.css`, `ui.css` or token files.
