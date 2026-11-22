# 05: Weight screen

Spec: `docs/specs/insight-b6-b8.md` (S-08, C-17 weight variant). Stories 30-38.

**Status:** resolved

**Blocked by:** 01 Progress tab with Exercise charts (LineChart).

**What to build:** The Weight screen shows three stats (now, change since start, this week's average), a weight LineChart (every Weigh-in as a small dot, the 7-entry rolling average as the main line, the lowest Weigh-in ringed, an optional dashed goal line), a Monday-to-Sunday weekly averages table, and a Log weight button using the existing Log weight sheet unchanged. An empty state with a Log weight button shows before the first Weigh-in. Weigh-ins never count toward the Streak. The goal line comes from the first weight Badge whose target is a weight, or is absent.

New pure Core weight view: rolling-average series, lowest Weigh-in, now, change since start, current-week average and weekly table. The three headline numbers come from the existing weight panel rules so Home and Weight cannot disagree. Extend the LineChart with the weight variant.

- [x] Core view has Core.Tests: fewer than 7 entries, one entry, gaps, ties for lowest, week boundaries, a weight Badge goal and none, unit conversion
- [x] A test proves Weight and Home headline numbers agree on the same data
- [x] LineChart weight variant and Table toggle; bUnit tests for the page: empty, populated, Log weight adds a new Weigh-in, read-only app, save failure notice
- [x] Both Themes, both Modes, 360 px
- [x] `dotnet build` 0 warnings, `dotnet test` green; commit `Insight: ticket 05 Weight screen`
