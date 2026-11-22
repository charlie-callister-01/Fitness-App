# 04: Core rules for Plan editing and the Week view

**Read first:** `.scratch/editors/HANDOFF.md`, then the spec.

**Parent:** spec `docs/specs/editors-b4-b5.md` (issue #24), "Plan rules (Core)" and "Week rules (Core)".

**What to build:** Core rules and Core.Tests only, no Ui.
- **Plan edit:** validation (Sets at least 1, Reps at least 1, max not below Reps, weekly target and Streak minimum at least 1, goal date not before start); Phase overlap and ordering with a message naming both Phases; row move up and down, remove, add from library (appended at the end); add Session and edit its name, default day and type; copy Plan (fresh ids, inactive, same content); make active, returning both Plan changes so the page commits them in one save. Phases stay in weeks; provide the conversion to dates from the Plan start date for display.
- **Week:** move a Session (this week only writes a move into that week's Week schedule; "Make this the default day" changes the Session's default day in the Plan); Break creation rejecting an end before the start and an overlap with an existing Break; a draft model over the existing re-arrangement rule with per-row undo, apply result and cancel.
- No change to the data model or exchange format.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Each rule has Core tests including: overlapping Phases, reversed dates, last row removal, copy gives new ids and inactive, make active leaves exactly one active Plan, two Sessions on one day, Break overlap, undo of one draft row.
- [ ] Editing a Plan never touches past Workouts (tested).
- [ ] No Ui file is edited; `dotnet build` has 0 warnings and all tests pass.
