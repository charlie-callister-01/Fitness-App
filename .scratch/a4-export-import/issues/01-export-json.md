# 01: Export a slice as JSON

**What to build:** Spec `docs/specs/a4-export-import.md` section "Export" (JSON half). `Core.Exchange`: the Export request (default last 12 weeks, six ticks, `Backup` and `PastDays` factories), slicing by tick and date range, Pain-check stripping, referenced Exercises always included, personal notes only in a Backup, JSON via `ExchangeFormat.Write` with `exportedAt`/`exportedFrom`, suggested file name, summary lines, `CountsAsBackup`, and the Backup-date recorder (stamps only for a Backup).

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Every Export case in the spec's Testing Decisions (JSON side) passes, through the public surface only
- [ ] Output reads back through `ExchangeFormat.Read` with no errors
- [ ] A Backup holds every collection, empty ones as `[]`
