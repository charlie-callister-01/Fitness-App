# 06: IndexedDB module and `IDataStore` in the Web host

**What to build:** The real storage: a thin JavaScript module over IndexedDB and a C# `IDataStore` in the Web host that uses it, one document per record. Spec: `docs/specs/a3-storage.md` ("Store layout and versioning", "Blocked upgrades"; stories 3, 22, 28, 30, 42, 43).

**Blocked by:** 01 (Storage interface, in-memory store and contract suite)

**Status:** in-progress (JS module and interop wrapper done; IDataStore adapter waits on ticket 01, see TODO in IndexedDbInterop.cs)

- [x] One object store per record kind keyed by the record key, holding the record as JSON text in the exchange format's serialisation, plus one `meta` store of key/value pairs; no secondary indexes
- [x] Database version is the layout version; `onupgradeneeded` creates the stores for layout version 1 and is written so later versions can add steps
- [x] Apply-batch and replace-all each run as a single IndexedDB transaction and are all-or-nothing
- [x] The connection closes itself on a version-change event; a blocked upgrade is reported as a distinct state ("close other windows and reopen")
- [x] Quota-exceeded and storage-unavailable errors map to the closed failure kinds; other errors become "unexpected" with detail
- [ ] The C# store lives only in the Web host and implements the `Core` interface; `Core` and `Ui` gain no browser types or dependencies
- [ ] The store is registered in the host's dependency injection, but real startup wiring waits for ticket 07
- [x] Interop is kept to a small string-in, string-out surface so it is easy to run from the browser test in ticket 08
- [x] The existing build and unit test runs stay green
