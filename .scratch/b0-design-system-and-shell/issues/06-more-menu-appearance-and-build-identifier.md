# 06: More menu, Appearance and build identifier

**What to build:** The More tab lists the secondary screens as links (Plans, Exercise library, Weight, Weekly review, Export and Import, Settings), each a placeholder route with an EmptyState, so later stages fill content without touching navigation. A temporary Appearance section on the More page holds the Theme and Mode pickers (absorbing ticket 03's stub page and the base-component gallery) until Settings in B10 replaces it. The build identifier stays visible on the More page because the deploy checks use it. Finishes B0: the owner can install the app, see five tabs, reach every future screen's route, flip all four looks and use it in airplane mode. Spec: `docs/specs/b0-design-system-and-shell.md`. Design: `docs/design/design-document.md` (§3, §6 S-11 for what replaces Appearance).

**Blocked by:** 03 (Theme switching end to end), 05 (App shell: layouts, TabBar and routes)

**Status:** ready-for-agent

- [ ] The More page lists every secondary entry as a link to a routed placeholder; the active tab stays More on those pages
- [ ] Appearance holds the working Theme and Mode pickers; the temporary stub page and gallery are removed or folded in so no duplicate controls remain
- [ ] The build identifier is shown on the More page and the existing build-identifier test still passes at its new location
- [ ] bUnit tests cover the More links and that choosing a Theme from Appearance updates the app
- [ ] Manual checks completed on a phone and recorded in `docs/pwa-manual-checks.md`: install with the new icon, all four looks, airplane-mode launch, status bar colour, deep link on each tab, 360 px width, increased system text size
- [ ] `docs/build-plan.md` marks B0 implemented (reviews and PR still to do); all app, unit and browser tests green
