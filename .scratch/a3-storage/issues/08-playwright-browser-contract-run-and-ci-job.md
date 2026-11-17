# 08: Playwright browser contract run and CI job

**What to build:** The same contract suite that proves the in-memory store is run against the real IndexedDB store in headless Chromium, as its own CI job, so the browser-specific code is tested and the normal test run stays fast. Spec: `docs/specs/a3-storage.md` ("Testing Decisions"; stories 39, 40).

**Blocked by:** 06 (IndexedDB module and `IDataStore`), 07 (Real storage environment and startup wiring)

**Status:** done

- [x] No test page was added to the Web host (so nothing test-only can ship). Instead the `Web.BrowserTests` project runs the shared `DataStoreContract` cases against the production `IndexedDbDataStore`, with each JS module call executed in headless Chromium; the C# store code runs on the test host, not in WASM
- [x] Playwright drives headless Chromium and every shared contract case passes (27 cases, none mirrored)
- [x] Browser-only cases are added: atomic batch and atomic replace-all really roll back on a failing write, the store survives a page reload, an upgrade from layout version 1 keeps data, a second page cannot take the writer lock while the first holds it
- [x] Runs as a separate CI job so the existing build-and-test workflow is unaffected; failures upload useful output
- [x] Not tested here, and stated as such: quota-full, the real persistence grant, the browser clearing data, iOS Safari (these go on the manual checklist in ticket 09)
- [ ] The CI job deliberately fails when a contract case fails. Shown locally only (JS temporarily changed, rollback test failed, reverted); not yet seen failing on GitHub Actions, and the new workflow has not run on GitHub yet
