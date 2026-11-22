# 12: Plan editor Phases and Sessions per Track (PR 2)

**Read first:** `.scratch/a4-5-tracks/HANDOFF.md`, `docs/specs/a4-5-tracks.md` ("Screens (PR 2)"), `.scratch/editors/issues/08-plan-editor-sessions-tab.md` (notes), `src/Ui/Pages/PlanPhasesTab.razor`, `PlanSessionsTab.razor(.cs)`, `PlanEditState.cs`, ticket 11's result.

**Parent:** spec `docs/specs/a4-5-tracks.md`, PR 2, user stories 15 to 17.

**What to build:** the Phases and Sessions tabs edit the **selected Track**.
- **Phases tab:** the selected Track's Phases (focus, first and last week, dates shown, optional goal), with the Track's own overlap, gap and reversed-range messages from `PlanPhaseRules`. A Track with none shows "No Phases for this Track" with an Add Phase action. Week numbers are Plan-wide, and the dates come from the Plan start.
- **Sessions tab:** the selected Track's Sessions. Add Session on a daily Track makes a daily Session (no day picker; the sheet says "Every day"); on a weekly Track the day picker lists Monday to Sunday only. The Session sheet's type list includes Mobility. Editing a daily Session's day is not offered. The benchmark toggle and interval appear on the main Track only. Row editing (Sets, Reps, Cue, Move up and down, Remove, Add Exercise from library) is unchanged.
- A Phase override's Session choice lists only the selected Track's Sessions.
- Problems (a Phase problem, a Session row problem) still disable Save and name the tab and, now, the Track (`ProblemTab` text such as "Phases (Mobility)").
- Tokens only, both Themes and Modes, 360 px.

**Blocked by:** 11.

**Status:** ready-for-agent

- [ ] bUnit tests: switching Track swaps the Phases and Sessions shown; Phase overlap inside one Track refused naming both Phases; a Phase in another Track with the same weeks is allowed; a Track with no Phases; add a daily Session (no day picker); weekly day list has no Daily; Mobility offered as a type; benchmark controls only on the main Track; override Session list is the Track's own; a problem on a non-selected Track still disables Save and names the Track; past Workouts unchanged after Save; read-only reasons.
- [ ] `dotnet build` 0 warnings; Ui tests green.
