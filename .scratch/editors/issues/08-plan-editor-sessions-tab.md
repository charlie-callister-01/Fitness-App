# 08: Plan editor Sessions tab

**Read first:** `.scratch/editors/HANDOFF.md`, then the spec.

**Parent:** spec `docs/specs/editors-b4-b5.md` (issue #24), user stories 33 to 37; design S-05, C-20, F-04.

**What to build:** the Sessions tab shows each Session's Exercises as C-20 editable rows (drag handle shown decoratively, name and summary, Edit expanding an inline panel with Sets, Reps and max, Step, Cue, Benchmark toggle, and Move up, Move down and Remove text buttons). Add Exercise from library opens the picker (with the live Add a new Exercise from ticket 05) and appends the row. A Session's name, default day and type are editable, and a new Session can be added. Inline messages for invalid sets or reps disable Save. No drag and drop. Saves use the guarded save, rules from ticket 04.

**Blocked by:** 07, 05.

**Status:** done

- [x] bUnit tests: expand and edit a row, Move up and down reorder, Remove, add from library appends at the end, new Exercise created from the picker lands in the Session, add Session, invalid sets or reps block Save with the message, past Workouts unchanged after Save, read-only disables edits with the reason.
- [x] Reordering is fully usable without a pointer (buttons, keyboard).
- [x] Tokens only; `WaitForAssertion` after async clicks; `dotnet build` has 0 warnings.

## Notes from implementation
- `PlanSessionsTab` replaces the placeholder (same three parameters). `PlanEditState.SessionsProblem` (first bad row, prefixed with its Session's name) joins `ProblemTab` ("Sessions") and `Compose()` refuses while it is set; each bad row also shows its own inline `role="alert"` message under the row.
- No per-row Step increment exists in the data model (`SessionExercise` has only Sets, Reps, RepsMax, Cue; the increment is Plan-wide in Settings), so the row panel shows no Step field (a test pins that).
- Benchmark toggle changes the library Exercise (`ExerciseEdits.WithBenchmark`) and is saved at once through the tab's own `GuardedSave`, not with the Plan Save: the flag belongs to the Exercise, not the Plan, so it does not mark the Plan dirty.
- Session name, default day and type (and adding a Session) use one `FormSheet`; blank names are refused by Core (`EditSession`/`AddSession`) with the message shown in the sheet. Edits apply to the draft and go out with Save.
- Reordering is Move up / Move down real buttons (first/last disabled accordingly); the drag handle is decorative (`aria-hidden`). Typed non-numbers in Sets or Reps become 0 and are refused with the rule's message; a blank Reps or Highest Reps means none.
- A row with no Reps shows as "N sets". The old "Sessions tab is a placeholder" test was removed.
