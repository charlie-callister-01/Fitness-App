# 04: Weigh-ins, Breaks, Badges and Guides

**What to build:** Weigh-ins (date, kg, independent of Workouts), Breaks (date range with reason), Badges (name, metric, target only; progress is never stored) and Guides (keyed by Exercise ID, structured fields plus video URL, personal notes held separately) can be written and read through the seam, with validation (Guide for unknown Exercise, Break end before start, non-positive weight, bad URL) giving exact paths. A Guides-only partial document is accepted. Spec gains the matching sections.

**Blocked by:** 01

**Status:** done

- [ ] Spec sections and example for the four entities added; conformance test updated
- [ ] Round trip of each entity is identical, personal notes kept separate from Guide content
- [ ] Invalid cases yield exact paths and messages
- [ ] Guides-only and Weigh-ins-only partial documents accepted
