# Web.BrowserTests

Runs the storage contract against the real IndexedDB module (`src/Web/wwwroot/js/indexeddb-store.js`) in headless
Chromium, via Playwright for .NET.

## How it works

- `IndexedDbDataStoreContractTests` subclasses the shared `DataStoreContract` (in `Core.Tests`, project-referenced), so every
  contract case is SHARED, not copied. The store under test is the production `IndexedDbDataStore` + `IndexedDbInterop`;
  `PlaywrightJsRuntime` replaces Blazor's `IJSRuntime` so each call executes in the browser against the real module.
  The C# store code runs on the test host, not in WebAssembly; the JavaScript module and IndexedDB are the real thing.
- The browser serves the real `wwwroot/js` files from a fake origin (`https://fitness.test/`) through Playwright routing.
  There is no test page in the Web host, so nothing test-only can ship.
- `IndexedDbBrowserBehaviourTests` adds what only a browser shows: real transaction rollback (bad last op in a batch,
  unknown kind, failing replace-all), reload persistence, a database created at layout version 1, and the writer lock
  across two pages of one browser context.
- Each test uses a new browser context and a uniquely named database, deleted afterwards by calling the module's `destroy` directly (that capability is not in the production wrapper).

## Whole-app host (`App/`)

The B11 hardening tests run the real published app, not single modules.

- `AppFixture` (collection `"whole app"`) gets a Release publish of `src/Web` once per run: the folder named by the
  `BROWSER_TESTS_APP_DIR` environment variable (CI publishes first and sets it), otherwise the run itself publishes into
  `bin/<config>/net10.0/published-app` (about a minute the first time). It serves that `wwwroot` from the same fake origin
  through Playwright routing: a path with no extension is an app route and gets `index.html`, a missing file is a 404. The
  service worker is blocked (`ServiceWorkers = Block`): offline is not under test and its fetches would bypass the routing.
- `await using var page = await app.OpenAsync("/route", new AppPageOptions { ... })` opens a new browser context and returns an
  `AppPage` once the screen has rendered (its `#main-content h1` exists). `AppPageOptions`: `Width`/`Height` (default 360 x 780),
  `Theme`/`Mode` (default Keep, Dark; chosen by clicking the Settings screen's Appearance choices, so they are saved as Settings),
  `TextScale` (root font size multiple; 2 = 200%), `ReducedMotion`, `WithFixture`. The context's colour scheme is dark, so
  `Mode.System` resolves to dark.
- `AppPage.Page` is the Playwright page; `GotoAsync(route)` moves inside the app with `Blazor.navigateTo` (no reload, so a save in
  flight is never cut off) and waits for the new screen's h1; `ReloadAsync()` restarts the app from stored data; `Errors` collects
  uncaught page errors and console errors; `DatabaseName` is this test's database.
- Each test gets its own context and a uniquely named database: an init script renames the production `fitness-app` database
  to a `test-<guid>` name before the app's scripts run. The same init script sets the root font size for `TextScale`.
- **Fixture:** `App/fixture-backup.json` is a small hand-written, complete format-2 Backup with no private data: Plan "Test Plan"
  with a weekly Main Track (Mon/Wed/Fri) and a daily "Morning mobility" Track (so Home has a Session every day), Workouts in
  November 2026 (the second Goblet squat Workout is a PR), Weigh-ins, an unlocked "First Session" Badge and a Goblet squat Guide.
  `WithFixture` restores it through the Import tab on `/backup` (paste, Check, Apply), the real Restore path; the automatic
  pre-import Backup download is accepted by the context. `FixtureBackupTests` keep it valid against the current exchange format.
  The A5 example dataset is git-ignored and is not used.

## Running

The default `dotnet test` skips this project (it is still built, so warnings-as-errors apply) because it needs a browser.
Opt in with `RunBrowserTests=true`:

```
dotnet build src/Web.BrowserTests
pwsh src/Web.BrowserTests/bin/Debug/net10.0/playwright.ps1 install chromium   # once
dotnet test src/Web.BrowserTests -p:RunBrowserTests=true
```

To skip the per-run publish while iterating, publish once and point the tests at it:

```
dotnet publish src/Web -c Release -o publish/browser-app
BROWSER_TESTS_APP_DIR=publish/browser-app dotnet test src/Web.BrowserTests -p:RunBrowserTests=true
```

Re-publish after changing the app, or the tests run the old build.

CI runs this as its own job (`.github/workflows/browser-tests.yml`), so `build-and-test.yml` is unaffected.

## Not tested here (manual checklist, A3 ticket 09)

Quota-full, the real persistence grant, the browser clearing site data, and iOS Safari. Headless Chromium cannot show them.
