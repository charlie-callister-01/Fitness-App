# 09: Ui: WeekStrip (C-24), week line and orb row

Read `AGENTS.md` first.

Stage: b3-home   Wave: 2   Blocked by: 02
Branch: b3-home/09-ui-week-strip-line-and-orbs   Project(s) you may edit: Ui, Ui.Tests

## What to build

Three presentational components using the Core records from ticket 02 (merged). No data access.

**1. `WeekStrip.razor`** (C-24). Parameter `Tiles` (`IReadOnlyList<WeekTile>`), callback `OnOpenDay(DateOnly)`. Seven tiles in a row, each at least 52 px tall and a 44 px wide tap target: weekday label, the Session code coloured by Session type (existing tokens `--session-strength`, `--session-cardio`, `--session-body`, `--session-test`), and a state glyph: done "✓", missed "○", partial "◐", Break day a quiet dash "–", upcoming and rest no glyph. Today has a 3 px outline using the focus token. Each tile is a button; tap raises `OnOpenDay` with its date. Each tile has an `aria-label` such as "Monday, Session A, done" / "Tuesday, rest day" / "Today, Wednesday, Session B, upcoming". The glyph is never the only signal (the aria-label carries the state in words).

**2. `WeekLine.razor`**. Parameter `Line` (`WeekLine`). Renders "This week {Done} of {Scheduled} · target {Target}" and, when there are Missed codes, " · Missed: A, C". Plain muted text.

**3. `OrbRow.razor`**. Parameters `Orbs` (`HomeOrbs`), callback `OnOpenProgress`. Three existing `StatusOrb`s in a row:
- Streak: value = `Current` as text, `Fraction` = ring fraction (`ThisWeekCount / Minimum`, capped at 1; 0 when Minimum is 0), caption "Streak (weeks)", `Greyed` = `OnBreak` (the orb then shows its own "On a Break" caption), accessible name "Streak: {n} weeks" (and ", on a Break").
- Phase: only when `PhaseOrb` is not null: value "{WeekInPhase}", caption "Phase week of {PhaseLengthWeeks}", name "Phase week {x} of {y}", `Fraction` `WeekInPhase / PhaseLengthWeeks`.
- Race: only when `RaceOrb` is not null: value "{DaysToGo}", caption "Days to race", name "{n} days to race", `Fraction` = `ElapsedShare`, tone Ochre.
- Each orb is wrapped in a button (44 px minimum) that raises `OnOpenProgress`.

Styling by CSS isolation (`.razor.css` per component), tokens only. **This ticket owns token and shared-CSS changes for Home.** Only if a needed token truly does not exist, add it to `src/Ui/wwwroot/tokens.css` **and** `docs/design/assets/tokens.css` (and `docs/design/assets/tokens.json`) in the same change and say so in the report. Prefer existing tokens.

## Read first
- `src/Core/Rules/Home/WeekTile.cs`, `HomeWeekRules.cs`, `HomeOrbs.cs` (ticket 02, merged): exact member names
- `src/Ui/Components/StatusOrb.razor`, `.razor.css`, `OrbTone.cs`
- `src/Ui/Components/TabBar.razor` and `.razor.css` (a tappable, labelled strip for styling reference)
- `src/Ui.Tests/Components/StatusOrbTests.cs` (bUnit style)
- `src/Ui/wwwroot/tokens.css` (session and focus tokens)
- `docs/design/design-document.md` sections C-11 and C-24 only; `docs/design/assets/mockups/S-01-home-dark.png`
- Glossary terms used: Session, Streak, Phase, Break, Missed session, Partial workout

## Files you may create or edit
- `src/Ui/Components/WeekStrip.razor`, `WeekLine.razor`, `OrbRow.razor` and their `.razor.css` (new)
- `src/Ui.Tests/Components/WeekStripTests.cs`, `WeekLineTests.cs`, `OrbRowTests.cs` (new)
- Only if truly needed: `src/Ui/wwwroot/tokens.css`, `docs/design/assets/tokens.css`, `docs/design/assets/tokens.json`
Anything else is out of scope. If you need it, write a blocked report.

## Acceptance
- [ ] Seven tiles in order with the right glyph and `aria-label` for every state; today outlined; tap raises `OnOpenDay` with the tile's date
- [ ] Line text with and without Missed codes
- [ ] Orb row: Streak always; Phase and Race omitted when null; Break greys the Streak orb and keeps its number; each orb raises `OnOpenProgress`
- [ ] No raw hex or px in component CSS; tap targets at least 44 px
- [ ] Tests written first (red run shown in the report), full `dotnet build` and `dotnet test` green
- [ ] No Core or Web changes

## Do not
- Do not edit `Home.razor`, `components.css` or `ui.css`.
- Do not add motion.
