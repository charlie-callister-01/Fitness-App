# F2: Writer takes EditResult; flows shrink onto a shared saving base; view builder uses the Core done rule

Source: thermo review item 3 and Spec finding 5, plus switching the Ui sites to F1's rules. Spec: `docs/specs/b2-workout-log-menus-and-finishing.md` (stories 81, 82).

**What to build:**
1. **`WorkoutWriter.ChangeAsync` accepts an edit that returns `EditResult`** (keep an overload for plain `Func<Workout, Workout>` so existing call sites still work, or convert them). A refusal is produced inside the single merge against the snapshot's latest Workout and commits nothing; the writer returns a result the caller can show (the refusal message). Delete the "pre-check then re-run with `.Workout ?? w`" pattern from `FinishFlow` and `LoggedAfterwardsFlow`.
2. **`FinishFlow` and `LoggedAfterwardsFlow` share one small base or helper** for `Saving`, the `Changed` event, the writer and the can-act guard; each keeps only its own phase state and rules. Remove the two `SaveAsync` overloads in `LoggedAfterwardsFlow` and the duplicated `Find`. The behaviour stays: end time committed before the Pain check opens, failed saves keep the flow where it was, a refused finish or future date shows its message.
3. **Ui sites use F1's rule:** `LogViewBuilder.IsHandled` and `ProgressOf` (and anything else in Ui) ask the Core "has Sets" rule; `LogViewBuilder` reads skip and swap info from `WorkoutOutcomes` (rows now carry their entry) instead of `SkipOf`'s own `FirstOrDefault` matching. An unlogged swap is no longer "handled" for the primary button.
4. **Read-only audit (Spec finding 5):** confirm `RunExclusive` guards `CanAct` for every B2 write; make the Pain check, Partial prompt and Logged afterwards sheet buttons disabled when the window cannot write (not only `Saving`), with a test per sheet.

**Blocked by:** F1.

**Status:** ready-for-agent

- [ ] Refusal comes back from the writer for finish (no confirmed Set), Pain check Yes without area or severity, and a future date; nothing is committed; the existing messages show unchanged
- [ ] No `?? w` fallbacks and no second evaluation of a Core rule remain in the flows
- [ ] The two flows share the saving plumbing; their files are visibly smaller
- [ ] An unlogged swap is not treated as done by the pager's primary button and progress count (test)
- [ ] Sheet buttons are disabled when read-only; tests show it
- [ ] Tests first (red run in the report); full suite green and the Ui suite run at least 3 times
- [ ] No data model, exchange format or storage change
