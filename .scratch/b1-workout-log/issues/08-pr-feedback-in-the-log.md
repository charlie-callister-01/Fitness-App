# 08: PR feedback in the log

**What to build:** Using ticket 02's rule, a confirmed Set that strictly beats the previous Best shows the PR marker (C-03: shield in Keep, "PR!" block in Clean), the reps input label gains ", PR", a short toast "PR! 22 kg × 9 beats 22 × 8" appears (static, reduced-motion safe), and the top bar shows the PR count. The marker moves, clears or updates immediately when a Set is edited, reopened or unmarked as Max. Derived on every render, nothing stored. Spec: `docs/specs/b1-workout-log-core-input.md`; design C-03, §8.3.

**Blocked by:** 02, 06.

**Status:** done

- [x] First ever result shows no PR; a strictly better Set shows marker, label, toast, count
- [x] Marker moves to a later better Set; editing a PR Set down clears it
- [x] Max set precedence reflected (page tests with seeded IsMax; Max marking UI arrives with 07)
- [ ] Time (lower is better): rule covered in Core (ticket 02); the page cannot log time Exercises yet, so no page test
- [x] PR is announced in words (never colour or shape alone)
- [x] bUnit tests
