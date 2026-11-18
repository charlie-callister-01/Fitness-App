# 06: Exercise header (Last, Best, Suggested target) and RPE

**What to build:** ExerciseHeader (C-04) shows Last and Best on one line in the owner's units and the Suggested target in a sunken well with its one-line reason, using `ExercisePerformance.Snapshot` with the active Plan; nothing (and no empty labels) the first time an Exercise is done. RpeSelector (C-05): five toggles 6 to 10 per Exercise, `aria-pressed`, tapping the selected value clears it, never blocks Confirm, saved on the entry. The Exercise name is plain text (Guide link is B4); no Exercise menu (B2). Spec: `docs/specs/b1-workout-log-core-input.md`; design C-04, C-05.

**Blocked by:** 05.

**Status:** ready-for-agent

- [ ] Last, Best and target render for an Exercise with history, and are absent for a new Exercise
- [ ] kg and lb display
- [ ] RPE set and clear stored on the entry; the next Suggested target reflects it (A2 rules)
- [ ] Tests: header contents, `aria-pressed`, toggle-off, RPE never blocks Confirm
