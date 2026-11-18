# 03: Theme switching end to end

**What to build:** The owner can pick a Theme and a Mode and the whole app restyles instantly, is remembered, travels in a Backup, opens with no flash of the wrong look, follows the phone's dark and light changes on System, and sets the browser chrome colour. A Ui service owns the current Theme and Mode: it reads them from `AppData`, commits a change as a Settings change, raises a change event, and calls a narrow interop interface (real implementation in Web, fake in tests). One shared JavaScript module resolves and applies the choice for both the Blazor side and a tiny inline pre-paint script in the host page, which reads a localStorage mirror of the choice; Settings stay the source of truth and the app reconciles the attributes and the mirror after startup. A failed commit still applies the choice for the session and reports through storage health; the service never throws into the UI. A temporary Appearance control (Theme and Mode pickers) sits on a stub page so the owner can try the four looks on a phone. Spec: `docs/specs/b0-design-system-and-shell.md`. Design: `docs/design/design-document.md` (§4, §11).

**Blocked by:** 01 (Theme and Mode as stored Settings), 02 (Design assets, offline cache and PWA identity)

**Status:** ready-for-agent

- [ ] Choosing a Theme or Mode sets `data-theme` and `data-mode` on `<html>` (System resolves to the actual dark or light) and commits the Settings change; the change event fires
- [ ] bUnit tests with a fake interop and a real `AppData` over the in-memory store pin: default is Keep and System; a choice reaches interop and Settings; a failing store (existing fault-injecting store) leaves the choice applied and reports the failure; a restored Backup with a different value re-applies
- [ ] The inline pre-paint script applies the mirrored choice before first paint; a Playwright check in `Web.BrowserTests` shows the attributes set before the app starts, System following a changed colour-scheme preference, and a blocked or cleared mirror falling back to the default without an error
- [ ] `<meta name="theme-color">` follows Theme and Mode using the design's values
- [ ] The Appearance control works on a phone in all four combinations with no flash on reload, and the choice survives a reload and an airplane-mode launch
- [ ] An unknown stored value behaves as the default; reduced motion respected; no raw hex colours in new component CSS
- [ ] Manual-check notes added for the real WebAssembly interop on a phone (the browser tests do not run WASM); all existing tests pass
