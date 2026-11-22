# 01: Track model and the mechanical ripple (PR 1)

**Read first:** `.scratch/a4-5-tracks/HANDOFF.md`, then `docs/specs/a4-5-tracks.md` ("Data model"), `docs/adr/0007-explicit-tracks-and-format-version-2.md`, `src/Core/Model/Plan.cs`, `src/Core/Serialisation/PlanReader.cs`.

**Parent:** spec `docs/specs/a4-5-tracks.md`, PR 1, "Data model".

**What to build:** the Core model holds explicit Tracks and the whole solution compiles and passes again with no behaviour change. This is a deliberately mechanical ticket.
- `Core.Model`: add `Track` (`Id`, `Name`, its rhythm, `Phases`), where the rhythm is one value that is either weekly (`WeeklyTarget`, `StreakMinWorkoutsPerWeek`, `StreakCountsCustomWorkouts`) or daily (`DayDonePercent`), so no state is representable that the format forbids (no bag of nullable fields). `Plan` loses `WeeklyTarget`, `StreakMinWorkoutsPerWeek`, `StreakCountsCustomWorkouts` and `Phases` and gains `Tracks`. `Session` gains `TrackId`. `SessionType` gains `Mobility`. The default-day value gains `Daily` (the `Session.DefaultDay` type changes from `DayOfWeek` to a small type or enum that can hold `Daily`; pick the one that keeps weekly code free of casts).
- A tiny `TrackRules` (or similar) gives `Main(plan)` (the first Track), `TrackOf(plan, session)` and `SessionsOf(plan, track)`. Nothing else is added.
- A **v1-to-v2 upgrader** in Core (`Serialisation`): takes the data a v1 Plan carried and returns the v2 `Plan` with one weekly Track named "Main", ID `<planId>-main`, holding the old target, Streak fields and Phases, and every Session pointing at it. The existing v1 reader calls it, so v1 documents already produce the new model.
- The writer still writes **format 1** in this ticket, taken from the main Track, so every existing round-trip test keeps passing. This is temporary; ticket 02 replaces it with the real format 2 writer and reader. Extra Tracks are not written yet.
- Every caller (rules, Home, Exchange, Ui pages) reads the main Track where it read the Plan's old fields. No rule changes behaviour; per-Track rules are tickets 04 onward. Adjust the Ui minimally to compile (Plan editor Settings and Phases keep editing the main Track).
- Tests: add one shared test builder for Plans (a Plan with a main Track and chosen Sessions) and move existing tests onto it. Add tests for the upgrader (a v1 Plan with Phases and Sessions becomes one Main Track with identical values; two Plans get distinct Track IDs; Session count and order kept).

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] `Plan` has no weekly target, Streak fields or Phases; `Track` and `Session.TrackId` exist; the rhythm cannot be built in an impossible state.
- [ ] A v1 document reads into one Main Track (`<planId>-main`) through the upgrader; the data-spec v1 Plan example still reads.
- [ ] `dotnet build` has 0 warnings; every existing Core, Ui and Web test passes with only mechanical fixture changes (a test whose expected result changes is a finding, written in this file under Notes, not loosened).
- [ ] No rule gains per-Track behaviour yet; no screen shows Tracks.
