# RUN: b3-home

Stage branch: b3-implementation
Kickoff: `Read AGENTS.md. You are the orchestrator. Run stage b3-home following .scratch/b3-home/RUN.md.`
Baseline: `dotnet test` green on the stage branch at the commit that adds this file, Core 1048, Ui 358, Web 39 (browser tests not run).
Spec: `docs/specs/b3-home.md`. Tickets: `.scratch/b3-home/issues/NN-*.md`. Reports go to `.scratch/b3-home/reports/NN.md`.
Ticket branches: `b3-home/NN-slug` (slug as in the ticket file name).

| Wave | Ticket | Title | Blocked by | Merge order | Status |
|---|---|---|---|---|---|
| 1 | 01-core-today-card | Core: TodayCard state | None | 1 | merged |
| 1 | 02-core-week-strip-and-orbs | Core: week strip, week line and orbs | None | 2 | merged |
| 1 | 03-core-nudge-selection-and-recent-prs | Core: Nudge selection and Recent PRs | None | 3 | merged |
| 1 | 04-core-weight-panel-and-weigh-in | Core: weight panel and Weigh-in creation | None | 4 | merged |
| 1 | 05-core-custom-workout-creation | Core: Custom workout creation and start | None | 5 | merged |
| 1 | 06-ui-start-sheets | Ui: Do a different Session and Log a past Workout sheets | None | 6 | todo |
| 2 | 07-core-home-view-builder | Core: Home view builder | 01, 02, 03, 04 | 1 | merged |
| 2 | 08-ui-today-card | Ui: TodayCard component | 01 | 2 | todo |
| 2 | 09-ui-week-strip-line-and-orbs | Ui: WeekStrip, week line and orb row | 02 | 3 | todo |
| 2 | 10-ui-nudge-and-dismissals | Ui: Nudge component and dismissal store | 03 | 4 | todo |
| 2 | 11-ui-recent-prs-weight-panels-and-sheet | Ui: Recent PRs, Weight panel and Log weight sheet | 03, 04 | 5 | todo |
| 2 | 12-ui-custom-workout-draft-page | Ui: Custom workout draft page | 05 | 6 | todo |
| 3 | 13-ui-home-page | Ui: Home page | 06, 07, 08, 09, 10, 11, 12 | 1 | todo |
| 4 | 14-ui-home-wiring | Ui: Home wiring | 13 | 1 | todo |

Waves run in order. Within a wave all tickets run in parallel. A wave starts only when every ticket it depends on is `merged`.

## Parallelism and file ownership

- Wave 1 tickets create only new files (plus `WorkoutStarter.cs`, edited by 05 alone). They cannot collide.
- Wave 2: tickets 07 and 08 to 12 create only new files, except 09, which alone may edit the token files (and then only if a token is truly missing). Nothing in wave 2 edits `Home.razor`.
- `Home.razor`, `Home.razor.cs`, `Home.razor.css` and `HomeTests.cs` belong to 13, then to 14 (strictly after 13).
- No ticket edits `components.css`, `ui.css`, `Program.cs`, `LogFixture.cs` or any DI setup.

## Planner-owned work (not for implementers)

After wave 4 is merged and green: independent re-verification (`dotnet build`, `dotnet test`, read the reports, check each ticket's acceptance), B3's manual-checks section in `docs/pwa-manual-checks.md`, the status update in `docs/build-plan.md` and `docs/README.md`, the two reviews, the fix pass and the PR body. The trial notes in `docs/agents/workflow-change-log.md` are filled in then.

## Notes
- Wave 1 run stopped when the implementer tool hit its usage limit. Tickets 01 to 05 were left finished in their worktrees (01, 02 and 03 uncommitted, 04 without a report; only 05 wrote a report). The planner committed 01 to 03 to their ticket branches, merged 01 to 05 into the stage branch in merge order, and confirmed `dotnet build` clean (0 warnings) and `dotnet test` green: Core 1152 (baseline 1048 + 104 new), Ui 358, Web 39. Tests-first (the red run) is unverified for 01 to 04 because they have no reports.
- Ticket 06 (Ui start sheets) had no work in its worktree: still todo. Wave 2 cannot start until 06 is merged only for ticket 13; tickets 07 to 12 depend only on 01 to 05 and can start now.
