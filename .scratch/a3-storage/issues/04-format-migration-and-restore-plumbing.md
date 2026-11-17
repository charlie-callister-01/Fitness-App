# 04: Format migration and restore plumbing

**What to build:** When an app update moves the exchange format on, stored data is upgraded once at startup so old history keeps loading, and `AppData` can replace the whole dataset atomically so A4's Backup restore needs no storage code. Spec: `docs/specs/a3-storage.md` ("Format migration", "Store layout and versioning"; stories 25 to 31).

**Blocked by:** 03 (`AppData`: commit)

**Status:** done

- [x] The recorded record format version is stored in metadata and written on every save; records are always written in the current format version
- [x] At startup, a recorded version older than the app's makes `AppData` read the stored records through the versioned reader and rewrite them in one replace-all, then record the new version
- [x] The layout version is a separate number from the format version; neither forces a change to the other
- [x] A failed migration leaves the stored data exactly as it was and starts the app with a clear state instead of a partial dataset
- [x] An unsupported future format version still stops loading with the A1 message and writes nothing
- [x] `AppData` offers replace-all from a validated document: all-or-nothing, replaces everything including records not in the document, publishes the new snapshot only after the store succeeds, and leaves old data and snapshot intact on failure
- [x] Tests cover: migration from a synthetic older version (rewritten and version recorded), a migration that fails midway changes nothing, replace-all success and failure, replace-all of an invalid document rejected without changes
