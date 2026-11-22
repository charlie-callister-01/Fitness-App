# Insight follow-ups (owner decisions, 23 Nov 2026)

- **Badge art choice (shelved until after the A4.5 merge).** Art is derived from metric and target today. After A4.5 lands, add an optional `art` field to the Badge in the exchange format and the model in one change, let the Badge editor pick from the starter art family, keep derived art as the default when the field is absent, and add a locked variant for custom frames. Do not add the field before A4.5 merges (it changes the format at the same time).
- **Pain Nudge action:** ticket 11 in `issues/`.
- **Decisions confirmed, no change:** 14-day chart gap, Best of the week rule (first-ever results are baselines), estimated 1RM Badge targets, benchmark target from the first matching Badge, past-week review wording.
- **Optional:** `WeighInRules` now accepts "1e2" and surrounding spaces because it shares the decimal parser; the owner did not object.
