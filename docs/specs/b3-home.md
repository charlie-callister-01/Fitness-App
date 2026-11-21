---
labels: ready-for-agent
stage: B3
---

# B3: Home

Parent docs: [`../build-plan.md`](../build-plan.md) (Track B), [`../design/design-document.md`](../design/design-document.md) (C-08 Nudge, C-09 TodayCard, C-11 StatusOrb, C-24 WeekStrip, S-01, F-02, F-03; S-08 for the Log weight sheet), the `S-01-*` mockups in `../design/assets/mockups/`, [B2 spec](b2-workout-log-menus-and-finishing.md) (what B3 wires up), [A2 spec](a2-core-rules.md) (Nudge, Streak, Weeks, Missed, Rearrangement rules), [`../product-brief.md`](../product-brief.md) (Home dashboard, Nudges), `../../GLOSSARY.md` (Plan, Phase, Session, Workout, Custom workout, Missed session, Break, Streak, Nudge, PR, Weigh-in, Partial workout). Handoff: `.scratch/b3-home/HANDOFF.md`.

## Problem Statement

Home is still the B1 placeholder: a title and one Start or Continue button. The owner cannot see the week, the Streak, how far into the Phase they are, how many days to the race, or any Nudge. They cannot do a different Session than the one offered, cannot build a Custom workout, cannot make up a Missed session, cannot log a Workout for a day they forgot to log, and cannot log their weight without leaving for a screen that does not exist yet. Rest days, a finished Workout, a Break and a finished Plan all look like the same blank page. Until Home is real the app is a log with no dashboard, and the owner still opens Obsidian to see how the week is going.

## Solution

B3 replaces the placeholder with the real Home (S-01), complete in every state, wired to the Core rules that already exist:

1. **TodayCard (C-09)** in six states: today's Session (Start), in progress (Continue, "3 of 6 done · 24 min"), rest day ("Next is Session E on Saturday", no button), done today (duration, Sets, PRs), Break active (quiet, no button), Plan finished. No active Plan keeps the existing empty state.
2. **Alternatives under the card:** **Do a different Session** (a sheet of this week's undone Sessions, F-03), **Build a Custom workout** (a simple draft page), and a small **Log a past Workout** link (a sheet: date, then Session or Custom, starting a Logged afterwards Workout).
3. **WeekStrip (C-24)**, the "This week 1 of 5 · target 5 · Missed: A" line, and three **StatusOrbs** (Streak, Phase, days to race), each tapping through to Progress.
4. **Nudges (C-08)**: at most two, Home only, from the A2 Nudge rules, each with one action and a Dismiss. A dismissed Nudge stays hidden for the rest of the week, remembered on this device.
5. **Make up a Missed session (F-02):** the Missed-session Nudge opens the same draft page, pre-filled with the Missed Session's Exercises.
6. **Recent PRs and Weight panels**, and the **Log weight sheet** (always adds a new Weigh-in), reused later by the Weight screen.
7. A Home layout that is one column on a phone and two columns from 900 px, in both Themes and both Modes, with the read-only and failed-save behaviour used by the log.

The owner sees this as: open the app, see today's Session and one tap to Start it; rest day, see what is next; forgot to log yesterday, tap Log a past Workout; a Session was missed, tap Make up in the Nudge; step on the scales, tap Log weight.

## User Stories

### Today's Workout
1. As the owner, I want Home to show today's Session with a Start button, so that starting a Workout is one tap.
2. As the owner, I want the card to show the Session name and what it is (type, number of Exercises), so that I know what I am about to do.
3. As the owner, I want Start to create the Workout and open the log, so that I can begin straight away.
4. As the owner, I want a second tap on Start while it is saving to do nothing, so that I never create two Workouts.
5. As the owner, I want the card to show Continue and progress ("3 of 6 done · 24 min") when a Workout is in progress, so that I can pick up where I left off.
6. As the owner, I want a past-dated Workout I have not finished to also show Continue, so that I can finish it.
7. As the owner, I want the card to say it is a rest day and name the next Session and its weekday ("Next is Session E on Saturday") with no button, so that I know what is coming and am not pushed to train.
8. As the owner, I want a rest day with nothing left this week to name the first Session of next week, so that the card is never blank.
9. As the owner, I want the card to show what I did today (duration, Sets, PRs) once I have finished a Workout, so that I get closure and see the result.
10. As the owner, I want tapping a finished Workout on Home to open its summary, so that I can review it.
11. As the owner, I want a Partial Workout to show as done, with Partial noted, so that it is not mistaken for unfinished.
12. As the owner, I want a quiet card with no button and no Nudges during a Break, so that Home does not nag me while I am injured or away.
13. As the owner, I want the card to say when the Plan has finished, so that I know why there is no Session to start.
14. As the owner, I want the card to say when the Plan has not started yet and give its start date, so that an early Home is not confusing.
15. As the owner with no active Plan, I want the existing "No active Plan" state kept, so that I can see what to do next.

### Do a different Session
16. As the owner, I want a Do a different Session action directly under the card, so that I can swap today's plan without any detour.
17. As the owner, I want it to open a sheet listing this week's undone Sessions (code, name, type, default day), so that I can pick one.
18. As the owner, I want picking a Session to start its Workout for today and open the log, so that the choice leads straight to logging.
19. As the owner, I want the original Session to stay outstanding after I pick another, so that it can still be made up.
20. As the owner, I want the sheet to say so when no undone Sessions are left and to offer Build a Custom workout, so that the sheet is never a dead end.
21. As the owner, I want the action hidden while a Workout is in progress or during a Break, so that I cannot start a second Workout by accident.

### Build a Custom workout
22. As the owner, I want a Build a Custom workout action under the card, so that I can train something that is not in the Plan.
23. As the owner, I want it to open a draft page, so that I can choose Exercises before I start.
24. As the owner, I want to add Exercises with the shared picker (similar first, then all, this week's Missed Exercises offered first), so that I find them quickly.
25. As the owner, I want to remove an Exercise from the draft, so that I can change my mind before starting.
26. As the owner, I want Start to stay disabled until the draft has at least one Exercise, so that I never start an empty Workout.
27. As the owner, I want Start to create a Custom workout and open the log, so that logging works the same as for a Session.
28. As the owner, I want a Custom workout to count toward the Streak only when my Settings say Custom workouts count, so that the rule stays under my control.

### Make up a Missed session
29. As the owner, I want a Nudge "You missed Session A on Monday. Make it up this week?" with a Make up action, so that I am reminded without being nagged.
30. As the owner, I want Make up to open the draft pre-filled with that Session's Exercises, so that I do not rebuild it.
31. As the owner, I want to remove or add Exercises on that draft, so that I can fit the time I have.
32. As the owner, I want the Missed session to stay recorded as Missed after I make it up, so that history is honest.
33. As the owner, I want the Nudge to appear only after the Session's day has passed undone, so that it is never premature.

### Log a past Workout
34. As the owner, I want a small Log a past Workout link under the alternatives, so that a gym visit I did not log has a way in.
35. As the owner, I want it to open a sheet with a date field (no future dates) and a choice of any Session or a Custom workout, so that I can describe what I did.
36. As the owner, I want confirming to start a Logged afterwards Workout for that date and open the log, so that I can enter what I did.
37. As the owner, I want a clear message if the date is not allowed, so that I understand why nothing happened.
38. As the owner, I want the link hidden during a Break and while a Workout is in progress, so that the same rules apply as for the other starts.

### Week at a glance
39. As the owner, I want a seven-tile WeekStrip (Monday to Sunday) with each day's Session code, type colour and a done, missed or partial mark, so that I see the week in one look.
40. As the owner, I want today outlined in the strip, so that I can find it.
41. As the owner, I want tapping a tile to open the Week view, so that I can go deeper.
42. As the owner, I want a line "This week 1 of 5 · target 5 · Missed: A", so that I know where I stand against the target.
43. As the owner, I want a day with a Break marked quietly in the strip, so that it is not shown as missed.

### Status orbs
44. As the owner, I want a Streak orb showing the number of weeks, with its ring showing this week's progress toward the minimum, so that I see progress and risk at once.
45. As the owner, I want the Streak orb to keep its number and grey its ring with the caption "On a Break" during a Break, so that it never looks broken.
46. As the owner, I want a Phase orb ("week 5 of 8"), so that I know where I am in the Plan.
47. As the owner, I want a days-to-race orb whose ring shows how much of the Plan has gone, so that the goal date stays in view.
48. As the owner, I want the race orb left out when the Plan has no goal date, so that Home shows nothing meaningless.
49. As the owner, I want tapping an orb to open Progress, so that the detail is one tap away.

### Nudges
50. As the owner, I want at most two Nudges, most important first, so that Home stays calm.
51. As the owner, I want each Nudge to have one action and a Dismiss, so that I can act or clear it in one tap.
52. As the owner, I want a dismissed Nudge to stay hidden for the rest of that week and come back the next week if it still applies, so that Dismiss means "not now".
53. As the owner, I want the "one short of target" Nudge to offer the different-Session sheet, so that I can act on it.
54. As the owner, I want the "No Backup in 14 days" Nudge to take me to Export and Import, so that I can back up.
55. As the owner, I want the repeated-pain Nudge to be dismissible without a forced action, so that it is a gentle prompt, not a chore.
56. As the owner, I want Nudges never shown during a Workout, only on Home, so that the log stays quiet.

### Recent PRs and Weight
57. As the owner, I want a Recent PRs panel listing my latest PRs, newest first, each with the previous best, so that I see what is going up.
58. As the owner, I want an empty state for the panel when there are no PRs yet, so that it never looks broken.
59. As the owner, I want a Weight panel with my latest Weigh-in, this week's average and the change from last week's average, in my display unit, so that I see the trend without opening another screen.
60. As the owner, I want an empty Weight panel to say there are no Weigh-ins and offer Log weight, so that the first one is easy.
61. As the owner, I want a Log weight button at the top of Home, so that logging weight is always available.
62. As the owner, I want the Log weight sheet to show one large decimal field prefilled with my last value and "Last: 86.3 on Sun 15 Nov", so that I only adjust the number.
63. As the owner, I want Save to add a new Weigh-in dated today, in canonical kg whatever unit I entered, so that stored data stays consistent.
64. As the owner, I want a clear message for a value that is not a sensible weight, so that a typo is not saved.

### Layout, state and robustness
65. As the owner, I want Home in the order the design gives on a phone, so that today's Workout is first and the rest follows.
66. As the owner on a tablet or desktop, I want two columns from 900 px, so that wide screens are used well.
67. As the owner, I want Home to look right in Keep and Clean and in dark and light, so that the Theme choice never breaks it.
68. As the owner, I want Home to update when data changes (a Workout finishes, a weigh-in saves) without a reload, so that it is never stale.
69. As the owner, I want every start and save action disabled with a plain reason when the app is read-only (another window open, storage unavailable), so that I never silently lose a change.
70. As the owner, I want a failed save to leave Home unchanged and show the shared banner, so that I know it did not happen.
71. As the owner using a screen reader, I want the card, strip tiles, orbs and Nudges to have meaningful labels, so that Home is usable without sight.

## Implementation Decisions

**Core (pure, no I/O, "today" passed in)**
- **Home view builder.** One rule takes the data snapshot, the active Plan's Settings and today, and returns a complete description of S-01: header (date, Phase label), the TodayCard state, the WeekStrip, the week-progress line, the three orbs, the visible Nudges, recent PRs and the Weight panel. The page only renders it. This is the one new test seam.
- **TodayCard state, in order of precedence:** no active Plan; Break active today; a Workout in progress (any date); a Workout finished today (Done); Plan finished (the Phase position is after the end); Plan not yet started; a Session due today and undone (Start); otherwise rest day. Start means a Session scheduled today (including one moved to today) with no Workout from it this week. The existing "next to start" rule is not used for the card, because it also offers earlier Missed Sessions, which belong to Make up and Do a different Session. Rest day names the next undone Session later this week, or the first Session of next week when none is left.
- **Done today** shows duration, confirmed Set count and PR count from the existing summary rule; a Partial Workout is marked Partial.
- **Week strip and line.** Seven days from the week layout. Each tile: weekday, Session code, Session type, and a state (done, missed, partial, Break day, or upcoming). The Session code is the part of the Session name before the first colon when it is short, otherwise its first letter. The line reads done Workouts this week, the Plan's weekly target and the Missed Sessions' codes.
- **Orbs.** Streak: the current Streak and this week's counted Workouts against the minimum. Phase: week in Phase over Phase length. Race: days to the Plan's goal date and the share of the Plan elapsed; absent when there is no goal date. During a Break the Streak orb keeps its number, greys its ring and carries the caption "On a Break".
- **Nudges.** The existing Nudge rules supply the list, most important first. The builder removes dismissed ones and keeps at most two. A Nudge's identity is its kind, its Session or body area, and the week's Monday. Each Nudge maps to one action: Missed session to Make up; one short of target to the different-Session sheet; no Backup to Export and Import; repeated pain has no action beyond Dismiss in B3 (its action arrives with the Exercise library, B4).
- **Recent PRs.** The most recent PRs (five) across finished Workouts, newest first, each with its previous best, using the existing PR rule and the existing "history as of the Workout's date" rule.
- **Weight panel.** Latest Weigh-in, the average of this calendar week's Weigh-ins (Monday to Sunday), and the change from the previous week's average when both exist. Values are canonical kg in Core; the page converts to the display unit.
- **Custom workout creation.** A new rule creates an in-progress Workout with no Session (so a Missed session stays Missed) from a list of Exercises. Make up copies the Missed Session's planned rows; Exercises picked from the library get the Set count and rep target of their row in the active Plan when they have one, otherwise a plain row. It reuses the existing planned-rows logic and the existing Custom-workout offer order for the picker. No data model change.
- **Weigh-in creation.** A new rule validates a weight (a sensible positive number in the entered unit), converts to kg and builds a Weigh-in with a fresh ID dated today.

**Ui**
- **Home page** replaces the placeholder entirely and reads the builder's output. It subscribes to data changes as the placeholder does.
- **New components:** TodayCard (C-09), Nudge (C-08), WeekStrip (C-24), and the sheets (different Session, Log a past Workout, Log weight), all built on the shared Sheet. StatusOrb (C-11) is reused. One component per C-number; layout through CSS isolation; tokens only.
- **Draft page** for Build a Custom workout and Make up: a plain list of Exercise rows with a remove control, an Add button using the shared ExercisePicker, and Start. The Make up route carries the Session. The draft is in-memory only and is lost on reload, which is acceptable for a short step.
- **Start paths** use the existing starter for Sessions, a Custom start for the draft, and the existing Logged afterwards start for past dates; save failures go to the shared save-failure notice. All writes go through the existing write path.
- **Nudge dismissals** are stored through the existing device-preferences store (browser local storage), keyed by the Nudge's identity including the week. They are not part of the data model, not backed up, and reset if browser data is cleared, which only brings a Nudge back.
- **Entry points into later stages** (Week view, Progress, Export and Import) are the existing placeholder routes; B3 only navigates to them.
- **Read-only and in-progress rules.** When writing is not allowed every starting and saving action is disabled with the plain message used in the log. While a Workout is in progress the alternatives and Log a past Workout are hidden. During a Break the alternatives, Log a past Workout and Nudges are hidden; Log weight stays.
- **Tokens.** New tokens (if any, for example Session type colours) go in both token files and the design document tables in the same change.
- **Docs.** B3 adds its manual checks section to the PWA manual checks (Home states, Nudges, entry points, Logged afterwards from Home, Continue after a past-dated start, Log weight).

## Testing Decisions

- **A good test checks behaviour you can see**, not how it is built: the card state, the Nudge list, what a tap writes. No test asserts private structure or markup details beyond labels and visible text.
- **Core, through the Home view builder** (highest seam): every TodayCard state and precedence conflict (Break and in progress together, finished today and another Session due, Plan finished with a Workout in progress, moved Session due today, week with nothing left); the strip states including Break days and Partial; the week line; each orb including the Break, no-goal-date and week-boundary cases; Nudge capping at two, order, and dismissal by week; recent PRs ordering, cap and previous best; the weight average and change across week boundaries and with missing weeks. Custom workout creation and Weigh-in validation get their own focused tests (zero, negative, absurd values, unit conversion, whole-kg round trip).
- **Ui, through the Home page** with the in-memory store: each state renders with the right actions; Start and the different-Session sheet start the right Workout; the draft page adds, removes, disables Start when empty and starts a Custom workout; Make up pre-fills and leaves the Session Missed; Log a past Workout refuses a future date and starts a Logged afterwards Workout; Log weight saves a Weigh-in; dismiss hides and persists across a re-render; read-only and failed-save behaviour using the fault-injecting store and a held writer lock; labels for the card, tiles, orbs and Nudges.
- **Prior art:** the B2 log view builder tests and finish-flow page tests; the existing Nudge, Weeks, Schedule and Rearrangement rule tests; the B1 and B2 sheet and picker tests; the fault-injecting store and fake clock helpers. A bUnit click returns before an async save completes, so tests wait for a visible result before asserting.
- **Browser tests** are not part of B3. Visual checks against the `S-01-*` mockups, in both Themes and both Modes, are manual.

## Out of Scope

- The Week view screen, moving a Session and suggested weeks (B5). The WeekStrip only navigates there.
- Progress, Benchmarks, Calendar and the Weight screen (B6). The orbs only navigate to Progress.
- Weekly review and Nudge copy and dismissal polish across screens (B8).
- Export, Import and Backup screens (B9). The Backup Nudge only navigates there.
- The Exercise library and "Add a new Exercise" in the picker (B4); the editable Plan-row component (C-20, B5).
- First run (S-12) and the Settings screen (B10). The no-Plan state is the existing empty state.
- Back-dated Weigh-ins, editing or deleting a Weigh-in, weekly averages table.
- Storing dismissals in the data model, or any other data model, exchange format or storage change.
- Push notifications, animation, the Badge unlock toast, and Streak or Badge celebrations on Home.
- The open items carried from B1 and B2 (added-Exercise position, undoing Logged afterwards, Distance + time, Text kind).

## Further Notes

- **Decisions confirmed with the owner (21 Nov 2026):** dismissals in browser local storage; the Log weight sheet always adds a new Weigh-in (so the weekly average counts every entry); a simple draft page with no C-20; a Log a past Workout entry point on Home.
- **Assumptions to verify at ticket time, not to change the data model for:** how the log presents a Custom workout whose planned rows were created from the draft (rows versus "+ Added" entries); the Session code rule above; where Session type colours already exist as tokens; the exact parameters of StatusOrb for the Break caption and the ring.
- **Deviations from the design to record in the PR:** Nudge actions (repeated pain has Dismiss only; one short of target offers the different-Session sheet); Log a past Workout is not in the S-01 layout; the rest day card for a Session that is only Missed.
- **Open items from B2 that touch this stage and stay open:** a past-dated Workout is in progress until finished (Home shows Continue); undoing Logged afterwards cannot restore the start time.
- **Agent workflow trial:** this is the first stage written for the planner and implementer split ([ADR 0007](../adr/0007-planner-and-implementer-split-with-file-based-dispatch.md)). Tickets use `docs/agents/ticket-template.md` at about 80k tokens, and `RUN.md` carries the waves. Shared files (`Home.razor`, `components.css`, tokens) must be owned by one ticket at a time.
