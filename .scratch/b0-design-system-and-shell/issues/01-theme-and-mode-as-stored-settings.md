# 01: Theme and Mode as stored Settings

**What to build:** Core's Settings carry the owner's Theme (Keep or Clean) and Mode (System, Dark or Light), so a Backup restores the look. Defaults are Keep and System. The two fields are optional additions to the exchange format: no `formatVersion` bump, an older document without them reads unchanged, and an unknown or malformed value falls back to the default for that field and reports a format issue (never fails the import). Existing code and tests that construct Settings keep compiling. Spec: `docs/specs/b0-design-system-and-shell.md`. Design: `docs/design/design-document.md`. Start from an up-to-date `main` that includes A4 if A4 has merged (A4 reads and writes Settings); keep the change additive so the two merge cleanly.

**Blocked by:** None (can start immediately). Rebase on A4 if it has merged first.

**Status:** done

- [x] Settings has a Theme and a Mode with the documented defaults; `Settings.Default` uses them
- [x] The writer writes both optional fields; the reader accepts them and applies the defaults when absent
- [x] An unknown or malformed Theme or Mode falls back to the default for that field and reports a format issue, using the existing reader conventions; the rest of the document still reads
- [x] A document written before this change (no `theme`, `mode`) reads unchanged and round-trips
- [x] `docs/data-spec.md` is updated: Settings table, first-run defaults and the example; the data-spec conformance test is extended so document and reader agree
- [x] The whole-dataset write, snapshot and storage paths carry the fields (a Settings commit changing only Theme persists and reloads)
- [x] Existing Core tests still pass untouched apart from additions; no `formatVersion` change
