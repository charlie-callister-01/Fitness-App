## Project context

A standalone fitness tracking app replacing the owner's Obsidian/Dataview system (goal: HYROX London Doubles, 24 Apr 2027). Not a learning project: the owner wants it built quickly and well. The owner works in C# professionally. Phone-first, single user for now, possibly released later.

## Where things are

The grilling session is complete. Start any new session by reading `docs/README.md` (index), then `GLOSSARY.md`. Product: `docs/product-brief.md`. Stack and structure: `docs/technical-brief.md`. Stages and next step: `docs/build-plan.md`. Decisions: `docs/adr/`. Every question and answer: `docs/grilling-log.md`. Use the glossary's terms exactly (Session vs Workout, and so on).

## Build workflow

Work is cut into stages. For each stage, in order:

1. `/to-spec`: write the spec for the stage.
2. `/to-tickets`: break the spec into tickets.
   **Ticket sizing:** each ticket must be a chunk one agent can finish within roughly 150-200k tokens of context (reading, writing, tests and fixes). Split anything bigger; don't bundle unrelated work to save tickets.
3. Implement the tickets.
4. `/code-review`: normal review of the stage's changes.
5. `/thermo-nuclear-code-quality-review`: the strict maintainability review (`.claude/skills/thermo-nuclear-code-quality-review/SKILL.md`). It's user-invoked only, so if it can't be run as a skill, read that file and apply it to the stage's changes. Its rules are written with TypeScript in mind (`any`, `unknown`); apply the same intent to C# (nullable abuse, `object`, casts).
6. Apply the fixes from both reviews.
7. `/pr`: write the PR body, with extra explanation of what changed and why, and any non-obvious decisions.

Don't skip or merge steps, and don't start the next stage until the current stage's PR is written. A UI/UX phase comes before UI work begins; data-model and core-logic stages don't depend on it.

## How to run `/grill-with-docs`

Always run it in three stages, in order. Don't move to the next stage until the current one is settled:

1. **Usage and experience**: who uses it, when, where (e.g. mid-set at the gym on a phone), what it should feel like, what they're trying to achieve.
2. **Practical**: concrete features, screens, workflows, edge cases, data and rules.
3. **Technical**: platform, storage, architecture, libraries, sync, export, testing.

Keep the questions extensive, one at a time, and record terms in `GLOSSARY.md` and decisions in `docs/adr/` as they're resolved.

## Agent skills

### Issue tracker

Issues are tracked in GitHub Issues (charlie-callister-01/Fitness-App) via the `gh` CLI. See `docs/agents/issue-tracker.md`.

### Triage labels

Default five-label vocabulary (needs-triage, needs-info, ready-for-agent, ready-for-human, wontfix). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: one `GLOSSARY.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
