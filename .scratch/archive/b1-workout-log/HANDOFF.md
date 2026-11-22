# Handoff: next stage after B0

Point a fresh chat at this file. State as of 18 Nov 2026.

## Where things are
- **On main and deployed** (https://charlie-callister-01.github.io/Fitness-App/): A0 to A4 and **B0** (PR #18). Design (Track B) is in `docs/design/`. Track B is sliced into stages B0 to B11 in `docs/build-plan.md`.
- Open PRs: none that matter. Stale local branches and worktrees exist (a0-scaffold-and-delivery, a3-*, a4-*, worktree-a1-spec, worktree-a2-spec); safe to ignore or delete.
- Tests: Core 814, Ui 103, Web 37 (`dotnet test` from the repo root), plus 47 browser tests (`dotnet test src/Web.BrowserTests -p:RunBrowserTests=true`; needs Playwright chromium installed).

## Workflow (CLAUDE.md, don't skip steps)
`/mattpocock-skills:to-spec` → `/mattpocock-skills:to-tickets` → `/mattpocock-skills:implement-spec` (integration branch, one worktree implementer per ticket, merge each, run suite) → `/mattpocock-skills:code-review` → thermo-nuclear review (`.claude/skills/thermo-nuclear-code-quality-review/SKILL.md`; user-invoked only, apply it by reading the file) → one fix pass → `/mattpocock-skills:pr`. Specs go in `docs/specs/`, tickets in `.scratch/<stage>/issues/`, PR body in `.scratch/<stage>/pr-body.md`. Use the glossary terms exactly (Theme vs Mode, Session vs Workout).

## Next stage options (ask the owner which)
1. **B1: Workout log, core input** (recommended; the heart of the app). Design S-02 (§6, §8.1 to 8.3), components C-02 to C-06, prototype `docs/design/assets/prototype-log.html`. Uses the Full-screen layout (no tab bar). Likely needs splitting by log kind at ticket time. Depends on A2 (rules) and A3 (storage), both done.
2. **A5: Starter data** (needs the owner's old Obsidian files: Config.md, view.js, the 27 Exercise files, weight history; see "Inputs still needed" in the build plan). Handoff may exist at `.scratch/a5-starter-data/HANDOFF.md` if it was merged.
3. Screens that need only B0 and Core and could run in parallel with B1: B4 (Guides, Exercise library), B6 (Progress, Weight), B7 (Trophies).

## Things B0 left for later stages
- **Failed saves** are not reported through `AppData.Health` (it has no failed-commit channel). `ThemeService.LastSaveFailure` shows a local note only. Settle a central place when the first real screen needs it (B1 will).
- Shared pieces land where first needed: ExercisePicker (C-14) in B2, Log weight sheet in B3. Don't pre-build.
- B0's More page has a temporary Appearance section and a Debug-only component showcase; B10 replaces Appearance with Settings (S-11).
- Deferred review items: IconCatalogue as a generated dictionary, StatusOrb Tone/Greyed merge, EmptyState closed switch, `--radius-*` vs `--r-*` token families, icons shipped twice (embedded and static), SVG provenance metadata (~1 MB) not stripped in the build.
- `InAppHistory` (back arrow fallback) is an approximation.

## Owner to do by hand (phone)
`docs/pwa-manual-checks.md`, B0 sections: install with the new icon, all four looks (Keep/Clean x Dark/Light), airplane-mode launch with font and icons, status bar colour, deep link and refresh on each tab at the live URL, 360 px width, increased text size, left rail on a wide window. Also still open from earlier stages: the A0 and A3 manual checks.

## Gotchas
- Don't run multi-line commit messages through bash heredocs with unbalanced quotes; write files with the Write tool.
- Windows: `gh` works from bash here; line endings warn about LF→CRLF, harmless.
- Agent worktrees: base them on the integration branch and check `git log` first (they sometimes start from an older commit).
