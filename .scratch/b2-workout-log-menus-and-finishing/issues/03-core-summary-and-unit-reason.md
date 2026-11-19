# 03: Core: Workout summary builder and unit-aware Suggested target reason

Parent spec: `docs/specs/b2-workout-log-menus-and-finishing.md` (Core: Workout summary builder, Suggested target reason in the display unit).

**What to build:** a pure rule that turns a finished Workout plus history into everything the finish summary shows: counts (Sets, PRs, done of planned), one PR per Exercise with its previous best (history excluding this Workout, reusing the per-Workout PR rules), every planned and extra Exercise's outcome from the outcomes rule with its RPE and reason or "was X", the Partial flag, the Pain check result as plain text ("Yes: Left knee, mild (during Goblet squat)", "No", "Not answered"), the Streak line from A2's Streak rule, any Badge newly unlocked by this Workout (Badge progress with and without it), and the duration (absent when logged afterwards or when a start or end is missing). Also fix the Suggested target reason so it is written in the user's display unit (lb users read "add 5 lb", not "add 2 kg"), closing the B1 open item.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] One PR per Exercise (the best Set), with previous best; first-ever result is a baseline, not a PR; Max set precedence respected
- [ ] Outcomes list planned order then extras; swapped shows the original; skipped shows its reason; RPE carried
- [ ] Streak line matches A2's Streak result including during a Break; a Badge unlocked only when this Workout crossed its target
- [ ] Duration is end minus start, and absent for Logged afterwards or missing times
- [ ] Pain check text covers none, reported (areas, severity, Exercise, side from the note) and unanswered
- [ ] Suggested target reason reads lb for lb users, kg for kg users; existing reason wording otherwise unchanged
- [ ] Core tests written first (show the red run in the report); all existing tests stay green
- [ ] No data model, exchange format or storage change; A2 rules are reused, not edited, apart from the unit change
