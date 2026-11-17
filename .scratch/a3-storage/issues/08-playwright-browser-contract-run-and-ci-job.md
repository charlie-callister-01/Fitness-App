# 08: Playwright browser contract run and CI job

**What to build:** The same contract suite that proves the in-memory store is run against the real IndexedDB store in headless Chromium, as its own CI job, so the browser-specific code is tested and the normal test run stays fast. Spec: `docs/specs/a3-storage.md` ("Testing Decisions"; stories 39, 40).

**Blocked by:** 06 (IndexedDB module and `IDataStore`), 07 (Real storage environment and startup wiring)

**Status:** done

- [x] A test page in the Web host (not reachable in production builds) runs the contract suite against the IndexedDB store and reports each case's result
- [x] Playwright drives headless Chromium to open that page and asserts every contract case passes
- [x] Browser-only cases are added: atomic batch and atomic replace-all really roll back on a failing write, the store survives a page reload, an upgrade from layout version 1 keeps data, a second page cannot take the writer lock while the first holds it
- [x] Runs as a separate CI job so the existing build-and-test workflow is unaffected; failures upload useful output
- [x] Not tested here, and stated as such: quota-full, the real persistence grant, the browser clearing data, iOS Safari (these go on the manual checklist in ticket 09)
- [x] The CI job deliberately fails when a contract case fails (shown once on a throwaway change or by a documented check)
