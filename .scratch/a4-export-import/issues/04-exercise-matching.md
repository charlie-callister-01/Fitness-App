# 04: Exercise matching and remapping

**What to build:** Spec "Import: Exercise matching". Match by ID, then normalised exact name, then near-match suggestions (Dice >= 0.75 or same word set, max 3), else new; `ExerciseMatch` result type; `ImportDecisions` Exercise choices; remap file IDs through every reference (Plan rows, goals, overrides, Workout planned/entries/swapped-from, Pain checks, Guides, Badge metric); log-kind difference ignored with a warning; deferred `unknown_exercise`/`unknown_session` checks with exact paths. Exposed through a preview entry point sufficient to test via the public surface.

**Blocked by:** 03.

**Status:** done

- [ ] Every matching case in the spec's Testing Decisions passes
- [ ] A near-match blocks readiness until decided; both choices produce correct data
