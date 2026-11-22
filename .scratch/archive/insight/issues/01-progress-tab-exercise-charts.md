# 01: Progress tab with Exercise charts

Spec: `docs/specs/insight-b6-b8.md` (S-07, C-17). Stories 1-13, 15.

**Status:** resolved

**Blocked by:** None (can start immediately).

**What to build:** The Progress screen stops being a placeholder. It has three tabs (Progress, Benchmarks, Calendar; the last two show a plain "coming" state until tickets 03 and 04). The Progress tab shows group chips (Strength, Carries, Engine, Bodyweight, Body) and a LineChart per Exercise logged twice or more. Strength Exercises plot an estimated 1RM; other log kinds plot their own measure through the existing progress-point rule. Each chart labels its best point ("Best 33.3 · +7.3 since W1"), draws PR points larger, breaks the line at gaps (never interpolates), shows Phase bands, gives a tap tooltip (date, value) and a Table toggle for screen readers. Exercises with one entry are listed as "1 entry · needs 2 for a chart". An empty state shows before any history. Weights follow the owner's units. Settle the estimated-1RM formula here (Epley unless a reason appears) and record it in the glossary.

New pure Core views: the progress series for an Exercise (points, PR flag, Phase, best point, change since first entry), the Exercise group derived from log kind (Text and tick-only never charted), and the progress list split into chartable and needs-2. The LineChart takes a prepared series, not Workouts. Unqualified views mean the main Track and take plain Plan, Workouts, Breaks, Weigh-ins and Exercises inputs.

- [x] Core views have Core.Tests: groups from every log kind, 1RM, PR flags, gaps, Phase assignment, one-entry split, change since first entry, unit conversion
- [x] LineChart (C-17) is inline SVG to spec with tooltip and Table toggle; works in both Themes, both Modes, 360 px
- [x] Progress page has bUnit tests via `PageFixture`: empty state, populated, chip filtering, Table toggle, one-entry rows
- [x] Tabs exist (Benchmarks and Calendar are placeholders), pages thin, logic in `*.razor.cs`
- [x] 1RM formula recorded in `GLOSSARY.md`
- [x] `dotnet build` 0 warnings, `dotnet test` green; commit `Insight: ticket 01 Progress tab with Exercise charts`
