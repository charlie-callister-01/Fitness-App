# 02: Token contrast test

Spec: `docs/specs/hardening-b11.md` (Implementation Decisions: Token contrast test).

**What to build:** A pure unit test in the Ui test project that reads the design tokens stylesheet, resolves each of the four Theme × Mode sets (Keep dark, Keep light, Clean dark, Clean light), and computes the WCAG contrast ratio for every foreground/background pair listed in design document §4.2. Text pairs need 4.5:1; large text and non-text UI (borders, focus outline, icons) need 3:1, as §4.2 classifies them. The pair list is defined once in the test. Any failing pair is fixed in the tokens, and the §4.2 table is updated in the same change.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] The test covers every pair in §4.2 for all four sets and names the set, pair and ratio on failure.
- [x] Changing a token so a pair drops below its threshold makes the test fail (checked once by hand, not committed).
- [x] All pairs pass; any token changes are reflected in the §4.2 table.
- [x] `dotnet build` 0 warnings; `dotnet test` green.
