# 04: Text scaling and 360 px overflow

Spec: `docs/specs/hardening-b11.md` (Implementation Decisions: Text scaling, Overflow checks).

**What to build:** Make text grow with the browser and OS text size. Move font-size tokens and every font size in component and page styles from `px` to `rem` (16 px root), and make line heights unitless. Borders, outlines, outline offsets, touch-target minimums, radii, shadows and spacing tokens stay as they are. Tiles whose names ellipsis carry the full name in their accessible name. Add browser checks that no screen, empty or with the fixture, scrolls sideways at 360 px wide with the normal root font size and at 200%, and fix whatever they find (wrap Exercise names, let rows grow).

**Blocked by:** 01

**Status:** resolved

- [x] No font size or line height in the Ui styles is in `px`; non-text sizes are unchanged.
- [x] At 360 px, every screen (empty and fixture) has scroll width ≤ viewport width at 100% and 200% root text.
- [x] Ellipsised tile names have the full name in their accessible name.
- [x] At the default text size the app looks the same as before (spot-check Home, Workout log and Progress against the mockups).
- [x] `dotnet build` 0 warnings; all tests green.
