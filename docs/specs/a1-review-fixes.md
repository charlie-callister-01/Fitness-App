---
labels: ready-for-agent
stage: A1 (review fixes)
---

# A1 review fixes: shared read context, parse-then-validate split

Source: `/mattpocock-skills:code-review` (Standards and Spec axes) and the thermo-nuclear code quality review of the A1 stage, run on the integration branch against `origin/main`. Parent spec: `docs/specs/a1-data-model-and-spec.md`. All behaviour-preserving unless stated; the existing 133 Core tests must stay green.

## Problem Statement

A1 works and is tested, but both reviews found that the reader plumbing will not scale into A2 to A4. Each collection reader rebuilds its own context and carries the same `(reader, field, sink, exerciseIds)` clump; `HashSet<string>?` is used as an on/off mode with null guards copied across readers; ID validation exists twice; about 15 sites hand-count errors with `errorsBefore`; and reference checks that run mid-parse produce wrong errors (a valid Workout gets `unknown_session` because its Plan has an unrelated error; a Phase with a bad week range disappears from the gap/overlap check). The spec also asks for a "how to read this" header and a standalone validation function, neither of which exists.

## Solution

Two ordered changes, so each fits one agent:

1. **Plumbing refactor** (no behaviour change except the header): one shared read context, a typed reference scope, one canonical ID check, a scoped error-collecting helper, and small cleanups.
2. **Parse-then-validate split**: a structural parse that collects only field errors, followed by a standalone validator over the parsed document for cross-record rules. This makes the known wrong-error bugs structurally impossible and provides the standalone validation function.

## User Stories

1. As a developer adding a new reader in A2 to A4, I want one shared read context, so that I don't copy per-reader plumbing again.
2. As a developer, I want reference checking to be an explicit typed scope (unchecked or known set), so that null is not doing the job of a mode flag.
3. As a developer, I want ID validity defined in exactly one place, so that the rule cannot drift between readers.
4. As a developer, I want a scoped helper that returns a record or nothing when it produced errors, so that I don't hand-count errors and can't forget the unknown-field warning.
5. As the owner, I want a valid Workout never to get `unknown_session` just because its Plan has an unrelated bad field, so that error messages point at the real problem.
6. As the owner, I want a Phase with a bad week range to produce its own error without causing misleading `phase_gap` errors on its neighbours.
7. As the owner, I want a Plan whose `isActive` has a wrong type to report that error only, not also `no_active_plan`.
8. As the owner, I want exported JSON to include a short "how to read this" block, so that an LLM given the file understands the format.
9. As a developer, I want to validate an in-memory `ExchangeDocument` without going through JSON, so that Import (A4) and the app can check documents they build.
10. As the owner, I want a skipped or swapped entry checked against the Workout's planned rows when a snapshot exists, so that a swap never references an Exercise that was not planned.
11. As the owner, I want reading and writing of decimals to be independent of the device culture, so that a comma-decimal locale cannot corrupt Backups.
12. As a developer, I want a standalone validator with its own direct tests, so that cross-record rules are tested without building JSON.

## Implementation Decisions

### Part 1: plumbing (behaviour-preserving except item 8)

- A single read context is built once at the top of the reader and holds the issue sink, typed reference scopes for Exercise and Session IDs, and unique-ID tracking per record kind. The per-reader private context classes are deleted. The field reader carries the context so field and sink parameters disappear.
- A small reference-scope type replaces nullable sets: either unchecked (reference checking off, e.g. a Plan-only document) or known (a set of IDs), with one check operation that reports the unknown-reference error. No call site branches on null.
- Exercise and Session reference reading become single calls on the field reader; pass-through wrappers are deleted.
- One canonical ID validation (pattern and message) used by every reader, including optional IDs and reference IDs. The hand-rolled Workout ID check is removed.
- A scoped helper replaces every `errorsBefore` pattern: it runs a record-reading function, returns the record or null if new errors appeared, and always calls the unknown-field warning at scope end. Optional-int reading stops diffing error counts.
- Small cleanups: simplify and rename the field-lookup helper whose name hides a side effect; remove the stale parameter doc comment; extract the Pain check status rules into their own small validator; move the `end_before_start` timestamp check beside the timestamp reader; tidy dense inline ternaries in the whole-dataset tests; add a flow-overview comment on the top-level reader.
- **"How to read this" header:** the writer emits a short read-me block describing the format (pointer to the version, units, ID rules). The reader accepts it without a warning and ignores it. It is defined in the data spec.

### Part 2: parse-then-validate (blocked by Part 1)

- **Parse phase:** produces the model and collects only structural errors (missing field, wrong type, bad value, out of range, unknown enum, bad ID format, malformed JSON, version). It never performs cross-record checks. It must keep enough shape for invalid-but-parseable records (for example a Session with a bad field) so their IDs are still visible to validation; this removes the "remember errored IDs" workaround and the mutable per-Plan session ID state.
- **Validate phase:** a standalone, pure validator over an `ExchangeDocument` returning issues with the same path/code/message shape. It owns every cross-record rule: ID uniqueness per kind, Exercise and Session references (checked only when the referenced collection is present in the document), exactly one active Plan when Plans are present, Phase week coverage (contiguous, non-overlapping, starting at week 1; a Phase with an invalid range is reported once and excluded from gap/overlap comparisons rather than causing noise), Guide-to-Exercise references, Workout `sessionId`, and skipped/swapped entries against `planned` rows when a snapshot exists (new error code, documented in the data spec).
- The reader's public entry point runs parse then validate and returns the same `ReadResult`; `ExchangeFormat` also exposes the validator directly for in-memory documents. Error codes and paths already in the data spec are unchanged; the new code is documented there.
- The only intended behaviour changes: the spurious `unknown_session` and `no_active_plan` errors and the misleading `phase_gap` noise disappear, and the skipped/swapped planned-row check is added.

## Testing Decisions

- Same seam as A1: the exchange-format reader. Existing tests are the safety net for Part 1 and must pass unchanged (apart from helper renames in test builders).
- New tests assert external behaviour only: exact path and code for each case. Cases: header written and read without warning; decimals round-trip under a comma-decimal culture (for example de-DE); valid Workout with a Plan that has an unrelated error yields no `unknown_session`; Phase with a bad range yields its own error and no neighbour `phase_gap`; `isActive` wrong type yields no `no_active_plan`; skipped/swapped entry not in `planned` yields the new error and is accepted when `planned` is empty.
- Part 2 adds direct tests of the standalone validator on in-memory documents (including a document that never went through JSON).
- Optional: consolidate the three test-data builders into one fluent builder if it reduces duplication.
- Prior art: the A1 tests in `Core.Tests/Serialisation`.

## Out of Scope

- Declarative field specs / a generic `Field<T>` mechanism and a `Result<T>` contract replacing sentinel defaults. The thermo-nuclear review suggested it (est. -150 lines) but it is a larger rewrite of the field reader; revisit before A4 if the readers keep growing.
- Merging the Plan and Settings streak/increment fields into a shared record (check during Part 2; only if trivial).
- Any A2 to A5 work, UI, or storage.

## Further Notes

- **Suggested tickets (each one agent, sequential):** (1) Plumbing refactor Part 1 items, including the header and culture test; (2) parse-then-validate split with the standalone validator and the bug-fix tests. Ticket 2 is blocked by ticket 1 because both rewrite the same files.
- Review sources: the Standards and Spec reports and the thermo-nuclear report from this session; the verdict was "approve with required refactor", with findings 1 to 4 landing before A2.
- A first attempt at Part 1 was stopped before merging (usage), so nothing from it is on the integration branch.
- Run `/mattpocock-skills:code-review` (not the built-in) again after these land, then `/pr`.
