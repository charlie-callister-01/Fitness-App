# 02: Chart date range

Spec: `docs/specs/insight-b6-b8.md` (C-15). Story 14.

**Status:** ready-for-agent

**Blocked by:** 01 Progress tab with Exercise charts.

**What to build:** The owner can zoom the Progress charts to a range: Last 4 weeks, Last 12 weeks, Whole Plan or Custom. The DateRangePicker (C-15) is a field showing the range in words ("Last 12 weeks · 24 Aug – 16 Nov"); tapping opens a sheet with the presets and, for Custom, two native date inputs. Start after end shows an inline message and does not apply. The chosen range filters the chart series and the best-point and change figures use the range's points. The range is a screen choice, not stored data. The component is generic so Data and settings (B9) can reuse it for Export.

- [ ] Range resolution (presets relative to `today`, Whole Plan from Plan start, validation) is a pure Core rule with Core.Tests, including Plan start later than a preset start
- [ ] DateRangePicker component with bUnit tests: presets, Custom, inline error, accessible labels
- [ ] Progress charts respond to the range; an empty range shows a plain message
- [ ] Both Themes, both Modes, 360 px; native date inputs only, no custom calendar
- [ ] `dotnet build` 0 warnings, `dotnet test` green; commit `Insight: ticket 02 Chart date range`
