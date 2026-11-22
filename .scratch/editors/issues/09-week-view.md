# 09: Week view: rows, Move, Break and suggested week

**Read first:** `.scratch/editors/HANDOFF.md`, then the spec.

**Parent:** spec `docs/specs/editors-b4-b5.md` (issue #24), user stories 41 to 48; design S-06, F-08.

**What to build:** the Week page replaces its placeholder. Rows show the day, Session and a chip (Done, Missed, Today). A Move button opens a sheet to pick a day with "This week only" or "Make this the default day". Add a Break opens a sheet with from, to, reason and an explanation, and Start Break; an overlapping Break or an end before the start is refused with a plain message. When a Break ends, the suggested re-arranged week appears as a draft: summary banner, changed rows highlighted, per-row Undo, Cancel and Apply. Cancel writes nothing; Apply writes the Week schedule in one save; outstanding Sessions become Missed only after their day passes. Saves use the guarded save, sheets the form sheet, rules from ticket 04. Home and the week strip reflect changes after saving.

**Blocked by:** 02, 04.

**Status:** ready-for-agent

- [ ] bUnit tests: rows and chips, Move this week only vs default day (next week unchanged for the first), Break added and Home shows On a Break, Break overlap and bad range refused, draft shows changes and one summary line, per-row Undo, Cancel changes nothing, Apply stores the Week schedule once, read-only disables with the reason, failure keeps state.
- [ ] Tokens only; works at 360 px; `WaitForAssertion` after async clicks; `dotnet build` has 0 warnings.
