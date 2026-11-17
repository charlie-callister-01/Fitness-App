# 09: Manual checks and docs

**What to build:** The things only a phone can prove are written down as a short checklist, and the docs describe the storage layout and the two version numbers so the next stage (A4) starts from the truth. Spec: `docs/specs/a3-storage.md` ("Testing Decisions", "Further Notes").

**Blocked by:** 08 (Playwright browser contract run and CI job)

**Status:** done

- [x] A storage section in the PWA manual checks covering: persistent-storage grant (installed and not installed), quota-full behaviour and message, browser-cleared data detection, two tabs (second is read-only), a blocked upgrade, and iOS Safari behaviour, each with how to trigger and what to expect
- [x] The technical brief's Storage section describes the final layout (a store per kind plus `meta`), the separate layout and record format versions, quarantine of unreadable records, and the single-writer rule
- [x] An ADR is added if the layout, single-writer or quarantine rules are hard to reverse and surprising; otherwise a one-line note in the technical brief says none was needed
- [x] The build plan's status for A3 and the docs index point at the delivered code and the next step (A4)
- [x] Any terms introduced are checked against `GLOSSARY.md` and added only if they are domain terms
