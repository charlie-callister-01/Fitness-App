# 01: Whole-app browser host and fixture

Spec: `docs/specs/hardening-b11.md` (Implementation Decisions: Whole-app browser host).

**What to build:** Give the browser tests a way to run the real app. A test run publishes the Web project once (Release) and serves the published output from the existing fake origin through Playwright routing (no real web server, nothing test-only ships; the service worker may be blocked). Add a page helper that opens the app at a chosen viewport, Theme, Mode, root text scale and reduced-motion preference and waits until Blazor has rendered. Commit a small hand-written fixture Backup (no private data, valid for the current exchange format version): one active Plan with two Tracks and Sessions across a week, a few logged Workouts with at least one PR, one unlocked Badge, Weigh-ins, and a Guide. Restore it through the app's real Import/Restore path. Add a publish step to the browser-tests CI job.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Browser tests can open any route of the published app from the fake origin, each test in a new context with a uniquely named database.
- [ ] The page helper sets viewport, Theme, Mode (through the app's real preference mechanism), root text scale and reduced motion.
- [ ] The fixture restores through the real Import/Restore UI path, and Home then shows the fixture's Plan and Today.
- [ ] A smoke test opens Home empty and with the fixture in each of the four Theme × Mode sets.
- [ ] The default `dotnet test` still skips browser tests; the browser-tests CI job publishes first and passes.
- [ ] The browser tests README explains the whole-app host and fixture; `dotnet build` stays at 0 warnings.
