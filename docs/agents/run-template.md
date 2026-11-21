# RUN.md template (for the agent workflow)

Used by the planner when writing `.scratch/<stage>/RUN.md`, committed on the stage branch together with the tickets before any dispatch. The orchestrator edits only the Status column and Notes.

Statuses: `todo`, `running`, `done` (reported done, not yet merged), `merged` (merged and suite green), `blocked` (implementer stopped), `failed` (merge or suite red).

```markdown
# RUN: <stage name>

Stage branch: <stage>-implementation
Kickoff: `Read AGENTS.md. You are the orchestrator. Run stage <stage> following .scratch/<stage>/RUN.md.`
Baseline: `dotnet test` green on the stage branch at <sha>, Core <n>, Ui <n>, Web <n>.

| Wave | Ticket | Title | Blocked by | Merge order | Status |
|---|---|---|---|---|---|
| 1 | 01-slug | <title> | None | 1 | todo |
| 1 | 02-slug | <title> | None | 2 | todo |
| 2 | 03-slug | <title> | 01 | 1 | todo |

Waves run in order. Within a wave all tickets run in parallel. A wave starts only when every ticket it depends on is `merged`.

## Notes
<Orchestrator: blockers copied from reports, tool limitations such as "ran sequentially", anything for the planner.>
```
