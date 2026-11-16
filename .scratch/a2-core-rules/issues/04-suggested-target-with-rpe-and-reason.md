# 04: Suggested target with RPE and reason

**What to build:** For each Exercise the log screen can show a Suggested target (weight and reps, or the kind's equivalent) built from the last Workout where it was done and that Workout's RPE, always with a one-line reason. Spec: `docs/specs/a2-core-rules.md` (Suggested target stories 32 to 43; named assumptions 2 to 4 in Further Notes).

**Blocked by:** 03 (Best, PRs, Max sets and progress points)

**Status:** ready-for-agent

- [ ] Load: add one rep until the top of the planned rep range, then add the Plan's weight increment and drop reps by 2
- [ ] Carry: add 10 m up to 40 m, then add weight (distance reset per the spec's assumption); time: beat best; other numeric kinds: best plus a per-kind step
- [ ] RPE 9 or 10 on the last Workout holds the same weight and reps; RPE 6 or below gives a bigger jump; no RPE gives normal progression
- [ ] Every target carries a one-line reason that names what drove it
- [ ] No history means no target (never a made-up number); the target uses only the last Workout where the Exercise was actually done, so a skip does not reset it
- [ ] The weight increment comes from the active Plan, and the first row for the Exercise in Plan order defines its kind and rep range
- [ ] The per-Exercise snapshot from ticket 03 now includes the Suggested target
- [ ] Tests assert the exact target and reason for each rule, including top of range, carry at 40 m, RPE 9 hold and RPE 6 jump
