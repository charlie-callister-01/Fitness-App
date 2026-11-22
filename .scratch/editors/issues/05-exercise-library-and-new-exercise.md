# 05: Exercise library, New Exercise and the picker's live "Add a new Exercise"

**Read first:** `.scratch/editors/HANDOFF.md`, then the spec.

**Parent:** spec `docs/specs/editors-b4-b5.md` (issue #24), user stories 14 to 23; design S-04.

**What to build:** the library page replaces its placeholder: search, tag chips, groups by Session then "Not in a Session", rows showing log kind, tags and Guide status, a + that opens the New Exercise form sheet (name, how it is logged, tags, optional cue, "Write a Guide now", Cancel and Add Exercise). A no-match search offers "Add '<text>' as a new Exercise". In the Swap, Add and Custom workout picker, "Add a new Exercise" becomes live, opens the same form, and returns to the picker with the new Exercise chosen. The picker uses the Core choice rules from ticket 03. Saves go through the guarded save and sheets through the form sheet. Empty library shows an empty state. "Write a Guide now" goes to the Guide page route (built in ticket 06; link to it by URL).

**Blocked by:** 02, 03.

**Status:** ready-for-agent

- [ ] bUnit tests over the in-memory store: browse and group, search and tag filter, add an Exercise (stored with a permanent id), duplicate name refused with a message, no-match search offer, read-only disables Add with the reason, failure shows the notice and keeps the form, double tap saves once.
- [ ] Picker tests: "Add a new Exercise" is enabled, opens the form, and the new Exercise is selected on return in the log Swap and Add paths and the Custom workout draft.
- [ ] Theme and Mode behave; tokens only; works at 360 px.
- [ ] Assertions after async clicks use `WaitForAssertion`; `dotnet build` has 0 warnings.
