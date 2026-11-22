# 04: Base components: Button, Icon, StatusOrb, EmptyState

**What to build:** The four base components every later stage reuses, built from the design: Button (C-01: Primary, Secondary, Stepper, Icon variants, disabled state, renders a link when given a destination), Icon (one named icon inlined so it takes `currentColor`, closed list of names), StatusOrb (C-11: value, ring fraction, caption, accessible label, greyed for a Break, pure SVG) and EmptyState (C-21: illustration, bold line, muted line, optional primary action, the four illustration variants). Shown on a temporary gallery section so the owner can see them in all four looks. Spec: `docs/specs/b0-design-system-and-shell.md`. Design: `docs/design/design-document.md` (§5 C-01, C-11, C-21, §4.11). Nothing is invented beyond the design.

**Blocked by:** 02 (Design assets, offline cache and PWA identity)

**Status:** done

- [x] Button renders every variant with the design's classes and sizes (44 px minimum, primary 64 px); a disabled button is unclickable and looks disabled; an Icon button without an accessible name refuses to render (pinned by a test)
- [x] Icon names are a closed list so a typo does not compile; a test asserts every name has a file and every shipped icon file has a name
- [x] StatusOrb clamps the fraction to 0 to 1, renders the empty ring for a non-finite value, exposes the accessible label (e.g. "Streak, 4 weeks, 2 of 3 this week") and has a greyed Break state with the "On a Break" caption
- [x] EmptyState shows the right illustration per variant, one optional action, and the Clean Theme art treatment is CSS only
- [x] Components behave identically in both Themes (only tokens change), use only tokens (no raw hex), and have a visible focus outline
- [x] bUnit tests assert visible text, roles, labels and behaviour, not CSS internals beyond the documented `fa-*` classes
- [x] The temporary gallery shows all variants; existing tests pass
