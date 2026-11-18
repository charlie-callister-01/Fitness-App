# 10: Remaining log kinds

**What to build:** Every other kind the data model stores, through the same SetRow grid and the same page, one scenario each: bodyweight × reps (fixed "bw" tag, add-weight option), carry (kg per hand × metres), seconds, minutes, reps only, time (`m:ss`, `108`, `1:04:10`), distance km, distance m, tick-only (one big toggle), body measurements (one wide input). Text-kind Exercises show a note that they cannot be logged yet (a Set cannot hold text) and never lose a value. A deleted Exercise or unsupported kind says what cannot be shown and never crashes. Input modes per design §8.1; steppers per ticket 07. PR rules per kind work through ticket 08. Spec: `docs/specs/b1-workout-log-core-input.md`.

**Blocked by:** 07, 09.

**Status:** ready-for-agent

- [ ] One bUnit scenario per kind: type in the right fields, Confirm, assert the stored Set in canonical units
- [ ] Correct `inputmode`, labels, stepper visibility and step size per kind
- [ ] Text note and unsupported or deleted Exercise handled without a crash
- [ ] Layout checked at 360 px for the two-input and wide-input rows
