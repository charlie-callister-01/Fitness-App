# 02: Parse-then-validate split with standalone validator

**What to build:** Reading a document becomes two phases: a structural parse that collects only field errors, then a standalone pure validator over an `ExchangeDocument` that owns every cross-record rule. This removes the misleading errors the owner currently sees (a valid Workout flagged `unknown_session` because its Plan has an unrelated bad field; `no_active_plan` when `isActive` merely has the wrong type; `phase_gap` noise next to a Phase with a bad week range) and lets Import (A4) and the app validate in-memory documents without going through JSON. A skipped or swapped entry is also checked against the Workout's planned rows when a snapshot exists. Spec: `docs/specs/a1-review-fixes.md` (Part 2).

**Blocked by:** 01 (both rewrite the same reader files)

**Status:** done

- [x] Parse phase collects only structural errors (missing, wrong type, bad value, range, unknown enum, bad ID format, malformed JSON, version) and never does cross-record checks; invalid-but-parseable records (e.g. a Session with a bad field) keep their IDs visible to validation, replacing the "remember errored IDs" workaround and mutable per-Plan session ID state
- [x] Standalone pure validator over `ExchangeDocument` returns issues in the existing path/code/message shape and owns: ID uniqueness per kind; Exercise and Session references (only when the referenced collection is present); exactly one active Plan when Plans are present; Phase week coverage (contiguous, non-overlapping, starts at week 1, with an invalid-range Phase reported once and excluded from gap/overlap comparison); Guide-to-Exercise references; Workout `sessionId`; skipped/swapped entries against `planned` rows when a snapshot exists
- [x] Reader's public entry point runs parse then validate and returns the same `ReadResult`; `ExchangeFormat` exposes the validator directly for in-memory documents
- [x] New error code for skipped/swapped-not-in-planned documented in `docs/data-spec.md`; existing codes and paths unchanged
- [x] Tests (exact path and code): valid Workout with a Plan that has an unrelated error yields no `unknown_session`; Phase with a bad range yields its own error and no neighbour `phase_gap`; `isActive` wrong type yields no `no_active_plan`; skipped/swapped entry not in `planned` yields the new error and is accepted when `planned` is empty
- [x] Direct validator tests on in-memory documents, including one that never went through JSON
- [x] All Part 1 and pre-existing tests still pass
- [ ] Optional (not done): three test-data builders consolidated into one fluent builder if it reduces duplication; Plan/Settings streak/increment fields merged into a shared record only if trivial
