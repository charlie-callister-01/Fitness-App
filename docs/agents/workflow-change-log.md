# Agent workflow trial: change log and how to revert

Introduced 2026-11-21 on branch `docs-agent-workflow` as a single commit, "Agent workflow trial: planner and implementer split". Decision record: [ADR 0007](../adr/0007-planner-and-implementer-split-with-file-based-dispatch.md). First stage to use it: B3 (Home).

## What changed

Added:
- `AGENTS.md` (repo root): implementer and orchestrator rules, report format, kickoff prompts.
- `docs/agents/ticket-template.md`: self-sufficient ticket format, 80k sizing, parallelism rule.
- `docs/agents/run-template.md`: the per-stage `RUN.md` wave table.
- `docs/adr/0007-planner-and-implementer-split-with-file-based-dispatch.md`.
- This file.

Edited (small, all inside the one commit):
- `CLAUDE.md`, Build workflow steps 2 and 3: ticket sizing 150-200k to 80k, the ticket format override, and "hand the stage to the implementer agent" instead of implementing in this session.
- `docs/build-plan.md`, Workflow steps 2 and 3: same sizing and hand-off wording.
- `docs/README.md`: index rows for `AGENTS.md` and ADR 0007, and the `agents/` row.

Not changed: `GLOSSARY.md` (workflow terms are not domain terms; Implementer, Orchestrator, Wave are defined in `AGENTS.md` and the ADR), any code, any existing spec or ticket.

## Revert (workflow not wanted)

Before it is merged: delete the branch (`git branch -D docs-agent-workflow`).

After it is merged: `git log --grep "Agent workflow trial"` to find the commit (or the merge commit of its PR), then `git revert <sha>` (for a merge commit, `git revert -m 1 <sha>`). That removes every file above and restores the old wording of steps 2 and 3. Stage artefacts already created under the trial (`RUN.md`, `reports/`) are in `.scratch/` and can stay or be deleted.

## Revert then reimplement

1. Revert as above (this keeps the history of the attempt).
2. Revert the revert later (`git revert <revert-sha>`) to restore everything exactly, or cherry-pick the original commit onto a fresh branch and edit.
3. Parts that are independent of each other, if you only want some of it: the sizing change (80k) is two lines in `CLAUDE.md` and `docs/build-plan.md`; the templates in `docs/agents/` stand alone; `AGENTS.md` can be used without the ADR.

## Trial notes (fill in during B3)

- Tool used and version:
- Did worktree subagents read `AGENTS.md`?
- Tickets that were too big at 80k:
- Blocked or failed tickets, and why:
- Planner takeovers:
- Cost or time compared with B2:
- Verdict (keep, adjust, revert):
