# 07: Badge editor

Spec: `docs/specs/insight-b6-b8.md` (C-22). Stories 44-49, 68, 69.

**Status:** ready-for-agent

**Blocked by:** 06 Trophies: Badges tab and detail.

**What to build:** The owner can create, edit and delete a Badge on a phone. The editor offers art (from the starter family), a Name, a Metric (Workouts, Streak, Best week, PR count, Exercise best, Body weight) and metric-specific fields (an Exercise and a target such as "24 × 8"; a weight target or "drop by" kg). A live line shows "Now: 22 × 9. Unlocks when…". Save and Delete (with confirm) go through the one guarded-save path; a failed save keeps the draft with the usual notice; a read-only app disables the editor. Badges are saved with the owner's data and appear in Backup and export (no format change; the model already has Badges). No Track picker in this stage.

New pure Core Badge editing rules in the same shape as the Editors' rules, returning the plain-reason edit result: validate a draft (name required, metric supported, target positive or a negative drop for body weight, Exercise required for Exercise best), the live line, and add, replace and delete with a new permanent ID on add. Metric choices map to the supported metric names.

- [ ] Core rules have Core.Tests: every validation failure reason, each metric mapping, live-line text, add assigns a permanent ID, replace, delete
- [ ] Editor bUnit tests via `PageFixture`: create, edit, delete with confirm, validation messages, save failure keeps draft (`FaultInjectingDataStore`), read-only app (`WriterLockResult.HeldElsewhere`)
- [ ] A round-trip test shows an edited Badge survives export and import
- [ ] Both Themes, both Modes, 360 px
- [ ] `dotnet build` 0 warnings, `dotnet test` green; commit `Insight: ticket 07 Badge editor`
