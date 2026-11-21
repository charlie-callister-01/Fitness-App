# 04: Core: weight panel and Weigh-in creation

Read `AGENTS.md` first.

Stage: b3-home   Wave: 1   Blocked by: None
Branch: b3-home/04-core-weight-panel-and-weigh-in   Project(s) you may edit: Core, Core.Tests

## What to build

Two pure rules in `Core.Rules.Home`.

**1. Weight panel.** `WeightPanelRules.Compute(IReadOnlyList<WeighIn> weighIns, DateOnly today)` returns `WeightPanel(WeighIn? Latest, double? WeekAverageKg, double? ChangeKg)`.
- `Latest`: the Weigh-in with the latest date (ties: the last in the list).
- `WeekAverageKg`: the mean of all Weigh-ins dated in today's Monday-to-Sunday week (`WeekCalendar`); null when there are none.
- `ChangeKg`: `WeekAverageKg` minus the mean of last week's Weigh-ins; null unless both weeks have at least one. Positive means heavier.
- All values are canonical kg. The Ui converts for display.

**2. Weigh-in creation.** `WeighInRules.Create(string? text, UnitConverter converter, DateOnly today, IIdGenerator ids)` returns a result that is either `Valid(WeighIn)` or `Invalid(string Message)`.
- Parse `text` as a decimal number, accepting a comma or a dot as the decimal separator and ignoring surrounding spaces. Empty or unparsable gives Invalid "Enter your weight as a number, for example 86.3."
- Convert from the display unit with `converter.WeightFromDisplay(value)`. The resulting kg must be greater than or equal to 20 and less than or equal to 400, otherwise Invalid "That doesn't look like a body weight. Check the number."
- Valid: a new `WeighIn(ids.NewId(), today, kg)`. Round kg to 2 decimal places.
- Also `WeightPanelRules.LastLabelInfo(IReadOnlyList<WeighIn> weighIns)` returning the latest `WeighIn?` for the sheet's "Last: 86.3 on Sun 15 Nov" prefill (may simply return `Latest`; one test).

## Read first
- `src/Core/Model/BodyAndGuide.cs` (`WeighIn`)
- `src/Core/Rules/Units/UnitConverter.cs` (`WeightFromDisplay`, constructors)
- `src/Core/Rules/Weeks/WeekCalendar.cs`
- `src/Core/Ids/` (`IIdGenerator`) and a test fake for it: grep `IIdGenerator` in `src/Core.Tests`
- `src/Core.Tests/Rules/Units/` (style), `src/Core.Tests/Rules/TestData/Dates.cs`
- Glossary terms used: Weigh-in

## Files you may create or edit
- `src/Core/Rules/Home/WeightPanelRules.cs`, `src/Core/Rules/Home/WeighInRules.cs` (new)
- `src/Core.Tests/Rules/Home/WeightPanelRulesTests.cs`, `src/Core.Tests/Rules/Home/WeighInRulesTests.cs` (new)
Anything else is out of scope. If you need it, write a blocked report.

## Acceptance
- [ ] Panel: no Weigh-ins gives all null; one this week and none last week gives an average and a null change; both weeks gives the change; a Weigh-in on Sunday belongs to that week and Monday to the next; latest picks the latest date
- [ ] Create: valid kg value; valid lb value converts to kg (86.3 lb is about 39.14 kg); comma decimal accepted; empty, text, zero, negative, 19.9 kg and 400.1 kg are Invalid; 20 and 400 are Valid; kg rounded to 2 dp; date is today; ID from the generator
- [ ] Tests written first (red run shown in the report), full `dotnet build` and `dotnet test` green
- [ ] No data model, exchange format or storage change

## Do not
- Do not persist anything here; no `AppData` use. Do not use the system clock.
