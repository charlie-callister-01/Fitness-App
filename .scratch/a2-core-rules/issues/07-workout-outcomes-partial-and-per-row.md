# 07: Workout outcomes: Partial workout and per-row outcome

**What to build:** For any Workout the app can say, row by row, what happened to each planned Exercise (done, skipped with reason, swapped with its replacement, or left unrecorded and treated as skipped), whether the Workout is a Partial workout, and which entries were extras. Every screen reports outcomes from this one place. Spec: `docs/specs/a2-core-rules.md` (Outcomes stories 62, 63; "Outcomes" decisions).

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Per planned row, compare the Workout's `planned` snapshot with its entries: done, skipped (with reason), swapped (with the replacement Exercise), or no entry which counts as skipped with no reason
- [ ] An entry with no matching planned row is reported as an extra Exercise; a Custom workout (no snapshot) has only extras
- [ ] A Workout is a Partial workout when any planned row was skipped or unrecorded, and it still counts
- [ ] Unknown Exercise references are ignored rather than throwing
- [ ] Tests build Workouts with mixed done, skipped, swapped, unrecorded and extra rows and assert the exact outcome per row
