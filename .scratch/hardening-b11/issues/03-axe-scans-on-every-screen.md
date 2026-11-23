# 03: axe scans on every screen

Spec: `docs/specs/hardening-b11.md` (Implementation Decisions: Accessibility scans).

**What to build:** Load axe-core (pinned, bundled with the test project, never fetched from a CDN at run time) into the whole-app browser tests and scan every screen S-01 to S-13 at its route, in all four Theme × Mode sets, both empty and with the fixture, plus the open state of each sheet that needs data to appear. Run against WCAG 2.0/2.1 A and AA tags. Fix every serious and critical violation in the app. Suppressions live in one place in the test project, each with rule, selector and a one-line reason, and only for third-party or proven false positives. Moderate and minor findings go to `docs/REFACTOR.md`.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] One parameterised test per screen × Theme × Mode × (empty, fixture); sheets opened where they need data.
- [ ] No serious or critical violations remain; each fix is in the app, not the test.
- [ ] Any suppression has rule, selector and reason in the single suppression list.
- [ ] Moderate and minor findings are listed in `docs/REFACTOR.md`.
- [ ] bUnit tests updated where markup changed; `dotnet build` 0 warnings; all tests green.
