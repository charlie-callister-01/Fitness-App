# 09: Pager, Next and temporary Finish

**What to build:** The Exercise pager of numbered chips with a tick on complete Exercises and jumping between Exercises without losing anything; the "Up next" strip in the ConfirmBar; primary label **Confirm Set** while Sets remain, **Next: <Exercise>** when this Exercise is complete, **Finish** after the last Exercise. Finish is temporary and minimal: stamp `EndedAt` = now, save, return Home (no Partial prompt, Pain check or summary; B2 replaces it wholesale). Home then shows Start again. Back keeps the Workout in progress. Spec: `docs/specs/b1-workout-log-core-input.md` ("Finish in B1 is minimal and temporary").

**Blocked by:** 05.

**Status:** ready-for-agent

- [ ] Chips, ticks, jump and return preserve entered Sets; resume lands on first incomplete Exercise
- [ ] Label changes Confirm Set, then Next: X, then Finish
- [ ] Finish stamps end time once (double tap does not stamp twice), returns Home; Home no longer offers Continue
- [ ] Code note naming B2 as the replacement
- [ ] bUnit tests
