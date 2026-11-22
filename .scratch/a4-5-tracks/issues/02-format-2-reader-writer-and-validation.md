# 02: Format 2 reader, writer and validation (PR 1)

**Read first:** `.scratch/a4-5-tracks/HANDOFF.md`, `docs/specs/a4-5-tracks.md` ("Data model", "Versioning and upgrade"), `docs/data-spec.md` (Track, Session, Phase, Error codes), `src/Core/Serialisation/` (`ExchangeFormat`, `V1Reader`, `PlanReader`, `SessionRowReader`, `DocumentValidator`, `WeekScheduleReader`, `ExchangeReadMe`).

**Parent:** spec `docs/specs/a4-5-tracks.md`, PR 1.

**What to build:** exchange `formatVersion` 2 end to end.
- `ExchangeFormat.CurrentVersion` is 2. A v2 reader (reusing the v1 reader's shared field readers; do not copy them) reads `tracks`, `Session.trackId`, the `mobility` type, `defaultDay: daily`, `dayDonePercent` and a Track's `phases`. The v1 reader stays, goes through the upgrader from ticket 01, and is otherwise unchanged. The writer writes format 2 and the `readMe` block describes Tracks.
- Validation with an exact `path` and `code` for each rule (the data-spec Track section is the source): no `tracks` or none (`missing_field`), main Track not weekly (`invalid_value` at `tracks[0].cadence`), unknown cadence (`unknown_value`), duplicate Track ID across the document (`duplicate_id`), weekly fields on a daily Track and `dayDonePercent` on a weekly Track (`invalid_value`), `dayDonePercent` outside 1 to 100 (`out_of_range`), missing `trackId` (`missing_field`), `unknown_track` (new), `daily` on a weekly Track's Session and a daily Track's Session without `daily` (`invalid_value`), a benchmark Session off the main Track (`invalid_value`), Phase IDs unique across all Tracks, `phase_gap` and `phase_overlap` per Track (call the shared `PhaseRanges.Check` once per Track), a Phase override naming a Session of another Track (`unknown_session`), a Week schedule move of a daily Session (`invalid_value`).
- Plan-level v1 fields in a v2 Plan are unknown fields (a warning).
- Update `docs/data-spec.md` where the code differs from what this spec commit wrote (the Track section, the v2 Plan example with a daily Track, the error codes) and make the conformance test parse every JSON block in it, including the two-Track Plan example.
- Remove the temporary v1 writer from ticket 01. Keep the Plan, Workout and worked-example round trips; they are now format 2.

**Blocked by:** 01.

**Status:** ready-for-agent

- [ ] A Plan with a weekly main Track and a daily Track (own Phases, daily Session with `daily`, `mobility` type) reads through the real reader and writes back identically.
- [ ] Each rule above has a failing-input test with exact `path` and `code`; `unknown_track` is reported at the Session's `trackId`.
- [ ] A v1 document and a v1 Backup still read (upgraded to one Main Track) and give the same Plans' values as before.
- [ ] `formatVersion` 3 is `unsupported_version`; the message names the current version.
- [ ] `docs/data-spec.md` matches the code and its examples are parsed by the conformance test.
- [ ] `dotnet build` 0 warnings; all earlier tests pass.
