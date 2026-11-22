# 04: Calendar tab

Spec: `docs/specs/insight-b6-b8.md` (S-07 Calendar, C-18). Stories 21-29.

**Status:** ready-for-agent

**Blocked by:** 01 Progress tab with Exercise charts.

**What to build:** The Calendar tab shows the whole Plan, start to race day, as a heatmap of weeks by days. Done days take the Session type colour, Partial days are half-filled, Missed days dashed, Break days hatched, PR days carry a small triangle, today is outlined, future days are empty. State is distinguishable without colour. A Phase strip above the weeks shows each Phase's name and length. The panel scrolls sideways; tapping a day opens its Workout; each day has one accessible label ("Wed 18 Nov: Session C, done, 1 PR"); a legend decodes the styles.

New pure Core calendar view built on the existing week, Break and Phase rules: per day a state, Session type, PR flag, today flag, accessible label and Workout reference; plus the Phase strip. Main Track only; take plain inputs so per-Track rows can be added later.

- [ ] Core view has Core.Tests: every state, Break over a Missed day, Partial, PR flag, future days, today, Plan with no Phases, label text
- [ ] CalendarHeatmap component with bUnit tests: cell state to class and label, tap opens the Workout, legend present
- [ ] Calendar tab tests via `PageFixture`: populated and no-history empty state
- [ ] Both Themes, both Modes, 360 px with sideways scroll
- [ ] `dotnet build` 0 warnings, `dotnet test` green; commit `Insight: ticket 04 Calendar tab`
