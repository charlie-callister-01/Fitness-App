# 03: Workouts, Sets and Pain checks

**What to build:** Workouts can be written and read through the seam: date, optional Session (none for a Custom workout), a snapshot of what was planned at the time, logged-afterwards flag, start and end times, and per-Exercise entries with status done, skipped (optional reason) or swapped (original recorded), RPE and Sets. A Set holds weight (kg), reps, distance, duration and a Max set flag, all in canonical units. A Pain check is unanswered, none, or reported (body areas, severity mild/moderate/sharp, optional note, optional Exercise). Validation catches dangling Session/Exercise references, negative weight, zero reps, RPE outside 1 to 10, and unknown enum values, with exact paths. Spec gains the matching sections.

**Blocked by:** 01, 02

**Status:** ready-for-agent

- [ ] Spec sections and example for Workout, Set, Pain check added; conformance test updated
- [ ] Round trip covers back-dated and Custom workouts, all three Pain check states, swapped and skipped entries, Max sets
- [ ] Planned snapshot is stored independently so later Plan edits cannot alter it
- [ ] Invalid cases yield exact paths and messages
