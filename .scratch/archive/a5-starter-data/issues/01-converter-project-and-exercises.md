Status: resolved
Stage: A5
Blocked by: none

# Ticket 01: Converter project, LegacyFolder, config parsing, IDs, and Exercises

## Scope
- Add `.scratch/a5-starter-data/out/` to `.gitignore`.
- Create `tools/LegacyConverter` (console, .NET 10, references `Core` only) and `tools/LegacyConverter.Tests` (xUnit, references `LegacyConverter` and `Core`), and add both to `FitnessApp.sln`.
- Define `LegacyFolder` (in-memory file names and text), `ConversionNote`, `ConversionResult`, and the public entry point `LegacyConverter.Convert(LegacyFolder)`.
- Parse `Config.md` settings and markdown tables (`Phases`, `Sessions`, `Exercises`, `Badges`).
- Implement the lowercase-hyphen ID function (pinned by a golden list in tests), the `LogKind` map (`load`, `carry`, `reps`, `secs`, `mins`, `km`, `m`, `time`, `kg`, `cm`, `text`, `-`), benchmark flags from the `T` session, and the 3 retired Exercises (`Chest-supported row`, `Farmers carry`, `Incline DB press`) while dropping `Bodyweight`.

## Acceptance
- `dotnet test` passes with 0 warnings.
- ID function, log-kind mapping (including failure on unknown kind), benchmark flags, and retired Exercises are covered by unit tests.
