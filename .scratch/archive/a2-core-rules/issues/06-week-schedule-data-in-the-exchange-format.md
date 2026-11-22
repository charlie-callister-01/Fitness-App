# 06: Week schedule data in the exchange format

**What to build:** A one-week-only move of a Session to another day can be stored in the data and survives Backup and restore. The exchange document gains an optional Week schedule collection, written, read and validated like the other collections, with no format version bump. Spec: `docs/specs/a2-core-rules.md` (Week schedule stories 70, 71; "Week schedule and rearrangement" decisions).

**Blocked by:** None (can start immediately), but it must start from an integration branch that already contains the A1 review fixes (shared read context and the standalone `DocumentValidator`).

**Status:** done

- [x] Model, writer and reader carry an optional collection of Week schedules, each keyed by a week's Monday date and listing Session moves (Session ID to a date inside that week)
- [x] Absent means none; present-but-empty means none; round trip is lossless
- [x] The validator reports, with exact path and code: an unknown Session, a move date outside the keyed week, a key that is not a Monday, duplicate weeks, and two moves of the same Session in one week
- [x] Unknown-field warnings and ID/uniqueness rules follow the existing conventions
- [x] `docs/data-spec.md` documents the collection with an example parsed by the real reader in the conformance test, and the version number is not bumped
- [x] Tests through the exchange-format seam, including a document that never went through JSON
