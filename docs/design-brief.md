# Design brief: Fitness App (for the UI/UX design agent)

This document is self-contained. You do not need the rest of the repo to do the design work. Where it names a term in **bold**, that is the app's exact vocabulary; use it in all labels and copy.

## 1. What you're designing

A **phone-first** training tracker for one person (the owner), replacing an Obsidian/Dataview setup. It helps them follow a structured training plan towards **HYROX London (Doubles), 24 Apr 2027**, log workouts quickly in the gym, see progress, and learn how to do each exercise. It may be released publicly much later, but design for one user now.

Training goals behind the product: get fitter and stronger, reduce pain and injury risk, improve cardio health, lose a little weight and add muscle, and have a goal to work towards.

## 2. Who and where

- One user, a keen but not elite trainee who works in software. Comfortable with technology; wants speed and customisation over hand-holding.
- **Primary context: in the gym, mid-workout, on a phone**, often one hand, sweaty, 60-90 seconds between sets, sometimes poor signal, sometimes bright light. The app is opened, the next thing is seen, a number is logged in a few taps, and the phone goes away.
- Secondary contexts: morning ("what's today?"), after a workout ("did I improve?"), Sunday weigh-in and weekly review, and occasional planning/editing, which can be slower and denser.
- Laptop use is incidental. Design phone first; give desktop a sensible responsive layout, nothing more.

## 3. Design principles (in priority order)

1. **Logging is the product.** The Workout log screen must be the best screen in the app. Everything else may be clunkier if it has to be.
2. **Simple list of inputs.** One row per Set, prefilled with last time, confirm or edit. No clutter, no decoration, no modals mid-set.
3. **One-handed, big targets.** Primary actions reachable by thumb at the bottom of the screen; generous tap areas; no precision gestures; no scrolling needed to log a Set.
4. **Quiet.** A logbook that shows suggestions when looked at. No push notifications, no pop-ups that interrupt logging. **Nudges** are calm in-app messages on the home screen only.
5. **Fast and minimal at the gym; motivating elsewhere.** Game elements (badges, heatmap, streak, PRs, race countdown) live on their own screens and the home screen, never in the way of logging.
6. **Always explain.** Suggestions say why ("You hit 10 reps last time, so +2 kg, 8 reps"). Exercises say why they're in the plan.
7. **Customisable by the user, on the phone.** Editing plans, exercises, guides, badges and settings must feel as considered as logging, though less frequent.

## 4. Hard constraints

- **Delivery:** a PWA (installable web app, works offline), built in **Blazor WebAssembly with Razor components** (C#). A native app may reuse the same components later. So: design as a **component system** that can be built in plain HTML/CSS with minimal JavaScript. Avoid designs that depend on heavy custom animation, canvas-only UI, or gestures that browsers handle poorly. Charts are rendered from data (line charts and a heatmap); keep them simple enough to build with SVG.
- **Phone-first sizes:** design for roughly 360-430 px wide portrait. Test small (360 px) and large phones. Include a sensible tablet/desktop layout.
- **Screen stays awake** during a Workout (browser wake lock), so the log screen can be glanced at repeatedly. No auto-dimming assumptions.
- **Dark mode required**, with a light mode. **High contrast** that stays readable in bright light. Don't rely on colour alone to convey state (use icons/labels too).
- **Offline:** the whole app works with no signal. Show a calm offline state only where something genuinely needs a connection (opening a video link).
- **Units:** kg/lb and km/miles are switchable in Settings (one stored unit, display only). Layouts must cope with both and with long values (e.g. "24 kg x 40 m", "4:25").
- **Accessibility:** WCAG AA contrast, visible focus states, labels on all inputs, respect "reduce motion", minimum 44x44 px touch targets (aim larger for log inputs), support system text scaling without breaking.
- **No accounts, no sharing, no social features, no push notifications, no cloud sync** (all deliberately out of scope).
- **Data is local.** There are no loading spinners for local data; design for instant. Loading/empty states exist for first run and for imports only.
- **Performance and size:** keep imagery minimal. Exercise guides may contain images later; design placeholders, but no heavy illustration dependence.
- Styling/UI library is **undecided on purpose**: your design tokens and component inventory will drive that choice. Prefer a design that doesn't require a heavyweight component library.

## 5. Vocabulary (use exactly)

- **Plan**: a full training programme; several can exist, one active. Contains **Phases** (e.g. Foundation, Build, HYROX prep) and **Sessions**.
- **Session**: a planned template (e.g. "A: Lower + core"), with a default day and a type (strength, cardio, body, test).
- **Workout**: one real occurrence on a date, from a Session or a **Custom workout**.
- **Exercise**: a movement, with a permanent identity, tags (muscle group, equipment, movement pattern) and a **Guide**.
- **Set**: one logged effort row. A **Max set** is a flagged 1RM/3RM-style attempt.
- **PR**: a Workout's best Set that strictly beats the previous best.
- **Suggested target**: next time's proposed weight/reps, always with a one-line reason.
- **RPE**: how hard an Exercise felt, a small optional input per Exercise.
- **Pain check**: an optional, skippable form at the end of a Workout.
- **Missed session**, **Partial workout**, **Swapped exercise**, **Skipped exercise**, **Break**, **Streak**, **Badge**, **Benchmark exercise**, **Phase goal**, **Weigh-in**, **Nudge**, **Backup**, **Export**, **Import**, **Prompt pack**, **Week schedule**.

Avoid: "routine", "programme", "day" (for a Session), "log" for a Workout, "XP", "level".

## 6. Screens to design

For each screen, design the normal state plus the listed states.

### 6.1 Home (dashboard)
- **Today's Workout card is the hero**: Session name and code, a short list or count of Exercises, estimated nature (strength/cardio), a big **Start** button. If a Workout is in progress: **Continue**. On a rest day: "Rest day: next is Session C on Wednesday". If a Workout is already done: a summary.
- Beside the hero: **Do a different Session today** (lists this week's undone Sessions) and **Build a Custom workout**.
- Below: **week progress** (Workouts done vs target, any Missed sessions), **Streak** and best Streak, a small **weight trend** with latest weigh-in, **recent PRs** (last few, each with previous best), the **race countdown** and current **Phase** progress ("week 3 of 11").
- **Nudges** area (max one or two at a time), e.g. "Session A was missed: make it up?", "One Session short of this week's target", "You flagged knee pain twice this week: consider an easier variation", "No Backup in 14 days". Each dismissible and actionable.
- Persistent quick action: **Log weight**.
- States: first run (no Plan), Break active (quiet, no pressure), Plan finished.

### 6.2 Workout log (the most important screen)
Design this in the most detail, and as a **clickable prototype to try on a phone**.
- Header: Session name, date, progress ("3 of 8 done"), PR count, an unobtrusive elapsed duration, and an optional **rest timer** control (off by default; starts when a Set is confirmed).
- Current Exercise: name (tappable, opens its Guide), planned sets x reps, the one-line **cue**, and a short "why" line. **Last** and **Best**, and the **Suggested target** with its reason.
- **Set rows:** one per planned Set, prefilled from last time. Input kinds vary by Exercise: weight x reps (including bodyweight `bwx8`), carry (kg per hand x metres), reps only, time (m:ss, lower is better), distance, minutes/seconds, free text, or a simple tick. Inputs must be forgiving and fast (numeric keypad, steppers, quick +/- for weight, easy "same as last" confirm). A Set can be flagged **Max set**. Live feedback: a **PR!** indicator when a Set beats the best, and a format warning if input is invalid.
- **RPE** per Exercise: small, optional, fast (e.g. a compact 1-10 selector).
- **Exercise menu** (light, secondary): Swap, Skip (with optional reason: time, pain, equipment), Add an extra Exercise, Mark as benchmark, View Guide.
- **Up next** strip showing the next Exercise.
- **Finish:** a Partial workout is allowed ("5 of 8 done; the rest are recorded as skipped"). On finish, an optional **Pain check**: "Any pain today?" with a one-tap **No** and a **Skip**; "Yes" opens body area(s) (back, shoulder, wrist, knee, other), severity (mild, moderate, sharp), an optional note and an optional Exercise. Skipping is different from "No".
- **Logged afterwards** variant for back-dated Workouts.
- States: in progress, resumed after leaving the app, swapped/skipped/added Exercise, PR achieved, finished summary (duration, Sets, PRs).
- Cardio and interval Sessions use the same Set rows (distance, time, or both, with pace calculated).

### 6.3 Guide page (the knowledge base)
Reached from an Exercise name. Sections: what it works, why it's in your plan, setup, how to do it, cues, common mistakes, easier and harder variations, watch-outs (back, shoulders, wrists, knees), a **video link** (embed later), your history and where it sits in the Plan, and **My notes**. Everything is **editable on the phone**. Design the read view and the edit view, and the "new Exercise, no Guide yet" state.

### 6.4 Exercise library
Browse and search all Exercises; filter by tag (muscle group, equipment, movement pattern); grouped by Session; add a new Exercise (name, how it's logged, tags, optional Guide). Used also by the Swap and Add pickers (show **similar Exercises** first, based on tags).

### 6.5 Plan editor and Plans list
- **Plans list:** several Plans, one active; create (from scratch, copy, or import), switch active.
- **Plan editor:** Plan settings (start date, goal and date, weekly target, Streak rules, weight increment); **Phases** (dates, a progress display, optional Phase goals, optional per-Phase Exercise overrides); **Sessions** (name, default day, type); the **Exercises within a Session** (reorder, sets, reps, cue, log kind, benchmark toggle); **Badges**.
- Must be usable one-handed on a phone: favour inline editing, reordering, and add-from-library over dense forms. Edits apply going forward; make clear that past Workouts are unchanged.

### 6.6 Week view
Each Session sits on its default day. The user can move a Session to another day **for this week only** (and optionally make it the new default). After a **Break** or a pile-up, the app proposes a re-arranged week as a **draft** the user accepts or edits (outstanding Sessions on coming days, avoiding back-to-back sessions for the same muscle group). Also shows Missed sessions, and how to mark a **Break** (date range plus a reason).

### 6.7 Progress, Benchmarks, Trophies, calendar
- **Progress:** a line chart per Exercise logged twice or more (one point per day, the day's best), grouped Strength (estimated 1RM), Carries, Engine, Bodyweight skills, Body. Show change since first entry and highlight the best point.
- **Benchmarks:** all benchmark Exercises with trends and the latest scheduled benchmark test vs the previous one.
- **Trophies:** **Badges** with progress bars (unlocked first) and a personal bests table; badges are user-editable.
- **Calendar:** a GitHub-style heatmap from the Plan start to race day, coloured by Session type, with a **Phase** strip beneath.
- No XP, levels or leaderboards.

### 6.8 Weight section
Trend chart (every weigh-in plus a 7-entry rolling average), current and start weight, weight lost, this week's average vs last week's, a weekly averages table, and an optional goal weight line. Quick **Log weight** entry usable any day.

### 6.9 Weekly review
Workouts vs target, Missed sessions, PRs this week, weight average vs last week, Pain checks, a one-line message ("on track", "behind", "target out of reach", "target hit"), and a one-tap **Export for the LLM** preset to the past week.

### 6.10 Export, Import, Backup, prompt pack
All must work on the phone alone.
- **Export:** a picker with a date range (default last 12 weeks) and tick boxes (Workouts, Plan, Weigh-ins, Pain checks, Guides, Exercises); Guides off by default; an obvious way to leave out health data (weight, pain) before pasting into a chat tool; **Export everything** for a Backup. Output as JSON (canonical) or Markdown (readable). Delivery: **Download file**, **Copy to clipboard**, and the phone's share sheet.
- **Prompt pack:** a **Copy LLM prompt** area with three prompts (Analyse my training, Write me a Plan, Write Guides for these Exercises); a short explanation of how to use each with a chat app.
- **Import:** a big **paste box** first, a file picker second. Kinds: restore a Backup, import a Plan (becomes a new Plan), merge small changes, import Guides. Always a **preview** in plain language ("adds 3 Exercises, changes the target on 2, creates a new Plan called X, touches no Workouts") with **Apply** and **Cancel**; Exercise name matching shows suggestions needing confirmation ("Did you mean Goblet Squat?") and unknown names offered as new; Guide imports let the user choose keep or replace per Guide. Clear, field-level error messages for bad files. Note that an automatic backup is made before applying.

### 6.11 Settings
Units (kg/lb, km/miles), Streak rules (minimum Workouts per week, what counts), weight increment, rest timer defaults, theme (system/dark/light), Backup status and "last backed up", and data storage health (persistent storage granted or not, install prompt).

### 6.12 First run and install
Welcome and first-run flow: starts with the owner's current Plan loaded via Import, an install-to-home-screen prompt, a request for persistent storage with a plain explanation, and a reminder to make Backups.

### 6.13 Game elements and the "extras"
Everything motivating lives here and on the Home screen, never in the way of logging. **No XP, levels, leaderboards or social sharing.** Design all of the following:
- **Badges:** a Badge has a name, artwork, a metric and a target (e.g. "10 Workouts", "4-week Streak", "Goblet squat 24 kg x 8"), shown **locked** (with a progress bar and "x of y"), **in progress**, or **unlocked** (with the date earned). A **badge list** (unlocked first, then closest to unlocking), a **badge detail** view (what it needs, current progress, history), and a **badge editor** (create, edit and delete Badges; pick a metric and target; metrics include Workouts, Streak, best week, PR count, any Exercise's best, and body weight vs a target, including "drop by this much"). The editor must be simple enough to use on a phone.
- **Starter badge set** (examples to design around; the final list comes from the owner's current Plan): Workout count milestones (1st, 10th, 25th, 50th, 100th), Streak milestones (2, 4, 8, 12 weeks), a best week (e.g. 5 of 5 Sessions), PR count milestones, a first benchmark test completed, a Phase completed, a first Max set, weight goals, carry and running distance targets, and a "back after a Break" badge. Each needs artwork in a consistent family, with locked and unlocked treatments.
- **Unlock moment:** a short, calm celebration that never blocks logging (for example a toast on the finish summary, then a place in the badge list). Provide a reduced-motion version. Sound and haptics are off by default.
- **PR moment:** feedback while logging (a clear "PR!" on the Set row) and a recap on the finish summary and Home ("recent PRs", each with the previous best).
- **Streak display:** the current Streak, the best Streak, and how the current week is going without it ever looking broken mid-week; a gentle state during a **Break**.
- **Race countdown and Phase progress:** days to the race, weeks to go, and the current Phase with a progress display and any Phase goals.
- **Calendar heatmap with a Phase strip** (see 6.7), including how Missed, Partial, Skipped, Swapped and Break days look.
- **Personal bests table** and the weekly "best of the week" highlights in the review.
- **Empty and first-time states** for every one of these (no badges yet, no PRs yet, no Streak yet) that feel encouraging, not blank.
- **Tone:** quietly motivating, a little playful, never childish or pressuring; the language is plain ("PR!", "4-week Streak"). Missed Sessions are described neutrally, never as failure.

## 7. Key flows to map (as step-by-step flows and annotated screens)

1. **Log a Workout** from the home screen: start, log Sets, swap an Exercise, add RPE, hit a PR, finish early (Partial), Pain check, summary.
2. **Make up a Missed session:** from a Nudge or the Home card to a Custom workout built from the missed Session's Exercises plus others.
3. **Do a different Session today.**
4. **Edit the Plan:** change an Exercise's sets/reps, add an Exercise from the library, reorder, change a Phase.
5. **Import a Plan an LLM wrote:** copy the Prompt pack, paste the reply, preview, resolve name matches, apply, select the new Plan.
6. **Export for an LLM:** the weekly review's one-tap export, and the picker flow with health data excluded.
7. **Weekly review** on a Sunday, including logging the weigh-in.
8. **Break and re-arrange the week.**
9. **Edit a Guide** and add personal notes.
10. **Restore a Backup** after storage has been cleared.

## 8. Behaviours the designs must reflect

- **Home** shows today's Workout first; the dashboard stats sit below it.
- **Streak** rules: Monday-to-Sunday weeks; enough counted Workouts in a week keeps the Streak; the current week never breaks it while in progress; weigh-ins and benchmark tests don't count; it is user-editable.
- **A Session becomes Missed only after its day has passed undone.** It stays Missed even if its Exercises are later done in a Custom workout.
- **Swapped** Exercises are recorded as swapped, not skipped. **Skipped** Exercises can carry a reason.
- **A PR** must strictly beat the previous best; for time-based Exercises, lower is better; a Max set is the source of truth for the best when present.
- **Suggested target** uses last Workout and RPE: hold at RPE 9-10, bigger jump at RPE 6 or below, always with a reason.
- **History is never rewritten** when a Plan is edited.
- **Back-dating** is allowed with no limit and is marked "logged afterwards".
- **No push notifications.** Nudges are in-app only.

## 9. Sample content to use in the designs

**Starting Plan:** Sessions: A Lower + core (Mon, strength), B Upper + shoulders (Tue, strength), C Engine + core (Wed, cardio), D Full body (Thu, strength), E Long session (Sat, cardio), W Weigh-in (Sun, body), T Benchmark test (every 4 weeks). Phases: Foundation (weeks 1-8), Build (weeks 9-19), HYROX prep (weeks 20-27). Start 2026-10-19, race 2027-04-24. Weekly target 5, Streak minimum 4, increment 2 kg.

**Example Exercises and log kinds:** Goblet squat (weight x reps, `20x10`), Pull-up (bodyweight, `bwx8`), Farmer's carry (kg per hand x metres, `24x40`), Plank (seconds, `45`), Run intervals (distance and time, 5 x 1 km), Rowing 500 m (time, `1:48`), Body weight (kg, `86.2`).

**Example Nudge:** "Session A was missed: make it up?"
**Example Suggested target reason:** "You hit 10 reps at 20 kg last time, so try 22 kg for 8."
**Example Pain check result:** "Left knee, mild, goblet squat."

## 10. What you need to deliver

1. **A design system:** colour tokens for dark and light (with contrast ratios), type scale, spacing, radii, elevation, touch-target sizes, iconography style, and how colour encodes Session types and states (PR, missed, partial, skipped, swapped, pain).
2. **A component inventory** with all states: Set row (per log kind), Exercise header, Suggested target, RPE selector, Nudge, cards (Workout, stat, PR, Badge), buttons, inputs, pickers (Exercise, date range), tabs/nav, chart styles (line chart, heatmap, progress bar), preview/diff list for Import, form patterns, empty/error/offline states.
3. **Navigation model:** what's in the primary navigation on a phone (suggest a bottom bar), what's secondary, and how the Workout log is entered and exited.
4. **Annotated mockups** of all screens in section 6, at phone size in dark and light, including the listed states, plus a tablet/desktop layout.
5. **Flow diagrams** for the flows in section 7.
6. **A clickable prototype of the Workout log screen** (including swap, skip, RPE, PR feedback, finish and Pain check), suitable for trying on a phone at the gym.
7. **Handoff notes** for building in Razor/CSS: component boundaries, spacing and token naming, interaction details, accessibility notes, and anything that would be costly to build (flag it and offer a cheaper alternative).
8. **A recommendation on the UI library / styling approach** (plain CSS with tokens vs a component library) given the Blazor constraint.

## 10b. Exactly what we want back: the design document and assets

The output will be dropped into the repo and fed to build agents (who have limited context each), so structure matters more than polish. Please return:

### The design document (one Markdown file, `design-document.md`)
Plain Markdown, no embedded binary, referencing assets by relative path. Sections in this order:
1. **Summary and principles**: one page on the design direction and the decisions you made against section 3.
2. **Answers to the open questions** in section 11, each with a short rationale and rejected alternatives.
3. **Navigation model** and a screen map (a list or diagram) showing how every screen is reached.
4. **Design tokens**, as a table plus a machine-readable block (`tokens.json`, see assets): colours (dark and light, with contrast ratios), type scale, spacing, radii, elevation, motion, touch-target sizes, and the colour encoding for Session types and states.
5. **Component inventory**: for each component, its purpose, anatomy, all states (default, focused, disabled, error, loading/empty/offline where relevant), variants, content rules and accessibility notes. Number them (C-01, C-02 ...) so tickets can refer to them.
6. **Screen specifications**: for each screen in section 6, numbered (S-01 ...): purpose, the components used (by C-number), the data shown (using the glossary terms), every state, the interactions, and a link to its mockup image. Write these so that a developer who cannot see the image could still build the screen.
7. **Flow specifications** for the flows in section 7, numbered (F-01 ...), as step lists with the screen and component at each step, plus edge cases.
8. **Log screen deep-dive**: the Set row for every log kind, keyboard and input behaviour, PR feedback, the swap/skip/add/benchmark menu, and the Pain check, in enough detail to build without asking.
9. **Responsive behaviour** from 360 px phones to desktop.
10. **Accessibility checklist** and how each requirement in section 4 is met.
11. **Build handoff notes** for Razor and plain CSS: suggested component boundaries, class and token naming, anything costly to build (flagged, with a cheaper alternative), and your recommendation on UI library vs plain CSS with reasons.
12. **Open issues and assumptions**: anything you could not decide, anything you assumed about data or behaviour, and anything you think the product brief got wrong.

Keep every section skimmable (tables and lists over prose), and keep each screen and component spec self-contained so it can be pasted into a ticket on its own.

### Assets (in an `assets/` folder next to the document)
| Asset | Format | Notes |
|---|---|---|
| Screen mockups | PNG at 2x (or SVG), one per screen and state, named `S-01-home-dark.png`, `S-01-home-light.png`, `S-01-home-nobreak.png` and so on | Phone portrait at 390x844 as the base size, plus 360 px width for the log screen, plus one tablet/desktop layout per major screen |
| Flow diagrams | SVG (or PNG) named `F-01-log-workout.svg` | One per flow in section 7 |
| Design tokens | `tokens.json` plus a `tokens.css` of CSS custom properties for dark and light | Names must match the document's token table exactly |
| Icons | SVG, 24x24 viewBox, single colour using `currentColor`, in `assets/icons/`, named by purpose (`icon-swap.svg`, `icon-skip.svg`, `icon-pr.svg`, `icon-pain.svg`, `icon-benchmark.svg` and so on), plus a list of what each means | Only icons the design needs; say if an open-licence set (name it and its licence) is used |
| App icon and install assets | SVG master plus PNG exports at 192x192 and 512x512 (any and maskable), a monochrome mask, a splash colour, and a favicon | For the PWA manifest |
| Badge artwork | SVG, one per starter Badge (see 6.13), each with a locked and an unlocked treatment, plus a generic badge frame/template the owner can reuse for custom Badges | Consistent family; named `badge-streak-4.svg`, `badge-streak-4-locked.svg` and so on; works in dark and light |
| Celebration and state graphics | SVG, e.g. the PR marker, the unlock toast, the Streak flame/mark and its Break state, race countdown graphic | Static versions plus a note on any motion, with a reduced-motion alternative |
| Chart specifications | SVG or PNG samples of the line chart (with best point and rolling average), the calendar heatmap with Phase strip, and the progress bar, plus their colour and spacing rules | Must be buildable with plain SVG |
| Illustrations / empty states | SVG, optional, minimal | First run, no Plan, no history yet, offline |
| Fonts | Name the typefaces and their source and licence; if custom, supply WOFF2 files for only the weights used in `assets/fonts/` | Prefer the system font stack or one open-licence family |
| Clickable prototype | An HTML file (`prototype-log.html`, self-contained, no build step) or a shareable prototype link, covering the Workout log flow on a phone | Include any assumptions in the document |
| Asset manifest | `assets/README.md` listing every file, what it is and which screen or component uses it | |

### Rules for assets
- Use only open-licence or originally created assets and state each licence; no copyrighted or trademarked artwork (including HYROX branding).
- Keep total asset weight small (a PWA): SVG wherever possible, PNG only for mockups, optimised.
- Every asset must be referenced by the document, and every referenced asset must exist.
- Do not use real personal data in mockups; use the sample content in section 9.

## 11. Open design questions (please propose answers)

- Primary navigation: bottom tab bar vs other, and which 4-5 destinations.
- Best Set row design for fast, one-handed entry (steppers vs keypad vs both), and how to show "same as last" confirmation.
- How to present Suggested target and Last/Best compactly without crowding the Set rows.
- How to make the benchmark control and the per-Exercise menu light but discoverable.
- How to show Missed vs Partial vs Skipped vs Swapped distinctly in lists and the calendar.
- Draft-then-accept patterns for suggested week re-arrangement and Import previews.
- How to make Pain check fast and low-friction, while keeping "skipped" distinct from "no pain".
- The "game" tone: how motivating without competing with logging; badge visual language.

## 12. Out of scope

Accounts, sharing, social features, push notifications, cloud sync, XP and levels, a stats-driven recommendation engine UI (later), embedded video player (links first), other languages, public marketing pages.
