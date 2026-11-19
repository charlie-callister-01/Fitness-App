# F3: Page and menu restructure: LogActions, one ExerciseAction, LogSession, thin page

Source: thermo review items 4 and 5 (and the `LogViewBuilder` divergent-change note). Rule: `.scratch/b2-workout-log-menus-and-finishing/HANDOFF.md` ("Keep `WorkoutLog.razor.cs` thin: only wiring").

**What to build** (behaviour unchanged; this is a restructure and the existing page tests are the safety net):
1. **`LogActions`** (new Ui class) owns the busy guard (`RunExclusive`/`CanAct`) and the writes for: Set confirm, Max, RPE, benchmark, Skip, Un-skip, Swap, Undo swap, Add, Remove added. The page forwards to it instead of seven near-identical handlers, and stops creating a `WorkoutWriter` or `LogViewBuilder` on every access.
2. **One `ExerciseAction`** (a small closed set of records: Swap(newId), Skip(reason), Unskip, UndoSwap, Add(newId), Remove, ToggleBenchmark) raised by `ExerciseMenu` through a single `OnAction` callback, and one `ApplyAsync(action)` in `LogActions`. `ExerciseMenu` takes one state record instead of 16 parameters (is skipped, swapped, added, has Sets, is benchmark, can write, picker choices, guide).
3. **Menu availability rule in Core** (pure, unit tested): which items are enabled or hidden for a given Exercise state ("no Undo swap or Remove after Sets", Skip hidden for an added Exercise, Un-skip for a skipped one), so the Ui no longer repeats `!CanWrite ||` per item and cannot drift from the Core refusals.
4. **`LogSession`** (new Ui class) holds the rest countdown, Wake Lock acquire/release rules and the event subscriptions the page now wires by hand (8 subscribe and unsubscribe lines), constructed once and disposed once. The page keeps rendering and parameter handling only.
5. **Split `LogViewBuilder`:** move the skip, swap and add views and slot logic into their own focused class(es) (it already started with `LogSlots`), so no file grows into the next 400-line page.
6. `PainExercises` (a view computation in the page) moves to the summary or view builder.

**Blocked by:** F2.

**Status:** ready-for-agent

- [ ] `WorkoutLog.razor.cs` is back near 150 lines and contains no write logic, no per-item enablement, and no event subscription bookkeeping beyond creating `LogSession` and `LogActions`
- [ ] `ExerciseMenu` has one state parameter and one action callback; the Razor file is clearly smaller
- [ ] Core availability rule has unit tests; the menu tests still pass unchanged in behaviour (adapt only parameter names)
- [ ] Every existing page test (Skip, Swap/Add, Menus, Guide/Rest, Finish, Afterwards, Undo actions) passes without weakened assertions
- [ ] Tests first for the new Core rule and any new class (red run in the report); full suite green and the Ui suite run at least 3 times
- [ ] No behaviour, data model, exchange format or storage change
