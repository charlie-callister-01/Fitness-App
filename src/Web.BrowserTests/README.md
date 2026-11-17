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
- Each test uses a new browser context and a uniquely named database, deleted with `destroy` afterwards.

## Running

The default `dotnet test` skips this project (it is still built, so warnings-as-errors apply) because it needs a browser.
Opt in with `RunBrowserTests=true`:

```
dotnet build src/Web.BrowserTests
pwsh src/Web.BrowserTests/bin/Debug/net10.0/playwright.ps1 install chromium   # once
dotnet test src/Web.BrowserTests -p:RunBrowserTests=true
```

CI runs this as its own job (`.github/workflows/browser-tests.yml`), so `build-and-test.yml` is unaffected.

## Not tested here (manual checklist, A3 ticket 09)

Quota-full, the real persistence grant, the browser clearing site data, and iOS Safari. Headless Chromium cannot show them.
