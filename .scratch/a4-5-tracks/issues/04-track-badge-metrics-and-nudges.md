# 04: Track Badge metrics and Nudges

**What to build:** Badges and Nudges understand Tracks. New Badge metrics `streak:<trackId>`, `days:<trackId>` and `bestweek:<trackId>` measure a Track (weeks for a weekly Track's streak, days for a daily one), and the old metrics keep their old meaning on a multi-Track Plan. The "one Session short of this week's target" and "Session X was missed" Nudges work per weekly Track and name the Track when the Plan has more than one. Spec: "Badges", "Nudges".

**Blocked by:** 02, 03.

**Status:** ready-for-agent

- [ ] `streak:<id>` gives the best run (weeks or days by cadence), `days:<id>` the total done days (daily) or counted Workouts (weekly), `bestweek:<id>` the best week of a weekly Track; case-insensitive.
- [ ] An unknown Track ID is "not measurable", like any unknown metric.
- [ ] `sessions` counts counted Workouts across all weekly Tracks; `streak` and `bestweek` refer to the main track; `prs` is unchanged. A single-Track Plan gives identical results to before.
- [ ] Per-Track Nudges: one short of target and Missed, each with the Track named only when there is more than one; no Nudge for daily Tracks in this stage; the existing "two at most" and Break-quiet behaviour is unchanged.
- [ ] `docs/data-spec.md` Badge-metric list updated.
