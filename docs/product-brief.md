# Product brief

Terms are defined in [`../GLOSSARY.md`](../GLOSSARY.md) and used capitalised here.

## What and why

A phone-first fitness tracker that replaces the owner's Obsidian/Dataview system. Training goals: get fitter, stronger and more resilient, reduce pain and injury risk, improve cardio health, lose a little weight and add muscle, and work towards **HYROX London (Doubles), 24 Apr 2027**. Tool goals: fast, pleasant logging at the gym; much more customisable and interactive than editing markdown tables; a built-in knowledge base of how to do each Exercise.

Not a learning project: it should be built quickly and well.

## Who

One user (the owner) for now, with a possible public release much later. No accounts, sharing or coach view. The data model has no "user" concept today; adding one is a separate future project.

## Experience principles

1. **Logging at the gym is the primary experience**, used mid-workout on a phone, often one-handed, between sets. Everything else can be clunkier if it has to be.
2. **The log screen is a simple list of input fields**, prefilled with last time and a Suggested target. No clutter.
3. **Quiet logbook with a few Nudges.** No push notifications. Nudges are in-app messages on the dashboard.
4. **Fast and minimal tool, with game elements and a knowledge base** tucked away on their own screens so logging never feels like a game.
5. Consistency matters most (not missing Sessions), then progress in weights and Exercises, then weight trend, then the race countdown.
6. **Gym ergonomics are requirements:** screen stays awake during a Workout (wake lock), large one-handed tap targets, no scrolling to log a Set, dark mode, readable in bright light.
7. **Units:** kg and km/m by default, with a switcher in the first version (kg/lb, km/miles). One canonical unit is stored; the switcher only changes display. Some days the user wants to see both.

## Screens and moments

- **Home dashboard:** today's Workout first (or "rest day: next is Session C on Wednesday"), then week progress (done vs target, any Missed sessions), Streak, weight trend, recent PRs, Nudges. One tap to start. A "do a different Session today" action and a "build a Custom workout" action sit beside it. A quick "log weight" action is always available.
- **Workout log:** one row per planned Set, prefilled from last time; confirm or edit. Shows Last, Best and a Suggested target with a one-line reason. A small RPE input per Exercise. An "up next" strip. The Exercise name links to its Guide. Per-Exercise menu: swap, skip, add an extra Exercise, mark as benchmark (kept light). Optional rest timer (starts when a Set is confirmed, off by default). Duration is calculated automatically. At the end: an optional, skippable **Pain check**.
- **Guide page:** what it works, why it's in the Plan, setup, how to do it, cues, common mistakes, easier/harder variations, watch-outs, a video URL (shown as a link first), and personal notes. Fully editable; new Exercises can get their own Guide.
- **Week view:** each Session has a default day; the user can move Sessions around for one week only, optionally making a change the new default. After a Break or a pile-up, the app suggests a re-arranged week (outstanding Sessions on coming days in plan order, avoiding back-to-back overlap of the same muscle group using tags). It is always a draft the user accepts or edits.
- **Weekly review:** Workouts vs target, Missed sessions, PRs, weight average vs last week, Pain checks, the old "on track / behind / target hit" message, and a one-tap Export for the LLM pre-set to the past week.
- **Progress:** line chart per Exercise logged twice or more (one point per day, the day's best), grouped Strength (e1RM), Carries, Engine, Bodyweight skills, Body.
- **Benchmarks:** all benchmark Exercises with trends.
- **Trophies and calendar:** Badges with progress bars, personal bests, GitHub-style training heatmap coloured by Session type with a Phase strip.
- **Weight section:** trend chart (every weigh-in plus 7-entry rolling average), current/start weight, weight lost, this week's average vs last week's, weekly averages table, optional goal weight line.
- **Plan editor:** everything editable on the phone (see below).
- **Exercise library:** all Exercises with tags, grouped views, add your own.
- **Export / Import / Backup screens** and **Settings** (units, Streak rules, increment, rest timer, etc.).

## Features by area

### Planning
- Several **Plans**, one active at a time. A new Plan can be made in the app from scratch, copied, or imported (an imported Plan is added as a new Plan and can be selected like the others).
- **Phases** sit inside a Plan, switch automatically by date (with a manual "move to next Phase"), can override a Session's Exercise list (otherwise inherit the Plan default), show progress ("week 3 of 11"), and can carry optional **Phase goals** that say when to move on early. A per-Phase progression rule is a later idea.
- **Sessions** have name, default day and type (strength, cardio, body, test). A **benchmark** Session is scheduled on an interval (every 4 weeks in the starting Plan), is pre-filled from benchmark Exercises, shows the result next to the previous test, and does not count towards the weekly target or Streak.
- **Editable in the app:** Plan settings (start date, goal and date, weekly target, Streak rules, weight increment), Phases, Sessions, the Exercises within a Session (order, sets, reps, cue), the Exercise library (names, tags, how it's logged), Guides, Badges.
- **History is never rewritten:** edits apply going forward; each Workout keeps what was planned at the time.
- **Starting Plan** is the current one, unchanged: Sessions A Lower+core (Mon), B Upper+shoulders (Tue), C Engine+core (Wed), D Full body (Thu), E Long session (Sat), Sunday weigh-in, benchmark every 4 weeks; Phases Foundation (weeks 1-8), Build (9-19), HYROX prep (20-27); start 2026-10-19, race 2027-04-24; 5 Sessions a week, Streak minimum 4, increment 2 kg; about 40 Exercises. It arrives through the normal Import (converted from `Config.md`).

### Doing
- **Workout** from a Session, or a **Custom workout** built from any Session's Exercises or the library (Missed session Exercises suggested first). A missed Session stays recorded as Missed even if its Exercises are later done in a Custom workout; a Session only becomes Missed once its day has passed undone.
- **Set-by-set logging**; the best Set drives PRs and the Suggested target. A **Max set** (e.g. 1RM or 3RM attempt) is the source of truth for an Exercise's best when one exists.
- **Swap** an Exercise (original recorded as swapped, not skipped), **skip** (optional reason: time, pain, equipment), or **add** an unplanned Exercise.
- **Partial workouts** count, with the rest recorded as skipped, and can be reopened the same day.
- **Back-dating** any Workout, no limit, marked "logged afterwards".
- **Weigh-in** is a standalone action on any day with a Sunday prompt (not a Session).
- **Pain check** at the end of a Workout: "No" is one tap; "Yes" asks for body area(s), severity (mild, moderate, sharp), optional note and optional Exercise. Skipping is recorded as unanswered, which is different from "no pain".
- **Breaks:** a date range (injury, illness, holiday) excused from Streak and weekly target; Plan dates don't move; Nudges go quiet.
- **Cardio** uses the same Set rows (distance, time or both), with pace calculated, so intervals and steady efforts both fit.

### Rules carried over from the old system
- **Log kinds:** load (kg x reps, `bwx8` for bodyweight), carry (kg per hand x m), reps, secs/mins/km/m, time (m:ss, lower is better), kg/cm body measurements (not PRs; show change since start), text, and tick-only. Input is forgiving (commas as decimals, `x`, `×`, `*`, spaces ignored).
- **PRs:** load = higher estimated 1RM (Epley: kg x (1 + reps/30)), bodyweight sets rank below any loaded set; carry = heavier kg then longer distance; time = lower; others higher. A PR must strictly beat the previous best. A Max set overrides the derived best.
- **Suggested target:** load: +1 rep until the top of the rep range, then +increment kg and drop reps by 2; carry: +10 m up to 40 m, then add kg; time: beat your best; other numbers: best plus a step. **RPE adjusts it:** at RPE 9-10 hold the same weight and reps; at RPE 6 or below suggest a bigger jump. Always shown with a one-line reason.
- **Weeks and Streak:** weeks run Monday to Sunday (ISO). A Streak week needs at least the minimum number of counted Workouts (starting Plan: 4). The current week never breaks the Streak while in progress. Any Workout with at least one completed Exercise counts; strength and cardio count, weigh-ins and benchmark tests don't. What counts and the minimum are editable.
- **Badges:** editable (name, metric, target) with a starter set; metrics include sessions, streak, best week, prs, or any Exercise; a target of `bodyweight` means latest weigh-in; a negative target on a measurement means "drop by this much". Progress is always calculated from history, never stored.
- **Consistency messages:** "on track", "behind", "target out of reach", "target hit", worked out from the day of the week.
- If an Exercise appears in several Sessions, the first row defines its kind.

### Exercises, tags and recommendations
- Every Exercise has a **permanent ID**; the name is only a label (renaming keeps history).
- Each Exercise records how it is logged and optional tags: muscle group, equipment, movement pattern.
- **Built now:** tags, per-Set data, RPE and swap history are captured from day one, and the swap button offers tag-based "similar Exercises".
- **Built later:** the stats-driven recommendation engine (needs months of data), and an Exercise database with stats.

### Nudges (first set, in-app only)
- "Session A was missed: make it up?"
- "You're one Session short of this week's target."
- "You flagged knee pain twice this week: consider an easier variation."
- "No Backup in 14 days."

### Game elements
Keep Badges, calendar heatmap, Streak, PR highlights and the race countdown. **XP and levels are shelved.**

## Moving data in and out (LLM-friendly)

Must work entirely on the phone.

- **Export:** a picker with a date range (default last 12 weeks) and tick boxes for Workouts, Plan, Weigh-ins, Pain checks, Guides and Exercises (Guides off by default; health data easy to leave out before pasting into a chat tool), plus "export everything" for Backups. Formats: **JSON** (canonical, importable, self-describing with a "how to read this" header) and **Markdown** (readable, for pasting into a chat). Delivery: download file, copy to clipboard, phone share-sheet if cheap. No cloud storage integration.
- **Prompt pack:** a "Copy LLM prompt" button with ready-made prompts, each including instructions, the exact JSON format and the user's current Exercise library (IDs, names, tags): **Analyse my training** (with an Export), **Write me a Plan**, **Write Guides for these Exercises**.
- **Import:** a big **paste box** first, file picker second. Kinds: restore a Backup; import a Plan (as a new Plan); merge small changes (new Exercises, edited targets, back-filled Workouts); import Guides (separate import). Every import shows a plain-language **preview** ("adds 3 Exercises, changes the target on 2, creates a new Plan called X, touches no Workouts") and changes nothing until confirmed; an **automatic backup** is saved just before applying; bad files are rejected with a message naming the exact field.
- **Matching Exercises:** IDs match exactly; names match exactly; near-matches are suggested ("Did you mean Goblet Squat?") and need confirmation, never merged silently; unknown names are offered as new Exercises.
- **Guide imports** are keyed by Exercise ID; the preview lets the user choose keep or replace per Guide; personal notes are never overwritten.
- **Versioning:** every Export carries a format version and the importer keeps reading older versions.

## Starting data (via Import)
- The starting Plan, converted from `Config.md`.
- The 27 existing Guides as starter Guides.
- Weight history from 1 Feb 2026 (start 113 kg), from the old daily notes. No Workout history exists to migrate.

## Parked on purpose
XP and levels; stats-driven recommendation engine; an Exercise database with stats; per-Phase progression rules; push notifications; sync between devices and accounts; public release; embedded video (links first, tap-to-load embed later); cloud storage integration; other units beyond the kg/lb and km/miles switcher.
