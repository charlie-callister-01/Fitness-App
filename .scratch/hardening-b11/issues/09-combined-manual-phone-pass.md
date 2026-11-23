# 09: Combined manual phone pass

Spec: `docs/specs/hardening-b11.md` (Solution, point 7).

**What to build:** Add a Hardening section to `docs/pwa-manual-checks.md`: largest phone text size; a screen-reader smoke test with TalkBack or VoiceOver on Home, the Workout log and a sheet; reduced motion on; a tablet or desktop window at ≥ 900 px; at the default text size the app looks the same as before ticket 04 (spot-check Home, Workout log and Progress against the mockups). Then do one pass on the phone covering it together with every owed check (A0, A3, B0–B3, Editors, A4.5 PR 2, Data and settings). Record each result with date and build. Remove the "owed phone checks" caveat from STATUS once done; anything that fails becomes a fix ticket or a REFACTOR item.

**Blocked by:** 08

**Status:** ready-for-human

Notes: the owner does the phone pass; an agent prepares the checklist and records results.

- [ ] Hardening section added to `docs/pwa-manual-checks.md`.
- [ ] Every owed section has a dated result for the current build.
- [ ] Failures are filed as fix tickets or REFACTOR items.
- [ ] STATUS caveat about owed checks removed.
