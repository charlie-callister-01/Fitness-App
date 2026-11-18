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

CI covers `wwwroot/js/theme.js` in headless Chromium (`ThemeScriptBrowserTests`: pre-paint attributes, System following the colour scheme, chrome colour, blocked mirror). It does **not** run WebAssembly, so the real Blazor interop (`Web/Theming/ThemeInterop.cs` calling `FitnessTheme.apply`) and a real phone need these checks. Use the temporary page `/appearance-stub` (link on Home).

1. All four looks. Pick each of Keep and Clean with each of Dark and Light. Expect the whole app to restyle instantly, and the browser bar (Android Chrome) to change colour: Keep dark `#4A4237`, Keep light `#E8DFC8`, Clean dark `#111315`, Clean light `#F4F5F7`.
2. No flash. With a non-default choice (for example Clean, Light), reload and cold-start the installed app several times. Expect the first painted frame to already be the chosen look, never Keep dark first.
3. Survives restart and airplane mode. Choose Clean, Light, close the app, switch on airplane mode, open it. Expect the same look (the script is in the offline cache).
4. System follows the phone. Choose Mode System, then switch the phone between dark and light (Quick Settings) with the app open. Expect the app to follow without a reload.
5. Source of truth. In DevTools (remote debugging) change `data-theme` on `<html>` by hand, then reload: the stored choice comes back. After restoring a Backup that has a different Theme or Mode, expect the app to show the restored look.
6. Blocked storage. In a private tab, the look still switches for the session and may reset on reload; nothing errors.
