# 04: Save-failure notice service

**What to build:** A small Ui service any screen reports a failed `CommitResult` to, rendering one shared plain-language banner for: store failed, blocked (startup `CannotRead` / `StorageFailed`), writer lock held, and rejected by validation (first issue's message). `ThemeService` adopts it in place of its local `LastSaveFailure` note; its existing failure behaviour (choice stays applied for the session, never throws) is kept. Core and storage are unchanged. Spec: `docs/specs/b1-workout-log-core-input.md` ("Saving and failure reporting"). B0 handoff: failed saves are not reported through `AppData.Health`.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Each failure kind produces a distinct, plain message; success clears the banner
- [ ] The banner is rendered once in the shell and full-screen layouts so any screen can use it
- [ ] `ThemeService` reports through it; its failure test is adapted, not dropped
- [ ] bUnit tests using the existing fault-injecting store and a held writer lock
