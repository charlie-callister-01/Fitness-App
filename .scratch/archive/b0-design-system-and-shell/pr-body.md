## Summary

B0 gives the app its design system and permanent frame: the Keep and Clean Themes (each with Dark, Light and System Modes) switch live and are stored in Settings, there are five tabs and a More menu, and four base components exist. No feature screens yet; every later UI stage (B1 onwards) builds on this. Spec: `docs/specs/b0-design-system-and-shell.md`. Tickets: `.scratch/b0-design-system-and-shell/issues/01..06`.

```text
Core
└── Settings            + Theme (keep|clean), Mode (system|dark|light)   [optional fields, no formatVersion bump]

Web
├── index.html          tokens.css → components.css → ui.css → app.css; <meta theme-color>
├── js/theme.js         ONE place: mirror key, attribute names, chrome colours, system-scheme listener
├── Theming/ThemeInterop        IThemeInterop → FitnessTheme.apply
└── manifest, icons     designed app icons; service worker now caches .woff2 .svg .txt

Ui
├── Theming/ThemeService        AppData.Settings ⇄ Theme/Mode ⇄ interop (serialised choices)
├── Components/         Button · Icon · StatusOrb · EmptyState · TabBar · AppearancePicker · SkipLink
├── Layouts/            Main (tab bar / rail) · FullScreen · PushedPage
└── Pages/              Home Week Progress Trophies More + placeholders for the More entries
```

```text
open app
  theme.js (in <head>) reads localStorage mirror → sets data-theme/data-mode on <html>   (no flash)
  Blazor starts → AppData loads Settings
    ThemeService.InitialiseAsync → apply Settings value, rewrite mirror   (Settings always win)
choose Theme/Mode (More → Appearance)
  ThemeService.ChooseAsync (serialised)
    apply to <html> now → commit Settings change
      commit fails → choice stays for the session, picker shows "could not be saved"
```

Non-obvious decisions:

- **Theme and Mode are Settings in Core** (so a Backup restores the look), not device-only. They are optional, additive fields: an old document reads unchanged, a bad value falls back to the default with a warning, no `formatVersion` bump. The cost is a localStorage **mirror** so the pre-paint script can apply the look before Settings load; the mirror is only a cache and is rewritten from Settings at startup.
- **`theme.js` is a classic synchronous script, not an inline script or ES module,** because a module cannot run before first paint. It is the single definition of the mirror key, attribute names and chrome colours. A file-based test ties its Theme/Mode lists to the Core enums, `tokens.css` selectors and `splash.json`.
- **Offline cache bug fixed:** the service worker did not cache `.woff2` or `.svg`, so the font, icons and artwork would have vanished in airplane mode. A test now fails if any shipped static file type is not covered. `.txt` was added too, for the OFL licence file.
- **A failed Theme save is reported locally** (`ThemeService.LastSaveFailure`, shown in the picker), not through `AppData.Health`: `Health` only describes storage state (persistence, availability, writability) and has no failed-commit channel. Routing it centrally needs a design decision for a later stage.
- **A race was found and fixed in review:** the service reacted to the change event from its own commit, so a quick second tap was undone by the first commit. Choices are now serialised and own-commit events ignored.
- **Plain CSS with tokens, no component library** (design §11). The design's `components.css` is copied unmodified; `ui.css` (loaded after it) overrides its 390 px `.fa-root` cap so the 560 px column and left rail from 900 px work.
- **Icons ship twice** (embedded for inline rendering, and as static assets that the offline test scans). Left as is; dropping the static copy needs a spec change.
- **The component showcase** (all base components, for eyeballing the four looks) is in the More page in Debug builds only.
- Phone-only checks are written into `docs/pwa-manual-checks.md`, not run.

Deferred (noted by review, not worth blocking B0): rewriting `IconCatalogue` as a generated dictionary; merging StatusOrb's `Tone` and `Greyed`; the closed-enum switch in EmptyState; making Theme/Mode required constructor parameters; unifying the `--radius-*` and `--r-*` token families; `InAppHistory` is an approximation for "can Back stay in the app".

## Evidence

- **Before:** one placeholder Home page, placeholder icons, system font; service worker would not cache `.woff2` or `.svg`.
  **After:** `dotnet test` (Debug and Release): Core 814, Ui 103, Web 37, all passing; Playwright browser tests (`-p:RunBrowserTests=true`): 47 passing, including 12 new `ThemeScriptBrowserTests`.

Tests that pin the behaviour:

```text
OfflineCacheCoverageTests   every file under Ui/Web wwwroot is matched by the service worker include list   (failed against the old list: .svg, .woff2)
ThemeService                two quick taps → the newer choice is what is saved                              (failed before the serialising fix)
                            failed save + unrelated commit → session choice survives
ThemeScriptContractTests    theme.js Theme/Mode lists and defaults == Core enums == tokens.css selectors; chrome colours == splash.json/manifest
ThemeScriptBrowserTests     mirrored choice applied before app starts; System follows a changed colour scheme; blocked mirror → default, no error
ThemeAndModeSettingsTests   old document (no theme/mode) reads unchanged; unknown value → default + warning; Theme-only commit reloads
Button                      Icon button without an accessible name refuses to render; disabled link is <a aria-disabled>, not clickable
TabBar                      aria-current on the active tab; More stays active on every More entry
```

Not run: anything on a real phone (install with the new icon, airplane-mode font/icons, status bar colour, 360 px width, text scaling, deep links). The ticket acceptance boxes for these are left unticked and listed in `docs/pwa-manual-checks.md`.

## Merge Danger

**Door:** two-way. Everything is additive and revertable; the only data change is two optional Settings fields.

Old Backups still read (absent fields take the defaults). A Backup written after this change contains `theme` and `mode`, which an older build ignores, so rolling back the app does not break restores.

**Blast Radius:** whole-app look and navigation.

- Every screen now sits in the new shell and styles; the placeholder Home is replaced. Anything that linked to the old Home markup or `ui.css` rules will change.
- Published size grows by about 1.25 MB uncompressed (mostly SVG provenance metadata; the assets README says to strip it in the build, not done here).
- Service-worker update: the cache list changed, so installed copies re-download on the next update. Worth the airplane-mode check in `docs/pwa-manual-checks.md` after deploy.
- The sub-path deploy (`/Fitness-App/`): links use relative hrefs and the skip link/back arrow avoid base-resolved `#` links, but this is only unit-tested, so check a deep link and refresh on the live site.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_016NV3mrbPnEsidtPTUeRyTp
