# 13: Nudges

**What to build:** The dashboard gets a computed list of Nudges (in-app messages only): make up a Missed session, one Workout short of the weekly target, repeated pain in the same body area, and no Backup in 14 days. Spec: `docs/specs/a2-core-rules.md` (Nudges stories 84 to 90; "Nudges" decisions; assumption 7).

**Blocked by:** 05 (Weeks, counted Workouts, Streak and consistency messages), 09 (Week schedule computation and Missed sessions)

**Status:** ready-for-agent

- [ ] Nudge kinds: missed session (one per Missed session this week), one short of target (from Thursday onwards), same pain area reported twice in a week, and no Backup in 14 days
- [ ] The Backup Nudge takes the last Backup timestamp as an input value from the caller (its storage is A3 and A4) and fires at exactly 14 days
- [ ] All Nudges are quiet during a Break
- [ ] Each Nudge carries a stable kind, its message text and the data it refers to (Session, body area)
- [ ] Output ordering is deterministic with the most important first, with no duplicates
- [ ] Nothing is stored; the list is recomputed from data and an explicit "today"
- [ ] Tests cover each kind, the exact 14-day boundary, a Break silencing all Nudges, and ordering
