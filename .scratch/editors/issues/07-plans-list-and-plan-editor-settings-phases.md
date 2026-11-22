# 07: Plans list and Plan editor (Settings and Phases tabs)

**Parent:** spec `docs/specs/editors-b4-b5.md` (issue #24), user stories 24 to 32, 37 to 40; design S-05, C-20, F-04.

**What to build:** the Plans page replaces its placeholder: the active Plan outlined as "Active · week n of m", other Plans with Make active (previous Plan stops being active in the same save), and New Plan with From scratch and Copy this Plan. The Plan editor is a page with segmented tabs Settings, Phases and (placeholder for ticket 08) Sessions. Settings: start date, goal and goal date, weekly target, Streak minimum, Custom workouts counting, weight increment. Phases: rows with focus and the dates the weeks cover, edit start and end, optional Phase goal, overlap and reversed-range messages naming both Phases. The banner "Edits apply going forward. Past Workouts are unchanged." Save disabled until valid; "Discard changes?" on leaving with unsaved changes; Home and the Week view update after Save. Saves use the guarded save, sheets the form sheet, rules from ticket 04.

**Blocked by:** 02, 04.

**Status:** ready-for-agent

- [ ] bUnit tests: Plans list states (one Plan, several, none), Make active leaves exactly one active Plan, copy and from scratch create inactive Plans with new ids, settings edits persist, Phase overlap refused with both names, invalid values disable Save, discard prompt, read-only disables everything with the reason, failure keeps edits.
- [ ] Home reflects an edited weekly target after Save (test).
- [ ] Sessions tab renders as a clearly labelled placeholder until ticket 08.
- [ ] Tokens only; `WaitForAssertion` after async clicks; `dotnet build` has 0 warnings.
