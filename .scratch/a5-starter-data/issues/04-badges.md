Status: resolved
Stage: A5
Blocked by: 03

# Ticket 04: Badges

## Scope
- Map 25 of 26 Badges from `Config.md`: `sessions`, `best week`, `streak`, `prs`, `mobility streak` (`streak:mobility`), `mobility days` (`days:mobility`), and Exercise-metric Badges in canonical units (`Longest continuous run` 20 min -> 1200 s, `SkiErg 1000 m` 4:30 -> 270 s, 4:00 -> 240 s, `Waist` -3 / -6, etc.).
- Drop `Bodyweight deadlift` (non-numeric `bodyweight` target) and record a `ConversionNote`.

## Acceptance
- `dotnet test` passes with 0 warnings.
- All mapped Badge metrics/targets, Track metric references, and the dropped Badge note are tested.
