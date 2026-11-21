# Planner and implementer split, with file-based dispatch

Decided 2026-11-21, after a grill session on a cheaper build workflow. A **trial**: it starts with stage B3 and is revertable (see `docs/agents/workflow-change-log.md`).

Decision:
- The planning session (Claude Code) writes the spec, the tickets and a per-stage `RUN.md`, and keeps the two reviews, the fix pass and the PR. It does not write the stage's application code.
- Implementation is done by a separate, lighter agent in another tool (first trial: Antigravity CLI). The design is **tool-agnostic**: `AGENTS.md` is plain markdown, tool-specific files only point to it, and swapping tools changes one kickoff line.
- Each ticket is self-sufficient: it names exactly the files to read and to edit, so the implementer reads `AGENTS.md` and the ticket and nothing else. `CLAUDE.md` stays the planner's file.
- Tickets are about 80k tokens at most (down from 150-200k), one project per ticket where possible, so more of them run in parallel and a failure is cheap.
- Dispatch is file-based. `.scratch/<stage>/RUN.md` is a wave table (ticket, blocked by, wave, merge order, status). A thin orchestrator (the same agent given a different kickoff prompt, or this session using a script) launches each wave in parallel, one git worktree and branch per ticket, merges in order locally, runs build and tests, and ticks the table. It does no coding.
- Implementers commit only to their ticket branch. They never push, never use `gh`, never edit docs or status. Tickets stay local files committed to the stage branch before dispatch.
- Implementers write tests first and end with `reports/NN.md`. They stop and write a `blocked` report rather than guess. The orchestrator never marks a ticket merged unless build and tests are green, and the planner re-verifies before the reviews.

Why: the owner wants to cut cost per ticket and keep the planning context clean, with parallelism as a bonus. The previous workflow (Claude implementers via `implement-spec`) spent planner-grade models on mechanical ticket work.

Rejected: Claude subagents as implementers (the owner wants to try other tools and cheaper models); a shared working tree for parallel agents (collisions on shared files); GitHub Issues as the ticket store (the other tool would need `gh` auth and the tickets would not be in its worktree); one big instruction file for both roles (the implementer would pay for context it must not use).

Risks to watch in the B3 trial: a lighter model drifting from the spec despite "stop, don't guess"; worktree subagents not inheriting `AGENTS.md` (mitigated by "Read AGENTS.md first" in every ticket); tickets that turn out too big at 80k. If wave 1 goes badly, fix the templates before wave 2; the planner may take over any ticket the implementer cannot finish.
