# 01: Guarded save, adopted everywhere that saves

**Parent:** spec `docs/specs/editors-b4-b5.md` (issue #24), "Prefactor wave" and "Guarded save module".

**What to build:** one Ui module that owns a save: a second tap while saving does nothing, a read-only app refuses, the change is committed, a record that has disappeared is reported as "no longer there", and a failure goes to the existing failure notice. Callers pass a change and read a result (saved, refused read-only, refused busy, failed). Home, the Custom workout draft, the log (actions, finish and Logged afterwards flows), Theme saving, and the Workout and Exercise writers all use it, and their own copies of the busy flag, CanWrite check, try/finally and notice report are deleted. Behaviour seen by the owner does not change.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] The guarded save module exists and is tested once at its own interface over the in-memory store and the fault-injecting store: busy guard, read-only refusal, "no longer there", failure notice, success.
- [ ] Home, the Custom workout draft, the log, Theme saving and the writers use it; no page or flow keeps its own saving flag plus try/finally plus notice report.
- [ ] The `WhileSavingAsync`-style helpers in Home and the draft are gone.
- [ ] All existing Ui tests pass with only their copied-internals assertions changed; Core and Web tests unchanged.
- [ ] `dotnet build` has 0 warnings.
