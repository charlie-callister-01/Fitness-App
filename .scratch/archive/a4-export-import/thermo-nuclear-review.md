# A4 thermo-nuclear review

Diff reviewed: `git diff d44a060...HEAD` (src/Core/Exchange plus the small Storage/Rules edits). No file is near 1k lines (largest: MarkdownRenderer, 251).

## Fixed
- Two near-identical "closing sentence" rules (`KindResult.ClosedForKindsThatLeaveWorkoutsAlone` and `MergeImport.Closed`): `IImportKind.cs`, `MergeImport.cs`, `PlanImport.cs`, `GuidesImport.cs`. One `KindResult.Closed()` now serves Plan, Guides and Merge (Plan and Guides never produce Workout changes, so the Workout check is a no-op for them).
- `IImportKind.Reads` was a set of wire-name strings (hand-typed in Merge/Plan/Guides, derived in Restore) compared against `PathHead` strings: `IImportKind.cs`, the four kinds, `ImportPreviewer.cs`. It is now `IReadOnlySet<RecordKind>`; the previewer filters findings through the existing `KindAt`. No string literals left.
- Previewer hand-built a hollow `ExerciseMatching([], new Dictionary, [], [], [], doc)` for Restore: `ExerciseMatch.cs`, `ImportPreviewer.cs`. Now `ExerciseMatching.Untouched(document)`.
- `ExerciseMatcher.Match` mixed matching with the unknown-Exercise reference check inline: `ExerciseMatcher.cs`. Extracted `UnknownExercises` (beside `UnknownSessions`).
- `ExerciseNames.Dice` recomputed `Bigrams(a)` after mutating the first list: `ExerciseNames.cs`. Total is taken up front.
- `ExportSlicer` repeated `request.Range is null || request.Range.Overlaps(...)`: added `Overlaps(request, ...)` beside `InRange`.
- `MergeImport.Merge<T>` re-read the Wording name on every loop iteration: hoisted.
- `ImportInput.Inspect` null-coalesced a non-nullable `string` (nullable abuse): removed.
- Stray blank line at the end of `RestoreImport.cs`.

## Skipped
- `MarkdownRenderer.Num` pass-through to `TargetRules.Num`: a one-line alias used 7 times; inlining adds noise, and the shared helper is already the canonical one.
- `MarkdownRenderer` `document.ExportedAt!.Value`: `Exporter` always sets it before rendering; changing it would widen the surface for no behaviour gain.
- Singular/plural ("record/records", "is/are") inline in `RestoreImport` and `Wording`: a few small uses; a helper would not delete a concept.
- `Merge<T>` optional `choice`/`settle` delegates: the only two per-kind differences in a table-style call list; a policy object would be heavier.
- `Exporter` `_ =>` Markdown arm: two-value enum, left as is.
- `PromptPack` / `PromptPackFormat` split already follows the rules-vs-format-text boundary (108 / 194 lines).
