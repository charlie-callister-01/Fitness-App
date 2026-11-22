# 09: Exchange: Import, Export, Markdown and prompt pack (PR 1)

**Read first:** `.scratch/a4-5-tracks/HANDOFF.md`, `docs/specs/a4-5-tracks.md` ("Exchange, Import, Export, prompt pack"), `docs/specs/a4-export-import.md`, `src/Core/Exchange/` (`PlanImport`, `ImportPreviewer`, `ImportApplier`, `MergeImport`, `RestoreImport`, `Exporter`, `ExportSlicer`, `MarkdownRenderer`, `PromptPack`, `PromptPackFormat`, `ExerciseReferences`).

**Parent:** spec `docs/specs/a4-5-tracks.md`, PR 1.

**What to build:** Tracks travel through every exchange path.
- **Plan Import** remaps a Track ID that collides with an existing one (fresh ID from the injected generator). `trackId` on the Plan's Sessions, Track Phase overrides and qualified Badge metrics in the same file (`streak:<id>`, `days:<id>`, `bestweek:<id>`) follow, exactly as Phase and Session IDs do now. Importing the same file twice says "nothing to change". A v1 Plan file imports (through the upgrader) and gets a Main Track whose ID is remapped if `<planId>-main` collides. The preview mentions Tracks when the Plan has more than one.
- **Merge** treats Tracks as part of the Plan; no new Import kind. **Restore** of a v1 or v2 Backup works.
- **Export** JSON carries Tracks, `trackId`, `daily` and `mobility`. With the Plan tick off, Workouts keep their A4 `sessionId` behaviour. **Markdown** shows each Track as a heading with its cadence, rules and Phases.
- **Prompt pack:** the "Write me a Plan" format reference lists Tracks and the Session fields, with the enum lists (`SessionType`, default day, cadence) generated from the C# enums, and the worked example has two Tracks including a daily mobility Track (use the data-spec example). The text states that the first Track is the main one and must be weekly. A test reads, validates and previews the example as a clean Plan Import.
- **Exercise references** (`ExerciseReferences`) and any Import step that walks Session rows read all Tracks' Sessions.

**Blocked by:** 02, 03.

**Status:** ready-for-agent

- [ ] Plan Import remaps colliding Track IDs and updates `trackId`, Track Phase overrides and qualified Badge metrics; a second identical import says "nothing to change".
- [ ] A v1 Backup and a v1 Plan file import; a v2 Backup with two Tracks round-trips through Export JSON then Restore.
- [ ] Export Markdown shows Tracks; the prompt example reads, validates and previews cleanly.
- [ ] A4 Export, Import and prompt-pack tests pass for one-Track Plans with only mechanical changes.
- [ ] `dotnet build` 0 warnings; all tests pass.
