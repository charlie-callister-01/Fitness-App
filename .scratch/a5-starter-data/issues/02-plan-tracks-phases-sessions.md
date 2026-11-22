Status: resolved
Stage: A5
Blocked by: 01

# Ticket 02: Plan, Tracks, Phases, Sessions, Settings, and Sets × reps parsing

## Scope
- Build `Settings` from `Settings.Default` with `StreakMinWorkoutsPerWeek` (4), `StreakCountsCustomWorkouts` (`false`), `WeightIncrementKg` (2), `GoalWeightKg` (`null`).
- Build `Plan` (`hyrox-2027`, "HYROX London", active, start `2026-10-19`, goal `"HYROX London Doubles"`, goal date `2027-04-24`, weight increment 2 kg).
- Build explicit `Tracks`: `main` (weekly, target 5, streak min 4, custom off, Phases: Foundation 1–8, Build 9–19, HYROX prep 20–27) and `mobility` (daily, `DayDonePercent: 75`, no Phases).
- Build `Sessions` (`a-lower`, `b-upper`, `c-engine`, `d-full-body`, `e-long`, `benchmark` on `friday` with `benchmarkIntervalWeeks: 4`, `m-mobility` daily), dropping `W` and noting it.
- Implement explicit "Sets × reps" table parsing (ranges, `per side` cue append, duration/text cues with null reps, fail/note on unreadable).

## Acceptance
- `dotnet test` passes with 0 warnings.
- Plan, Tracks, Phases, Sessions, `W` omission, and all "Sets × reps" forms (plus failure on unreadable) are tested.
