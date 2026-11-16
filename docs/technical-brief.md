# Technical brief

See also: [ADR 0001](adr/0001-schema-first-with-permanent-ids-and-versioned-exchange-format.md), [ADR 0002](adr/0002-csharp-blazor-pwa-with-shared-ui-library.md), [ADR 0003](adr/0003-local-first-indexeddb-storage-behind-an-interface.md).

## Stack

- **Platform:** a PWA now (installable from a link, works offline), with an installable native app later. The owner works in C# professionally and prefers it.
- **Language and UI:** C# on the current .NET LTS (**.NET 10, confirmed as LTS**; see [ADR 0004](adr/0004-dotnet-10-lts-pinned.md)). **Blazor WebAssembly** PWA now; **.NET MAUI Blazor Hybrid** later, reusing the same Razor components.
- **Why not TypeScript + Capacitor:** the lowest-friction web-to-app path and a bigger community, but the owner would read unfamiliar code in every review. Trade-offs of the chosen route: a larger runtime download on first load (cached afterwards), a smaller PWA-specific community, and browser storage needs a small JavaScript bridge.
- **Why PWA first:** instant deploy by pushing, no Apple developer account or Mac needed to test, offline works with a service worker. Moving to a native shell later is cheap only if UI, logic and storage stay separate layers.
- **UI library / styling:** undecided on purpose. Waits for the owner's Claude Design document.

## Solution layout (four projects from day one)

| Project | Contents |
|---|---|
| `Core` | Data model and **all rules** (PRs, Suggested targets, Streaks, weeks, Badges, Pain/RPE handling, unit conversion, import validation and matching, export, format migrations). No dependencies. Storage and clock are interfaces. |
| `Core.Tests` | xUnit tests. |
| `Ui` | A Razor class library holding every screen and component. Contains no rules. |
| `Web` | A thin Blazor WebAssembly PWA shell referencing `Ui` (and `Index.html`, service worker, manifest). |

Later: a thin MAUI shell referencing the same `Ui`. Only `index.html`/`App.razor` and the storage implementation differ per host. Platform differences go behind interfaces defined in `Ui`/`Core`.

Rule: screens call `Core`; they never contain rules. The strict maintainability review (see the build workflow) polices this.

## Storage

Local-first, single device. **IndexedDB as a document store behind a storage interface.** The whole dataset is loaded into memory at startup, all queries run in `Core` (so they are unit-testable), and each change saves the one record that changed. The data volume is small (a few thousand Workouts over years). An in-memory implementation of the interface is used by tests.

Safety: request **persistent storage** on first run; make the PWA installable; Nudge when there's no Backup in 14 days; automatic Backup before every Import.

Not chosen: browser-SQLite libraries such as SqliteWasmBlazor and Bit.Bsqlite (young, some pre-release, maintenance unverified), or a backend. A later MAUI build implements the same interface with SQLite or files.

## Data and exchange format

- **Schema first:** a written data spec comes before any screens; export/import is built before the UI.
- Every record has a **permanent ID**; names are labels. Imports match Exercises by ID, then exact name, then suggest near-matches for confirmation.
- One **versioned JSON** format is the Backup, the LLM analysis Export, and the Plan/Guide/changes Import. A Markdown rendering is produced for pasting into chats. Serialisation with `System.Text.Json`.
- The importer must keep reading old format versions.
- One canonical unit stored (kg, km, m); display units are a setting.
- Imports: preview, automatic backup, field-level error messages, never silent merges.
- The prompt pack embeds the format spec and the current Exercise library.
- Workouts keep a record of what was planned at the time, so Plan edits never rewrite history.
- Time: weeks are ISO Monday to Sunday. Time and "today" come from an injected clock so rules are testable.

## Testing

- **xUnit** with heavy coverage of `Core`: rules, import validation and matching, export, format migrations.
- **bUnit** for key components (the log screen, the Import preview).
- **Playwright**: one or two end-to-end flows once the UI exists.
- Every ticket includes its tests; the review steps check for them.

## Delivery

- **Hosting:** GitHub Pages now (deployed by GitHub Actions; Blazor needs a base-path setting and a 404 redirect for refreshes); **Cloudflare Pages later**.
- **CI:** GitHub Actions builds and runs all tests on every PR, and deploys when `main` changes.

## Browser features to use
Wake Lock (screen stays on during a Workout), persistent storage request, service worker offline cache, web app manifest and install prompt, clipboard read/write, Web Share (if cheap), file download and file picker.

## Known limits (deliberate)
Single device, no sync or accounts (a separate future project); no push notifications; no cloud storage integration; storage can still be cleared by the browser, hence Backups.
