# 06: Page headings and sheet focus

Spec: `docs/specs/hardening-b11.md` (Implementation Decisions: Focus, Reduced motion).

**What to build:** Make focus after navigation reliable, and prove the shared Sheet's focus handling in a real browser. Audit every page and route state (including empty, read-only and not-found states) so each renders exactly one `<h1>`, programmatically focusable, with no visible outline on that programmatic focus. Move any modal surface not using the shared Sheet onto it. Add browser tests for focus after navigation, and for PainCheckSheet (C-13), ExercisePicker (C-14), DateRangePicker (C-15) and one confirm-style sheet: focus starts inside, Tab stays inside, Esc asks to close, and focus returns to the opener. Add a browser check that with reduced motion the unlock toast has no animation and pressed buttons do not move. The Sheet is not rewritten to native `<dialog>`.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] Every routed page state renders exactly one `<h1>` (asserted in browser tests over all routes, empty and fixture).
- [ ] After in-app navigation, focus is on the new page's `<h1>`.
- [ ] For each of the four sheets: initial focus inside, Tab and Shift+Tab stay inside, Esc closes (or is refused where the sheet's owner refuses), focus returns to the opener.
- [ ] The reduced-motion check passes.
- [ ] `dotnet build` 0 warnings; all tests green.
