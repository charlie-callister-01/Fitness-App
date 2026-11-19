# Draft manual phone checks for B2 (collected from ticket reports)

Ticket 09 moves these into `docs/pwa-manual-checks.md` under "Workout log manual checks (B2)", and adds the checks for the menus, Skip, Swap, Add, Pain check, summary and Logged afterwards (from tickets 04, 05, 06, 08 and 09 reports).

## From ticket 07 (rest timer, Wake Lock, Guide sheet)
1. Rest timer Off (the default): confirm a Set and no "Rest" text appears.
2. Choose 90 s: confirm a Set and "Rest 1:30" shows at the right of the ConfirmBar strip, counting down each second.
3. Confirm the next Set mid-countdown and it restarts at 1:30.
4. Let it reach 0:00: it disappears with no sound, toast or flash.
5. Lock the phone or switch apps for 30 s, then return: the number matches real elapsed time, not a frozen count.
6. Choose Off mid-countdown and the readout clears. Leave the log mid-countdown and nothing keeps running.
7. Wake Lock: open a Workout, leave the phone idle past its normal sleep time, and the screen stays on.
8. Switch to another tab or app and come back: the screen stays on again (reacquired). Leave the log (Back or Home) and the screen sleeps normally after the usual timeout.
9. Wake Lock on a browser without support (or with battery saver on): the log works normally with no error shown.
10. Guide sheet: open View Guide on an Exercise with a Guide and on one without ("No Guide yet"). The video link opens in a new tab and the log is still there when you return. Type a weight, open the Guide, tap Back to the Set, and the typed text, open row and pager position are unchanged. Escape or a tap outside also closes the sheet. Check at 360 px and in all four looks, and that a long Guide scrolls.
11. Offline: install the PWA, turn on airplane mode, open the log, then open the Exercise menu, the Workout menu and the Guide sheet; all open and close with focus working (relies on `Sheet.razor.js`, which the publish manifest was confirmed to include).

## From ticket 04 (menus)
- Sheet focus: opening a menu moves focus inside it, Tab stays inside, Escape closes and focus returns to the button (bUnit only checks that the component asks for this).
- Mode in the Workout menu offers Match phone, Dark, Light (the mockup shows two).

## Notes for the docs and PR
- Added Exercise position limitation (ticket 06): the model does not record which row an added Exercise follows; it is placed after the row owning the nearest earlier entry. A proper fix needs a position field (A-stage change). Mention in the PR and open items.
- Past-dated Workout has no end time, so it is in progress until finished (ticket 02).
- Undoing Logged afterwards cannot restore the start time (re-dating discards it) (ticket 02).
