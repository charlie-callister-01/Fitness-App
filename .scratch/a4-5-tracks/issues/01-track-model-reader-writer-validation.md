# 01: Track model, reader/writer and validation

**What to build:** Tracks exist in the data format. A Plan can list named Tracks (weekly or daily), a Session can belong to one, and a Session can be of type `mobility` and, on a daily Track, have the default day `daily`. The real reader and writer accept, validate and round-trip all of it, with an exact path and code for every error, and `data-spec.md` documents it with an example the conformance test parses. A Plan with no Tracks reads, writes and round-trips exactly as before. Spec: `docs/specs/a4-5-tracks.md` ("Data model").

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] A Plan with `tracks` (weekly and daily), Sessions with `trackId`, the `mobility` type and `daily` default day, and a Track's own `phases` read through the real reader and write back identically.
- [ ] New code `unknown_track` (a `trackId` that is not a Track of the same Plan) is reported at the exact path.
- [ ] Each Track rule has a failing-input test with exact path and code: duplicate Track ID (across the document), weekly fields on a daily Track, `dayDonePercent` on a weekly Track or outside 1 to 100, a daily Session on a weekly Track or a weekly Session with `daily`, a daily Session without `daily`, a benchmark Session with a non-main `trackId`.
- [ ] Phase rules run per list: gaps and overlaps inside a Track's own Phases are `phase_gap` and `phase_overlap`; Phase IDs stay unique across the Plan's and every Track's Phases; an override naming a Session on another Track, or not governed by that list, is `unknown_session`.
- [ ] `Settings`, Backup and storage are untouched; an existing stored Plan reads as "no Tracks" with no migration; `formatVersion` stays 1.
- [ ] `docs/data-spec.md` gains the Track section, the Session/Phase/Badge-metric notes and the error code, plus a multi-Track Plan example parsed by the conformance test.
- [ ] The existing Plan, Workout and worked examples and all A1 to A4 tests pass unchanged.
