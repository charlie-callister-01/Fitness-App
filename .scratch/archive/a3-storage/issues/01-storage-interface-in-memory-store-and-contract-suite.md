# 01: Storage interface, in-memory store and contract suite

**What to build:** `Core` gets the storage seam: an `IDataStore` that can load everything, apply a batch of changes atomically, replace the whole dataset atomically and keep device metadata, with an in-memory implementation that behaves like IndexedDB and a reusable contract test suite that every implementation must pass. Spec: `docs/specs/a3-storage.md` (Shape, "Storage operations", "Failure kinds"; stories 1 to 9, 25 to 27, 39 to 43).

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Record kinds: Exercise, Settings, Plan, Weigh-in, Break, Badge, Guide, Workout, Week schedule, each with its key rule (permanent ID; Guide by Exercise ID; Week schedule by Monday date; Settings a fixed singleton key)
- [x] `IDataStore` offers: load all (raw stored records per kind plus metadata), apply a batch of puts and deletes as one atomic unit (a single save is a batch of one), replace all (atomic clear and write), and metadata read and write (last Backup timestamp, layout version, record format version, initialised marker)
- [x] Expected failures are returned as values from a small closed set (storage full, storage unavailable, another tab holds the writer lock, unexpected with detail), never thrown
- [x] No browser type appears in the interface; `Core` keeps no dependencies
- [x] The in-memory store: put replaces by key, delete of a missing key is not an error, batches and replace-all are all-or-nothing, values are copies (mutating a loaded record never changes storage), an empty store loads empty
- [x] A reusable abstract contract fixture that any implementation can subclass; the in-memory store runs it in the normal `Core.Tests` run
- [x] A fault-injecting wrapper test helper that can make a store report full, unavailable or writer-lock-held on demand, and can fail the last operation of a batch
- [x] Contract tests cover: round trip of every kind exactly (including a Guide keyed by Exercise ID, a Week schedule keyed by date, and a Workout with Sets and a Pain check), put-replaces, delete, atomic batch, atomic replace-all removing everything not in the new set, metadata round trip, copy semantics, empty store
- [x] No sleeping and no real clock in the tests
