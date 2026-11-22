# Agent trial: antigravity on Fitness-App

Guide and findings sheet for trying a second agent beside Claude Code.

## 1. Setup (once)

1. **Instruction file.** Moved `CLAUDE.md` to `AGENTS.md` (`chore/workflow-refit`) and replaced `CLAUDE.md` with `@AGENTS.md` (UTF-8, no BOM). Set `workflow_track: beta` in `docs/agents/workflow-settings.md`.
2. **Skills for Antigravity, local only.** Copied `.claude/skills/*` (`start-session`, `end-session`, `ship`, `thermo-nuclear-code-quality-review`) to `.agents/skills/` and added `.agents/` to `.git/info/exclude`.
3. **No second instruction file.** No `GEMINI.md` or `.agents/rules/` added.
4. **Global rules.** Checked `C:\Users\calli\.gemini\config`:
   - None (`AGENTS.md`, `GEMINI.md`, and `config/rules/` do not exist).

**Rollback:** `git rm CLAUDE.md`, `git mv AGENTS.md CLAUDE.md`, remove `workflow_track: beta`, delete `.agents/`.

## 2. Each session

- Start and end the same way in both agents: `/start-session` ... `/end-session`.
- The LOG line carries the agent: `## YYYY-MM-DD [antigravity]: ...` or `## YYYY-MM-DD [claude]: ...`.

| Date | Agent | What I did | Handover friction (what it missed, redid or got wrong from the other agent's work) |
|---|---|---|---|
| 2026-11-22 | antigravity | Workflow refit (`chore/workflow-refit`) + beta `AGENTS.md` + `.agents/skills/` setup | Global `~/.claude/skills/refit-workflow` was still v1 (`a9258ae`); applied `W\beta\` refit directly. Sandbox required bypass for `C:\Users\calli\source\repos\Workflow` and git writes. |

## 3. Skills

| Skill | Works / partly / broken | Why | Fix idea |
|---|---|---|---|
| start-session | | | |
| end-session | | | |
| ship | | | |
| thermo-nuclear-code-quality-review | | | |

## 4. Instruction file

- Did Antigravity follow `AGENTS.md`? Rules it ignored or misread:
  - 
- Lines that only make sense to one agent (candidates for a per-agent section):
  - 
- Did Claude Code behave the same as before the switch?
  - 

## 5. Markers

- Cost or usage per session where `agy` shows it, against Claude for similar work:
- Corrections: how often you had to stop or redirect it:
- Only possible in one agent (features, tools, speed):
- Anything else worth measuring (see roadmap ticket 21):

## 6. Verdict (for decision 06)

- Keep `AGENTS.md` as the main file? **keep / tune / drop**, because:
- Should the toolbox install `.agents/skills/` (or a neutral skills folder) for other agents? Why:
- Run the two agents side by side, one per kind of task, or not at all? Why:
- What 06 should decide next:
