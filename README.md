# Fitness-App
Customisable fitness application for me 

## Build, test, run

```
dotnet build
dotnet test
dotnet run --project src/Web --launch-profile lan
```

The `lan` profile listens on all interfaces at port 5276. Open `http://<your-PC-LAN-IP>:5276` on a phone on the same network (allow port 5276 through the Windows firewall if the phone can't connect). Use `--launch-profile http` to open a browser on localhost instead.

## CI

`.github/workflows/ci.yml` runs on every pull request: it installs the SDK pinned in `global.json`, then runs `dotnet build -c Release` and `dotnet test -c Release --no-build` from the repo root. Warnings are errors, so any warning or failing test fails the check. Deployment is a separate workflow.

## Deploy (GitHub Pages)

`.github/workflows/deploy.yml` runs on every push to `main`: a `build-and-test` job (Release build and tests), then a `deploy` job that runs `scripts/publish-pages.sh`, uploads `out/wwwroot` with `upload-pages-artifact` and publishes it with `deploy-pages`. The deploy job only runs if build and tests pass. The app is served at `https://<owner>.github.io/Fitness-App/`.

**One-time owner step (done; the deploy job fails without it):** in the repo, Settings > Pages > Build and deployment > Source, choose **GitHub Actions**. Re-check this if deploys ever start failing.

**Base path:** one value, `BASE_PATH` at the top of `.github/workflows/deploy.yml` (`/Fitness-App/`, the repo name). For a root host such as Cloudflare Pages, set it to `/`. Nothing else needs to change; the service worker works out its own base from its URL.

What the publish script (`scripts/publish-pages.sh <base-path> <out-dir> [version]`, needs bash and openssl) does on top of `dotnet publish`:
- passes the version as `-p:InformationalVersion`; the workflow passes the commit SHA, which is the build identifier the app shows;
- rewrites `<base href="/">` in `index.html` to the base path, and updates that file's hash in `service-worker-assets.js` (the service worker verifies it);
- writes `404.html` from `deploy/404.template.html`, which redirects any unknown path to `<base>/?/path`; the small script in `src/Web/wwwroot/index.html` restores the path, so refresh and deep links load the app;
- adds `.nojekyll` so Pages serves `_framework` and `_content` untouched.

To try it locally: `bash scripts/publish-pages.sh /Fitness-App/ out abc1234` then inspect `out/wwwroot` (`out*/` is git-ignored). Phone checks (install, offline, update) are in `docs/pwa-manual-checks.md`.
