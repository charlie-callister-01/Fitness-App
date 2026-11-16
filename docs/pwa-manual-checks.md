# PWA: how it works and the manual phone checks

Files (all in `src/Web/wwwroot`): `manifest.webmanifest`, `service-worker.js` (development: does nothing, no caching), `service-worker.published.js` (becomes `service-worker.js` on publish), icons (`icon-192.png`, `icon-512.png`, `icon-maskable-512.png`, `apple-touch-icon.png`, `favicon.png`).

## How offline and updates work

- On publish, .NET generates `service-worker-assets.js`: every published asset (runtime, assemblies, `_content/Ui/ui.css`, icons, `index.html`, manifest) with a hash, plus a `version` that is a hash of the whole set. It changes whenever any asset changes.
- The cache is named `offline-cache-<version>`. Install downloads the full list into the new cache. Activate deletes every other `offline-cache-*` cache.
- A new build installs in the background but **waits** (no `skipWaiting`) until the old version's tabs/windows are closed, then activates. So a page never mixes old shell with new assemblies. Practically: after deploying, open the app once online (it fetches the update), close it fully, open it again: you now have the new build. The placeholder shows the build identifier so you can tell.
- The worker derives its base path from its own URL, so hosting under a sub-path needs no edit to it. `index.html`'s `<base href>` is rewritten at publish time by `scripts/publish-pages.sh`, which also updates `index.html`'s hash in `service-worker-assets.js` (the worker checks it, so skipping that would break the offline install). See the README Deploy section.
- No permissions are requested and no data is collected.

## Automated or inspectable checks (no phone)

1. `dotnet publish src/Web -c Release -o <dir>`.
2. `<dir>/wwwroot/service-worker-assets.js` lists `_content/Ui/ui.css`, all `_framework/*.wasm`, icons, `index.html` and `manifest.webmanifest`.
3. Changing any asset (for example `css/app.css`) and republishing changes `version`.
4. Chrome DevTools, Application tab: Manifest shows no errors and "Installability" passes; Service workers shows activated; Cache storage shows one `offline-cache-*`.

## Manual phone checks (needs the deployed HTTPS URL or localhost)

Install:
- Android Chrome: open the URL, use the install prompt or menu "Install app". It opens standalone with the dark theme colour and a maskable icon.
- iOS Safari: Share, "Add to Home Screen". Check the 180px icon and that it opens without browser chrome.

Offline:
1. Open the installed app once while online and wait for it to load fully.
2. Close it, switch on airplane mode, open it again. The placeholder (app name and build id) must appear.

Update pickup:
1. Note the build id on the phone. Deploy a new build.
2. Open the app online, close it completely (swipe away), open it again. The build id is new.
3. Go offline and reopen: still the new build, working. In DevTools (desktop) only one `offline-cache-*` should remain.
