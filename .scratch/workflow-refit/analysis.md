# Workflow refit: analysis

Toolbox W: `C:\Users\calli\source\repos\Workflow` at `a9258ae`. Preset `full`, platform `github` assumed (`/setup-workflow` has not been run; there is no `docs/agents/workflow-settings.md`). Repo `CLAUDE.md` is 46 lines.

Counts (38 rows): same 6, duplicate 8, conflicting 7, missing 12, repo-only 5.

| # | item | repo (path:lines) | toolbox (path) | label | note |
|---|---|---|---|---|---|
| 1 | Instruction file | CLAUDE.md:1-46 (context, where things are, 7-step build workflow, grill order, skill pointers) | templates/CLAUDE.md (start here, solution map, commands, workflow, context discipline) | duplicate | Same job. Repo lacks commands, branch rules and context discipline. |
| 2 | Session start | CLAUDE.md:7 (read docs/README.md, GLOSSARY, briefs, then .scratch/<stage>/HANDOFF.md) | skills/start-session | duplicate | Same intent. Repo has a fixed reading list and no git or PR check. |
| 3 | /start-session | none | skills/start-session | missing | Needs STATUS/HANDOFF (rows 5, 6). |
| 4 | /end-session | none (docs/README.md:3 and build-plan status edited by hand) | skills/end-session | missing | Would own STATUS/HANDOFF/LOG and the branch sweep. |
| 5 | Current-state file | docs/README.md:3 (long status paragraph) plus build-plan.md stage rows | templates/docs/STATUS.md (under 40 lines) | duplicate | Two hand-edited copies, already stale (README:3 says B3 merged twice and B2 "PR not yet opened"). |
| 6 | HANDOFF | .scratch/<stage>/HANDOFF.md x9 (e.g. a3-storage:1-40, full task brief) | templates/docs/HANDOFF.md (pointer list under 10 lines, no state) | conflicting | Same name, different meaning. |
| 7 | Session LOG | none (grilling-log.md is Q&A) | templates/docs/LOG.md | missing | One line per session. |
| 8 | Plan and phases | docs/build-plan.md (tracks, stages, order) | templates/docs/PLAN.md and phases/P-NN.md | duplicate | Stages = phases; status prose = Specs list. |
| 9 | Stage pipeline | CLAUDE.md:10-22 and build-plan.md:3-14 (written twice in the repo) | presets/full.md Pipeline | duplicate | Same spine. Differences are in rows 10-25. |
| 10 | Grilling | CLAUDE.md:24-32, three ordered stages | presets/full.md: grill-with-docs, unstaged | repo-only | Grilling is already complete, so shrink it. |
| 11 | Spec location | docs/specs/<stage>.md (13 files); stages A1, B3 | issue-tracker template: .scratch/pNN-<slug>/spec.md plus phases/P-NN.md | conflicting | Spec location and stage naming differ. |
| 12 | Tickets and tracker | CLAUDE.md:38, docs/agents/issue-tracker.md:1 say GitHub issues; practice is .scratch/*/issues/NN-*.md | templates/docs/agents/issue-tracker.md (Local Markdown, ADR 0002) | conflicting | Config disagrees with practice. |
| 13 | Ticket sizing | CLAUDE.md:15-16, 150-200k tokens per ticket | none for tickets | repo-only | Keep. |
| 14 | Implement | CLAUDE.md:16; build-plan: one implementer at a time, never parallel | presets/full.md: /implement, implement-spec for 3+ independent tickets | conflicting | Parallelism is opposed. |
| 15 | Code review | CLAUDE.md:17: mattpocock-skills:code-review, not the built-in | same skill | same | Keep the "not built-in" guard. |
| 16 | Thermo skill file | .claude/skills/thermo-nuclear.../SKILL.md (192 lines, vendored, TS wording, 1000-line rule) | skills/thermo-nuclear.../SKILL.md (195 lines, C# examples, ~500-line limit, refactor_backlog) | duplicate | Two copies; CLAUDE.md:18 carries a C# note the toolbox copy already has. |
| 17 | Thermo cadence | CLAUDE.md:18: every stage, output .scratch/<stage>/thermo-nuclear-review.md | presets/full.md: end of phase only, output docs/REFACTOR.md | conflicting | Cost and findings home differ. |
| 18 | Findings and backlog | .scratch/<stage>/review-findings.md, thermo-nuclear-review.md | docs/REFACTOR.md, .scratch/archive/ | missing | Per-stage files go stale. |
| 19 | Fix size check | CLAUDE.md:19 "apply fixes" | CLAUDE.md template: over 160k suggest a fix spec, over 200k make one | missing | Repo already makes review-fixes specs ad hoc. |
| 20 | PR body | CLAUDE.md:20: /pr with extra explanation; .scratch/<stage>/pr-body.md x8 | skills/ship step 4 (/pr, temp file, attribution) | duplicate | Same skill underneath. |
| 21 | /ship | none (only scripts/publish-pages.sh; PRs opened by hand) | skills/ship, tools/gh-pr.md, platforms/github.md | missing | Adds clean-tree, pushed and review checks plus gh pr create. |
| 22 | PR explainer | none | skills/pr-explainer (HTML Artifact) | missing | Overlaps the "extra explanation" rule. Costs tokens per PR. |
| 23 | Branch and commit rules | none stated; observed a4-5-explicit-tracks-spec, "Docs: ...", "Editors: PR body" | feature/ fix/ chore/, "type(scope): message (phase N)", never commit to main | conflicting | Repo follows neither and states no rule. |
| 24 | Fast lane | CLAUDE.md:22 "Don't skip or merge steps" | presets/fast-lane.md (under 30 lines skip grill, spec, tickets) | conflicting | Directly opposed. |
| 25 | Branch sweep and archive | none; 13 stage folders in .scratch, no archive | end-session step 8, archive rule | missing | .scratch is committed and grows. |
| 26 | workflow-settings.md | none | templates/docs/agents/workflow-settings.md | missing | Needed by any adopted skill (test_command, code_pattern). |
| 27 | platform.md | none | platforms/github.md | missing | Not a Pocock file; needed only for /ship. |
| 28 | agents/domain.md | docs/agents/domain.md | same template | same | Only skill-name prefix differs. |
| 29 | agents/triage-labels.md | same five labels | same template | same | Toolbox adds a Status-line sentence. |
| 30 | agents/issue-tracker.md | GitHub conventions, 46 lines | Local Markdown | duplicate | The file to rewrite; the decision is row 12. |
| 31 | ADRs | docs/adr/0001-0007 | templates/docs/adr/0000-template.md (three-test rule) | same | Repo has no written criterion. |
| 32 | GLOSSARY.md | root GLOSSARY.md | root convention | same | |
| 33 | Context discipline | none | templates/CLAUDE.md section | missing | Three lines. |
| 34 | CI | .github/workflows/ci, build-and-test, browser-tests, deploy | platforms/github.md (.github in code_pattern) | same | Only code_pattern needs setting. |
| 35 | day-log, vault-note | none | skills/day-log, vault-note | missing | Optional; the Obsidian replacement makes it unlikely. |
| 36 | Product, tech and design docs | docs/*-brief.md, data-spec.md, design/, grilling-log.md, specs/ | none | repo-only | Keep. |
| 37 | docs/README.md index | docs/README.md:1-30 | none | repo-only | The status paragraph at :3 is row 5. |
| 38 | Scripts and deploy | scripts/publish-pages.sh, deploy/ | none | repo-only | Keep. |

## Overlap of the project's per-stage pipeline with the toolbox

- Steps 1-3 (to-spec, to-tickets, implement) use the same skills as the full pipeline, but differ on spec home (row 11) and parallelism (row 14).
- Per-stage HANDOFF briefs, the docs/README.md:3 paragraph and the build-plan status duplicate the combined job of /end-session, STATUS, HANDOFF and LOG.
- code-review plus thermo every stage versus code-review per spec and thermo per phase (row 17).
- `/pr` plus pr-body.md versus `/ship` and `/pr-explainer`.
- One PR per stage is compatible with the toolbox's one PR per spec.
- day-log and vault-note have no overlap.
