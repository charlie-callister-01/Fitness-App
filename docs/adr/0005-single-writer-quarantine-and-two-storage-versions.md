---
status: accepted
---

# One writer window, quarantine over failure, and two storage version numbers

Three storage rules that are surprising and costly to change once real data exists. All refine [ADR 0003](0003-local-first-indexeddb-storage-behind-an-interface.md).

1. **One writer window.** The first window takes an exclusive Web Lock and may write. Any other window opens read-only and its commits fail with "another window holds the writer lock". The UI says "Fitness App is open in another window".
2. **Quarantine, not failure.** A stored record that cannot be read or fails validation is left out of the in-memory snapshot, stays in storage untouched, and is reported (kind, key, message). The app starts with everything else. Only a problem with the data as a whole (for example written by a newer app) stops loading, and then nothing is written.
3. **Two version numbers.** The *layout version* is the IndexedDB database version (which object stores exist, changed by a JavaScript upgrade step). The *record format version* is the exchange `formatVersion` the records were written in, kept in the `meta` store. They change independently.

## Considered Options

- **Merge edits from several windows** (or last-write-wins). Rejected: one person on one phone gains nothing from it, and silent overwrites are the failure that matters most.
- **Fail startup on any bad record.** Rejected: one corrupt Plan would lock the owner out of years of history. Deleting or repairing bad records automatically was also rejected, as it destroys the evidence needed to recover them.
- **A single version for both layout and format.** Rejected: the exchange format is also the Backup and the Import format and has its own compatibility promise (ADR 0001); tying it to how IndexedDB happens to be organised would force a format bump for a storage change, or the reverse.

## Consequences

- A window that cannot take the lock (or a browser without Web Locks support, which is treated as a single window) is a known gap: the lock only protects where it exists.
- A quarantined record keeps taking space and keeps being reported until the owner or a later feature fixes or replaces it. If the only active Plan is quarantined the app starts as "cannot read" and blocks commits, so the only way forward is restoring a Backup.
- A format change needs a migration in `Core` (rewrite on startup); a layout change needs a JavaScript upgrade step and a layout version bump. Neither exists in practice yet: both versions are still 1.
