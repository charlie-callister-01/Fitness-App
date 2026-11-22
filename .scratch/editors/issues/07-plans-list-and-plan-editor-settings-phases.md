# 07: Plans list and Plan editor (Settings and Phases tabs)

**Read first:** `.scratch/editors/HANDOFF.md`, then the spec.

**Parent:** spec `docs/specs/editors-b4-b5.md` (issue #24), user stories 24 to 32, 37 to 40; design S-05, C-20, F-04.

**What to build:** the Plans page replaces its placeholder: the active Plan outlined as "Active · week n of m", other Plans with Make active (previous Plan stops being active in the same save), and New Plan with From scratch and Copy this Plan. The Plan editor is a page with segmented tabs Settings, Phases and (placeholder for ticket 08) Sessions. Settings: start date, goal and goal date, weekly target, Streak minimum, Custom workouts counting, weight increment. Phases: rows with focus and the dates the weeks cover, edit start and end, optional Phase goal, overlap and reversed-range messages naming both Phases. The banner "Edits apply going forward. Past Workouts are unchanged." Save disabled until valid; "Discard changes?" on leaving with unsaved changes; Home and the Week view update after Save. Saves use the guarded save, sheets the form sheet, rules from ticket 04.

**Blocked by:** 02, 04.

**Status:** done

- [x] bUnit tests: Plans list states (one Plan, several, none), Make active leaves exactly one active Plan, copy and from scratch create inactive Plans with new ids, settings edits persist, Phase overlap refused with both names, invalid values disable Save, discard prompt, read-only disables everything with the reason, failure keeps edits.
- [x] Home reflects an edited weekly target after Save (test).
- [x] Sessions tab renders as a clearly labelled placeholder until ticket 08.
- [x] Tokens only; `WaitForAssertion` after async clicks; `dotnet build` has 0 warnings.

## Notes from implementation
- Plan editor is `/plans/{PlanId}` on `PushedPageLayout`. For ticket 08: the shared edit state is `PlanEditState` (`Draft` Plan, `Saved` Plan, `Form` for the typed Settings, `Change(Func<Plan, Plan>)`, `IsDirty`, `Compose()`); the tab components are `PlanSettingsTab`, `PlanPhasesTab` and `PlanSessionsTab` (the placeholder; replace its content, keep its three parameters `State`, `Disabled`, `OnEdited`). `PlanEditState.IsDirty` already compares `Draft.Sessions` with `Saved.Sessions`, so session edits made through `State.Change(...)` enable Save and the discard prompt with no further work. `Compose()` carries the draft Sessions through; add session validation there (and a problem entry beside `SettingsProblem` and `PhasesProblem`, plus `ProblemTab`) if needed.
- Added Core: `PlanProgress` (`TotalWeeks`, `ActiveLabel`) and Phase goal checks in `PlanEditRules.ValidatePhases` (Exercise present, weight 0 or more, reps at least 1). Total weeks m = last week of the last Phase; with no Phases, weeks from the start to the goal date; otherwise "Active · week n" with no "of m".
- Phases are edited by week number (first week, last week) with the covered dates shown beside them, not date pickers: the rule and storage are in weeks. Phases keep list order while edited and are sorted by week on Save.
- New Plan sheet: one name field, a "Copy from" select (defaults to the active Plan) and the buttons From scratch and Copy this Plan. A new Plan opens in its editor.
- `MoreTests` no longer renders the Plans page as a placeholder (it needs the clock and data); it checks the link and route instead.
