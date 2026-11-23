# 07: Shared live region and announcer

Spec: `docs/specs/hardening-b11.md` (Implementation Decisions: Announcements).

**What to build:** Add one always-present, visually hidden polite live region in the shell and a small injectable announcer service in the Ui project (beside the save-failure notice) that components call with a message; repeating the same message announces it again. Route through it: Badge unlocked (C-23), a new PR in the Workout log, the rest timer reaching zero (once; the timer stays `aria-live="off"`), Export, Backup and Import results, and existing per-page polite lines that announce events rather than show state. Visible status text stays on screen as today. The save-failure banner stays `role="alert"`; nothing else is assertive.

**Blocked by:** 01

**Status:** resolved

- [x] The live region is present on every page from first render and is polite.
- [x] bUnit tests cover the announcer (message set, repeat message re-announced) and each component's call.
- [x] Browser tests see the region's text for Badge unlocked, rest timer end, a new PR and an Import result.
- [x] The rest timer announces only at zero, not each second.
- [x] Save failures remain `role="alert"`; no new assertive regions.
- [x] `dotnet build` 0 warnings; all tests green.
