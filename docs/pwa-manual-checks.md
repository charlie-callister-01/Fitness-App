# PWA: how it works and the manual phone checks

Files (all in `src/Web/wwwroot`): `manifest.webmanifest`, `service-worker.js` (development: does nothing, no caching), `service-worker.published.js` (becomes `service-worker.js` on publish), icons (`icon-192.png`, `icon-512.png`, `icon-maskable-192.png`, `icon-maskable-512.png`, `icon-monochrome-512.png`, `apple-touch-icon.png`, `favicon.svg`, `favicon-32.png`, `favicon-16.png`). The shared design assets (`tokens.css`, `components.css`, the Jersey 15 font, icons, graphics, illustrations, badges) are copies in `src/Ui/wwwroot`, served under `_content/Ui/`.

## How offline and updates work

- On publish, .NET generates `service-worker-assets.js`: every published asset (runtime, assemblies, `_content/Ui/*.css`, fonts, SVG artwork, icons, `index.html`, manifest) with a hash, plus a `version` that is a hash of the whole set. It changes whenever any asset changes.
- The cache is named `offline-cache-<version>`. Install downloads the full list into the new cache. Activate deletes every other `offline-cache-*` cache.
- A new build installs in the background but **waits** (no `skipWaiting`) until the old version's tabs/windows are closed, then activates. So a page never mixes old shell with new assemblies. Practically: after deploying, open the app once online (it fetches the update), close it fully, open it again: you now have the new build. The placeholder shows the build identifier so you can tell.
- The worker derives its base path from its own URL, so hosting under a sub-path needs no edit to it. `index.html`'s `<base href>` is rewritten at publish time by `scripts/publish-pages.sh`, which also updates `index.html`'s hash in `service-worker-assets.js` (the worker checks it, so skipping that would break the offline install). See the README Deploy section.
- No permissions are requested and no data is collected.

## Automated or inspectable checks (no phone)

1. `dotnet publish src/Web -c Release -o <dir>`.
2. `<dir>/wwwroot/service-worker-assets.js` lists `js/indexeddb-store.js`, `js/storage-environment.js`, `_content/Ui/tokens.css`, `_content/Ui/components.css`, `_content/Ui/fonts/jersey-15-latin-400-normal.woff2`, `_content/Ui/icons/*.svg`, `_content/Ui/badges/*.svg`, all `_framework/*.wasm`, icons, `index.html` and `manifest.webmanifest`.
3. Changing any asset (for example `css/app.css`) and republishing changes `version`.
4. Chrome DevTools, Application tab: Manifest shows no errors and "Installability" passes; Service workers shows activated; Cache storage shows one `offline-cache-*`.

## Manual phone checks (needs the deployed HTTPS URL or localhost)

Install:
- Android Chrome: open the URL, use the install prompt or menu "Install app". It opens standalone with the dark theme colour and a maskable icon.
- iOS Safari: Share, "Add to Home Screen". Check the 180px icon and that it opens without browser chrome.

Offline:
1. Open the installed app once while online and wait for it to load fully.
2. Close it, switch on airplane mode, open it again. The placeholder (app name and build id) must appear.
3. Design assets offline: the heading is in the pixel display font (Jersey 15, not a system fallback), and an icon or illustration on a page (for example the EmptyState artwork once a screen uses it) still shows. In DevTools, Network (offline) shows `_content/Ui/fonts/...woff2` and `_content/Ui/icons/...svg` served from the service worker.

Installed icon:
- Android: the home-screen icon is the designed icon, shown correctly in both a round mask and a squircle mask (nothing important clipped). With themed icons on (Android 13+), it appears as a single-colour silhouette.
- iOS: the 180px icon shows without a black border.
- The splash screen background is the Keep dark brown (`#2B2620`), not blue.

Update pickup:
1. Note the build id on the phone. Deploy a new build.
2. Open the app online, close it completely (swipe away), open it again. The build id is new.
3. Go offline and reopen: still the new build, working. In DevTools (desktop) only one `offline-cache-*` should remain.

## Storage manual checks (A3)

What the storage layer does in a real browser and CI cannot show. Status: **no screen shows storage messages yet** (the UI stages come after the design). Until then the facts are in `AppData` (`Startup.State`, `Startup.Issues`, `Startup.Failure`, `Health`), the wording is the UI's job, and the checks below say what to look at: Chrome DevTools, Application tab (IndexedDB `fitness-app` database, Local storage, Storage panel) on desktop or via remote debugging for a phone. Re-check the visible wording when the storage UI is built.

What is **covered by CI** (`browser-tests.yml`, headless Chromium, see `src/Web.BrowserTests/README.md`): the whole `IDataStore` contract against real IndexedDB, transaction rollback, reload persistence, the layout-version-1 database shape, and the writer lock between two pages of one Chromium context.

What is **NOT covered by CI** and is on this list: quota-full, the real persistent-storage grant, the browser clearing site data, the blocked upgrade, iOS Safari (and the writer lock outside Chromium).

1. Persistent-storage grant (not installed, then installed).
   - Trigger: open the deployed URL in a normal browser tab, then install the app (see Install above) and open the installed copy. Startup asks on every launch until it is granted.
   - Expect: the answer is one of granted, not granted, unsupported, in `Health.Persistence`. In DevTools, Application, Storage, "Persistent storage" shows the grant. Chrome usually grants only when installed or heavily used, so "not granted" in a plain tab and "granted" once installed is normal. Not granted blocks nothing: the app works and the UI is expected to show the "install the app and make Backups" advice. Safari and Firefox can answer differently (Firefox asks the owner).
2. Quota-full.
   - Trigger: desktop Chrome, DevTools, Application, Storage, tick IndexedDB and use "Simulate custom storage quota" set very low, or fill the phone's free space, then save something.
   - Expect: the commit does not appear saved: `CommitResult.StoreFailed` with `StoreFailureKind.StorageFull`, the old snapshot unchanged, and the stored data unchanged after a reload. The UI wording (not built yet) is meant to say storage is full, that the entry is kept on screen, and to free space and try again. After freeing space the same save works.
3. Browser-cleared data detection.
   - Trigger: use the app until something is saved (so the marker is written). In DevTools, Application, "Clear site data" with IndexedDB ticked but **Local storage left unticked** (clearing both makes it look like a first run, which is unavoidable and covered by the Backup Nudge). Reload.
   - Expect: `StartupState.BrowserCleared`, not `FirstRun`: an empty snapshot, writing allowed, the UI is meant to offer restoring the latest Backup (restore is A4). Also try the reverse: clear only Local storage; the "initialised" record inside IndexedDB survives and the data is intact, so the app starts `Ready` with its data. (Clearing IndexedDB removes the "initialised" record with it, so the marker alone then decides.)
4. Two windows (second is read-only).
   - Trigger: open the app in one window, then open the same URL in a second window or tab (on a phone: installed app plus the same URL in the browser).
   - Expect: the first window works. The second shows `Health.AnotherWindowOpen = true`, `CanWrite = false`, and any commit returns `StoreFailureKind.WriterLockHeld` with nothing saved. The UI wording is meant to be "Fitness App is open in another window". Closing the first window and reopening the second lets it write. A browser without Web Locks is treated as a single window (it can write) and reports `Health.WriterLock = Unsupported`, so there is no lock to test there; if asking the browser fails, the window stays read-only (`WriterLock = Unknown`). CI only covers Chromium.
5. Blocked upgrade.
   - Trigger: needs two builds with different layout versions. Layout version is still 1, so no real upgrade exists yet. To rehearse: in a throwaway local change raise `LAYOUT_VERSION` in `src/Web/wwwroot/js/indexeddb-store.js` to 2 and add a harmless step to `upgradeSteps`. Open the old build in one window, then the new build in another.
   - Expect: an idle old window closes its connection on `versionchange` and the upgrade finishes by itself. If the upgrade is blocked anyway, startup returns `StorageFailed` where `Startup.Failure.IsBlockedByAnotherWindow` is true, and the UI is meant to say "close other windows and reopen". Nothing is written. Revert the throwaway change.
6. iOS Safari.
   - Trigger: install via Share, Add to Home Screen; also open the URL in a plain Safari tab. Use the app, close it, reopen it, go offline and reopen it.
   - Expect: data saved earlier is there after reopening. Persistence may be reported not granted or unsupported in a plain tab; an installed copy is more likely to be kept. Safari can delete script-writable data after about seven days without use, for a non-installed site: so check the browser-cleared message after a long gap, and keep making Backups. The installed Home Screen copy and the Safari tab do NOT share storage on iOS, so data entered in one is not in the other: note which one is used. Web Locks and the storage estimate can be missing on older iOS; the app must still start.
7. Open offline after install, with the storage modules loaded.
   - Trigger: install the app, open it once online until it has loaded (the service worker caches `js/indexeddb-store.js` and `js/storage-environment.js` with the rest), save something, close it, switch on airplane mode, open it again.
   - Expect: the app starts (no blank screen or failed module import), the saved data is still there and `StartupState` is `Ready`. Saving works offline because storage is on the device. Also check after an update pickup (see above) that the new modules are used and the old data still loads.

## Theme and Mode manual checks (B0 ticket 03)

CI covers `wwwroot/js/theme.js` in headless Chromium (`ThemeScriptBrowserTests`: pre-paint attributes, System following the colour scheme, chrome colour, blocked mirror). It does **not** run WebAssembly, so the real Blazor interop (`Web/Theming/ThemeInterop.cs` calling `FitnessTheme.apply`) and a real phone need these checks. Use the Appearance section on the More page.

1. All four looks. Pick each of Keep and Clean with each of Dark and Light. Expect the whole app to restyle instantly, and the browser bar (Android Chrome) to change colour: Keep dark `#4A4237`, Keep light `#E8DFC8`, Clean dark `#111315`, Clean light `#F4F5F7`.
2. No flash. With a non-default choice (for example Clean, Light), reload and cold-start the installed app several times. Expect the first painted frame to already be the chosen look, never Keep dark first.
3. Survives restart and airplane mode. Choose Clean, Light, close the app, switch on airplane mode, open it. Expect the same look (the script is in the offline cache).
4. System follows the phone. Choose Mode System, then switch the phone between dark and light (Quick Settings) with the app open. Expect the app to follow without a reload.
5. Source of truth. In DevTools (remote debugging) change `data-theme` on `<html>` by hand, then reload: the stored choice comes back. After restoring a Backup that has a different Theme or Mode, expect the app to show the restored look.
6. Blocked storage. In a private tab, the look still switches for the session and may reset on reload; nothing errors.

## App shell manual checks (B0 ticket 05)

bUnit covers tab marking, layout choice and routing. It cannot check CSS breakpoints, the real router under the sub-path, or a phone, so these are manual.

1. Deep link and refresh. On the deployed build open `/Fitness-App/week`, `/Fitness-App/progress`, `/Fitness-App/trophies`, `/Fitness-App/more` directly, then refresh each. Expect the right page and the right tab marked (the 404 redirect brings the deep link back); the tab bar links stay under `/Fitness-App/`.
2. Not found. Open `/Fitness-App/nowhere`. Expect the friendly page inside the shell and "Go to Home" landing on `/Fitness-App/`.
3. Widths. In DevTools device mode check 360 px (no sideways scroll, nothing clipped, bottom bar with five tabs), 600 px (a centred column of at most 560 px, bottom bar), 900 px and wider (a 220 px left rail, content to its right, no bottom bar).
4. Safe areas. On a notched phone installed to the Home Screen, the bar clears the home indicator and content clears the notch.
5. Skip link. Press Tab once on a desktop: "Skip to content" appears with a visible outline; Enter moves focus into the content and the address does not change.
6. Heading focus. Navigate between tabs with a screen reader or by watching `document.activeElement`: the page heading takes focus.
7. Layouts. A pushed-layout page shows a back arrow and no tab bar; the back arrow returns to the previous page. (The temporary gallery and Appearance pages are gone; both live on More.)

## More menu and B0 finish line (B0 ticket 06)

bUnit covers the More links, the placeholder routes, Appearance changing the Theme, and the build identifier. Needs a phone (tick off here when done):

1. More page. Open More: six entries (Plans, Exercise library, Weight, Weekly review, Export and Import, Settings), each opens a placeholder page and the More tab stays marked. The Appearance pickers work; the collapsed "Components (temporary)" section opens and shows buttons, orbs, empty states and icons in the chosen look.
2. Build identifier. The "Build ..." line is at the bottom of More (no longer on Home); update pickup (above) is checked against it.
3. Install with the new icon, and all four looks (Keep and Clean, Dark and Light) on every tab and on More.
4. Airplane-mode launch: the app opens, fonts and illustrations show, the Theme survives.
5. Status bar colour matches the chosen look.
6. Deep link on each tab: `/Fitness-App/week`, `/progress`, `/trophies`, `/more`, and one More entry such as `/settings`, then refresh.
7. 360 px width: no sideways scroll, nothing clipped, More entries comfortable to tap.
8. Increased system text size: the tab bar, More entries and Appearance chips still fit and read.

## Workout log manual checks (B1)

bUnit covers the log page over the real store: what is stored for every kind, `inputmode` and labels as attributes, `aria-invalid`, `aria-pressed`, the stepper names, the PR marker, toast and label, Resumed, and the read-only and failed-save states. It cannot check CSS, a real phone keyboard, focus rings, safe areas, the browser Back button or a killed tab, and no browser test was added for B1 (owner's decision). These are manual (tick off here when done). Start a Workout from the temporary Start button on Home; the log is at `/Fitness-App/log/<Workout id>`. Visual targets are `docs/design/assets/mockups/S-02-log-*.png`. You need a Plan with a Session that has a spread of Exercises (weight x reps, bodyweight, carry, seconds, minutes, reps only, time, distance, tick-only, body measurement) and some history for at least one of them, so Last, Best and a Suggested target show. Most of this is quicker on a phone than in DevTools, but the CSS ones (1 to 5) can be done in DevTools device mode first.

CSS and layout (deferred from tickets 05 and 10):

1. Visible focus. With a keyboard (desktop, or a Bluetooth keyboard on the phone) Tab through the log: Back, the pager chips, each input, the state tick, Add Set, Remove Set, Mark as Max set, the RPE buttons, the steppers and the primary button. Each shows a clear focus outline (the Theme's focus colour) that is never clipped by the row or the bottom bar. Check in Keep and Clean.
2. Touch targets. In DevTools, inspect: pager chips, Back, the Add Set, Remove Set and Max set links, the RPE buttons and the state tick are at least 44 px tall and wide; the steppers are 60 px wide and 64 px tall; the primary button is 64 px tall. Taps one-handed with a thumb land first time, with no mis-taps between neighbouring controls.
3. Safe-area bottom bar. On a phone with a gesture bar or notch, installed to the Home Screen: the Up next strip and the primary button sit fixed at the bottom, clear the gesture bar (the primary button is not under it), and stay put while the Set rows scroll. With the keyboard open the bar is still reachable. With many Sets the last row can scroll clear of the bar.
4. 360 px, no sideways scroll. In DevTools device mode at 360 px (and on a small phone), for each row shape: the two-input row (weight x reps, carry, bodyweight), the wide-input row (reps, seconds, minutes, time, distance, body measurement; it spans the middle columns) and the tick row (one big Done button). Nothing is clipped, nothing scrolls sideways, the number, both fields, the "x" and the state cell all fit, a long Exercise name or Session name wraps, and a validation message under the row wraps. Also check the Max set label and a long Plan cue.
5. Wide screens. At 600 px and 900 px the log stays one centred column of about 430 px, with no tab bar or rail.
6. All four looks. For Keep and Clean, each in Dark and Light, compare the log with the mockups: `S-02-log-dark.png`, `S-02-log-light.png`, `S-02-log-clean-dark.png`, `S-02-log-clean-light.png` (and `S-02-log-360-dark.png`, `S-02-log-desktop.png`, `S-02-log-exercise-done-dark.png`, `S-02-log-error-dark.png`, `S-02-log-pr-dark.png`). Keep should be nearly plain on this screen. The pending (muted) rows, the outlined current row, the green tick, the sunken Suggested target well and the pager chips all read clearly. Things that are not built in B1 and will be missing against the mockups: the Exercise and Workout menus, rest timer, skip and swap marks, pain check, summary, and a date or timer line. (The menus, rest timer, skip and swap marks, Pain check, summary and Logged afterwards line came in B2; check them in the B2 section.)

Keypad and input:

7. Numeric keypad per kind. Tap into each field and check the keyboard: Weight, Added weight, Weight per hand, Distance (km or m), Minutes and body measurements open the decimal keypad; Reps, Metres (carry) and Seconds open the whole-number keypad; Time opens the ordinary text keyboard (so the colon is available). The tick-only row has no field. A comma on the keypad is accepted as the decimal point (22,5 stores 22.5).
8. Select-all on focus. Tapping into a pre-filled field (and tabbing into it) selects all its text, so typing replaces it. This also works on the second field and when Enter moves focus from the first field to the second.
9. Enter and the keypad's action key. In a two-input row Enter (or Next) moves from the first to the second field, and Enter in the last (or only) field confirms the Set.
10. Per kind, with real fingers. One Set each: weight x reps (`22.5` x `8`), bodyweight (the first field starts as `bw`; type a weight there to add load), carry (kg per hand x metres), seconds (`45`, `45s`), minutes (`3`, `3min`), reps only, time (`1:48`, `108`, `1:04:10`), distance km and m, tick-only (one tap), body measurement. Each confirms, shows the green tick and keeps its value after a reload. Known limits are listed under "Open items for the owner" in `build-plan.md`: Minutes does not accept `12:30`, Text Exercises show a "cannot be logged yet" note, and there is no distance + time row.
11. Validation. Type `abc` or leave a field blank and tap Confirm Set: the plain message appears under that row, the bad field is outlined, focus goes back to it, and what you typed is kept. Nothing is checked while typing.
12. Steppers. For weight x reps and carry the steppers show the Plan's increment (2 kg by default; switch the units to lb and they show the lb step, such as 5 or 4.5) and change only the current Set's weight, carrying the new value down to later unconfirmed Sets and not to confirmed ones. Seconds steps 5 s, bodyweight steps 1 rep. Time, distance, minutes, reps only and tick have no steppers.

Back, leaving and resuming:

13. Back during a Workout. Confirm one Set, then tap the on-screen Back, then the phone's Back gesture or button. You land on Home (or the previous page) and Home shows Continue, not Start. Continue reopens the log on the same Exercise with "Resumed where you left off", the confirmed Set still there. Start is not offered while a Workout is in progress.
14. Kill the tab mid-Workout. Confirm two Sets of the first Exercise, tap into the next Set's field and type a value without confirming, set an RPE, then swipe the app away (or kill the browser from the app switcher, or end the tab) and reopen it. Expect: the confirmed Sets and the RPE are back, the typed-but-unconfirmed value is gone (at most the field you were typing is lost), and the log opens on the first incomplete Exercise with the Resumed note.
15. Unconfirmed extra Set is lost. Tap Add Set to add an extra row (and optionally type in it), do not confirm it, then kill the tab and reopen. Expect the extra row to be gone: only confirmed Sets are saved. Confirm the extra Set first and repeat: it stays.
16. Deep link and refresh. Refresh the log in the browser: it restores with the Resumed note. A log URL for a Workout that does not exist shows "Workout not found" and a Back to Home button; for a finished one it shows the summary (B2).
17. Airplane mode. Open the installed app online once, switch on airplane mode, Start a Workout (or Continue) and log a few Sets, including the PR check below. Everything saves (it is on the device), the pager, Next and Finish work, and after closing and reopening still offline the Workout is intact.
18. Next and Finish. The primary button reads Confirm Set while Sets remain, then "Next: <Exercise>" when this Exercise is complete, then Finish after the last. The Up next strip names the following Exercise ("Last Exercise done" at the end). Finish (B2) goes to the Pain check and then the summary, and Done returns Home, where Start is offered again; tapping Finish twice quickly must not stamp twice. The Workout menu offers Finish Workout at any time, with the Partial prompt when Exercises are left (see the B2 checks below).

PRs and the header:

19. The PR marker and toast. Log a Set that strictly beats the Exercise's Best (more estimated 1RM for a lift). Expect the PR marker in the state cell (the shield in Keep, a "PR!" block in Clean), a short toast under the rows ("PR! 22 kg x 9 beats 22 x 8"), and the top bar count ("1 PR"). Screen readers read ", PR" on the reps label. The first ever result for an Exercise is not a PR. A Set equal to the Best is not a PR. Tap the green tick to reopen and edit the PR Set down: the marker, toast and count go away at once; a later, better Set moves the marker to itself. A Max set counts as the Best even when a heavier volume Set exists. Also check a time Exercise (lower is better) that has a previous time: a faster time (`1:40` after `1:48`) shows the PR; a slower one does not. This path has Core tests but no page-level test.
20. A PR in each Theme. Repeat check 19 in Keep Dark, Keep Light, Clean Dark and Clean Light (change them in More, Appearance, then come back with Continue). The marker, toast and count are readable and have enough contrast in all four, and the marker is stationary (no animation). With the phone's "remove animations" setting on, nothing moves.
21. Header. For an Exercise with history, Last and Best show on one line and the Suggested target sits in a sunken well with its reason. An Exercise done for the first time shows none of these and no empty labels. The log uses the owner's units everywhere (see the lb check below).
22. RPE. Tap 8: it is marked pressed. Tap 8 again: cleared. RPE never blocks Confirm or Next. Tapping RPE before any Set is confirmed is allowed (see open items).
23. Add Set, Remove Set and Max set. Add Set appends a row; an unconfirmed extra row can be removed; "Mark Set n as Max set" labels the row "Max set" and the same link now reads "Unmark" and removes the label. All of it survives a reload (the Max set and every confirmed Set; not an unconfirmed extra row, see check 15).

lb check:

24. There is no screen to change units yet (Settings is B10), so this check needs a Backup whose Settings say lb, restored through Import (or the units set in DevTools on a throwaway copy of the data); skip it until that exists and note it as pending. With lb on, open a Workout with history. Last, Best and the Suggested target show in lb; the pre-filled weights show in lb; the steppers show the lb step; typing `50` stores 50 lb converted to kg (check by switching back to kg: it reads 22.7). The PR toast shows lb. The Suggested target's reason says lb too (fixed in B2).

Storage trouble:

25. Failed save and read-only. With DevTools (desktop) simulate a quota-full or offline storage failure while confirming a Set: the plain banner appears, the typed value stays, and a retry works after the cause is cleared. With the app open in a second window, the second window's log is read-only with a clear message and Confirm is disabled. (The CI-covered and not-covered storage cases are in the A3 section above.)

## Workout log manual checks (B2)

bUnit covers the log page over the real store: every menu item and what it stores, the Skip, Swap, Add, Pain check and Logged afterwards flows, the Partial prompt, the summary content, the rest countdown (on a fake clock), the Wake Lock calls (on a fake), Logged afterwards re-dating and undo, and the read-only and failed-save states of every action. It cannot check CSS, real focus movement, a phone's sleep behaviour or a killed tab, and no browser test was added for B2 (owner's decision). These are manual (tick off here when done). The log is at `/Fitness-App/log/<Workout id>`; start a Workout from Home (the real Home is B3, so the temporary Start button is still there). You need a Plan with a Session of several Exercises, some history for at least one of them, and a few other Exercises in the library (similar ones, and more than 20 in total for the search box). Visual targets are `docs/design/assets/mockups/S-02-*.png` (menus, skip, swap, rest, pain, summary, log-afterwards).

Rest timer and Wake Lock:

1. Rest timer Off (the default): confirm a Set and no "Rest" text appears.
2. Workout menu, choose 90 s. Confirm a Set and "Rest 1:30" shows at the right of the Up next strip in the ConfirmBar, counting down each second.
3. Confirm the next Set mid-countdown and it restarts at 1:30.
4. Let it reach 0:00: it disappears with no sound, toast or flash.
5. Lock the phone or switch apps for 30 s, then return: the number matches real elapsed time, not a frozen count.
6. Choose Off mid-countdown and the readout clears. Leave the log mid-countdown and nothing keeps running. The choice is still there after closing and reopening the app.
7. Wake Lock: open a Workout, leave the phone idle past its normal sleep time, and the screen stays on.
8. Switch to another tab or app and come back: the screen stays on again (reacquired). Leave the log (Back or Home) and the screen sleeps normally after the usual timeout. Finish a Workout and the screen is allowed to sleep on the summary.
9. Wake Lock on a browser without support (or with battery saver on): the log works normally with no error shown.

Sheets and menus (check each in Keep and Clean, Dark and Light, at 360 px, and with the text size increased):

10. Focus. Opening a menu or sheet moves focus inside it, Tab stays inside, Escape closes it and focus returns to the button that opened it (bUnit only checks that the component asks for this). A long list scrolls inside the sheet and the buttons at the bottom stay reachable, clear of the gesture bar.
11. Exercise menu (the button beside the Exercise name): Swap, Skip, Add an Exercise, Mark as benchmark and View Guide are listed with their one-line hints, each at least 44 px tall. Mark as benchmark saves at once and shows the benchmark mark in the header; the item then reads Unmark.
12. Workout menu (the button in the top bar): Rest timer (Off, 60, 90, 120 s), Theme, Mode (Match phone, Dark, Light; the mockup shows two), Logged afterwards and Finish Workout. Changing Theme and Mode applies at once with the sheet still open and every look stays readable.
13. View Guide. Open it on an Exercise with a Guide and on one without ("No Guide yet"). The video link opens in a new tab and the log is still there on return. Type a weight, open the Guide, tap Back to the Set, and the typed text, the open row and the pager position are unchanged. A long Guide scrolls.
14. Skip. Skip with no reason, and with each reason chip: the pager chip shows the skipped glyph (a dash) and the Exercise shows "Skipped" with the reason, in glyph, word and colour (not colour alone). Skipping an Exercise that has confirmed Sets asks first, then drops them. Un-skip brings it back as not yet done. After a Skip the log moves to the next Exercise not yet handled.
15. Swap. The picker lists similar Exercises first (shared muscle or equipment), then the rest; a search box appears over 20 Exercises and filters as you type. Swapping an Exercise that has Sets asks first. After a swap the header shows the new Exercise with "Swapped · was <original>", the planned Sets are kept as rows, the pager chip shows the swap glyph, and the new Exercise's own Last, Best and Suggested target show. Undo swap is offered until a Set is confirmed.
16. Add an Exercise. Picks from the same picker, and the new Exercise appears after the one you were on with a "+ Added" mark. Add Set and log it like any other. Remove Exercise is offered only while it has no Sets. Finishing as Partial drops an added Exercise that has no Sets. Known limit: an added Exercise is placed after the row that owns the nearest earlier entry (see open items in `build-plan.md`), so after a swap or skip the position may differ from where you added it.
17. "Add a new Exercise" at the bottom of the picker is shown disabled (it arrives with the Exercise library, B4).
18. Reload after each of the above (Skip, Swap, Add, rest choice): the log comes back as it was. Kill the tab after a Swap or Add and reopen: nothing is lost.

Finishing:

19. Finish a fully done Workout from the primary button: no Partial prompt, straight to the Pain check, then the summary. Finish Workout in the menu with Exercises left: the Partial prompt says how many are done; Keep going changes nothing; Finish records the rest as skipped (they show as skipped on the summary, and Home and the Week still count the Workout toward the Streak). With no confirmed Set at all, Finish says so in plain words and does nothing.
20. Pain check. "Any pain today?" opens with focus on No. No saves "none"; Skip saves nothing (the summary then says "Not answered"); Escape or a tap outside is Skip. Yes expands in place: body-area chips (all 11 areas show; the design text lists five), severity chips, side, "During which Exercise?" and a note. Save without an area and a severity says so and saves nothing. Focus is not lost when Yes expands. On a 360 px phone the three buttons are equal, the chips wrap, the textarea and select are comfortable with the keyboard open, and the bottom safe area is respected.
21. Kill the tab right after Finish, while the Pain check is showing, then reopen `/Fitness-App/log/<Workout id>`: the summary shows "Pain check: Not answered" and does not ask again.
22. Summary. In all four looks and at 360 px: Sets and Exercises done, PRs with the previous best, the Exercise rows (44 px tall) with RPE, skipped and swapped ones worded, the Pain check line, the Streak line, the duration, and the Done link, which goes Home with Back behaving sensibly. When Finish unlocks a Badge, the unlock well shows (it is a static well with no animation or auto-hide, so reopening the summary shows it again).
23. Reduced motion. With the phone's "remove animations" setting on, nothing in the sheets, the toast or the PR marker animates (the CSS has one global reduced-motion rule; bUnit cannot see it).
24. Finish in airplane mode: it works (it is on the device). Finish in a storage-full state (DevTools): the banner appears and the Workout stays open. Open the log in a second window: it is read-only with the plain message, and every menu action and Finish is disabled.

Logged afterwards:

25. Workout menu, Logged afterwards: a native date picker opens in a sheet (the phone's own picker), starting on yesterday, with days after today greyed out or refused. Save a past date, for example last Sunday: the banner reads "Logged afterwards · Sun 15 Nov. No timers." (your date, same format), with its glyph. A date weeks or years back is accepted.
26. On that Workout: confirming a Set shows no "Rest" even with a rest timer chosen, and the screen is not kept awake (leave the phone idle past its sleep time: it sleeps). Re-dating while a countdown is running clears it.
27. PR check as of the date. Pick a date before your newest result for an Exercise, then log a Set heavier than the best before that date but lighter than the current Best: it shows the PR marker. Last, Best and the Suggested target in the header also come from before that date.
28. Finish it: the summary shows "—" for the duration, and the PRs are judged against history up to that date.
29. Undo. With the Workout logged afterwards, the same menu item reads "Undo Logged afterwards": it takes the banner off, puts today's date back and brings the timers back. The start time is not restored (undoing cannot get it back), so the finished Workout has no duration either way. Reload after re-dating and after undoing: the state persists. History and the Week mark a finished one as logged afterwards. A past-dated Workout is in progress until you finish it, so Home still offers Continue.
30. 360 px and all four looks: the banner and the date sheet fit, nothing scrolls sideways, and the native date field is at least 44 px tall.

Offline:

31. Install the PWA, turn on airplane mode, open the log, then open the Exercise menu, the Workout menu, the Guide, the skip sheet, a picker and the date sheet: all open and close with focus working (this relies on `Sheet.razor.js`, which the publish manifest was confirmed to include). Do a Skip, a Swap and a Logged afterwards while offline and reload: all are kept.

## B3: Home

bUnit covers what each Home state shows, the visibility rules, Start, Continue, the sheets, Nudge actions and dismissals, and Log weight over the real store. It cannot check CSS, the phone keyboard, the native date picker, focus rings, safe areas or the desktop layout. Visual targets are `docs/design/assets/mockups/S-01-home-*.png` and `S-08-weight-log-dark.png`.

1. Phone layout, all four looks: Today card, alternatives, week strip, line, orbs, Nudges, Recent PRs and Weight stack in that order and fit at 360 px with nothing scrolling sideways. The Keep tapestry band shows on the Today card; Clean hides it.
2. Tap targets: Start, Continue, each strip tile, each orb, Dismiss and the Log weight button are at least 44 px and easy to hit one-handed.
3. Today card states on a real Plan: a Start day, a rest day (check "Next is X on Day" and the "next week" wording), done today (duration, Sets, PRs), a Break, and a finished Plan.
4. Do a different Session: opens as a sheet, rows are easy to tap, picking one starts it and opens the log; the Missed Session is still shown as Missed on Home afterwards.
5. Log a past Workout: the native date picker opens, starts on yesterday and will not go past today; picking a Session or Custom opens a Logged afterwards Workout for that day.
6. Make up: a Missed session's Nudge opens the draft pre-filled; remove one Exercise, add another, Start; the Workout opens and the Session stays Missed.
7. Log weight: the sheet opens with the last value in your display unit, the numeric keypad appears, saving shows the new value in the Weight panel. Try lb if you use it. Two Weigh-ins on one day are both kept.
8. Nudges: Dismiss hides one and the next fills in; reload the app and it stays hidden; next Monday it returns. With no Backup, the Back up action opens the Backup page.
9. Read-only: open the app in a second window; the second window shows Home read-only with Start and Log weight Save disabled and a plain reason.
10. Offline: with airplane mode on, Home opens and every action above except the browser's own pickers works.
11. Desktop (900 px and wider): the two-column layout is not expected to appear yet because the shell caps the column at 560 px; note what you see.

## Editors manual checks (B4 + B5)

bUnit covers what each Editors page shows and stores over the in-memory store. It cannot check CSS, the phone keyboard, native date inputs, focus rings, the offline browser state or the desktop layout. Visual targets are `docs/design/assets/mockups/S-03-*`, `S-04-*`, `S-05-*`, `S-06-*` and `S-12-offline-dark.png`.

- [ ] 1. Guide read view: sections in the design's order (What it works, Why it's in your plan, Setup, How to do it, Cues, Common mistakes, Easier / harder, Watch-outs), header shows the Session, sets by reps and Best, last three results with PRs marked. Check Keep and Clean, both Modes, 360 px.
- [ ] 2. Watch a demo online: the link opens in a new tab and you can come back to the app.
- [ ] 3. Watch a demo offline (airplane mode): the offline message appears and says the rest of the app still works; nothing else breaks. Also try with pop-ups blocked and note what you see (a blocked pop-up is not detected).
- [ ] 4. Guide edit on the phone: each section is a labelled text area that is easy to type in with the phone keyboard; Save keeps the text; an invalid video link is refused with a plain message and nothing is lost.
- [ ] 5. Discard changes: with unsaved edits, tap Cancel, the back arrow and the browser Back; each asks "Discard changes?" with Keep editing and Discard. Refresh and closing the tab are not guarded (known).
- [ ] 6. Personal notes: Edit notes saves on its own and does not touch the sections.
- [ ] 7. From the log, View Guide, Open full Guide, then Back: you land on the same Exercise and Set with any typed text still there.
- [ ] 8. Exercise library: groups by Session then "Not in a Session", search appears only above 20 Exercises, tag chips filter, rows show log kind, tags and Guide status. Fits at 360 px and looks right on desktop.
- [ ] 9. New Exercise: the form is usable with the phone keyboard; an empty or duplicate name is refused with a plain message; "Write a Guide now" opens the Guide editor after Add.
- [ ] 10. Picker: in the log's Swap and Add and in a Custom workout, "Add a new Exercise" opens the form and returns with the new Exercise chosen; a no-match search offers "Add '<text>' as a new Exercise".
- [ ] 11. Plans list: the active Plan is outlined "Active · week n of m"; Make active on another Plan leaves exactly one active Plan and Home follows; From scratch and Copy this Plan create inactive Plans.
- [ ] 12. Plan editor: Settings, Phases and Sessions tabs fit at 360 px and on desktop; an overlapping Phase names both Phases; invalid Sets or Reps disable Save with a message; Move up, Move down and Remove work with the keyboard and by tap; the banner says edits apply going forward.
- [ ] 13. Plan editor discard: leaving with unsaved changes asks "Discard changes?"; after Save, Home and the Week view show the new Plan (weekly target, Session days).
- [ ] 14. Week view: this week's Sessions on their days with Done, Missed and Today chips. Move "This week only" leaves next week alone; "Make this the default day" changes it for every week.
- [ ] 15. Add a Break: date inputs, an overlapping Break and an end before the start are refused; Home then shows the Break and the Streak is protected.
- [ ] 16. Suggested week: with a Break over a Session day, the draft shows changed rows highlighted and one summary line; Undo on a row, Cancel changes nothing, Apply stores it once. Nothing becomes Missed before its day has passed.
- [ ] 17. Read-only: open a second window; Guide, library, Plans, Plan editor and Week show the reason and every save control is disabled.
- [ ] 18. Offline: with airplane mode on, the library, Guide (apart from the video), Plan editor and Week view open and save.

## Insight manual checks (B6 + B7 + B8)

bUnit and Core tests cover what each Insight screen shows and stores. They cannot check CSS, the phone, native date inputs, the offline browser state or the desktop layout. `Web.BrowserTests` (Playwright) have not been run against Insight yet: run them once before merging. Visual targets are `docs/design/assets/mockups/S-07-*`, `S-08-*`, `S-09-*` and `S-13-*`.

- [ ] 1. Looks nobody has checked yet: every Insight screen (Progress with its Benchmarks and Calendar tabs, Weight, Trophies with Badges, Bests and Calendar, Badge detail and editor, Weekly review, the UnlockToast) in Keep and Clean, both Modes, at 360 px. Charts, heatmap, Badge art, orbs and tap targets stay readable and nothing scrolls sideways except the Calendar panel.
- [ ] 2. Home to Insight: the Streak orb opens Progress on its Calendar tab, the Phase and days-to-race orbs open Progress. On a Sunday, log your weight and the Today card shows "See your Weekly review"; before the weigh-in, or on any other day, it does not.
- [ ] 3. Progress charts: groups, the date range presets and Custom (a start after the end shows an inline message), tap a point for the tooltip, the Table toggle, and PR rings. An Exercise with one entry says "needs 2 for a chart".
- [ ] 4. Benchmarks: latest against previous, better or worse (lower is better for times), the target bar and the next test date.
- [ ] 5. Calendar: the whole Plan scrolls; every state (done, Partial, Missed, Break, future, today, PR) is distinguishable without colour; tapping a day opens that Workout.
- [ ] 6. Weight: three stats, the chart with the rolling line, goal line and lowest ring, the weekly averages table; Log weight adds a new Weigh-in and Home agrees.
- [ ] 7. Badges: unlocked first, then closest; the detail lists the contributing events; add, edit and delete a Badge, and the live "Now / Unlocks when" line; export a backup and see the Badges in it. A failed save keeps the draft.
- [ ] 8. UnlockToast: finish a Workout that unlocks a Badge; the toast shows for about 4 s, tapping opens the Badge; with reduced motion it appears and goes without animation.
- [ ] 9. Weekly review: the current week, then Previous week back through past weeks; a Break week says so; the export note appears instead of the button until Export.
- [ ] 10. Nudges (Home only, at most two): copy reads calmly: "Knee pain reported twice this week. Consider an easier variation." (no button, Dismiss only), "You missed Lower on Monday. Make it up this week?" (Make up), "One workout short of your target of 3 this week." (Pick a Session), "No Backup in 14 days. Make one so you don't lose your data." (Back up).
- [ ] 11. Desktop (900 px and wider): Progress and Benchmarks charts sit two across, Weight puts the chart beside the weekly table, the Weekly review lists its sections two across, the Badge grid widens to six columns. The shell column now widens to 960 px from 900 px on the Insight pages, as on Home: check Home looks unchanged, and that 360 px and 600 px stay one column capped at 560 px.
- [ ] 12. Read-only (second window open): charts and lists show; Badge saves and Log weight are disabled with the usual message.
- [ ] 13. Offline (airplane mode): every Insight screen opens and Badge and Weigh-in saves work.

### Decisions confirmed by the owner (23 Nov 2026)

- Chart gap rule: a gap of more than 14 days between two points breaks the line. Confirmed, keep 14.
- Best of the week is the biggest relative jump over the previous Best. Confirmed; a first-ever result is already a baseline and never Best of the week.
- Load Badge targets are stored as estimated 1RM (Epley), not as a weight and reps. Confirmed.
- A benchmark's target comes from the first Badge that names the Exercise. Confirmed.
- A past week's review says Behind, not On track, when the target was not reached. Confirmed.
- Badge art is derived from the Badge's metric and target for now. Choosing art is shelved until after the A4.5 merge, then add an optional art field to the format in one go (see `.scratch/insight/FOLLOWUPS.md`).
- Nudge copy review (C-08): the repeated-pain Nudge ("Consider an easier variation") still has Dismiss only. The owner wants it to get an action that shows easier variations; that is a follow-up ticket (`.scratch/insight/issues/11-pain-nudge-easier-variations-action.md`).
