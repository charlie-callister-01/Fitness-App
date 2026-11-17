# 05: Preview a Plan or Guides Import

**What to build:** Spec "Import: the four kinds" (Plan, Guides) and "Preview shape". `ImportPreviewer.Preview(snapshot, inspected, kind, decisions)`: the `ImportPreview` type (items, matches, open choices, `IsReady`), the Change list applied to a copy of the snapshot and validated with the A1 validator (issues become plain-language blockers), the ignored-items list, and summary wording. Plan: new inactive Plan (first Plan active), fresh IDs on collision with overrides following. Guides: add / replace-if-empty / keep-by-default with per-Guide choice, personal notes never overwritten. Records compared by stored JSON.

**Blocked by:** 04.

**Status:** ready-for-agent

- [ ] Plan and Guides cases from the spec's Testing Decisions pass, including summary wording
- [ ] Re-previewing the same file after applying would say nothing to change (stable IDs)
