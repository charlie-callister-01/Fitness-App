# 11: Pain Nudge action: show easier variations

Spec: `docs/specs/insight-b6-b8.md` (Nudges), design C-08 (every Nudge has one action and one dismiss). Decided by the owner, 23 Nov 2026.

**Status:** ready-for-agent

**Notes:** Not started. Do after the Insight PR is merged. Needs a small design check against C-08 and S-01 before building.

**Blocked by:** None (the Insight stage is merged first).

**What to build:** The repeated-pain Nudge ("Knee pain reported twice this week. Consider an easier variation.") gets one action, **See easier variations**, besides Dismiss. It opens a sheet listing easier alternatives for the Exercises the owner flagged pain on, using the existing similar-Exercises rule in Core, and offers to open the Exercise in the library. Nothing is changed in the Plan automatically. The Nudge stays Home-only, never push, and at most two are shown.

Add the action to the Nudge kind's Home action mapping, a pure Core view for the alternatives (flagged body area to the Exercises that hit it this week to similar easier choices), and a sheet component. If no easier alternative exists, the sheet says so plainly.

- [ ] Core view has Core.Tests: pain area to flagged Exercises, similar alternatives, none found, ordering
- [ ] Home bUnit tests via `PageFixture`: action present, opens the sheet, Dismiss still works, at most two Nudges
- [ ] The Nudge copy review note in `docs/pwa-manual-checks.md` is updated
- [ ] `dotnet build` 0 warnings, `dotnet test` green
