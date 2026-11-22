# 06: Trophies: Badges tab and detail

Spec: `docs/specs/insight-b6-b8.md` (S-13, C-10). Stories 39-43, 54.

**Status:** resolved

**Blocked by:** 03 Benchmarks tab (ProgressBar).

**What to build:** Trophies has tabs Badges, Bests and Calendar (Bests is a placeholder until ticket 08; Calendar reuses the heatmap from ticket 04). The Badges tab shows a BadgeTile grid (three columns): art, name (truncated, full name in the accessible label), "x / y". Unlocked Badges come first, then the rest closest to unlocking first (ties by name). Unlocked, in-progress and locked tiles differ by more than colour. A "Badges unlocked: 3 / 20" line and ProgressBars for the closest few. Tapping a tile opens the Badge detail: large art, name, what it needs, progress and the events that contributed (Workouts that raised a count, PR dates, Weigh-ins). Empty state before any history.

New pure Core trophy view over the existing Badge rules: ordering, unlocked count, closest few and per-Badge detail. Progress is never stored. A metric that cannot be measured shows "not measurable".

- [x] Core view has Core.Tests: ordering, ties, drop-by targets, not-measurable Badge, none unlocked, all unlocked, contributing events per metric
- [x] BadgeTile component and Badges tab bUnit tests via `PageFixture`: populated, empty, detail opens
- [x] Both Themes, both Modes, 360 px; tile art uses the existing starter art family
- [x] `dotnet build` 0 warnings, `dotnet test` green; commit `Insight: ticket 06 Trophies Badges tab and detail`
