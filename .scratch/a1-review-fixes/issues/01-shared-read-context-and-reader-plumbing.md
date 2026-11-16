# 01: Shared read context and reader plumbing cleanup

**What to build:** Exchange-format readers share one read context instead of each carrying its own plumbing, so a developer adding a reader in A2 to A4 doesn't copy the `(reader, field, sink, exerciseIds)` clump again. Reference checking becomes an explicit typed scope (unchecked or a known ID set) rather than a nullable set. ID validity lives in one place. A scoped helper replaces every hand-counted `errorsBefore` pattern and always emits the unknown-field warning. Exported JSON gains a short "how to read this" block that the reader accepts silently. Behaviour is otherwise unchanged; the existing 133 Core tests stay green. Spec: `docs/specs/a1-review-fixes.md` (Part 1).

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] One read context built once at the top of the reader holds the issue sink, typed Exercise and Session reference scopes, and unique-ID tracking per record kind; per-reader private context classes are deleted and the field reader carries the context (no separate field/sink parameters)
- [ ] Reference scope type is either unchecked or known-set with a single check operation reporting the unknown-reference error; no call site branches on null
- [ ] Exercise and Session reference reading are single calls on the field reader; pass-through wrappers deleted
- [ ] One canonical ID validation (pattern and message) used by every reader, including optional and reference IDs; the hand-rolled Workout ID check is removed
- [ ] Scoped helper runs a record-reader, returns the record or null when new errors appeared, and always calls the unknown-field warning at scope end; all `errorsBefore` sites and the optional-int error-count diff are gone
- [ ] Small cleanups done: field-lookup helper simplified and renamed (no hidden side effect), stale parameter doc comment removed, Pain check status rules extracted into their own small validator, `end_before_start` check moved beside the timestamp reader, dense ternaries in whole-dataset tests tidied, flow-overview comment on the top-level reader
- [ ] Writer emits the "how to read this" block (version pointer, units, ID rules); reader accepts and ignores it with no warning; defined in `docs/data-spec.md`
- [ ] New tests assert exact path and code: header written and read with no warning; decimals round-trip under a comma-decimal culture (e.g. de-DE)
- [ ] All pre-existing Core tests pass unchanged (apart from test-builder helper renames)
