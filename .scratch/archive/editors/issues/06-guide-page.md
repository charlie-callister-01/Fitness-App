# 06: Guide page

**Read first:** `.scratch/editors/HANDOFF.md`, then the spec.

**Parent:** spec `docs/specs/editors-b4-b5.md` (issue #24), user stories 1 to 13; design S-03, F-09.

**What to build:** a Guide page per Exercise at its own route. Read view: title with where it sits in the Plan and Best, the sections in the design's order, Watch a demo (opens the link; offline shows the offline empty state), My notes with Edit notes, and Your history (last three, Personal records marked). Edit view: each section a labelled textarea, Cancel and Save, "Discard changes?" on leaving with unsaved edits (Keep editing or Discard). "No Guide yet" empty state with Write a Guide. Invalid video link refused with a plain message. Personal notes save separately. The log's View Guide sheet gets an "Open full Guide" link and shares the same read content; returning to the log restores the same Set. Saves use the guarded save, sheets the form sheet, and Core rules from ticket 03.

**Blocked by:** 02, 03.

**Status:** done

- [x] bUnit tests: read view order, no-Guide state and Write a Guide, edit and save creates the Guide, notes save alone, discard prompt, invalid link, read-only disables Edit and Save with the reason, failure keeps the edits on screen.
- [x] The offline video state is checked manually (added to `pwa-manual-checks.md` in ticket 10).
- [x] Opening the full Guide from the log and returning leaves the Workout and the Set unchanged (test).
- [x] Tokens only; `WaitForAssertion` after async clicks; `dotnet build` has 0 warnings.

## Notes from implementation

- Route `/exercises/{ExerciseId}/guide`, `GuidePage` (PushedPageLayout). The read view is shared: `GuideContent` (now with `IncludeNotes`, `OnWatchDemo`, static `HasContent`) is used by the log sheet and the page; the sheet keeps its plain new-tab video link, the page uses a Watch a demo button backed by `GuidePage.razor.js` (`openVideo` returns false when `navigator.onLine` is false; a JS failure also shows the offline state). A blocked pop-up cannot be detected with `noopener`; that is a manual check.
- Discard prompt uses `RegisterLocationChangingHandler`, so the back arrow, links and browser Back all ask. A browser refresh or tab close is not intercepted.
- Returning to the log: new singleton `LogEditStates` (registered in Web `Program.cs` and `PageFixture.Wire`) keeps the log's unsaved state (Exercise showing, typed text) when the page is disposed and hands it back on return.
- No Guide: `EmptyStateKind.NoHistory` plus a separate Write a Guide button (the EmptyState button cannot be disabled). Import Guides is not offered (B9).
- Two existing sheet tests now select `a.fa-guide__video`, since the sheet has a second link.
