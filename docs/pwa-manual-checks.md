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
6. All four looks. For Keep and Clean, each in Dark and Light, compare the log with the mockups: `S-02-log-dark.png`, `S-02-log-light.png`, `S-02-log-clean-dark.png`, `S-02-log-clean-light.png` (and `S-02-log-360-dark.png`, `S-02-log-desktop.png`, `S-02-log-exercise-done-dark.png`, `S-02-log-error-dark.png`, `S-02-log-pr-dark.png`). Keep should be nearly plain on this screen. The pending (muted) rows, the outlined current row, the green tick, the sunken Suggested target well and the pager chips all read clearly. Things that are not built in B1 and will be missing against the mockups: the Exercise and Workout menus, rest timer, skip and swap marks, pain check, summary, and a date or timer line.

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
16. Deep link and refresh. Refresh the log in the browser: it restores with the Resumed note. A log URL for a Workout that is finished or does not exist shows "Workout not found" and a Back to Home button.
17. Airplane mode. Open the installed app online once, switch on airplane mode, Start a Workout (or Continue) and log a few Sets, including the PR check below. Everything saves (it is on the device), the pager, Next and Finish work, and after closing and reopening still offline the Workout is intact.
18. Next and Finish. The primary button reads Confirm Set while Sets remain, then "Next: <Exercise>" when this Exercise is complete, then Finish after the last. The Up next strip names the following Exercise ("Last Exercise done" at the end). Finish returns to Home and Home shows Start again; tapping Finish twice quickly must not stamp twice. (Finish is temporary until B2.)

PRs and the header:

19. The PR marker and toast. Log a Set that strictly beats the Exercise's Best (more estimated 1RM for a lift). Expect the PR marker in the state cell (the shield in Keep, a "PR!" block in Clean), a short toast under the rows ("PR! 22 kg x 9 beats 22 x 8"), and the top bar count ("1 PR"). Screen readers read ", PR" on the reps label. The first ever result for an Exercise is not a PR. A Set equal to the Best is not a PR. Tap the green tick to reopen and edit the PR Set down: the marker, toast and count go away at once; a later, better Set moves the marker to itself. A Max set counts as the Best even when a heavier volume Set exists. Also check a time Exercise (lower is better) that has a previous time: a faster time (`1:40` after `1:48`) shows the PR; a slower one does not. This path has Core tests but no page-level test.
20. A PR in each Theme. Repeat check 19 in Keep Dark, Keep Light, Clean Dark and Clean Light (change them in More, Appearance, then come back with Continue). The marker, toast and count are readable and have enough contrast in all four, and the marker is stationary (no animation). With the phone's "remove animations" setting on, nothing moves.
21. Header. For an Exercise with history, Last and Best show on one line and the Suggested target sits in a sunken well with its reason. An Exercise done for the first time shows none of these and no empty labels. The log uses the owner's units everywhere (see the lb check below).
22. RPE. Tap 8: it is marked pressed. Tap 8 again: cleared. RPE never blocks Confirm or Next. Tapping RPE before any Set is confirmed is allowed (see open items).
23. Add Set, Remove Set and Max set. Add Set appends a row; an unconfirmed extra row can be removed; "Mark Set n as Max set" labels the row "Max set" and the same link now reads "Unmark" and removes the label. All of it survives a reload (the Max set and every confirmed Set; not an unconfirmed extra row, see check 15).

lb check:

24. There is no screen to change units yet (Settings is B10), so this check needs a Backup whose Settings say lb, restored through Import (or the units set in DevTools on a throwaway copy of the data); skip it until that exists and note it as pending. With lb on, open a Workout with history. Last, Best and the Suggested target show in lb; the pre-filled weights show in lb; the steppers show the lb step; typing `50` stores 50 lb converted to kg (check by switching back to kg: it reads 22.7). The PR toast shows lb. Known: the Suggested target's reason text can still say "kg" for lb users (see open items).

Storage trouble:

25. Failed save and read-only. With DevTools (desktop) simulate a quota-full or offline storage failure while confirming a Set: the plain banner appears, the typed value stays, and a retry works after the cause is cleared. With the app open in a second window, the second window's log is read-only with a clear message and Confirm is disabled. (The CI-covered and not-covered storage cases are in the A3 section above.)
