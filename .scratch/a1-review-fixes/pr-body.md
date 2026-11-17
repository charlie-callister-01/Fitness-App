## Summary

A1 review fixes: the exchange-format reader now parses structure first, then validates cross-record rules in a standalone validator, on top of one shared read context. Specs: `docs/specs/a1-review-fixes.md`; tickets `.scratch/a1-review-fixes/issues/`.

```text
ExchangeFormat.Read(json)
  V1Reader                              # parse phase: structural errors only
    ReadContext (issue sink + ParseFindings)   # one per read, carried by FieldReader
    FieldReader.ReadRecord / ReadRecords<T>    # scoped: record or null, always warns unknown fields
    ...records keep their IDs even with bad fields (placeholders, noted in ParseFindings)
  DocumentValidator.Validate(document, findings)   # cross-record rules, pure
  ReadResult

ExchangeFormat.Validate(ExchangeDocument)          # same validator, for in-memory documents (Import A4, app)
```

```diff
 src/Core/Serialisation/
 ├── ReadContext.cs            # one context; per-reader context classes deleted
+├── DocumentValidator.cs      # ID uniqueness, Exercise/Session refs, one active Plan,
+│                             # Phase week coverage, Guide refs, planned-row checks
+├── PainCheckRules.cs         # Pain check status rules, extracted
+├── ExchangeReadMe.cs         # "how to read this" block
 ├── FieldReader*.cs           # TryAskFor -> TryTakeField, TryTakeRequired helper, CheckOrder
 └── *Reader.cs                # no errorsBefore counting, no null-as-mode reference sets
```

What changed and why:

- **One ID rule, one scoped helper, one context.** The `(reader, field, sink, exerciseIds)` clump is gone, so A2 to A4 readers don't copy it. Reference checking is a validator concern, not a nullable-set mode in every reader.
- **Parse-then-validate.** Fixes three wrong errors the owner would have seen: a valid Workout getting `unknown_session` because its Plan had an unrelated bad field; `no_active_plan` when `isActive` merely had the wrong type; and `phase_gap` noise beside a Phase with a bad week range. A bad-range Phase now gets its own single error and is left out of gap/overlap only for itself, so a real gap or overlap between valid Phases is still reported.
- **New check:** a skipped or swapped entry must reference a planned row when `planned` is non-empty (`not_in_planned`, documented in `docs/data-spec.md`). Empty `planned` (a Custom workout) is accepted.
- **`readMe` block** written after `formatVersion`; the reader ignores it with no warning.
- **Culture:** decimals round-trip under de-DE.

Non-obvious decisions:

- The parse phase marks unreadable fields in a typed `ParseFindings` (which Phases had unreadable weeks; whether any `isActive` failed to read) instead of the validator or reader matching on path/code strings. The validator treats blank-ID placeholders as unset.
- Week-range rule (`endWeek` not before `startWeek`) lives only in the validator; the parse phase only checks each week is a whole number >= 1.
- `ExchangeFormat.Write` copies the serialised `JsonElement` straight to the writer to insert `readMe`, so numbers are written exactly as before (no `JsonNode` round trip).
- Error order changed: parse errors first, then cross-record errors in document order.
- Skipped on purpose: declarative `Field<T>` / `Result<T>` rewrite (out of scope in the spec, revisit before A4), the optional test-builder consolidation, and merging the Plan/Settings streak fields.
- Reviews: `mattpocock-skills:code-review` (Standards and Spec) and the thermo-nuclear review, both applied (fixes in `be0c1ae` and `3d9660b`).

## Evidence

- **Before:** 133 Core tests; the three wrong errors above reproduce.
  **After:** `dotnet test`: Core.Tests 166 passed, Ui.Tests 9 passed, 0 failed. All 133 earlier tests pass unchanged.

```text
# now passing (exact path + code asserted)
workout with sessionId "a-lower", plan has unrelated bad field   -> no unknown_session
plans[0].isActive = "yes"                                        -> wrong_type only, no no_active_plan
phases: [1-8 ok, 9-3 bad range, 12-19 ok]                        -> one out_of_range at the bad phase;
                                                                    the 8->12 gap is still reported
entry skipped/swapped on an exercise not in non-empty planned    -> not_in_planned
same entry with planned empty                                    -> accepted
Write -> Read under de-DE                                        -> 84.35 stays 84.35
```

## Merge Danger

**Door:** two-way

Behaviour change is limited to error reporting plus one additive output field; no stored data or released format exists yet.

**Blast Radius:** Core serialisation

Exported JSON now contains a `readMe` object, which an older reader would flag as an unknown field (a warning, not an error); nothing has shipped, so no real consumers are affected. Error order changed (parse errors before cross-record errors), so anything snapshotting error lists would need updating. A3 to A5 build on `ExchangeFormat.Validate` and the read context.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
