Status: resolved
Stage: A5
Blocked by: 02

# Ticket 03: Guide parsing, multi-Exercise duplication, and Exercise tags

## Scope
- Parse `Exercises/*.md` front-matter `exercises:` list and headed/bolded sections (`Works`, `Why it's in your plan`, `Setup`, `How to do it`, `Cues`, `Common mistakes`, `Easier / harder`, `Watch-outs for you`, `My notes`).
- Write each Guide once per covered Exercise with identical content, keeping `watchOuts`, skipping empty `personalNotes` (e.g. bare `-`), and setting `videoUrl` to `null`.
- Record unknown Guide section text as `ConversionNote`s.
- Populate `ExerciseTags` from a table in the converter informed by each Guide's `Works:` line (note if missing).

## Acceptance
- `dotnet test` passes with 0 warnings.
- Complete Guide, missing sections, multi-Exercise Guide, empty/populated `My notes`, and tags are tested.
