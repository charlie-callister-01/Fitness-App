# 11: Ui: Recent PRs panel, Weight panel and Log weight sheet

Read `AGENTS.md` first.

Stage: b3-home   Wave: 2   Blocked by: 03, 04
Branch: b3-home/11-ui-recent-prs-weight-panels-and-sheet   Project(s) you may edit: Ui, Ui.Tests

## What to build

Three presentational components. They hold no saving logic.

**1. `RecentPrsPanel.razor`.** Parameters: `Prs` (`IReadOnlyList<RecentPr>`, ticket 03, merged), `Converter` (`UnitConverter`, for display units). Title "Recent PRs". Each row: the Exercise name, the new best Set formatted for its log kind, the previous best ("was 20 kg × 9"), and the Workout date ("Sun 15 Nov"). Reuse the existing formatting the finish summary uses for a PR Set and a previous best (read `FinishSummary.razor`); do not write a second formatter. Empty list: a quiet line "No PRs yet. Your first one will show here."

**2. `WeightPanel.razor`.** Parameters: `Panel` (`WeightPanel`, ticket 04, merged; alias the type to avoid the component name clash), `Converter`, `Today`, callback `OnLogWeight`. Title "Weight". Shows the latest Weigh-in in the display unit with its date ("86.3 kg · Sun 15 Nov"), this week's average ("This week avg 86.1 kg") when present, and the change from last week's average with a sign ("+0.4 kg" / "-0.4 kg"; "no change" at zero; omitted when null). Convert kg with `Converter.DisplayWeight`, one decimal place, and `Converter.WeightLabel`. No Weigh-ins: "No Weigh-ins yet." and a **Log weight** button raising `OnLogWeight`.

**3. `LogWeightSheet.razor`** (design S-08 "Log weight sheet") on the shared `Sheet`. Parameters: `Open`, `InitialText` (the prefilled value, already in the display unit, may be empty), `LastText` (for example "Last: 86.3 kg on Sun 15 Nov", may be empty), `UnitLabel`, `Saving`, `CanWrite`, `ReadOnlyMessage`, `Message` (a validation message to show), callbacks `OnSave(string text)` and `OnClose`. Title "Log weight". One large decimal field (`inputmode="decimal"`, labelled "Weight ({UnitLabel})"), the `LastText` line, **Save** (primary; disabled while `Saving` or when `!CanWrite`, with the read-only reason shown) and **Cancel**. Save raises `OnSave` with the field text as typed; **validation is done by the caller** and shown through `Message` (role alert). Cancel, Escape and backdrop raise `OnClose`.

CSS isolation, tokens only, 44 px targets.

## Read first
- `src/Core/Rules/Home/RecentPrs.cs`, `WeightPanelRules.cs` (tickets 03 and 04, merged)
- `src/Ui/Components/FinishSummary.razor` and `.razor.css` (PR row formatting to reuse; if the formatting is private, report a blocked ticket rather than copy-paste divergent code)
- `src/Core/Rules/Units/UnitConverter.cs`
- `src/Ui/Components/Sheet.razor`, `LoggedAfterwardsSheet.razor` (sheet pattern), `Button.razor`
- `src/Ui.Tests/Components/SheetTests.cs` (bUnit style)
- `docs/design/design-document.md` section S-08 (Log weight sheet) only; `docs/design/assets/mockups/S-08-weight-log-dark.png`
- Glossary terms used: PR, Weigh-in

## Files you may create or edit
- `src/Ui/Components/RecentPrsPanel.razor`, `WeightPanel.razor`, `LogWeightSheet.razor` and their `.razor.css` (new)
- `src/Ui.Tests/Components/RecentPrsPanelTests.cs`, `WeightPanelTests.cs`, `LogWeightSheetTests.cs` (new)
Anything else is out of scope (including `FinishSummary.razor`). If you need it, write a blocked report.

## Acceptance
- [ ] PR rows show name, new best, previous best and date; empty state text; kg and lb both format correctly
- [ ] Weight panel: latest with date, average, signed change, "no change", omitted change; empty state with Log weight button raising `OnLogWeight`
- [ ] Sheet: prefilled field and Last line; Save raises `OnSave` with the typed text; `Message` shown as an alert; Save disabled when saving or read-only; Cancel closes
- [ ] No raw hex or px in component CSS
- [ ] Tests written first (red run shown in the report), full `dotnet build` and `dotnet test` green
- [ ] No Core, Web, shared CSS or token changes

## Do not
- Do not call `AppData` or `WeighInRules` from the sheet. Do not edit `FinishSummary.razor`.
