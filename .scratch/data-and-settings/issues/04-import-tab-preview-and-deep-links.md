# 04: Import tab, Preview (C-16), and deep links (S-10 Import, F-05, F-10)

**What to build:** Build the **Import** tab on `/backup` over `Core.Exchange`: a large paste box first with **Check**, a file picker underneath, and a note that an automatic Backup is made before applying. Inspecting pasted or uploaded text strips surrounding text/code fences (showing a notice when extra text was ignored), detects and allows switching the Import kind (Backup, Plan, Merge, Guides), and refuses invalid or newer-version files with a concise field-level error banner. Valid input renders `ImportPreview` (C-16): a plain-language summary, colour-coded diff rows (add, change old → new, needs a decision), duplicate-date Workout warnings, and ignored sections. Near-match Exercises require an explicit **Yes** / **Keep as new** choice, and Guide imports offer **Keep mine** / **Replace** per existing Guide, updating the preview live. Tapping **Apply** downloads an automatic pre-import Backup JSON via `IBackupSink` (aborting with nothing changed if the download fails), checks for stale previews, commits atomically via `ImportApplier`, respects `ReadOnlyWell` when read-only, and shows a clear confirmation. Wire deep links (`?tab=import&kind=...`) from **Plans** ("Import a Plan") and **Guide** ("Import Guides" on the no-Guide state).

**Blocked by:** 02: Export tab, Backup download, and Weekly review link (S-10 Export, F-06)

**Status:** ready-for-agent

- [ ] Import tab shows the paste box + **Check** first, file picker second, and automatic Backup note; `/backup?tab=import&kind=...` pre-selects the requested kind.
- [ ] Extra text/code fences around pasted JSON are stripped with a notice; invalid or newer-version files show field-level errors with nothing applied.
- [ ] Detected Import kind can be switched between Backup, Plan, Merge, and Guides, re-running preview cleanly.
- [ ] `ImportPreview` (C-16) displays the summary, colour-coded diff rows, Backup restore data-loss warning, duplicate-date warnings, and ignored sections.
- [ ] Near-match Exercise decisions (**Yes** / **Keep as new**) and per-Guide decisions (**Keep mine** / **Replace**) re-run preview live and block Apply until required near-match decisions are answered.
- [ ] Tapping **Apply** triggers automatic pre-import Backup download via `IBackupSink`, aborts with nothing changed if download fails, handles stale previews by asking to re-check, commits atomically, and disables Apply with `ReadOnlyWell` when read-only.
- [ ] **Plans** list offers **Import a Plan** and empty **Guide** state offers **Import Guides**, each navigating to `/backup` on the Import tab with the matching kind.
- [ ] UI tests cover paste/file input, error banner, kind switching, near-match Exercise and Guide decisions, automatic Backup failure aborting Apply, stale preview handling, read-only disabling, and Plans/Guide deep links with 0 build warnings.
