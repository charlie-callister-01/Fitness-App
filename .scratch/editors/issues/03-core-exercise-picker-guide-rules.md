# 03: Core rules for New Exercise, picker choices and Guides

**Read first:** `.scratch/editors/HANDOFF.md`, then the spec.

**Parent:** spec `docs/specs/editors-b4-b5.md` (issue #24), "Exercise and Guide rules (Core)" and "Picker".

**What to build:** Core rules and Core.Tests only, no Ui.
- **New Exercise:** validates the name (non-empty, not a duplicate ignoring case and surrounding spaces, naming the existing Exercise in the message), the log kind (all kinds except the two body measurements; confirm against the design's nine) and optional tags and cue; assigns a permanent id through the existing id generator; returns the Exercise or a reason.
- **Picker choices:** one interface taking the context (Swap, Add, Custom workout, Plan editor) and a query, returning the groups, with matching, ordering, exclusion and the search threshold now in Core; includes the "Add '<text>' as a new Exercise" offer for a no-match search.
- **Guide edit:** sections as plain text and lists; saving creates the Guide if none exists; an empty section is stored as absent; personal notes save independently of the sections; the video link must be an http or https address or empty.
- **Guide history view:** the last three results for the Exercise with Personal records marked, and where it sits in the Plan (Sessions and Phase), reusing the existing performance rules.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Each rule has Core tests covering valid, invalid and boundary inputs (duplicate names, empty names, bad links, no Guide yet, fewer than three results).
- [ ] The picker choice rules match the current behaviour of the Swap, Add and Custom workout pickers (existing results are unchanged) and are tested without rendering.
- [ ] No Ui file is edited; `dotnet build` has 0 warnings and all tests pass.
