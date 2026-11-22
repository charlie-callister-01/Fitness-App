# 06: Guide page

**Read first:** `.scratch/editors/HANDOFF.md`, then the spec.

**Parent:** spec `docs/specs/editors-b4-b5.md` (issue #24), user stories 1 to 13; design S-03, F-09.

**What to build:** a Guide page per Exercise at its own route. Read view: title with where it sits in the Plan and Best, the sections in the design's order, Watch a demo (opens the link; offline shows the offline empty state), My notes with Edit notes, and Your history (last three, Personal records marked). Edit view: each section a labelled textarea, Cancel and Save, "Discard changes?" on leaving with unsaved edits (Keep editing or Discard). "No Guide yet" empty state with Write a Guide. Invalid video link refused with a plain message. Personal notes save separately. The log's View Guide sheet gets an "Open full Guide" link and shares the same read content; returning to the log restores the same Set. Saves use the guarded save, sheets the form sheet, and Core rules from ticket 03.

**Blocked by:** 02, 03.

**Status:** ready-for-agent

- [ ] bUnit tests: read view order, no-Guide state and Write a Guide, edit and save creates the Guide, notes save alone, discard prompt, invalid link, read-only disables Edit and Save with the reason, failure keeps the edits on screen.
- [ ] The offline video state is checked manually (added to `pwa-manual-checks.md` in ticket 10).
- [ ] Opening the full Guide from the log and returning leaves the Workout and the Set unchanged (test).
- [ ] Tokens only; `WaitForAssertion` after async clicks; `dotnet build` has 0 warnings.
