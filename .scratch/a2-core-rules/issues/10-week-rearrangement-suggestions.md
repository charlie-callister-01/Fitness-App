# 10: Week-rearrangement suggestions

**What to build:** After a Break or a pile-up the app can propose a re-arranged week as a draft: outstanding Sessions placed on the coming days in Plan order, avoiding back-to-back days that load the same muscle group. It never applies anything itself, and returns "nothing to rearrange" when the week is fine. Spec: `docs/specs/a2-core-rules.md` (Week schedule and rearrangement stories 72 to 75; assumption 9).

**Blocked by:** 09 (Week schedule computation and Missed sessions)

**Status:** done

- [x] Given the outstanding Sessions (missed this week plus not yet reached), the coming non-Break days and the Plan order, return a proposed placement
- [x] Avoid placing Sessions on consecutive days that share a `muscleGroups` tag, and avoid doubling up a day unless unavoidable
- [x] The result is always a proposal (a draft the caller can accept or edit); nothing in the data changes
- [x] "Nothing to rearrange" is a distinct result when the week is already workable
- [x] Tests cover a Break pile-up, a tag clash being avoided, and the nothing-to-do case
