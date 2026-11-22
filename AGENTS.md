## Project context

A standalone fitness tracking app replacing the owner's Obsidian/Dataview system (goal: HYROX London Doubles, 24 Apr 2027). Not a learning project: the owner wants it built quickly and well. The owner works in C# professionally. Phone-first, single user for now, possibly released later.

## Start here
1. `/start-session`: fetches, checks the branch is current, reads `docs/STATUS.md` and the files listed in `docs/HANDOFF.md`. Do not re-derive state from git.
2. Docs: `docs/README.md` (index), `GLOSSARY.md` (use terms exactly: Session vs Workout, Track, etc.), `docs/build-plan.md` (stages and order), `docs/specs/<stage>.md` (stage specs), `docs/adr/` (decisions), `docs/grilling-log.md`.
3. Workflow settings: `docs/agents/workflow-settings.md` (preset `full`, platform `github`).

## Solution map
- `src/Core`, `src/Ui`, `src/Web`: Blazor WebAssembly PWA (.NET 10, `TreatWarningsAsErrors` enabled). Core holds pure domain/rules (no clock, no storage); Ui holds shared Razor components/pages.
- `src/Core.Tests`, `src/Ui.Tests`, `src/Web.Tests`, `src/Web.BrowserTests`: unit, bUnit, and Playwright contract tests.

## Commands (run from repo root)
- Build/test: `dotnet build` (must stay at 0 warnings) and `dotnet test`.
- CI: `.github/workflows/ci.yml` on PRs; `.github/workflows/browser-tests.yml`; `.github/workflows/deploy.yml` deploys `main` to GitHub Pages.
- Open PRs only with `/ship`.

## Workflow (preset: full)
- Branch `feature/<name>`, `chore/` or `fix/` (`throwaway/`, `research/`); never commit to `main`. Commits: `type(scope): message` (stage ticket commits such as `A4.5: ticket NN <title>` remain allowed where a stage specifies them).
- **Normal stage:** `/mattpocock-skills:to-spec` (`docs/specs/<stage>.md`) -> `/mattpocock-skills:to-tickets` (`.scratch/<stage>/issues/`, ~150-200k tokens per ticket; split anything bigger) -> `/mattpocock-skills:implement` sequentially, one implementer at a time (`implement-spec` only if the owner approves) -> `/mattpocock-skills:code-review` (always this exact skill, not the built-in `/code-review`) -> `/thermo-nuclear-code-quality-review` (findings to `docs/REFACTOR.md`, applied to C#) -> size check & fixes -> `/ship` -> `/end-session`.
- **Size check after review:** estimate fix work (lines to read or change x ~12 tokens + ~20k). Over 160k: suggest a fix spec; over 200k: make one.
- **Fast lane:** docs, config or fixes under ~30 lines skip spec and tickets: branch, change, `/ship`.
- **Grilling** (when new features need it; initial product grill is complete): run `/mattpocock-skills:grill-with-docs` in three stages in order (**1. Usage and experience** -> **2. Practical** -> **3. Technical**), keeping questions extensive and one at a time; record terms in `GLOSSARY.md` and decisions in `docs/adr/`.

## Context discipline
- Don't read whole large files or build output. Grep first, read ranges.
- Delegate wide searches/reviews to subagents; keep only conclusions in the main thread.
- Keep this file under ~120 lines; put detail in `docs/`.

## Agent skills
- Issue tracker (Local Markdown): `docs/agents/issue-tracker.md`. Triage labels: `docs/agents/triage-labels.md`. Domain docs: `docs/agents/domain.md`. Platform: `docs/agents/platform.md`.
