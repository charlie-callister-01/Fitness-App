# 02: Page context and form sheet, adopted by existing pages and sheets

**Parent:** spec `docs/specs/editors-b4-b5.md` (issue #24), "Page context" and "Form sheet module".

**What to build:** two shared modules and the existing callers moved onto them.
1. **Page context:** gives a page `CanWrite`, the read-only message, today's date and the active Plan. The active-Plan rule ("only one Plan is active") becomes a Core rule that Ui calls, replacing the three re-derivations. The `place` string of the read-only wording and the `canWrite` argument threaded through the finish flow disappear. May be merged into ticket 01's module if that is simpler.
2. **Form sheet:** owns open state, the read-only well (one ARIA role), the disabled rule (saving or read-only), the error message and close. Log weight, Log a past Workout, Do a different Session, Pain check, Logged afterwards and the Today card's read-only well move onto it.
3. Rename the log-named test fixture to a neutral page fixture and update its users.

Behaviour seen by the owner does not change.

**Blocked by:** 01.

**Status:** ready-for-agent

- [ ] The active-Plan rule is in Core with tests; no Ui page or builder calls `FirstOrDefault(p => p.IsActive)` itself.
- [ ] Home, the Custom workout draft and the log read CanWrite, the read-only message, today and the active Plan from the page context; `Today` is no longer copied per page.
- [ ] The five sheets and the Today card use the form sheet; the read-only well markup exists once and uses one ARIA role.
- [ ] The `canWrite` argument on the finish flow calls is gone.
- [ ] The neutral page fixture is used by Home, the draft and the log tests.
- [ ] All existing tests pass; `dotnet build` has 0 warnings.
