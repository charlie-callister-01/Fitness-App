# Fitness App

A personal training tracker for planning workouts, logging them quickly at the gym, and learning how to do each exercise. It replaces an Obsidian/Dataview system.

## Language

### Planning

**Plan**:
A complete training programme made of Tracks, Phases and Sessions, aimed at a goal. Several Plans can exist, but only one is active at a time.
_Avoid_: Programme, routine, schedule

**Phase**:
A block of weeks inside a Track with its own focus (e.g. Foundation, Build, HYROX prep).
_Avoid_: Block, stage, cycle

**Phase goal**:
An optional target on a Phase (e.g. a lift hitting a weight for reps) that shows when the user is ready to move on early.
_Avoid_: Exit criteria, milestone

**Track**:
A named path inside a Plan (for example gym and cardio, or morning mobility) with its own Cadence, rules and optional Phases. Every Session belongs to one Track. Tracks can be added, renamed and reordered, never removed, and a Track's Cadence never changes.
_Avoid_: Programme, category, lane

**Main track**:
The first Track in a Plan; it must be weekly. Custom workouts, the benchmark Session and Home's Today card attach to it, and the plain Badge metrics (`streak`, `bestweek`, `sessions`) measure it. It is not a flag: reordering Tracks changes which one is main.
_Avoid_: Default track, primary track

**Daily Track**:
A Track whose Sessions are on every day. A day is done when at least the Track's day-done percentage (default 75) of the Workout's planned rows is done, and its Streak is a run of consecutive done days. A daily Session is never a Missed session and cannot be moved.
_Avoid_: Habit, routine

**Cadence**:
Whether a Track runs weekly (a weekly target and a weekly Streak) or daily (a day-done percentage and a daily Streak). Chosen when the Track is created.
_Avoid_: Frequency, schedule

**Benchmark exercise**:
An Exercise the user has marked as one to track over time. The scheduled benchmark Session is pre-filled from these, and they are shown together with their trends.
_Avoid_: Test lift, key lift

**Session**:
A planned template in a Plan (e.g. "A: Lower + core"), belonging to one Track: a list of Exercises with target sets and reps, tied to a day of the week (or to every day, on a daily Track).
_Avoid_: Workout (that is what you actually did), day

### Doing

**Workout**:
One real occurrence of training on a specific date, usually created from a Session but possibly a Custom workout.
_Avoid_: Session log, training day

**Custom workout**:
A Workout built on the day from Exercises picked from any Session or from the exercise library, not from one Session as written.
_Avoid_: Ad-hoc session, freestyle

**Missed session**:
A Session whose planned day passed with no Workout made from it. It stays recorded as missed even if its Exercises are later done in a Custom workout.
_Avoid_: Skipped session

**Set**:
One continuous effort of an Exercise (e.g. 10 reps at 20 kg), logged as a row in a Workout. The best Set drives PRs and the next-time target.
_Avoid_: Round, rep block

**Swapped exercise**:
A planned Exercise replaced during a Workout by a different one. The original is recorded as swapped, not skipped.
_Avoid_: Substituted, replaced

**Break**:
A date range (injury, illness, holiday) excused from the weekly target and Streak. The Plan's dates do not move.
_Avoid_: Pause, rest week, deload

**Skipped exercise**:
A planned Exercise left undone in a Workout, with an optional reason (time, pain, equipment).
_Avoid_: Missed exercise (a Missed session is a different thing)

**Max set**:
A Set marked as a maximum attempt (e.g. a 1-rep or 3-rep max). When one exists for an Exercise it is the source of truth for that Exercise's best; otherwise the best is worked out from ordinary Sets.
_Avoid_: Test set, 1RM entry

**Estimated 1RM**:
The one-rep max worked out from a Set by the Epley formula: weight x (1 + reps / 30). It ranks Load Sets and is what a strength chart plots, so sets of different weights and reps compare fairly. Never stored. A Max set is separate: it is a real attempt, not an estimate.
_Avoid_: Calculated max

**PR**:
A Workout's best Set for an Exercise that strictly beats the previous best, by the rules for how that Exercise is measured.
_Avoid_: Record, personal best

**Suggested target**:
The weight and reps proposed for an Exercise next time, built from the last Workout and its RPE, always shown with a one-line reason.
_Avoid_: Try target, recommendation

**Partial workout**:
A Workout with some planned Exercises left undone. It still counts, and the rest are recorded as skipped.
_Avoid_: Incomplete workout

**Streak**:
The number of consecutive weeks (Monday to Sunday) with enough counted Workouts, per weekly Track, where the minimum and what counts are user-editable settings. The current week never breaks it while still in progress. A daily Track has a daily Streak instead: consecutive done days.
_Avoid_: Run, chain

**Badge**:
A user-editable achievement with a metric and a target, whose progress is always worked out from history.
_Avoid_: Trophy, achievement

**RPE**:
How hard an Exercise felt, rated by the user per Exercise, as a small optional input used as progress data.
_Avoid_: Effort, difficulty

**Pain check**:
An optional form at the end of a Workout recording any pain by body area. It can be skipped and feeds Nudges.
_Avoid_: Injury report, pain flag

**Weigh-in**:
A body-weight entry made on any day, independent of any Workout.
_Avoid_: Weight session

### Exercises and knowledge

**Exercise**:
A movement that can be logged and planned, with a name, a way of being measured, and a link to its Guide.
_Avoid_: Movement, lift

**Guide**:
The editable reference page for an Exercise: what it works, why it is in the Plan, how to do it, how to make it easier or harder, what to watch out for, a video link, and personal notes.
_Avoid_: Tutorial, notes page

**Exercise library**:
The permanent collection of every Exercise the user has, each with a lasting identity so renaming never loses history, and optional tags for muscle group, equipment and movement pattern.
_Avoid_: Exercise database, catalogue

### Moving data in and out

**Export**:
A chosen slice of the user's data (by date range and by kind of data) packaged to leave the app, either to back it up or to give to an LLM for analysis.
_Avoid_: Dump, download

**Backup**:
An Export of everything, complete enough to restore the whole app from.
_Avoid_: Snapshot, save file

**Import**:
Bringing data into the app from outside: restoring a Backup, adding a Plan as a new Plan, merging small changes, or adding Guides. Always previewed before anything changes.
_Avoid_: Upload, sync

**Prompt pack**:
A set of ready-made, copyable prompts (analyse my training, write me a Plan, write Guides) that carry the exact data format and the user's current Exercise library, for use with an LLM.
_Avoid_: Template prompt

**Week schedule**:
Which Sessions fall on which days in one particular week. It starts from each Session's default day and can be rearranged for that week only.
_Avoid_: Calendar, rota

### Appearance

**Theme**:
The app's visual skin, chosen in Settings: **Keep** (old-school fantasy: stone panels, rune-yellow text, linocut art) or **Clean** (plain and modern). Themes change only design tokens and artwork, never layout or behaviour.
_Avoid_: Skin, style

**Mode**:
Dark or light (or follow the system), chosen separately from the Theme. Each Theme has both Modes.
_Avoid_: Theme (for dark/light)

### Feedback

**Nudge**:
An in-app message on the dashboard prompting an action, such as making up a Missed session or taking care after a pain flag. Never a push notification.
_Avoid_: Reminder, alert
