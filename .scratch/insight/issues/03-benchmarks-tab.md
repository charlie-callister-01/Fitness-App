# 03: Benchmarks tab

Spec: `docs/specs/insight-b6-b8.md` (S-07 Benchmarks, C-19). Stories 16-20.

**Status:** resolved

**Blocked by:** 01 Progress tab with Exercise charts.

**What to build:** The Benchmarks tab lists each benchmark Exercise with its latest result against the previous, a ▲ when better, a ProgressBar toward a target where one exists, and the next test date. Lower-is-better measures (times) count a drop as better. An Exercise never tested says so. This builds the ProgressBar (C-19: label left, value right and always shown, state colour) which later tickets reuse.

New pure Core view built on the existing Benchmark rules: per benchmark Exercise, latest, previous, better flag, change, target-progress percentage where a target exists, and the next due date.

- [x] Core view has Core.Tests: better and worse for higher- and lower-is-better kinds, no previous, never tested, no target, next due date
- [x] ProgressBar component with bUnit tests (value text always present, state colour classes, accessible name)
- [x] Benchmarks tab has bUnit tests via `PageFixture`: populated, never tested, empty
- [x] Both Themes, both Modes, 360 px
- [x] `dotnet build` 0 warnings, `dotnet test` green; commit `Insight: ticket 03 Benchmarks tab`
