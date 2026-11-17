# 05: Storage environment and health

**What to build:** The facts only the browser knows (persistent storage, free space, whether another tab holds the writer lock, whether the browser wiped the data) become an interface in `Core` with a fake, and `AppData` turns them into a storage health value and startup states the UI can word. Spec: `docs/specs/a3-storage.md` ("Persistent storage and storage health", "Browser-cleared data", "Multiple tabs"; stories 10, 11, 14, 15, 19 to 21, 24).

**Blocked by:** 03 (`AppData`: commit)

**Status:** done

- [x] `IStorageEnvironment` in `Core` with no browser types: request persistent storage, estimate usage and quota, try to take the exclusive writer lock, read and write the cleared-data marker; a fake implementation for tests
- [x] A storage health value exposing: persistence granted, not granted or unsupported; usage and quota when available; whether the database is available; whether this tab can write
- [x] The persistence request is made at startup on every launch until granted; a denial blocks nothing and is reflected in health
- [x] A marker present with an empty or missing store is a distinct "browser cleared your data" startup state, not first run; a missing marker with an empty store is first run
- [x] If the writer lock is held elsewhere, the tab starts read-only, health says another window is open, and any commit returns the "another tab holds the writer lock" failure kind and changes nothing
- [x] If the store is unavailable, `AppData` runs on the in-memory store with health "unavailable" so the owner can still log
- [x] The marker and "initialised" metadata are written on the first successful save
- [x] Tests (fake environment, fault wrapper) cover: grant, denial, unsupported, cleared-data detection versus first run, read-only second tab, unavailable fallback, marker written on first save, health values
