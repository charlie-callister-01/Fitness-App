# 07: Steppers, Add Set and Max set

**What to build:** ConfirmBar steppers change the current Set's weight by the Plan's increment (lb when units are lb) and carry the new value down to later unconfirmed Sets of that Exercise. Add Set appends an extra row; an extra unconfirmed row can be removed. "Mark as Max set" flags the current Set, labelled on the row, and can be unmarked. All saved immediately. Step sizes by kind (5 s seconds, 1 rep bodyweight, hidden for time and distance) are wired here as a function of kind so ticket 10 only has to render those kinds. Spec: `docs/specs/b1-workout-log-core-input.md`; design C-06, §8.2.

**Blocked by:** 05.

**Status:** done

- [x] Stepper changes only the current row and carries down to unconfirmed later rows, not to confirmed ones
- [x] Increment comes from the Plan; lb display
- [x] Add and remove Set; Max set mark and unmark persisted on the Set
- [x] Stepper buttons have accessible names and 60 × 64 targets
- [x] bUnit page tests
