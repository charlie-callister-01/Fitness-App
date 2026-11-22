# Workflow refit: grill brief

**Goal:** adopt the toolbox where it helps, with no bloat. Every addition replaces something or is needed. CLAUDE.md stays under ~120 lines (currently 46). One source per rule.

Analysis: `.scratch/workflow-refit/analysis.md` (38 rows). Numbers below are decision-row numbers; the analysis row is in brackets.

**Decide only these rows** (allowed outcomes: refit = merge into one, replace = toolbox wins, trim = delete the duplicate, add, leave = repo wins). Recommended outcome in bold.

1. (row 1, duplicate) Repo CLAUDE.md has context plus a 7-step workflow; the toolbox template has commands, branch rules and context discipline. **Refit**: keep the repo's context and grill rule, add commands and branch rules.
2. (row 2, duplicate) Repo: CLAUDE.md:7 reading list. Toolbox: /start-session. **Replace**: /start-session reads STATUS and HANDOFF; CLAUDE.md says only "run /start-session".
3. (row 3, missing) /start-session. **Add** (depends on 5 and 6).
4. (row 4, missing) /end-session. **Add**; it replaces hand-edited status.
5. (row 5, duplicate) Repo: docs/README.md:3 paragraph plus build-plan status. Toolbox: docs/STATUS.md. **Replace**: create STATUS, cut the README paragraph to a pointer.
6. (row 6, conflicting) Repo: per-stage task-brief HANDOFFs. Toolbox: a pointer list under 10 lines. **Replace** with the toolbox's docs/HANDOFF.md; detail goes in the spec or tickets; stop writing per-stage ones.
7. (row 7, missing) LOG. **Add** (one line per session).
8. (row 8, duplicate) build-plan.md versus PLAN.md and phases. **Leave** build-plan as the plan, but move its status prose to STATUS (row 5).
9. (row 9, duplicate) Pipeline written in CLAUDE.md and again in build-plan:3-14. **Trim** build-plan's copy to a pointer; CLAUDE.md keeps one pipeline line.
10. (row 11, conflicting) Specs in docs/specs versus .scratch/pNN-slug/spec.md. **Leave** (repo wins), and record it in the tracker file (row 12).
11. (row 12, conflicting) Repo claims GitHub Issues but uses .scratch markdown. **Replace** with the toolbox local-markdown issue-tracker.md, adjusted for docs/specs; trims 3 lines in CLAUDE.md.
12. (row 14, conflicting) Implement: no parallel versus implement-spec. **Refit**: name /implement; say implement-spec is allowed only if the owner approves.
13. (row 16, duplicate) Two thermo skill copies. **Trim** the repo copy; use the toolbox one (global or installed); drop the CLAUDE.md C# note if it is covered.
14. (row 17, conflicting) Thermo every stage versus once per phase. **Leave** per Editors-style group (decide frequency), with output to docs/REFACTOR.md.
15. (row 18, missing) REFACTOR.md. **Add** (replaces per-stage thermo files).
16. (row 19, missing) Fix size check. **Add**, one line.
17. (row 20, duplicate) /pr plus pr-body.md versus /ship step 4. **Refit** into /ship (decision 18).
18. (row 21, missing) /ship. **Add**; replaces CLAUDE.md step 7, using `gh pr create`.
19. (row 22, missing) /pr-explainer. **Leave** (not adopted; owner is usage constrained).
20. (row 23, conflicting) Branch and commit format. **Refit**: adopt toolbox branch prefixes; keep the repo's commit style or switch to type(scope), the owner decides.
21. (row 24, conflicting) Fast lane. **Add** for docs, config and fixes under ~30 lines.
22. (row 25, missing) Branch sweep and archive. **Add** (via /end-session step 8) and archive finished .scratch stages.
23. (row 26, missing) workflow-settings.md. **Add**; required by 2, 3, 4, 18.
24. (row 27, missing) platform.md. **Add**; only needed for /ship.
25. (row 30, duplicate) docs/agents/issue-tracker.md. Resolved by decision 11.
26. (row 33, missing) Context discipline. **Add**, three lines.
27. (row 35, missing) day-log and vault-note. **Leave** (Obsidian is being replaced).

**Out of scope:** same and repo-only rows (15, 28, 29, 31, 32, 34, 10, 13, 36-38); product code.

**ADRs:** only for choices that are hard to reverse, surprising, and a real trade-off. Candidates: row 11 (changing the tracker home) or row 6 (dropping per-stage HANDOFFs). Probably none.

**Output:** fill the decisions table below.

| row | outcome | note |
|---|---|---|
| 1 | refit | Move CLAUDE.md to AGENTS.md (beta track); keep project context & grill order, add commands, branch/commit rules, context discipline (<120 lines); CLAUDE.md becomes `@AGENTS.md` |
| 2 | replace | AGENTS.md Start here points to `/start-session` instead of fixed reading list |
| 3 | add | Adopt `/start-session` |
| 4 | add | Adopt `/end-session` (owns STATUS, HANDOFF, LOG, branch sweep) |
| 5 | replace | Create `docs/STATUS.md`; trim `docs/README.md` status paragraph and `docs/build-plan.md` status prose |
| 6 | replace | Adopt `docs/HANDOFF.md` (<10-line pointer list); stop writing new per-stage task-brief HANDOFFs |
| 7 | add | Adopt `docs/LOG.md` (one line per session; tag `[antigravity]` / `[claude]` during multi-agent trial) |
| 8 | leave | Keep `docs/build-plan.md` as the plan (trimmed of stale status prose and duplicated workflow block) |
| 9 | trim | Keep one pipeline summary in `AGENTS.md`; replace `docs/build-plan.md:3-14` copy with a pointer |
| 10 | leave | Keep `docs/specs/<stage>.md` and `<stage>` naming (`a4-5-tracks` etc.) |
| 11 | replace | Rewrite `docs/agents/issue-tracker.md` to Local Markdown using `docs/specs/<stage>.md` and `.scratch/<stage>/issues/` |
| 12 | refit | Sequential by default (one implementer at a time); `implement-spec` only with owner approval |
| 13 | trim | Delete repo's vendored TS copy; install toolbox C#/~500-line copy (`thermo-nuclear-code-quality-review`) |
| 14 | leave | Keep current per-stage/group cadence (with `docs/REFACTOR.md` output) |
| 15 | add | Add `docs/REFACTOR.md` for thermo findings |
| 16 | add | Add fix size check (>160k suggest fix spec, >200k make one) in `AGENTS.md` |
| 17 | refit | Fold `/pr` + `pr-body.md` into `/ship` |
| 18 | add | Adopt `/ship` using `gh pr create` |
| 19 | leave | Do not adopt `/pr-explainer` |
| 20 | refit | Adopt `feature/`, `fix/`, `chore/` (`throwaway/`, `research/`) branches and `type(scope): message` commits (stage ticket commits like `A4.5: ticket NN <title>` remain allowed where a stage specifies them) |
| 21 | add | Adopt fast lane for docs, config, and fixes under ~30 lines |
| 22 | add | Adopt branch sweep (`/end-session`) and archive completed `.scratch/` stages into `.scratch/archive/` (keep `a4-5-tracks`, `a5-starter-data`, `data-and-settings`) |
| 23 | add | Add `docs/agents/workflow-settings.md` (`workflow_version: 61d2625`, `workflow_track: beta`) |
| 24 | add | Add `docs/agents/platform.md` (GitHub) |
| 25 | replace | Resolved by row 11 (`docs/agents/issue-tracker.md` rewritten to Local Markdown) |
| 26 | add | Add 3-line Context discipline section in `AGENTS.md` |
| 27 | leave | Do not adopt `day-log` or `vault-note` |
