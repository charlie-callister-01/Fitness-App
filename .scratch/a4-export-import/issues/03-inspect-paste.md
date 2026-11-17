# 03: Inspect a pasted file

**What to build:** Spec "Import: reading the paste". `ImportInput.Inspect(text)`: BOM/code-fence/surrounding-prose cleaning with a notice, versioned read, rejections as `path: message` capped at 10 plus "and N more", reader warnings as notes, and the suggested Import kind (Restore, Plan, Guides, merge).

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Every Inspect case in the spec's Testing Decisions passes
- [ ] `unsupported_version` says to update the app; older versions read normally
