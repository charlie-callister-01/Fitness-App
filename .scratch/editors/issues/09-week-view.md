# 09: Week view: rows, Move, Break and suggested week

**Read first:** `.scratch/editors/HANDOFF.md`, then the spec.

**Parent:** spec `docs/specs/editors-b4-b5.md` (issue #24), user stories 41 to 48; design S-06, F-08.

**What to build:** the Week page replaces its placeholder. Rows show the day, Session and a chip (Done, Missed, Today). A Move button opens a sheet to pick a day with "This week only" or "Make this the default day". Add a Break opens a sheet with from, to, reason and an explanation, and Start Break; an overlapping Break or an end before the start is refused with a plain message. When a Break ends, the suggested re-arranged week appears as a draft: summary banner, changed rows highlighted, per-row Undo, Cancel and Apply. Cancel writes nothing; Apply writes the Week schedule in one save; outstanding Sessions become Missed only after their day passes. Saves use the guarded save, sheets the form sheet, rules from ticket 04. Home and the week strip reflect changes after saving.

**Blocked by:** 02, 04.

**Status:** done

- [x] bUnit tests: rows and chips, Move this week only vs default day (next week unchanged for the first), Break added and Home shows On a Break, Break overlap and bad range refused, draft shows changes and one summary line, per-row Undo, Cancel changes nothing, Apply stores the Week schedule once, read-only disables with the reason, failure keeps state.
- [x] Tokens only; works at 360 px; `WaitForAssertion` after async clicks; `dotnet build` has 0 warnings.

## Notes from implementation
- Core: `Core/Rules/Schedule/WeekRows` gives the rows and chips (Done over Missed over Today) and `BreakTouchesWeek`; tests in `WeekRowsTests`. `StateChip` gained Missed and Today kinds (one CSS line in `components.css`).
- The suggested week is offered only when `SuggestDraft` returns a draft and a Break touches the current week. The rearrangement rule only proposes when an outstanding Session sits on a Break day (or is Missed or doubled up), so a Break that has already ended and left nothing outstanding shows no draft. Cancel and Apply put the draft away until a Break is added.
- "Make this the default day" also clears a move already made for that Session this week, in the same commit, so the new default is not overridden.
- A failed save keeps the sheet or draft; Apply is disabled when the draft has no changed rows.
- Branch note: the worktree started at main, so it was reset to `b4-implementation` before work began.
