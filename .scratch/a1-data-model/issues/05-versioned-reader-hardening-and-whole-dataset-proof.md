# 05: Versioned reader hardening and whole-dataset proof

**What to build:** The reader dispatches on format version so a later version bump only needs a migration from N to N+1; unknown newer versions are rejected with a message to update the app. Unknown extra fields in a known version produce warnings, not errors. A rich dataset touching every entity and edge case (unicode names, decimal numbers, every enum value) round-trips losslessly, and cross-entity references validate across the whole document. The data spec is finalised and its full example passes conformance.

**Blocked by:** 02, 03, 04

**Status:** ready-for-agent

- [ ] Version dispatch structure in place with version 1 reader; newer/missing/zero versions rejected clearly
- [ ] Unknown fields yield warnings with paths; document still accepted
- [ ] Whole-dataset round trip test passes
- [ ] Final spec example parsed by the real reader; spec and README index link updated
