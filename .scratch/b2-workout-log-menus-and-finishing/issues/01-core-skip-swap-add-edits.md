# 01: Core: skip, swap and add edits, benchmark helper, similar-Exercise ranking

Parent spec: `docs/specs/b2-workout-log-menus-and-finishing.md` (Core: pure Workout edits, Similar Exercises).

**What to build:** the pure Core rules the Exercise menu needs, as functions returning a new Workout (no I/O, no UI), next to B1's Workout edits. Skip an Exercise with an optional reason (removes its Sets and RPE) and un-skip it. Swap a planned Exercise for another (same position, a swapped entry naming the original, planned snapshot untouched) and undo a swap. Add an Exercise after a given position and remove an added Exercise that has no Sets. A helper that produces the Exercise record with its benchmark flag toggled. A ranking of library Exercises against a given Exercise by shared tags (muscle group, equipment, movement pattern), highest first, stable by name, excluding Exercises already in the Workout, untagged Exercises last. Reuse A2's outcomes rule for "extras" (added) rather than storing a new marker.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Skip stores a skipped entry with the optional reason and no Sets or RPE; skipping an Exercise with confirmed Sets replaces them; un-skip returns it to not-yet-done
- [ ] Swap produces a swapped entry for the new Exercise naming the original; the planned rows are unchanged; undo restores the original; swapping to an Exercise already in the Workout is refused
- [ ] Add inserts after the given Exercise and is reported as an extra by the outcomes rule; remove refuses an added Exercise that has Sets
- [ ] Benchmark helper flips only the flag
- [ ] Similar ranking: more shared tags first, ties by name, Workout members excluded, untagged last, no-tag current Exercise gives plain alphabetical order
- [ ] Core tests written first (show the red run in the report) in the style of the existing edit and outcomes tests, reusing the test-data builders; all existing tests stay green
- [ ] No data model, exchange format or storage change
