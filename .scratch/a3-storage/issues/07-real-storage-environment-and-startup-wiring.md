# 07: Real storage environment and startup wiring

**What to build:** The browser implementation of the storage environment (persist request, estimate, writer lock, cleared-data marker) and the host startup that ties it all together: real store, real environment, `AppData` load, with the in-memory fallback when the browser gives no storage. Spec: `docs/specs/a3-storage.md` ("Persistent storage and storage health", "Browser-cleared data", "Multiple tabs"; stories 10, 11, 14, 19 to 21, 24).

**Blocked by:** 05 (Storage environment and health), 06 (IndexedDB module and `IDataStore`)

**Status:** done

- [x] The browser `IStorageEnvironment`: persistent-storage request and check, usage and quota estimate, an exclusive Web Locks writer lock held for the life of the tab, and the cleared-data marker in browser local storage
- [x] Unsupported browser APIs report "unsupported" instead of throwing
- [x] Host startup builds the real store and environment, loads `AppData` and registers it (and its health) for injection into the UI; the existing clock and build-info registrations are kept
- [x] If IndexedDB is unavailable (for example private browsing) startup falls back to the in-memory store with health "unavailable"
- [x] The app still builds, installs as a PWA and opens offline as before (the existing PWA manual checks still apply)
- [x] `Ui` receives only `AppData` and the health value to inject; it contains no storage rules
- [x] The existing Core and Ui test runs stay green
