Status: resolved
Stage: A5
Blocked by: 04

# Ticket 05: History (Weigh-ins, Logs, values, side plank 2.3, weights.csv)

## Scope
- Parse Weigh-ins from `Life Settings.md` (`113` kg on `2026-02-01`), `Log/*.md` `bodyweight`, and optional `weights.csv` (`date,kg`), deduplicating/ordering deterministically with IDs `w-yyyy-MM-dd`.
- Parse `Log/*.md` Workouts: skip empty shells (and note them); keep tick-only files with ticked `done:` items; set `loggedAfterwards: true`, IDs `w-yyyy-MM-dd-<session lower>`, `sessionId` absent for `W`, copy `planned` from the Plan's Session rows.
- Parse Exercise values (`12×10`, `35 × 10`, `bwx8`, `secs`, `mins` -> ×60, `km`, `m`, `m:ss`, side plank `2.3` -> 2 sets of 30s with note if another Exercise uses that shape) and `done:` list as empty `done` entries; handle `W` waist entry.

## Acceptance
- `dotnet test` passes with 0 warnings.
- All value shapes, `2.3` special case, empty keys/logs skipped, `W` file, `weights.csv`, and mobility Workout below 75% are tested.
