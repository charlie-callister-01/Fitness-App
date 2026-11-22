Status: resolved
Stage: A5
Blocked by: 05

# Ticket 06: Single-document assembly, CLI, Restore & rules proof tests, real-folder check

## Scope
- Assemble the format-2 `ExchangeDocument` via `ExchangeFormat.Write` (deterministic, byte-identical on re-run).
- Implement CLI `Main` (`dotnet run --project tools/LegacyConverter -- <FitnessFolder> <outputFolder>`) writing `example-dataset.json` and `conversion-report.md`.
- Add Restore & rules proof tests: `ImportInput.Inspect`, `ImportPreviewer.Preview` (Restore), `ImportApplier.ApplyAsync` into `AppData` (`InMemoryDataStore`), `DocumentValidator`, idempotent second Restore, and Core rules assertions (daily mobility, main Track Streak, Phase position).
- Add opt-in real-folder test (skipped unless env var names the real folder) asserting 50 Exercises, 7 Sessions on 2 Tracks, 3 Phases, Guides, 25 Badges, 5 Workouts, 2+ Weigh-ins, and run it against `.scratch/a5-starter-data/legacy/phone-2026-11-19` + `Life Settings.md`.

## Acceptance
- `dotnet test` passes with 0 warnings.
- Opt-in real-folder test passes when pointed at the local legacy folder and generates `.scratch/a5-starter-data/out/example-dataset.json` and `conversion-report.md`.
