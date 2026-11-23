---
labels: ready-for-agent
stage: Hardening (B11)
---

# Hardening (B11)

Parent docs: [`../design/design-document.md`](../design/design-document.md) (§4.2 contrast, §4.7 touch targets, §4.9 motion, §9 responsive, §10 accessibility, C-12, C-13, C-14, C-15, C-23, S-01, mockup `S-01-home-desktop.png`), [`../pwa-manual-checks.md`](../pwa-manual-checks.md), [`../adr/0006-two-themes-keep-and-clean.md`](../adr/0006-two-themes-keep-and-clean.md), [`../../GLOSSARY.md`](../../GLOSSARY.md). Decisions come from the Hardening grill (23 Nov 2026, Q1–Q19).

## Problem Statement

Every screen is built, but nothing proves the app meets the accessibility and responsive rules the design document set out (§9, §10). Specifically:

- No automated check catches WCAG 2.1 AA violations on real screens, in any of the four Theme × Mode sets. The §4.2 contrast table was computed by hand once and is not re-checked when tokens change.
- Font sizes in the component styles are in `px`, so text does not scale with the browser or OS text size as §9 requires, and nothing checks that screens survive 360 px or larger text without scrolling sideways.
- From 900 px the tab bar is still a bottom bar (§9 asks for a 220 px left rail), and Home stays one column on a wide screen.
- Focus on navigation and sheet focus handling exist but are untested in a real browser, and status messages (Badge unlocked, export done, import applied) use live regions that appear only when needed, which screen readers often miss.
- Nobody has measured performance on a phone, and the manual phone checks from A0, A3, B0–B3, Editors and A4.5 PR 2 are still owed.

## Solution

Harden the finished app against §9 and §10 as written (WCAG 2.1 AA; any 2.2 items only if they turn up along the way), and lock that in with tests:

1. **Whole-app browser tests.** The browser tests serve the published app from their fake origin, load a small committed fixture through Restore, and scan every screen with axe in all four Theme × Mode sets, empty and with data. The same run checks for sideways scrolling at 360 px with normal and 200% text, the wide layout, focus behaviour and announcements.
2. **Token contrast test.** A unit test recomputes every §4.2 foreground/background pair from the tokens for all four sets.
3. **Text scaling.** Font sizes move to `rem`; borders, outlines, touch targets and radii stay in `px`.
4. **Wide layout.** From 900 px the TabBar (C-12) becomes a left rail with the same items, and Home uses the wide-first two-column layout from the mockup. The Workout log stays a single centred column.
5. **Focus and announcements.** Every page has exactly one `<h1>` (focus lands there after navigation). One always-present polite live region in the shell carries status announcements. Save failures stay `role="alert"`.
6. **Performance.** Measured on the owner's phone against a target; cheap build settings first if it misses.
7. **One combined manual phone pass**, covering the new Hardening checks and every owed check.

## User Stories

### Accessibility checks
1. As the owner, I want every screen scanned for WCAG 2.1 AA violations in CI, so that a later change cannot quietly break accessibility.
2. As the owner, I want the scan run in all four Theme × Mode sets (Keep dark, Keep light, Clean dark, Clean light), so that a colour problem in one set is not hidden by another.
3. As the owner, I want each screen scanned both empty and with data, so that empty states and filled states (charts, Set rows, Badges) are both covered.
4. As the owner, I want serious and critical axe violations to fail the build, so that the important problems are fixed rather than listed.
5. As the owner, I want any suppressed axe rule written next to the test with a one-line reason, so that I can see every exception in one place.
6. As the owner, I want moderate and minor axe findings recorded in the refactor backlog, so that they are not lost and do not block the PR.
7. As the owner, I want a test that recomputes contrast from the tokens for all four sets, so that a colour token change that breaks §4.2 fails at once, even for pairs no screen happens to show.
8. As the owner, I want a token pair that fails contrast fixed in the tokens (and the §4.2 table updated to match), so that the design document stays true.

### Text scaling and small screens
9. As the owner, I want text to grow when I raise my phone's text size, so that I can read the log mid-session without glasses.
10. As the owner, I want borders, focus outlines and touch targets to stay the same size when text grows, so that the layout does not become clumsy.
11. As the owner, I want no screen to scroll sideways at 360 px, so that every screen works on a small phone.
12. As the owner, I want no screen to scroll sideways at 360 px with text at 200%, so that large text never hides controls off-screen.
13. As the owner, I want long Exercise names to wrap and tile names to ellipsis with the full name in the accessible name, so that large text stays usable and screen readers hear the full name.

### Wide screens
14. As the owner on a tablet in landscape or a desktop, I want the tab bar to become a left rail, so that navigation uses the spare width rather than the bottom of a tall screen.
15. As the owner, I want the rail to show exactly the same items as the tab bar, including More, so that I never have to learn a second navigation.
16. As the owner, I want each rail item to show its icon beside its word, so that the rail is easy to scan.
17. As the owner, I want the rail to stay visible on the Workout log, so that I can leave the log the same way as any other screen.
18. As the owner, I want the Workout log to stay a single centred column of at most 430 px on wide screens, so that logging looks and works like it does on my phone.
19. As the owner, I want Home on a wide screen to show the TodayCard, week strip, orb row (Streak, Phase, days to race) and Nudges in the main column and Recent PRs and Weight beside them, so that I see the whole week at a glance (mockup `S-01-home-desktop.png`).
20. As the owner, I want screens between 431 and 899 px to keep the 560 px centred column, so that large phones and small tablets stay readable.
21. As the owner, I want the wide layout checked in the browser tests, so that the rail and Home's two columns do not regress.

### Focus and keyboard
22. As a keyboard or screen-reader user, I want focus to move to the page heading after each in-app navigation, so that I hear where I have landed.
23. As a keyboard or screen-reader user, I want every page to have exactly one `<h1>`, so that focus on navigation always has one clear target.
24. As a keyboard user, I want focus to start inside a sheet when it opens, so that I can act on it straight away.
25. As a keyboard user, I want Tab to stay inside an open sheet, so that I cannot wander into the page behind it.
26. As a keyboard user, I want Esc to ask the sheet to close, so that I can leave a sheet without hunting for a button.
27. As a keyboard user, I want focus to return to the control that opened the sheet, so that I keep my place on the page.
28. As a keyboard user, I want each of PainCheckSheet (C-13), ExercisePicker (C-14), DateRangePicker (C-15) and the confirm-style sheets checked in a real browser, so that the focus handling is proven, not assumed.
29. As a keyboard user, I want a visible 3 px focus outline on every interactive control, so that I can always see where I am.

### Announcements
30. As a screen-reader user, I want "Badge unlocked" announced politely, so that I hear the reward without being interrupted mid-action.
31. As a screen-reader user, I want new PRs announced politely as they happen in the log, so that I hear progress without looking.
32. As a screen-reader user, I want the rest timer's end announced once, not every second, so that I know when to start the next Set without constant chatter.
33. As a screen-reader user, I want export, Backup and import results announced politely, so that I know the action worked.
34. As a screen-reader user, I want a failed save announced at once (assertively), so that I never think data was saved when it was not.
35. As a screen-reader user, I want announcements to come from one region that is always on the page, so that they are reliably read out.

### Motion
36. As someone who asks for reduced motion, I want the button press nudge and the toast fade switched off, so that nothing moves.

### Performance
37. As the owner, I want a Lighthouse mobile score of at least 90 on the deployed app, so that the app feels quick on a mid-range phone.
38. As the owner, I want Home interactive in under 3 s on a warm start on my phone, so that opening the app at the gym is not a wait.
39. As the owner, I want the measurements recorded in the manual checks, so that I can compare later builds.
40. As the owner, I want cheap build settings tried first if a target is missed, so that Hardening does not turn into a large performance project.

### Manual checks
41. As the owner, I want one combined phone pass covering the Hardening checks and every owed check, so that I test on the phone once rather than piecemeal.
42. As the owner, I want the results recorded in the manual checks document with the date and build, so that STATUS can stop listing them as owed.

## Implementation Decisions

### Whole-app browser host (seam A)
- The browser test project gains a whole-app host alongside the existing storage fixture. A test run publishes the Web project once (Release) and serves the published output from the existing fake origin through Playwright routing, so no test page or test-only code ships and no real web server is needed. The service worker is not needed for these tests and may be blocked by the routing.
- The existing rule stands: the default `dotnet test` skips browser tests; they run with the browser-tests opt-in and in the browser-tests CI job, which gains a publish step before testing.
- A helper opens a page at a chosen viewport, Theme, Mode, text scale and reduced-motion preference, waits until Blazor has rendered, and exposes the page for assertions. Theme and Mode are set through the app's real preference mechanism (or the theme script's storage), not by injecting styles.
- **Fixture:** a small hand-written Backup committed with the browser tests: one active Plan with two Tracks, Sessions across a week, a few logged Workouts including at least one PR, one unlocked Badge, Weigh-ins, and a Guide. It is restored through the app's real Import/Restore path. It must be valid against the current exchange format version and contain no private data. The A5 example dataset is git-ignored and is not used.
- Each test uses a new browser context and a uniquely named database, as the storage tests do.

### Accessibility scans
- axe-core is loaded into the page by the tests (from a pinned package bundled with the test project, not a CDN at run time) and run against WCAG 2.0/2.1 A and AA tags.
- One parameterised test per screen and Theme × Mode set, run empty and with the fixture. The screens are S-01 to S-13 at their routes, plus the open state of each sheet that needs data to appear.
- Serious and critical violations fail. Suppressions are listed in one place in the test project, each with the rule, the selector and a one-line reason; only third-party or proven false positives may be suppressed. Moderate and minor findings found while building are added to the refactor backlog, not asserted.

### Overflow checks
- For every screen, empty and with the fixture, at 360 px wide: the document's scroll width must not exceed the viewport width, at the normal root font size and with the root font size at 200%.

### Token contrast test (seam B)
- A pure test in the Ui test project reads the design tokens stylesheet, resolves each of the four Theme × Mode sets, and computes the WCAG contrast ratio for every pair §4.2 lists, asserting 4.5:1 for text and 3:1 for large text and non-text UI (borders, focus outline, icons) as §4.2 classifies them.
- The pair list is defined once in the test, taken from §4.2. A failing pair is fixed in the tokens and the §4.2 table is updated in the same change.

### Text scaling
- Font-size tokens and every font size in the component and page styles move from `px` to `rem` (based on a 16 px root). Borders, outlines, outline offsets, touch-target minimums (`tap-min`, `tap-log`, `tap-primary`), radii and shadows stay in `px`. Spacing tokens stay as they are.
- Line heights become unitless where they are not already, so they scale with text.
- Tile names that ellipsis carry the full name in their accessible name (§9).

### Wide layout
- The TabBar (C-12) is the only navigation component: from 900 px the shell lays it out as a fixed 220 px left rail, items stacked, icon beside word, same items and order as the bottom bar including More. The shell's content area is offset by the rail. Safe-area padding moves with it (bottom for the bar, left for the rail where relevant).
- The Workout log keeps the rail and stays a single centred column capped at 430 px; its ConfirmBar spans the log column, not the rail.
- Home adopts the existing shared wide-first two-column rule from 900 px: main column holds the TodayCard, WeekStrip, orb row (Streak, Phase, days to race) and Nudges; the side column holds Recent PRs and Weight, grouped as in `S-01-home-desktop.png`. First run (Welcome, Two quick things) stays single column.
- 431–899 px keeps the existing 560 px centred column.
- Plan editor list-and-detail at 900 px is **not** built (see Out of Scope).

### Focus
- Focus on navigation already targets the page `<h1>`; every page and route state (including empty, read-only and not-found states) must render exactly one `<h1>`, focusable programmatically and with no visible outline on that programmatic focus.
- The shared Sheet already provides labelled modal dialog semantics, initial focus, Tab containment, Esc-to-request-close and focus return. It is not rewritten to native `<dialog>`; it is proven by browser tests. Any surface found not using the shared Sheet is moved onto it.

### Announcements
- The shell gains one always-present, visually hidden polite live region and a small injectable announcer service in the Ui project (beside the existing save-failure notice) that components call with a message. Repeated identical messages are still announced (the region is cleared and re-set).
- Routed through it: Badge unlocked (C-23), new PR in the log, rest timer end (once, when it reaches zero; the timer itself stays `aria-live="off"`), export, Backup and import results, and the existing per-page polite lines where they announce events rather than show state.
- The save-failure banner stays `role="alert"` (assertive). No other assertive announcements are added. Visible status text stays on screen as it is today; only the announcement moves.

### Reduced motion
- Already in place globally; the browser tests confirm that with reduced motion requested the toast has no animation and pressed buttons do not move.

### Performance
- Measured by hand on the deployed Pages build: Lighthouse mobile (target ≥ 90) and a warm-cache cold start to interactive Home on the owner's phone (target < 3 s). Results are recorded in the manual checks document with date and build identifier.
- If a target is missed, try in this order: confirm IL trimming is effective, turn off loading all globalisation data / use invariant globalisation where no culture-specific behaviour is needed (dates and numbers are already formatted by the app's own rules), and make sure compressed (Brotli) assets are served. AOT compilation and lazy-loaded assemblies are not attempted; if still short, record the gap and propose a follow-up spec.

### Delivery
- One PR from `feature/hardening`, seven tickets in this order, one implementer at a time:
  1. Whole-app browser host and the fixture.
  2. axe scans, overflow checks and the token contrast test (failing cases marked as known where the fix belongs to a later ticket, then cleared there).
  3. `rem` conversion and the fixes the scans find.
  4. Left rail and Home two columns.
  5. Focus (`<h1>` audit, sheet tests) and the live region.
  6. Performance measurement and fixes.
  7. Combined manual phone pass.

## Testing Decisions

- Good tests check what the owner sees and hears (rendered page, focus position, accessible names, live-region text, layout boxes), never component internals or CSS class names beyond what a selector needs.
- **Seam A (browser, whole app)** covers axe, overflow, the rail and Home's two columns (by element positions at 900+ px vs 360 px), focus after navigation, sheet focus containment / Esc / return for C-13, C-14, C-15 and a confirm-style sheet, live-region announcements (Badge unlocked, rest timer end, a PR, an import result), and reduced motion. Prior art: the existing Playwright storage tests and `ThemeScriptBrowserTests` (fake origin, routing, new context per test).
- **Seam B (unit)** covers token contrast for all four sets. Prior art: the existing pure tests in the Ui test project.
- **bUnit** tests are updated only where markup changes (Home's columns, components that now call the announcer). New bUnit tests cover the announcer service's behaviour (repeat messages re-announce). No other new seams.
- `dotnet build` stays at 0 warnings; `dotnet test` and the browser-tests job stay green.

## Out of Scope

- WCAG 2.2-specific criteria (unless one turns up while fixing 2.1 issues), and a full screen-reader walk-through of every flow beyond the checks above.
- Plan editor list-and-detail at 900 px (logged in the build plan as a later item).
- Two-column layouts for screens other than Home beyond what already exists (Progress, Weekly review, Weight already use the shared rule).
- AOT compilation, lazy-loaded assemblies, and automated performance budgets in CI.
- Rewriting the shared Sheet to native `<dialog>`.
- Any new feature, screen or data change.

## Further Notes

- The grill asked for native `<dialog>` for modal surfaces "wherever a surface isn't already one"; the shared Sheet already gives the same behaviour, so it is tested, not replaced (confirmed with the owner, 23 Nov 2026). Save failures stay assertive (`role="alert"`) as an agreed exception to "polite only".
- Focus on navigation already exists; this stage audits and tests it rather than adding it.
- The combined manual pass should also clear the STATUS caveat about owed phone checks.
