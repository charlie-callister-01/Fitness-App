# 09: Weekly review

Spec: `docs/specs/insight-b6-b8.md` (S-09, F-07). Stories 55-62.

**Status:** resolved

**Blocked by:** 05 Weight screen (weight view).

**What to build:** The Weekly review screen shows a verdict card ("On track", "Behind", "Target out of reach", "Target hit") with Workouts against target and the Streak; rows for Missed sessions, PRs this week, average weight against last week's, and Pain checks that week; and an **Export for the LLM** button preset to the last 7 days, Markdown, health data off. The owner can open any past week (select by its Monday); the default is the current week. A Break week says "On a Break" and is never called Behind. "Target hit" is added when counted Workouts reach the target. If the Export screen does not yet exist (B9), the button is hidden and a plain note is shown.

New pure Core weekly review view built on the existing consistency, Streak, Missed, PR, weight and Pain rules.

- [x] Core view has Core.Tests: each verdict, Target hit, Break week, past week, no Weigh-ins, no data, week boundaries
- [x] Page bUnit tests via `PageFixture`: each verdict, week selector, rows, export button hidden or present
- [x] Both Themes, both Modes, 360 px
- [x] `dotnet build` 0 warnings, `dotnet test` green; commit `Insight: ticket 09 Weekly review`
