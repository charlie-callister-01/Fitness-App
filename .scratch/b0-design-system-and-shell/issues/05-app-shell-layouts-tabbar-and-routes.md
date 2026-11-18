# 05: App shell: layouts, TabBar and routes

**What to build:** The app has its permanent frame. A main layout shows the five-destination TabBar (Home, Week, Progress, Trophies, More) at the bottom on phones and as a 220 px left rail from 900 px wide, with the content column capped at 560 px from 431 px up, safe-area padding, a skip-to-content link and a main landmark. A full-screen layout (no navigation, for the Workout log in B1) and a pushed-page layout (top bar with back arrow, no tab bar) exist and a page chooses its layout by declaring it. Routes for Home, Week, Progress and Trophies exist as placeholder pages showing an EmptyState; the not-found page is friendly and links home. The old placeholder Home page is replaced. The TabBar and the rail are one component with two presentations chosen in CSS. The active tab follows the route including nested routes. Deep links and refresh land on the right tab, including under the sub-path deploy. Spec: `docs/specs/b0-design-system-and-shell.md`. Design: `docs/design/design-document.md` (§3, §5 C-12, §9).

**Blocked by:** 02 (Design assets, offline cache and PWA identity), 04 (Base components)

**Status:** ready-for-agent

- [ ] Five tabs with icon and 11 px label, `aria-current="page"` on the active tab, a labelled navigation landmark; a nested route keeps its parent tab active (pinned by tests)
- [ ] At 360 px nothing scrolls sideways or clips; 431 to 899 px centres a 560 px column; 900 px and wider shows the left rail
- [ ] A full-screen page shows no tab bar; a pushed page shows a back arrow and no tab bar
- [ ] Placeholder routes and the not-found page render EmptyStates; the not-found page offers a way to Home
- [ ] Skip link works by keyboard; focus outline visible; the router still scans the Ui assembly and focuses the page heading on navigation
- [ ] bUnit tests cover tab marking, layout selection and routing; existing Home and build-identifier behaviour is preserved until ticket 06 moves it
- [ ] Deep link, refresh and base-path checks added to `docs/pwa-manual-checks.md`; app and tests green
