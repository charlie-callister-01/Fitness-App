# 08: Plan editor Sessions tab

**Read first:** `.scratch/editors/HANDOFF.md`, then the spec.

**Parent:** spec `docs/specs/editors-b4-b5.md` (issue #24), user stories 33 to 37; design S-05, C-20, F-04.

**What to build:** the Sessions tab shows each Session's Exercises as C-20 editable rows (drag handle shown decoratively, name and summary, Edit expanding an inline panel with Sets, Reps and max, Step, Cue, Benchmark toggle, and Move up, Move down and Remove text buttons). Add Exercise from library opens the picker (with the live Add a new Exercise from ticket 05) and appends the row. A Session's name, default day and type are editable, and a new Session can be added. Inline messages for invalid sets or reps disable Save. No drag and drop. Saves use the guarded save, rules from ticket 04.

**Blocked by:** 07, 05.

**Status:** ready-for-agent

- [ ] bUnit tests: expand and edit a row, Move up and down reorder, Remove, add from library appends at the end, new Exercise created from the picker lands in the Session, add Session, invalid sets or reps block Save with the message, past Workouts unchanged after Save, read-only disables edits with the reason.
- [ ] Reordering is fully usable without a pointer (buttons, keyboard).
- [ ] Tokens only; `WaitForAssertion` after async clicks; `dotnet build` has 0 warnings.
