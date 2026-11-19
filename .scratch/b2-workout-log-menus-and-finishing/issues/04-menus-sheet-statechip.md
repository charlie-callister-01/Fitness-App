# 04: Menus: shared Sheet shell, StateChip, Exercise menu and Workout menu

Parent spec: `docs/specs/b2-workout-log-menus-and-finishing.md` (Ui: menus and sheets, Exercise menu stories 1 to 5, benchmark 29 to 31, Workout menu 37 to 42). Design: §8.4, C-07.

**What to build:** the menu skeleton on the log page. A generic Sheet/dialog shell built once for every later sheet (labelled dialog, `aria-modal`, focus starts inside, focus trap, Escape closes, focus returns to the opener). StateChip (C-07) with glyph, word and colour for PR, Done, Partial, Skipped, Swapped, Pain. An Exercise menu button beside the Exercise name ("Menu for <name>") listing Swap, Skip, Add an Exercise, Mark as / Unmark as benchmark, View Guide; a Workout menu in the top bar listing rest timer Off/60/90/120 s, Theme, Mode, Logged afterwards, Finish Workout. In this ticket Mark/Unmark benchmark, Theme, Mode and the rest-timer choice (remembered per device, using B0's preference mechanism) really work; the other items are present but disabled with no behaviour yet, so later tickets only fill them in. Keep the page a thin wiring class: put menu logic in new classes or components. Benchmark commits the Exercise record and reports failure through the shared save-failure notice.

**Blocked by:** 01 (benchmark helper).

**Status:** ready-for-agent

- [ ] Both menus open and close by button, outside tap and Escape; focus returns to the button; items are at least `--tap-min` targets; the Exercise menu button has an accessible name
- [ ] Sheet shell is reusable (title, body, actions) and covered by component tests: focus start, Escape, return focus, `aria-modal`
- [ ] StateChip shows glyph and word for each state, never colour alone, and uses design tokens only
- [ ] Benchmark toggle saves at once, label and mark reflect the state, a failed save shows the banner and leaves the screen unchanged; read-only disables it
- [ ] Theme and Mode switch from the Workout menu; rest-timer choice persists across reload and defaults to Off
- [ ] New tokens (if any) added to both `tokens.css` copies, `tokens.json` and the design document tables in the same change; no raw hex or px in component CSS
- [ ] bUnit page tests written first (red run in the report); all existing tests stay green; no browser test
