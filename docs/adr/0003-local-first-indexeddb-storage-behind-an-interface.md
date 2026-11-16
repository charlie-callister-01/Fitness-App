---
status: accepted
---

# Local-first IndexedDB storage behind an interface

All data lives on the owner's phone in IndexedDB, as a document store behind a storage interface defined in `Core`. The whole dataset is loaded into memory at startup, all queries run in `Core`, and each change saves the one record that changed. There is no backend, no sync and no accounts for now.

## Considered Options

- **SQLite in the browser (EF Core)** via libraries such as SqliteWasmBlazor or Bit.Bsqlite. Rejected for now: young, some pre-release, maintenance unverified, and the data is small enough not to need SQL.
- **A backend with sync.** Rejected: roughly doubles the work, and the single-device plus Backup/Import workflow covers the need. A future public release would treat this as its own project.

## Consequences

- The browser can clear site storage, so the app requests persistent storage, encourages installing the PWA, nudges for a Backup every 14 days, and always makes an automatic backup before an Import.
- A later MAUI app implements the same interface with SQLite or files without touching `Core` or `Ui`.
- Tests use an in-memory implementation of the interface.
- If the data ever outgrows memory, only the storage implementation needs replacing.
