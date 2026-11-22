# 06: Swap and Add end to end (ExercisePicker C-14)

Parent spec: `docs/specs/b2-workout-log-menus-and-finishing.md` (Swap stories 13 to 23, Add 24 to 28). Design: C-14, §8.4, mockups `S-02-log-swap-picker-dark.png`, `S-02-log-swapped-dark.png`.

**What to build:** the shared ExercisePicker sheet (reused later by B4 and B5) titled "Swap <name>" or "Add an Exercise": a Similar group first (from the Core ranking), then All Exercises; rows with an icon by log kind, name and tags; Exercises already in the Workout hidden; a search box when the library has more than 20 Exercises and an empty-search message ("No match. Add a new Exercise", linking to the library only if it already exists, otherwise disabled with a note for B4). Swap replaces the Exercise in place keeping the planned sets, shows "Swapped · was X" and ⇄ in the pager, and the new Exercise's header, Last, Best and Suggested target come from its own history; a swap can be undone before any Set is logged, and swapping an Exercise with confirmed Sets asks first. Add inserts after the current Exercise marked "+ Added", with one Set row of the Exercise's own kind; an added Exercise with no Sets can be removed.

**Blocked by:** 01, 04, 05 (page conflicts; do after Skip).

**Status:** ready-for-agent

- [ ] Picker lists Similar first, then All, hides Exercises already in the Workout, searches only past 20 Exercises, and Cancel changes nothing
- [ ] Swap saves a swapped entry (original recorded as swapped, not skipped), keeps planned sets, shows "Swapped · was X", and the header uses the new Exercise's own history
- [ ] Undo swap and swap-with-Sets confirmation work
- [ ] Add inserts after the current Exercise with "+ Added" and an empty Set row; it can be logged like any Exercise; removal works only with no Sets
- [ ] Failed save shows the banner and leaves the screen unchanged; read-only disables both; survives reload
- [ ] Picker is a labelled dialog using the shared Sheet shell, with 44 px targets
- [ ] bUnit page tests written first (red run in the report); all existing tests stay green
