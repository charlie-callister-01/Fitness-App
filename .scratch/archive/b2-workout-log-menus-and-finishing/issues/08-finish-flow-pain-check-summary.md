# 08: Finish flow: Partial prompt, Pain check, summary

Parent spec: `docs/specs/b2-workout-log-menus-and-finishing.md` (Finishing stories 51 to 57, Pain check 58 to 65, Summary 66 to 73). Design: C-13, §8.5, mockups `S-02-log-finish-partial-dark.png`, `S-02-log-pain-check-dark.png`, `S-02-log-pain-check-yes-dark.png`, `S-02-log-summary-dark.png`.

**What to build:** replace B1's temporary Finish wholesale. Finish (the primary action after the last Exercise, or Finish Workout in the Workout menu) runs: the Partial prompt when any planned Exercise is not done ("Finish as Partial? 5 of 8 done; the rest are recorded as skipped. Partial still counts toward your Streak." with **Keep going / Finish**), then the finish write (Core Finish, committing the end time first), then PainCheckSheet C-13 (**No** primary and focused, **Yes** expanding in place with area chips, severity chips, optional Exercise select, optional side and note, **Skip** recording "not answered", Escape = Skip, **Save and see summary**), then the summary from the Core builder: duration, Sets, PRs with previous best, every Exercise's Done/Skipped/Swapped chip with reason or "was X" and RPE, the Pain check result, the Streak line, a Badge unlock toast, and **Done** to Home. Reopening a finished Workout's log URL shows the summary (an unanswered Pain check is not re-asked). A Workout with no confirmed Set shows a plain message instead of finishing; a second Finish tap while saving does nothing; a failed finish leaves the Workout open with the banner.

**Blocked by:** 02, 03, 06.

**Status:** ready-for-agent

- [ ] Fully done Workout skips the Partial prompt; Partial prompt Keep going changes nothing; Finish writes explicit skipped entries
- [ ] Killing the tab after the finish write but before the Pain check leaves a finished Workout with an unanswered Pain check
- [ ] Pain check: No, Skip and Yes are distinct saved results; Yes needs an area and a severity with a plain message otherwise; chips are `aria-pressed` toggles; Escape is Skip; saving state shown
- [ ] Summary content matches the Core builder for a Partial, a swap, a skip, a PR and a Badge unlock; duration shows "—" when absent
- [ ] Reopening a finished Workout's URL shows the summary rather than "not found"
- [ ] The temporary Finish and its code note are gone
- [ ] Read-only disables Finish; failed save shows the banner
- [ ] bUnit page tests written first (red run in the report); all existing tests stay green
