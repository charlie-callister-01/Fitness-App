# 04: Weekly Tracks, Phases and schedule per Track (PR 1)

**Read first:** `.scratch/a4-5-tracks/HANDOFF.md`, `docs/specs/a4-5-tracks.md` ("Rules (A2 change)"), `docs/specs/a2-core-rules.md` (weeks, Streak, Phases, schedule, rearrangement), `src/Core/Rules/Weeks/WeekRules.cs`, `Rules/Phases/PhaseRules.cs`, `Rules/Schedule/*`, `Rules/Rearrangement/WeekRearrangement.cs`, `Rules/CustomWorkout/CustomWorkoutOffer.cs`, `Rules/Starting/WorkoutStart.cs`, `Rules/Library/LibraryViewBuilder.cs`.

**Parent:** spec `docs/specs/a4-5-tracks.md`, PR 1.

**What to build:** every rule that read "the Plan's weekly rhythm" reads a **weekly Track** instead.
- `WeekRules` (counted Workouts, by week, Streak, best week, consistency message, plan week number) take a weekly Track. A Workout counts for the Track of its Session; a Custom workout (no Session) counts for the **main Track only**, only when that Track's `StreakCountsCustomWorkouts` is set. Breaks excuse every Track.
- `PhaseRules` (position, current Phase, "week N of M", exercise rows, goal readiness, move to next Phase) take a Track and use that Track's `Phases`. A Track with none has no current Phase. Week numbering is Plan-wide.
- `WeekScheduleRules` (layout, placement, Missed) and `WeekRearrangement` work per weekly Track over that Track's Sessions; a daily Session is never placed as outstanding, never Missed and never moved by a suggestion.
- `CustomWorkoutOffer`, `WorkoutStart` (a Workout for a Session of any Track; benchmark pre-fill stays main Track) and `LibraryViewBuilder` (group by Session; names the Track when the Plan has more than one) are made Track-aware.
- `BenchmarkWeeks` and the benchmark rules read the main Track's Sessions only.
- Existing callers (Home, Week, Nudges, Badges) keep calling with the main Track until their own tickets; the screens do not change.
- Public entry points, injected time, no assertions on helpers.

**Blocked by:** 01.

**Status:** ready-for-agent

- [ ] Two weekly Tracks are counted separately: a Workout counts only for its Session's Track; Streak, best week and consistency are per Track.
- [ ] A Custom workout counts for the main Track only and only when its setting allows it.
- [ ] A Track with its own Phases uses them; a Track with none has no current Phase; "week N of M", "move to next Phase" and goals follow the right list.
- [ ] Rearrangement runs per weekly Track and never moves or marks Missed a daily Session.
- [ ] A one-Track Plan gives results identical to before (the existing A2 and B3 tests pass unchanged).
- [ ] `dotnet build` 0 warnings; Core tests green.
