# 02: Design assets, offline cache and PWA identity

**What to build:** The design's tokens, component styles, display font, icons, graphics, illustrations and badge artwork ship inside the app and load in the right order, so every later screen can use them. The service worker caches the new file types so the font, icons and artwork work in airplane mode. The installed app shows the designed icon and splash colours. Assets are copied (not linked) from `docs/design/assets/`; shared static assets live with the Ui project, host-specific files (manifest, favicon, PWA icons) with Web. Tokens load before components, before any component-scoped CSS. The old placeholder styles and icons are removed. Plain CSS with tokens, no component library. Spec: `docs/specs/b0-design-system-and-shell.md`. Design: `docs/design/design-document.md` (§4, §11, app-icon `splash.json`).

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `tokens.css`, `components.css`, the Jersey 15 font and its OFL licence, `icons/`, `graphics/`, `illustrations/` and `badges/` are in the app and served; the font is self-hosted with `font-display: swap`, never from a CDN
- [ ] Setting `data-theme` and `data-mode` on `<html>` by hand in dev tools visibly restyles a test element in all four combinations
- [ ] The service worker include list matches `.woff2` and `.svg`; a file-based test in `Web.Tests` fails if any static file type shipped by Ui or Web is not matched by the list (shown failing against the old list)
- [ ] The manifest uses the designed icons (any 192 and 512, maskable 192 and 512, monochrome 512 with the monochrome purpose); the Apple touch icon and favicon (SVG, 32 and 16 px PNG) are replaced; `background_color` and `theme_color` are the Keep dark values; name, scope, id and base-path behaviour are unchanged; old placeholder icons are deleted
- [ ] The offline service-worker update path and the base-path-safe behaviour are unchanged and still work under the sub-path deploy
- [ ] `docs/pwa-manual-checks.md` gains steps: font, an icon and artwork present in airplane mode; the installed icon in round and squircle masks and themed icon
- [ ] `docs/design/assets/README.md` stays accurate; the app build stays warning-free and existing tests pass; published size change is noted in the PR
