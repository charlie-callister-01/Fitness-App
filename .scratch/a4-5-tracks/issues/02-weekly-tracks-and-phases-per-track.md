# 02: Weekly Tracks and Phases per Track

**What to build:** Every rule that was "per Plan" for a weekly rhythm now works per Track. A Session resolves to its Track (the main track when it has no `trackId`), and each weekly Track has its own counted Workouts, week progress, Streak, best week and consistency message. Current Phase, "week N of M", "move to next Phase" and Phase goals use the Track's own Phases or, when it has none, the Plan's. Week schedule moves and week rearrangement apply to weekly Tracks only. Spec: "Rules (A2 change)".

**Blocked by:** 01.

**Status:** ready-for-agent

- [ ] A `TrackRules` resolver gives, for a Plan and a Session, its Track, cadence and effective Phases; every A2 rule that took the Plan for target, Streak or Phase takes a Track.
- [ ] Two weekly Tracks are counted separately: a Workout counts only for its Session's Track; Streak, best week and consistency messages are per Track.
- [ ] Custom workouts count for the main track only, and only when `streakCountsCustomWorkouts` allows it.
- [ ] A Track with its own Phases uses them; one without falls back to the Plan's; "week N of M", "move to next Phase" and Phase goals follow the right list.
- [ ] A week schedule move of a daily Session is `invalid_value`; week rearrangement works per weekly Track and never moves daily Sessions.
- [ ] A Plan with no Tracks gives identical results to before: the existing A2 tests pass unchanged.
- [ ] Test through the public rule entry points with injected time; no assertions on the resolver directly.
