---
status: accepted
---

# Schema first, permanent IDs, and a versioned exchange format

The data model is written as a spec before any screens are built, and export/import is built before the UI. Every Exercise (and other record) has a permanent ID, and names are only editable labels. All data leaves and enters the app as one versioned, documented JSON format that serves as the Backup, the LLM analysis Export, and the Plan/Guide Import.

## Considered Options

- **Screens first, data model emerging from the UI.** Rejected: the old system did this and ended up with overlapping data (weight in two places, lifts in two places).
- **Names as IDs** (the old system's approach). Rejected: renaming an Exercise silently erased its history.
- **Separate formats for backup, LLM export and import.** Rejected: one format means one thing to learn, test and keep in sync.

## Consequences

- Every Export carries a format version, and the importer must keep reading old versions.
- Imports are always previewed and preceded by an automatic backup, since LLM-written data can be wrong.
- LLM prompts include the current Exercise library (IDs and names) so imported data lines up with existing records.
