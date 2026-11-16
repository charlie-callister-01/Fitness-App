# Grilling log

The essence of every question and the owner's answer, in order. Stage 1 was usage and experience, stage 2 practical, stage 3 technical (the data-model questions were pulled forward from stage 3 at the owner's request). Status: **complete, confirmed by the owner**. Full detail lives in the briefs; this is the audit trail.

## Stage 1: usage and experience

| Q | Topic | Answer |
|---|---|---|
| 1 | Who is it for | The owner only for now; potential to be released as an app much later |
| 2 | Moment of use | Logging at the gym is primary; wants an easy quick guide to the next Exercise, what's next, and a reason why |
| 3 | Use outside the gym | See Session progress, edit what's going on, and if a Session is missed, take from the others and make a custom one |
| 4 | Home screen | A home dashboard with today's Session as the main thing |
| 5 | What feels like success | Mostly not missing Sessions; second, progress in weights and Exercises |
| 6 | Pushing back | Logbook, with potential nudging features |
| 7 | Feel | Minimal tool for quick logging, some game elements, a knowledge repository on how to do Exercises, easy ways to change and implement different Plans and Exercise edits |
| 8 | Session vs Workout | Yes: Session is the planned template, Workout is what was done on a day |
| 9 | Missed Session | Yes: a missed leg day can turn an upper day into a full-body Custom workout, picking from the missed Session or elsewhere. An Exercise database with stats and recommendations is a later feature |
| 10 | Mid-workout guide | The log is a super simple list of input fields, with last time and a progressive-overload recommendation. The Exercise name links to a Guide page (full description, how-to, easier/harder, watch-outs, embedded video or link) |
| 11 | Dashboard layout | Yes: Workout first, then week progress, Streak, weight trend, PRs; others on their own screens |
| 12 | Game elements | Keep everything except XP and levels; shelve XP |
| 13 | Guide editing | Yes, but thinking of a more structured Exercise database that connects to a fully editable Guide |
| 14 | "Different types of plans" | Yes, potentially: several Plans, one active at a time |
| 15 | Nudges | Yes: missed Session, one short of target, pain warning; in-app only; keep in stage one for now |
| 16 | Per-set logging | Yes: one row per Set, prefilled from last time |
| 17 | Swapping an Exercise | Yes |
| 18 | Skip / add Exercises | Yes |
| 19 | RPE and pain | Add RPE as a small thing on each Exercise (good progress data); maybe an end-of-Workout pain form that can be skipped or not |
| 20 | Timers | Yes: optional rest timer, automatic duration |
| 21 | Forgot to log | Yes: back-dating allowed, marked "logged afterwards" |
| 22 | Weigh-in | Yes: standalone action with a Sunday prompt |
| 23 | Pain check detail | Yes |
| 24 | Stage 1 done? | The database should be set up now, as it's fundamental. Also wants easy LLM export of pain data, past and current Workouts, the Exercise plan, and an easy import for changes |

## Stage 2: practical (data model, rules, planning)

| Q | Topic | Answer |
|---|---|---|
| 25 | Schema first | Yes |
| 26 | Export format | Yes (JSON and Markdown), but must be doable on the phone only |
| 27 | Export picker | Yes; Guides potentially need a separate import function |
| 28 | Delivery | Yes (download, copy, share-sheet if cheap) |
| 29 | Import kinds | Yes: Backup; a Plan that can then be selected like other Plans; merging changes; and Guide imports |
| 30 | Import safety | Yes; maybe a specific JSON format so the app knows what goes where, part of a copyable prompt |
| 31 | Exercise identity | An ID linking to the Exercise's permanent database entry; import should look for the same or similar; the prompt may include existing Exercises to keep names the same |
| 32 | Tags | Yes; start the recommendation foundation now, have everything from the start |
| 33 | Prompt pack | Yes |
| 34 | Paste-first import | Yes |
| 35 | Exercise matching | Yes |
| 36 | Guide import | Yes |
| 37 | Recommendation scope | Yes: capture everything and ship tag-based "similar Exercises" now; the stats engine later |
| 38 | Format versioning | Yes |
| 39 | ADR 0001 | Yes |
| 40 | Video | Yes: a URL on each Guide, link first, embed later |
| 41 | PR rules | Yes, plus a "Max set" (3RM or 1RM attempt) as source of truth when it exists, otherwise the old rules |
| 42 | RPE and targets | Yes: RPE adjusts the Suggested target |
| 43 | Weeks and Streak | Yes, and a way to edit what constitutes the Streak |
| 44 | Partial Workouts | Yes |
| 45 | Badges | Yes: editable, progress calculated |
| 46 | Plan edits and history | Yes: edits go forward, history keeps what was planned |
| 47 | Phases | Yes, but a Phase should also have progress |
| 48 | Different Session today | Yes |
| 49 | Benchmarks | Yes: a benchmark Session, and the ability to "promote" any Exercise to a benchmark |
| 50 | Weekly review | Yes |
| 51 | What's editable | Yes: all of it, on the phone |
| 52 | Breaks | Yes, with a recommended move-around so missed Sessions are done on other days; some customisation as Sessions sometimes fall on different days |
| 53 | Phase "progress" | Both a progress display and optional goals |
| 54 | Benchmark control | Yes, but keep it light in the log UI |
| 55 | Moving Sessions in a week | Yes: per-week overrides, optionally a new default |
| 56 | Re-arrangement suggestions | Yes |
| 57 | Cardio logging | Yes: Set rows with pace |
| 58 | Weight section | Yes |
| 59 | Old data | Yes: weight history and the 27 Guides through Import |
| 60 | Starting Plan | Same for now, but easy to change later through a Plan import or edits; also wants to make a new Plan in the app |
| 61 | Stage 2 done | Yes |

## Stage 3: technical and workflow

| Q | Topic | Answer |
|---|---|---|
| 62 | Platform | PWA for now, later an installable app; asked whether it's better to build native now (answered with trade-offs) |
| 63 | Working style | Not a learning project: wants it built quickly and well, in sliced stages with a fixed workflow (to-spec, to-tickets, code review, thermonuclear review, fixes, PR with extra explanation), added to `CLAUDE.md` |
| 64 | Language | Prefers C# (used at work) but not strict; must translate easily to an installable app; wants a UI/UX phase before the build |
| 65 | C# route | Yes: C# core, Blazor WebAssembly PWA, MAUI Blazor Hybrid later |
| 66 | Tracks | Yes: owner makes a design document with Claude Design, to be added to the build plan later |
| 67 | Review skill | The thermonuclear skill file was provided (`SKILL.md`) and installed in `.claude/skills/` |
| 68 | Storage | Yes: IndexedDB document store behind an interface, loaded into memory |
| 69 | Data safety | Yes |
| 70 | Hosting | GitHub Pages for now, Cloudflare later |
| 71 | Layout | Yes: Core, Core.Tests, Ui, Web |
| 72 | Tests | Yes |
| 73 | CI and .NET | Yes; plus tickets must be sized to about 150-200k tokens per agent |
| 74 | Units | Yes, but add a unit switcher now (kg/lb, km/miles), as the owner likes to see both |
| 75 | Sync | Yes: later, separate project |
| 76 | Gym ergonomics | Yes |
| 77 | Stack ADRs | Yes: ADR 0002 and 0003 |
| 78 | Complete | Yes, confirmed; docs for everything, then a new session before `/to-spec` |

## Decisions that became ADRs
- [0001](adr/0001-schema-first-with-permanent-ids-and-versioned-exchange-format.md): schema first, permanent IDs, versioned exchange format.
- [0002](adr/0002-csharp-blazor-pwa-with-shared-ui-library.md): C# Blazor PWA with a shared Razor class library.
- [0003](adr/0003-local-first-indexeddb-storage-behind-an-interface.md): local-first IndexedDB storage behind an interface.
