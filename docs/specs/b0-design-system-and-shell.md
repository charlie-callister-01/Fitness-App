---
labels: ready-for-agent
stage: B0
---

# B0: Design system and app shell

Parent docs: [`../build-plan.md`](../build-plan.md) (Track B), [`../design/design-document.md`](../design/design-document.md) (§3 navigation, §4 tokens, §5 components C-01, C-11, C-12, C-21, §9 responsive, §11 build handoff), [`../design/assets/README.md`](../design/assets/README.md), [ADR 0006](../adr/0006-two-themes-keep-and-clean.md) (two Themes), [ADR 0003](../adr/0003-local-first-indexeddb-storage-behind-an-interface.md) (storage), `../../GLOSSARY.md` (Theme, Mode).

## Problem Statement

The design is finished and every later UI stage (B1 to B11) needs the same foundation, but the app today is a placeholder: one page that says "Fitness App", a system font, no navigation, no Themes, and placeholder icons. If each screen stage had to bring its own tokens, Theme switching, navigation and base components, they would each make slightly different choices and the Keep and Clean Themes would drift apart. The owner also cannot yet see what the app will feel like on the phone, or check that the Themes, fonts and icons survive airplane mode.

## Solution

B0 builds the foundation once, with no feature screens:

1. **The design system in the app.** The design's tokens, component styles, display font, icons and graphics ship inside the app, so every screen later uses names like `--space-3` and classes like `fa-btn` and never invents its own.
2. **Two Themes, two Modes, live.** The owner's choice of Theme (Keep or Clean) and Mode (System, Dark or Light) is a stored Setting that restyles the whole app instantly, with no flash of the wrong look when the app opens, and with the browser chrome colour following it.
3. **The app shell.** A bottom tab bar on phones and a left rail from 900 px wide, five destinations (Home, Week, Progress, Trophies, More), a More menu listing the secondary screens, and a layout with no navigation for full-screen pages (the Workout log, built in B1). Every destination exists as a real route showing a proper empty state, so later stages replace content and never touch navigation.
4. **The base components** every later stage reuses: Button (C-01), StatusOrb (C-11), TabBar (C-12), EmptyState (C-21), and one Icon component over the design's icon set.
5. **The real PWA identity.** The designed app icons, manifest colours and favicon replace the placeholders, and the offline cache is fixed so the font, icons and artwork work in airplane mode.
6. **A temporary Appearance control** (Theme and Mode pickers) so the owner can try all four looks on a phone. Settings (S-11) replaces it in B10.

The owner sees this as: open the app, see the designed look and the five tabs, flip between Keep and Clean and dark and light, install it with the real icon, and switch to airplane mode and see it all still work.

## User Stories

### Looks and Themes
1. As the owner, I want the app to open in the Keep Theme by default, so that it has the fun look I chose without any setup.
2. As the owner, I want to switch between the Keep and Clean Themes, so that I can use the plain look when I just want to log.
3. As the owner, I want to choose Dark, Light or Follow system as the Mode, separately from the Theme, so that bright gyms and dark evenings both work.
4. As the owner, I want the Theme and Mode to apply the instant I pick them, so that I can judge the look without reloading.
5. As the owner, I want my Theme and Mode remembered, so that I never have to set them again.
6. As the owner, I want my Theme and Mode included in a Backup and restored with it, so that a new phone looks like my old one.
7. As the owner, I want the app to open already in my chosen Theme and Mode, with no flash of the default look first, so that it feels solid rather than glitchy.
8. As the owner on Follow system, I want the app to change when my phone switches between dark and light, so that it follows sunset without a reload.
9. As the owner, I want the browser bar and status bar colour to match the Theme and Mode, so that the app looks like one piece on my phone.
10. As the owner, I want the same layouts, sizes and touch targets in both Themes, so that switching Theme never moves a button.
11. As the owner, I want number inputs and other form controls to stay plain and high contrast in both Themes, so that the fun look never makes logging harder.
12. As the owner, I want all text and controls to meet the contrast ratios in the design for all four Theme and Mode combinations, so that I can read the app in bright light.
13. As the owner, I want state to never depend on colour alone (icon or label as well), so that nothing is ambiguous.

### Navigation and layout
14. As the owner on my phone, I want a bottom tab bar with Home, Week, Progress, Trophies and More, so that I can reach the main areas with my thumb.
15. As the owner, I want the current tab clearly marked, so that I always know where I am.
16. As the owner, I want a More menu listing Plans, Exercise library, Weight, Weekly review, Export and Import, and Settings, so that the rare screens are one tap away but out of the way.
17. As the owner, I want a back arrow instead of the tab bar on pushed pages (such as a Guide), so that detail pages feel like detail pages.
18. As the owner, I want the tab bar hidden on full-screen pages like the Workout log, so that a stray tap cannot leave a Workout.
19. As the owner on a tablet or desktop (900 px and wider), I want the tab bar to become a left rail, so that the layout fits the bigger screen.
20. As the owner on a large phone or small tablet, I want the content kept in a centred column of readable width, so that nothing stretches across the screen.
21. As the owner on a 360 px wide phone, I want nothing to scroll sideways or clip, so that small phones work.
22. As the owner on a phone with a notch or gesture bar, I want the bars to respect the safe areas, so that nothing is hidden under them.
23. As the owner, I want deep links and refreshes to land on the right tab, so that reloading never throws me back to the start.
24. As the owner, I want a plain "Page not found" that offers a way back to Home, so that a bad link is not a dead end.
25. As the owner using a screen reader, I want the tab bar to be a labelled navigation landmark with the current tab announced, so that I can navigate by ear.
26. As the owner using a keyboard, I want a visible focus outline everywhere and a skip-to-content link, so that I can drive the app without a mouse.

### Components
27. As the owner, I want primary and secondary buttons in the bevelled Keep style or the flat Clean style, so that actions look right in either Theme.
28. As the owner, I want every button at least 44 px square (primary actions 64 px tall), so that I can hit them one-handed.
29. As the owner, I want disabled buttons to look clearly disabled and be unclickable, so that I know what is not available.
30. As the owner, I want icon-only buttons to have a spoken name, so that they are usable with a screen reader.
31. As the owner, I want a ringed gauge showing one number and its progress, so that Streak, Phase and race countdown read at a glance.
32. As the owner, I want the gauge to read as text to a screen reader ("Streak, 4 weeks, 2 of 3 this week"), so that it is not just a picture.
33. As the owner, I want the gauge to show a calm, greyed look when I am on a Break, so that it never looks broken.
34. As the owner, I want screens with nothing to show to display a friendly illustration, a line of encouragement and one action, so that they are never just blank.
35. As the owner offline, I want a calm "No signal. Everything else works offline." state only where something truly needs a connection, so that offline never feels like a failure.
36. As the owner, I want the icons to follow the text colour of whatever they sit in, so that they work in both Themes and Modes.

### Install and offline
37. As the owner, I want the installed app to show the designed icon on my home screen, so that it looks finished.
38. As the owner, I want the app icon to look right in the round, squircle and themed-icon shapes my phone uses, so that it is never clipped or tiny.
39. As the owner, I want the splash screen colours to match the default Theme, so that opening the app does not flash white.
40. As the owner, I want the display font, icons, badges and illustrations to work in airplane mode, so that the app looks the same offline.
41. As the owner, I want a new version of the design to reach my phone through the normal update path, so that I do not have to reinstall.
42. As the owner, I want the app's size to stay small, so that it loads quickly on a poor signal.

### Safety
43. As the owner, I want a Theme or Mode that fails to save to be reported plainly while the app still shows the choice for this session, so that a storage problem never breaks the screen.
44. As the owner, I want an unknown or missing Theme or Mode value (from an older Backup) to fall back to Keep and Follow system, so that restoring an old Backup never breaks the look.
45. As the owner, I want reduced-motion settings respected, so that nothing animates when my phone asks for none.
46. As the owner, I want text to grow with the phone's text size setting without breaking the layout, so that the app works at larger sizes.

## Implementation Decisions

### Settings: Theme and Mode become stored Settings (owner's decision)
- `Settings` in Core gains two display fields: **Theme** (`keep` or `clean`) and **Mode** (`system`, `dark` or `light`). Defaults: Keep and System. They are display preferences only: they never affect any stored measurement or rule.
- **Additive, no format bump.** The data spec says adding optional fields does not bump `formatVersion`. In the exchange JSON the two fields are optional (`theme`, `mode`) on the `settings` object; absent means the default; they are written when the writer writes Settings. `Settings.Default` and the existing positional construction in tests and fixtures keep compiling (the new members have defaults). The reader accepts an unknown or malformed value by falling back to the default for that one field and reporting a format issue per the existing reader conventions (a warning, never a failure to import); user story 44.
- `docs/data-spec.md` is updated (Settings table, first-run defaults, example) in the same change. The spec's conformance test is extended so the document and the reader agree.
- **Merge note.** Stage A4 (`a4-implementation`) touches the exporter and importer, which read and write Settings. The first ticket must be rebased on whatever A4 has merged by then, and A4's tests must keep passing; if A4 has not merged, the change stays additive so the two merge cleanly.

### Applying the Theme without a flash
- The Theme and Mode are applied by setting `data-theme` and `data-mode` on the `<html>` element (design §11). "System" resolves with the browser's colour-scheme media query, and a listener updates the app when the phone's scheme changes. The resolved value is what `data-mode` holds; the stored value stays "system".
- The stored Settings load asynchronously (IndexedDB, then WebAssembly start), which is too late to avoid a flash. So a **tiny inline script in the host page** applies a locally cached copy of the choice before first paint. This cache is a convenience mirror only: it is written whenever the Setting changes, **Settings is always the source of truth**, and after startup the app reconciles the `<html>` attributes to Settings (and rewrites the mirror) if they differ, for example after a Backup restore. The mirror is wrapped so that blocked or cleared browser storage just means the default look for a moment.
- The inline script and the Blazor-side switching share one small JavaScript module for the resolution and application logic, so there is one implementation. The mirror key and the attribute names are defined in one place each.
- The browser chrome colour (`<meta name="theme-color">`) follows the Theme and Mode, using the values in the design's `splash.json` (Keep dark `#4A4237`, Clean dark `#111315`, Clean light `#F4F5F7`, and a Keep light value taken from the tokens). The manifest's `background_color` and `theme_color` use the default Keep dark values (`#2B2620`, `#4A4237`).

### A Theme service in Ui
- A small service in `Ui` owns "the current Theme and Mode": it reads them from `AppData`'s snapshot, commits a change as a Settings change through the normal commit path (so it is saved atomically and reported if it fails), raises a change event, and calls the JavaScript module to apply the result. Components read it and never touch `<html>` or storage themselves.
- JavaScript is reached through a narrow interface in `Ui` (apply Theme and Mode, set chrome colour, subscribe to system scheme changes) so the service is testable with a fake. The real implementation lives in `Web`, next to the existing storage interop.
- **Failure behaviour (story 43).** If the commit fails, the choice is still applied for this session, the failure is surfaced through the app's existing storage-health reporting, and the next load shows the stored value. The service never throws into the UI.

### Design assets move into the app
- Copied from `docs/design/assets/` and made the app's own: `tokens.css`, `components.css`, the Jersey 15 font with its OFL licence text, `icons/`, `graphics/`, `illustrations/`, `badges/`, and the `app-icon/` exports. They are **copied, not linked**, because `docs/` is not part of the build. The design document remains the source of truth for what the tokens mean; if an asset must change, the copy and `docs/design/assets/` change together in the same pull request, and `docs/design/assets/README.md` stays accurate.
- The shared static assets (tokens, component styles, font, icons, graphics) live in the `Ui` project's static web assets so every screen and test host gets them; the host-specific files (manifest, favicon, PWA icons, inline Theme script) live in `Web`. `tokens.css` and `components.css` are loaded from the host page in that order, **before** any component-scoped CSS.
- **Plain CSS with tokens, no component library** (design §11). Layout uses Blazor CSS isolation (`Component.razor.css`); the token-driven Theme skin classes (`fa-*`) stay global so a Theme applies everywhere. Every colour, spacing, radius, size and border in component CSS comes from a token; a raw hex value in component CSS is a defect. The old placeholder styles (`ui.css`, `app.css` page rules) are removed or reduced to what the shell still needs.
- The design's Theme intensity rules (design §1) are a build rule: skin classes are defined once here, and later screens choose an intensity class rather than restyling.
- The font is self-hosted and never loaded from a CDN (design §11). The web font uses `font-display: swap`.

### Offline cache must cover the new asset types (found while exploring)
- The service worker's list of cached file types does not currently include **`.woff2`** or **`.svg`**, so the font, icons, badges and illustrations would work online and quietly vanish in airplane mode. Both are added to the include list. A regression test checks that every static file type the Ui and Web projects ship is matched by the include list, so a future asset type cannot silently go missing. The service worker's other behaviour (base-path-safe URLs, update path, `service-worker.js` excluded) does not change.
- The manual checks in `docs/pwa-manual-checks.md` gain steps for the font, an icon and a Theme in airplane mode.

### App icons, manifest and favicon
- The manifest icons are replaced with the design's exports: any 192 and 512, maskable 192 and 512, and the monochrome 512 (with the `monochrome` purpose); the Apple touch icon is replaced with the 180 px export; the favicon is the design's SVG with the 32 and 16 px PNG fallbacks. Manifest `name`, `short_name`, `start_url`, `scope`, `id` and the base-path-safe behaviour are unchanged. The old placeholder icon files are deleted.

### App shell and routing
- A **main layout** in `Ui` provides the shell: the TabBar on phones, the left rail from 900 px, the centred content column (cap 560 px from 431 px up), safe-area padding, a skip-to-content link and a main landmark. A second, **full-screen layout** has no navigation and is used by pages that opt in (the Workout log in B1); a third, **pushed-page layout** shows a top bar with a back arrow and no tab bar. Which layout a page uses is declared on the page, not decided by the shell.
- **Routes** for every destination exist now, each a thin placeholder page that renders an EmptyState naming what will live there, so later stages replace page content only: Home, Week, Progress (with Benchmarks and Calendar inside it later), Trophies, and More. The More page lists its entries as links to routes that also exist as placeholders (Plans, Exercise library, Weight, Weekly review, Export and Import, Settings); a temporary **Appearance** section on the More page holds the Theme and Mode pickers until Settings (S-11) lands in B10, which then moves it.
- The existing placeholder Home page (app name and build identifier) is replaced by the Home placeholder; the build identifier stays visible somewhere reachable (on the More page) because the deploy checks use it.
- The router keeps its current structure; the not-found page becomes a friendly page with a link to Home, in the main layout.
- The active tab follows the current route, including nested routes (a Guide opened from the library keeps More active). The tab bar and left rail are one component with two presentations chosen by CSS, not two components, so there is one set of labels and one set of tests.

### The base components (Ui, one Razor component per design C-number)
- **Button (C-01):** Primary, Secondary, Stepper and Icon variants; disabled state; a required accessible name for the Icon variant (the component refuses to render an icon button without one, which the test pins). Renders a button or a link depending on whether it is given a destination, with the same classes.
- **StatusOrb (C-11):** takes a value, a ring fraction (0 to 1), a caption and an accessible label, plus a "greyed" flag for Breaks. Pure inline SVG, no animation. The fraction is clamped, and a non-finite value renders the empty ring.
- **TabBar (C-12):** the five destinations with icon, 11 px label and `aria-current="page"` on the active one; a labelled navigation landmark.
- **EmptyState (C-21):** illustration, bold line, muted line, one primary action (optional); the four illustration variants (first run, no Plan or no Guide, no history, offline). The Clean Theme shows the same art at reduced opacity, handled in CSS.
- **Icon:** renders one named icon from the design's set inline so it inherits `currentColor`. Names are a closed list, so a typo is a compile-time error rather than a blank square. A test checks that every name in the list has a file and every shipped icon file has a name, so the two cannot drift.
- Nothing in this stage invents a component not in the design. Components later stages need (SetRow, TodayCard and so on) are out of scope.

### Accessibility and responsiveness built in
- Minimum touch targets (44 px, primary 64 px), visible 3 px focus outline, `rem` sizing for text, `prefers-reduced-motion` honoured, and `env(safe-area-inset-*)` padding are part of the base styles, not left to each screen. The existing global 44 px minimum on interactive elements is kept.
- Contrast for all four Theme and Mode combinations is the design's computed table (§4.2); B0 uses the token values unchanged, and the hardening stage (B11) re-verifies them.

### Not changed
- Core's rules, serialisation reader structure, storage interfaces and the IndexedDB module (apart from the two additive Settings fields and the data spec).
- The deployment workflow, base-path handling, 404 redirect and publish script.

## Testing Decisions

- **What makes a good test here:** it renders or runs the thing the owner would use and asserts what they could see or do (text, roles, which tab is current, which attribute is on `<html>`, what is saved), not component internals, CSS class names beyond the documented `fa-*` contract, or markup structure that is free to change.
- **Main seam: bUnit on the `Ui` components**, as `HomeTests` does today. The shell, TabBar, More menu, Button, StatusOrb, EmptyState and Icon are tested by rendering them with a fake Theme interop and a real `AppData` over the in-memory store (Core's existing in-memory store and fixed clock). The Theme service is tested at the same seam: choose a Theme, assert the fake interop received the right Theme and resolved Mode, assert Settings now holds it, assert a failing store (the existing fault-injecting store) leaves the choice applied and reports the failure.
- **Core seam: existing serialisation tests.** Settings round-trip with and without the new fields, absent fields take the defaults, an unknown value falls back and reports a format issue, an older document with no `theme` or `mode` reads unchanged. Extend `DataSpecConformanceTests` and the existing Settings round-trip tests; follow the pattern of the existing field-by-field tests in the Serialisation folder.
- **Offline asset coverage: one file-based test** in `Web.Tests` that reads the service worker's include list and asserts every static file extension shipped by `Ui` and `Web` matches it (this is the test that would have caught the missing `.woff2` and `.svg`). No browser needed.
- **One browser check: Playwright in `Web.BrowserTests`**, the only thing bUnit cannot show: the real JavaScript module and inline script apply the stored choice to `<html>` before first paint, "system" follows a changed colour-scheme preference, and a cleared or blocked mirror falls back to the default without an error. Prior art: the existing browser tests serve the real `wwwroot` files from a fake origin through Playwright routing, and run in their own CI job. The C# service is not run in WebAssembly there (as for storage), and the Blazor interop in a real WebAssembly runtime is checked by hand in `pwa-manual-checks.md`.
- **Manual checks (added to `docs/pwa-manual-checks.md`):** on a real phone, install with the new icon (round and squircle masks, themed icon), all four looks, airplane-mode launch with the font, icons and illustrations present, the status bar colour following the Theme, a deep link and refresh on each tab, 360 px width with no sideways scroll, and increased system text size.
- **Not unit tested:** the visual appearance of the CSS (checked against the design's mockups by eye, in the manual checks); contrast ratios (the design's table is the record; B11 re-checks).
- The 44 px target rule, `aria-current`, the Icon-button accessible-name rule and the clamped StatusOrb are each pinned by a test, because they are the rules later stages most easily break.

## Out of Scope

- Every feature screen: Home content, the Workout log, Guides, Plans, Week view, Progress, Trophies, Weekly review, Export and Import screens, Settings (S-11) and First run (S-12). B0 delivers their routes and placeholders only.
- All other components in the design (C-02 to C-10, C-13 to C-20, C-22 to C-24).
- The final Settings screen and Theme live preview (B10); B0's Appearance section is temporary.
- The install prompt, persistent-storage request, Wake Lock and rest timer.
- Charts, Badge artwork use (the files ship, nothing renders them yet), and the unlock toast.
- A component library or CSS framework; Sass or other build steps for CSS.
- Changing the design. Where the design is silent or wrong, B0 follows it and records the question for the owner rather than redesigning; the Keep light "sandstone" Theme's lower character noted in the design is accepted as is.
- RuneScape or other third-party fonts (design §12): not used or shipped.
- Core rules, storage behaviour, the deployment workflow and base-path handling.
- Translating or re-recording the design mockups.

## Further Notes

- **Seam check (settled with the owner):** bUnit on `Ui` is the main seam; one Playwright check for pre-paint Theme application; one file-based test for the offline cache types.
- **Decision recorded with the owner:** Theme and Mode live in Core's `Settings` (so a Backup carries them) rather than only in device storage. The cost is the data-spec change and the pre-paint mirror described above. This is an additive format change (no `formatVersion` bump), and it should be noted in the stage PR so the next change to the exchange format does not rediscover it.
- **Sizing for `/to-tickets`:** likely cuts, each within one agent's budget: (1) Core Settings fields, reader, data spec and conformance tests; (2) design assets copied in, tokens and component styles loaded, font and offline-cache include list and its test, real PWA icons and manifest; (3) the Theme JavaScript module, inline pre-paint script, interop interface, Theme service and its tests, browser check; (4) base components Button, Icon, StatusOrb, EmptyState with tests; (5) shell layouts, TabBar and left rail, routes and placeholders, More menu with the temporary Appearance section, not-found page, manual-check notes. The first ticket must start from an up-to-date `main` that includes A4 if A4 has merged.
- **Cheaper alternatives from the design (§11) apply:** no stone-texture animation, a single tiled SVG noise or none, one badge frame plus glyphs. B0 should use the cheaper option wherever the design offers one.
- **Design open issues carried forward (design §12):** Keep light is dark-first and less characterful; the display font must not be swapped for a public release; Wake Lock and the rest timer are for later stages.
