# 05: Exchange: Import, Export and prompt pack

**What to build:** Tracks travel through every exchange path. Plan Import gives colliding Track IDs fresh ones and everything pointing at them follows; Export (JSON and Markdown) carries Tracks inside the Plan; the "Write me a Plan" prompt shows an LLM the Track format with a worked example that includes a daily Track, and that example passes the real Import. Spec: "Exchange, Import, Export, prompt pack (A4 change)".

**Blocked by:** 01. (Can run in parallel with 02 to 04.)

**Status:** ready-for-agent

- [ ] Plan Import remaps a Track ID that collides with an existing one using the injected generator, and `trackId`, Track-Phase overrides and Badge metrics of the form `streak:<trackId>` / `days:<trackId>` / `bestweek:<trackId>` follow; Phase and Session remapping still works inside Tracks.
- [ ] Importing the same multi-Track Plan twice says "nothing to change" the second time (stable IDs).
- [ ] Merge treats Tracks as part of the Plan; no new Import kind; the preview wording mentions Tracks when a Plan has them.
- [ ] Export JSON carries Tracks, `trackId`, `daily` and `mobility`; the Plan tick off leaves Workouts' `sessionId` null behaviour unchanged; Markdown shows each Track as a heading with cadence, target and Phases.
- [ ] The prompt pack's Plan format reference lists Tracks, with enum lists generated from the C# enums, and the worked example has a daily Track; a test reads it, validates it and previews it as a clean Plan Import.
- [ ] A4 Export, Import and prompt tests pass unchanged for Plans with no Tracks.
