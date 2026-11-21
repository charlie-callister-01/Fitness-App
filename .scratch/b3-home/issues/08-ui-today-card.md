# 08: Ui: TodayCard component (C-09)

Read `AGENTS.md` first.

Stage: b3-home   Wave: 2   Blocked by: 01
Branch: b3-home/08-ui-today-card   Project(s) you may edit: Ui, Ui.Tests

## What to build

The Home hero card, `TodayCard.razor`, rendering a `TodayCard` record (Core, ticket 01) in every state. The component is presentational: it raises events and holds no starting or saving logic.

Parameters: `Card` (`Core.Rules.Home.TodayCard`; mind the name clash with the component, alias the type), `CanWrite`, `ReadOnlyMessage`, `Starting` (true while a Start is saving), callbacks `OnStart(Session)`, `OnContinue(Workout)`, `OnOpenSummary(Workout)` (for Done).

Per `Card.Kind` (copy and layout follow the `S-01-home-*` mockups in `docs/design/assets/mockups/`; wording below is authoritative):
- **Start:** Session name, type and number of Exercises ("Strength · 6 Exercises"), a primary **Start** button (disabled while `Starting` or when `!CanWrite`, with `ReadOnlyMessage` shown when read-only).
- **InProgress:** "In progress", "{DoneCount} of {PlannedCount} done" plus " · {minutes} min" when `Elapsed` is known, a primary **Continue** button (never disabled by read-only: it only opens the log).
- **RestDay:** "Rest day" and "Next is {Session name} on {weekday}" from the next Session and date; no button. With no next Session: "Rest day". When the next date is in a later week use the weekday plus "next week" ("Next is Full body on Monday next week"; the card has the date to decide, compare Monday-to-Sunday weeks).
- **DoneToday:** "Done today", the duration ("45 min", omitted when null), "{n} Sets", "{n} PRs" (singular for 1; omitted when 0), and "Partial" when partial. Tapping the card, or a **View summary** button, raises `OnOpenSummary`.
- **Break:** "On a Break", quiet text, no button.
- **PlanFinished:** "Your Plan has finished" with a muted line "Start a new Plan to keep going." (no button).
- **NotStarted:** "Your Plan starts on {date}" (for example "Mon 30 Nov"); no button.
- **NoPlan:** reuse the existing `EmptyState` with `EmptyStateKind.NoPlan`, title "No active Plan", message "Make a Plan active and today's Session will start from here."

Keep adds a zigzag tapestry band on top and Clean hides it: grep `src/Ui/wwwroot/` (`components.css`, `ui.css`, `tokens.css`) for an existing band or zigzag class and reuse it under the `data-theme` rule; if none exists, omit the band and say so in the report. Styling is CSS isolation (`TodayCard.razor.css`), tokens only, 44 px minimum tap targets, labelled regions (`aria-label` "Today"). Do not edit the shared CSS files.

## Read first
- `src/Core/Rules/Home/TodayCard.cs` and `HomeTodayRules.cs` (ticket 01, merged): the exact record members
- `src/Ui/Components/EmptyState.razor` and `EmptyStateKind.cs`, `Button.razor`, `StateChip.razor`
- `src/Ui/Components/FinishSummary.razor` and `.razor.css` (card styling and stat formatting pattern)
- `src/Ui/Pages/Home.razor` (current placeholder, for the existing Start/Continue behaviour only; do NOT edit it)
- `src/Ui.Tests/Components/StatusOrbTests.cs` (bUnit component test style)
- `docs/design/design-document.md` section C-09 (lines about TodayCard only) and the `S-01-home-*.png` mockups
- Glossary terms used: Session, Workout, Break, Partial workout, Plan

## Files you may create or edit
- `src/Ui/Components/TodayCard.razor` and `TodayCard.razor.css` (new)
- `src/Ui.Tests/Components/TodayCardTests.cs` (new)
Anything else is out of scope. If you need it, write a blocked report.

## Acceptance
- [ ] Each of the 8 kinds renders the text above and only the buttons listed
- [ ] Start raises `OnStart` with the Session; disabled when `Starting` or `!CanWrite` (read-only reason shown); a second click while `Starting` does nothing
- [ ] Continue raises `OnContinue`; Done raises `OnOpenSummary`
- [ ] Rest day wording for "later this week" and "next week"; singular "1 PR" and "1 Set"
- [ ] Card is a labelled region; no raw colour or px values in the CSS
- [ ] bUnit tests wait for visible results where a handler is async
- [ ] Tests written first (red run shown in the report), full `dotnet build` and `dotnet test` green
- [ ] No Core, Web or shared CSS or token changes

## Do not
- Do not edit `Home.razor`. Do not call `WorkoutStarter`, `AppData` or navigation.
- Do not add tokens; if one is missing, write a blocked report naming it.
