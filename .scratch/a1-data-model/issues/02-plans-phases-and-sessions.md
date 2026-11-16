# 02: Plans, Phases and Sessions

**What to build:** Plans, Phases and Sessions can be written and read through the same seam. A Plan holds start date, goal and goal date, weekly target, Streak rules, weight increment and an active flag. Phases have focus, week range, optional Phase goals and optional per-Session Exercise overrides. Sessions have name, default day, type, optional benchmark interval and ordered Exercise rows (sets, reps or range, cue). Validation names the exact field for: no or multiple active Plans, Phase week ranges overlapping or leaving gaps, Exercise rows pointing at unknown Exercise IDs, and out-of-range values. The data spec gains the matching sections.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] Spec sections and example for Plan, Phase, Session added; conformance test updated
- [ ] Round trip of a rich Plan (overrides, Phase goals, benchmark Session) is identical
- [ ] Each invalid case (active Plan count, Phase gap/overlap, dangling Exercise ID, bad ranges) yields the exact path and message
- [ ] Plan-only partial document is accepted
