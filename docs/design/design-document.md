# Fitness App: design document

Status: **v1.0 (2026-11-16): complete against brief §10b.** Every screen in brief §6 has a spec (§6) and mockups. Every flow in §7 has a spec and a diagram. The Workout log has a clickable prototype ([`assets/prototype-log.html`](assets/prototype-log.html)). Every asset is listed in [`assets/README.md`](assets/README.md).

Live references (private to the owner): the moodboard canvas (directions A–D, phone mockups, Clean theme) and the "Fitness App Style Guide — Keep & Clean" design system artifact. This document and `assets/` are the source of truth for build agents.

## 1. Summary and principles

A phone-first logbook with two **Themes** on one component set (ADR 0006):

- **Keep** (default): an old-school fantasy keep, inspired by old-school MMO interfaces and Arthurian linocut prints. Bevelled stone panels, rune-yellow pixel titles with a hard black shadow, orange labels, item-slot Badge tiles, ringed status orbs, zigzag tapestry borders.
- **Clean**: flat surfaces, the system font, rounded corners, no texture.

Each Theme has a dark and a light **Mode**. Token names are identical everywhere; only values change.

Decisions against brief §3:

| Principle | Decision |
|---|---|
| 1 Logging is the product | The theme dresses the frame and never the inputs. Set inputs are plain, high-contrast, 56px fields with tabular numbers in every Theme. Keep's intensity on the log is 1 out of 5. |
| 2 Simple list of inputs | One SetRow per Set, prefilled from last time; one primary action (Confirm Set) at the bottom. |
| 3 One-handed, big targets | 390 × 844 base, checked at 360. ConfirmBar fixed to the bottom with ±increment steppers. Minimum 44px, log inputs 56px, primary 64px. |
| 4 Quiet | Nudges on Home only, at most two. The tab bar is hidden during a Workout. |
| 5 Fast at the gym, motivating elsewhere | Theme intensity dial: log 1/5 · Settings, Import/Export 2/5 · Home, Week, Guides 3/5 · Trophies, Badges, calendar 5/5. |
| 6 Always explain | Suggested target always reads "target — reason" in a sunken well under Last/Best. |
| 7 Customisable | Theme and Mode are separate Settings; Badges get a phone-sized editor (C-22, S-13). |

No XP or levels: Keep borrows the *look* of an MMO (panels, slots, orbs), not its mechanics.

## 2. Answers to the open questions (brief §11)

| Question | Answer | Rejected |
|---|---|---|
| Primary navigation | Bottom tab bar with 5 destinations: Home, Week, Progress, Trophies, More (C-12). The Workout log is full-screen with no tab bar; it's entered via Start/Continue and left via Back (Workout stays in progress) or Finish. | Hamburger menu (two taps, hidden); 4 tabs (Trophies would get buried). |
| Set row entry | Both: tap a field to get the numeric keypad (select-all on focus), plus ±increment steppers in the ConfirmBar for the current Set. "Same as last" = the prefilled values; one tap on Confirm Set accepts them. | Steppers in every row (too wide at 360px); swipe-to-confirm (gesture risk). |
| Suggested target and Last/Best | One line for Last/Best, then a sunken well with the bold target and its reason, all above the Set rows (C-04). | Per-row ghost hints (crowded); a tooltip (hidden). |
| Benchmark and Exercise menu | One 44px menu button beside the Exercise name: Swap, Skip (with reason), Add Exercise, Mark as benchmark, View Guide. A benchmark Exercise shows a small banner glyph by its name. | Long-press (undiscoverable); a row of icons (clutter). |
| Missed / Partial / Skipped / Swapped | Glyph + word + colour (C-07): ○ Missed (dashed, neutral), ◐ Partial, — Skipped, ⇄ Swapped, plus ▲ PR, ✓ Done, ✚ Pain. The calendar uses the same glyphs inside day cells. | Colour-only heatmap states (fails "never colour alone"). |
| Draft-then-accept | A full-screen draft with changed items highlighted and one plain-language summary line, a sticky Apply/Cancel ConfirmBar, and per-item undo. Used by both the week re-arrangement (S-06, [`S-06-week-draft-dark.png`](assets/mockups/S-06-week-draft-dark.png)) and Import previews (C-16). | Silent auto-apply. |
| Pain check | A bottom sheet (C-13) after Finish with three large buttons: **No pain** (primary), **Yes**, **Skip**. Skip records "not answered" (distinct from "No"); Yes expands to body-area chips, severity, an optional side, Exercise and note. See [`S-02-log-pain-check-yes-dark.png`](assets/mockups/S-02-log-pain-check-yes-dark.png). | A modal form before the summary. |
| Game tone and Badge language | The Keep Theme *is* the game layer. Badges are item-slot tiles with linocut art and "x / y" progress; status orbs show Streak, Phase and race countdown; a calm unlock toast appears on the finish summary only. The Clean Theme keeps the same information without the art. | XP and levels; confetti. |

## 3. Navigation model and screen map

**Primary navigation:** a bottom tab bar on phones (C-12) and a left rail from 900 px. It has five destinations:

| Tab | Screen | Reaches |
|---|---|---|
| Home | S-01 Home | S-02 Workout log (Start / Continue / Do a different Session / Custom workout / Nudge Make up), S-08 Log weight sheet |
| Week | S-06 Week view | Move a Session, Add a Break, Suggested week draft |
| Progress | S-07 Progress · Benchmarks · Calendar | S-08 Weight (from the Body group and Home's weight panel) |
| Trophies | S-13 Badges · Bests · Calendar | Badge detail, Badge editor |
| More | menu | S-05 Plans + Plan editor, S-04 Exercise library, S-08 Weight, S-09 Weekly review, S-10 Export · Import · Backup · Prompt pack, S-11 Settings |

**Full screen with no tab bar:** S-02 Workout log. Enter via Start / Continue (Home), "Do a different Session", "Build a Custom workout" or a Nudge. Leave via Back (the Workout stays in progress; Home shows **Continue**) or **Finish** → Pain check → summary → Home.

**Pushed pages** (back arrow, no tab bar): S-03 Guide (from any Exercise name, the library or the Exercise menu), Badge detail and editor, the Import preview, New Exercise.

**First run:** S-12 replaces Home until a Plan exists (or a Backup is restored).

## 4. Design tokens

Machine-readable: [`assets/tokens.json`](assets/tokens.json) and [`assets/tokens.css`](assets/tokens.css), where `data-theme="keep|clean"` and `data-mode="dark|light"` go on `<html>`. Names below match those files exactly.

### 4.1 Colour (all four Theme × Mode sets)

| Token | Keep dark | Keep light | Clean dark | Clean light | Use |
|---|---|---|---|---|---|
| `bg` | `#2B2620` | `#D9CDB1` | `#111315` | `#F4F5F7` | Page ground behind panels. |
| `surface` | `#4A4237` | `#E8DFC8` | `#1B1E22` | `#FFFFFF` | Panels, cards, slabs, top bar, bottom action bar. |
| `surface-sunken` | `#2E2821` | `#CFC2A3` | `#0B0D0F` | `#E9ECEF` | Wells and slots: Suggested target box, Badge tiles, day tiles. |
| `surface-raised` | `#3A332A` | `#F2EBDA` | `#262A30` | `#E9ECEF` | Secondary buttons, steppers, icon buttons. |
| `bevel-light` | `#6B6152` | `#FFF8E6` | `#262A30` | `#E9ECEF` | Keep: lit (top/left) edge of a raised bevel. Clean: same as surface-raised (no bevel). |
| `bevel-dark` | `#1A1612` | `#8F8068` | `#262A30` | `#E9ECEF` | Keep: shadowed (bottom/right) edge of a bevel. Clean: same as surface-raised. |
| `outline` | `#141210` | `#3A3125` | `#111315` | `#F4F5F7` | Outer 2px frame line around Keep panels and buttons; Clean: matches bg (invisible). |
| `border` | `#5A5145` | `#A8997C` | `#3A4048` | `#C9CED4` | Dividers and non-interactive borders. |
| `text` | `#EFE6D2` | `#241D14` | `#F2F4F6` | `#15181B` | Body text on surface and bg. |
| `text-muted` | `#C9BDA3` | `#5A4D38` | `#A3ABB4` | `#535C66` | Secondary text: Last/Best, captions, labels. |
| `text-heading` | `#FFD83D` | `#6B2A1E` | `#F2F4F6` | `#15181B` | Display titles (Session, screen titles). Keep dark: rune yellow with the hard shadow. |
| `text-label` | `#FF981F` | `#8A3F00` | `#A3ABB4` | `#535C66` | Section labels and Exercise names in Keep (header orange). |
| `input-bg` | `#EFE6D2` | `#FFFFFF` | `#0B0D0F` | `#FFFFFF` | Set inputs. Always light-on-dark or dark-on-light at maximum contrast. |
| `input-text` | `#141210` | `#141210` | `#F2F4F6` | `#15181B` | Typed values in Set inputs. |
| `input-border` | `#141210` | `#3A3125` | `#6B737D` | `#7D8590` | Set input border (3:1 against its surroundings). |
| `input-pending` | `#5E5446` | `#6E6350` | `#A3ABB4` | `#535C66` | Prefilled values not yet confirmed (from last time). |
| `focus` | `#FFD83D` | `#6B2A1E` | `#FFC93C` | `#15181B` | Focus ring and the current Set's outline. |
| `primary` | `#FFD83D` | `#F2C230` | `#FFC93C` | `#F2B705` | Primary action fill: Confirm Set, Start. |
| `on-primary` | `#141210` | `#141210` | `#15181B` | `#15181B` | Text on primary. |
| `done` | `#2F7A4A` | `#2F7A4A` | `#277A4C` | `#277A4C` | Confirmed Set fill, done states. |
| `on-done` | `#FFFFFF` | `#FFFFFF` | `#FFFFFF` | `#FFFFFF` | Icon/text on done. |
| `done-text` | `#6FD08C` | `#1E6B3A` | `#5CC48A` | `#1E6B3F` | Done as text (set numbers, Last/Best in Keep). |
| `pr` | `#C93E23` | `#B83A22` | `#D2452F` | `#C23B26` | PR marker fill (shield/badge) and PR chips. |
| `pr-text` | `#FFA18A` | `#9E2F1A` | `#FF8A70` | `#C23B26` | PR as text or chip outline (PR!, recent PRs). |
| `on-pr` | `#FFFFFF` | `#FFFFFF` | `#FFFFFF` | `#FFFFFF` | Text on pr. |
| `state-partial` | `#E8A33A` | `#8A5800` | `#E0A341` | `#8A5800` | Partial workout (icon + label, never colour alone). |
| `state-skipped` | `#A89C86` | `#6E6350` | `#8A939C` | `#66707A` | Skipped exercise. |
| `state-swapped` | `#7FB5E6` | `#2F6FA8` | `#6FA8DC` | `#2F6FA8` | Swapped exercise. |
| `state-missed` | `#C9BDA3` | `#5A4D38` | `#B4BCC5` | `#535C66` | Missed session (neutral, never alarming). |
| `state-pain` | `#E88AB5` | `#A8336E` | `#E07AA8` | `#A8336E` | Pain check flags. |
| `session-strength` | `#E8A33A` | `#8A5800` | `#E0A341` | `#8A5800` | Session type: strength (calendar, week tiles). |
| `session-cardio` | `#7FB5E6` | `#2F6FA8` | `#5FA8D3` | `#2F6FA8` | Session type: cardio. |
| `session-body` | `#B9A0E0` | `#6E4FA8` | `#A98BD6` | `#6E4FA8` | Session type: body (Weigh-in). |
| `session-test` | `#FFD83D` | `#6B5400` | `#FFC93C` | `#6B5400` | Session type: test (Benchmark). |
| `nudge-bg` | `#CDBE97` | `#F2EBDA` | `#262A30` | `#FFF4D6` | Nudge box ground (Keep: chatbox vellum). |
| `nudge-text` | `#141210` | `#141210` | `#F2F4F6` | `#15181B` | Text in a Nudge. |
| `nudge-label` | `#8A1E10` | `#8A1E10` | `#FF9A80` | `#A12A12` | 'Nudge:' lead-in. |
| `orb-ring` | `#B08A3A` | `#8F8068` | `#3A4048` | `#AEB5BD` | Outer ring of status orbs. |
| `orb-track` | `#3A332A` | `#CFC2A3` | `#262A30` | `#E9ECEF` | Unfilled track of an orb or progress bar. |
| `tab-active` | `#6B2A1E` | `#6B2A1E` | `#262A30` | `#15181B` | Selected tab fill. |
| `on-tab-active` | `#FFD83D` | `#FFF8E6` | `#FFC93C` | `#FFFFFF` | Icon on the selected tab. |
| `art-green` | `#2F7A4A` | `#2F7A4A` | `#2F7A4A` | `#2F7A4A` | Badge art: knight green. |
| `art-vermilion` | `#D9472B` | `#D9472B` | `#D9472B` | `#D9472B` | Badge art: vermilion. |
| `art-ochre` | `#E8A33A` | `#E8A33A` | `#E8A33A` | `#E8A33A` | Badge art: ochre. |
| `art-ink` | `#141210` | `#141210` | `#141210` | `#141210` | Badge art: ink ground. |

### 4.2 Contrast (WCAG 2.1 AA, computed from the values above)

| Pair (foreground on background) | Min | Keep dark | Keep light | Clean dark | Clean light |
|---|---|---|---|---|---|
| `text` on `surface` | 4.5 | 8.0 | 12.5 | 15.2 | 17.8 |
| `text-muted` on `surface` | 4.5 | 5.3 | 6.2 | 7.2 | 6.8 |
| `text-heading` on `surface` | 3.0 | 7.1 | 8.0 | 15.2 | 17.8 |
| `text-label` on `surface` | 4.5 | 4.6 | 5.7 | 7.2 | 6.8 |
| `input-text` on `input-bg` | 7 | 15.1 | 18.7 | 17.7 | 17.8 |
| `input-pending` on `input-bg` | 4.5 | 6.0 | 5.9 | 8.4 | 6.8 |
| Set input boundary: `input-bg` fill or `input-border`, whichever is higher, on `surface` | 3 | 8.0 (fill) | 9.6 (border) | 3.5 (border) | 3.7 (border) |
| `focus` on `surface` | 3 | 7.1 | 8.0 | 10.9 | 17.8 |
| `on-primary` on `primary` | 4.5 | 13.5 | 11.2 | 11.6 | 9.8 |
| `on-done` on `done` | 4.5 | 5.2 | 5.2 | 5.3 | 5.3 |
| `done-text` on `surface` | 4.5 | 5.2 | 4.9 | 7.7 | 6.5 |
| `pr-text` on `surface` | 4.5 | 5.0 | 5.5 | 7.3 | 5.3 |
| `on-pr` on `pr` | 4.5 | 5.0 | 5.7 | 4.5 | 5.3 |
| `state-partial` on `surface` | 3 | 4.6 | 4.6 | 7.6 | 6.0 |
| `state-skipped` on `surface` | 3 | 3.7 | 4.4 | 5.4 | 5.0 |
| `state-swapped` on `surface` | 3 | 4.5 | 4.0 | 6.6 | 5.3 |
| `state-missed` on `surface` | 3 | 5.3 | 6.2 | 8.7 | 6.8 |
| `state-pain` on `surface` | 3 | 4.1 | 4.7 | 6.0 | 6.2 |
| `session-strength` on `surface-sunken` | 3 | 6.8 | 3.4 | 8.8 | 5.1 |
| `session-cardio` on `surface-sunken` | 3 | 6.7 | 3.0 | 7.4 | 4.5 |
| `nudge-text` on `nudge-bg` | 4.5 | 10.2 | 15.7 | 13.1 | 16.3 |
| `nudge-label` on `nudge-bg` | 4.5 | 5.0 | 7.8 | 7.0 | 6.7 |
| `on-tab-active` on `tab-active` | 4.5 | 7.7 | 10.0 | 9.4 | 17.8 |

Text needs 4.5:1 (display titles at 24px+ need 3:1). Control boundaries, focus rings and state glyphs need 3:1. Primary buttons are identified by their label.

### 4.3 Elevation and bevels (shadow tokens)

| Token | Keep dark | Keep light | Clean dark | Clean light | Use |
|---|---|---|---|---|---|
| `shadow-panel` | `inset 3px 3px 0 #6B6152, inset -3px -3px 0 #1A1612, 0 0 0 2px #141210` | `inset 3px 3px 0 #FFF8E6, inset -3px -3px 0 #8F8068, 0 0 0 2px #3A3125` | `none` | `0 1px 2px rgba(21,24,27,0.08)` | Panels, slabs, cards: Today's Workout, Exercise header, Badge grid frame. |
| `shadow-raised` | `inset 2px 2px 0 #6B6152, inset -2px -2px 0 #1A1612, 0 0 0 2px #141210` | `inset 2px 2px 0 #FFF8E6, inset -2px -2px 0 #8F8068, 0 0 0 2px #3A3125` | `none` | `none` | Secondary buttons, steppers, icon buttons, tabs. |
| `shadow-primary` | `inset 3px 3px 0 #FFF0A8, inset -3px -3px 0 #8A6D1F, 0 0 0 2px #141210` | `inset 3px 3px 0 #FFF0A8, inset -3px -3px 0 #8A6D1F, 0 0 0 2px #3A3125` | `none` | `none` | Primary button bevel (Confirm Set, Start). |
| `shadow-sunken` | `inset 2px 2px 0 #1A1612, inset -2px -2px 0 #5A5145` | `inset 2px 2px 0 #8F8068, inset -2px -2px 0 #FFF8E6` | `none` | `none` | Wells and slots: Suggested target, Badge tiles, day tiles. |
| `shadow-sheet` | `0 -3px 0 #6B6152` | `0 -3px 0 #FFF8E6` | `0 -8px 24px rgba(0,0,0,0.35)` | `0 -4px 16px rgba(21,24,27,0.10)` | Bottom action bar and tab bar edge. |
| `shadow-heading` | `2px 2px 0 #000000` | `1px 1px 0 #FFF8E6` | `none` | `none` | Text-shadow on display titles. Keep dark: the hard black shadow that makes rune-yellow readable. |

Keep-only textures: stone grain [`stone-texture.svg`](assets/graphics/stone-texture.svg), tiled behind `bg` at about 20% (optional; drop it if it costs frames), and the zigzag band [`tapestry-border.svg`](assets/graphics/tapestry-border.svg), tiled horizontally on TodayCard, the race card and the unlock toast.

### 4.4 Type

Fonts:
- **Jersey 15** (Google Fonts / Soft Type, SIL Open Font License 1.1; one weight, 400) is Keep's display and label face. It's self-hosted: [`assets/fonts/jersey-15-latin-400-normal.woff2`](assets/fonts/jersey-15-latin-400-normal.woff2) (16 KB), licence [`assets/fonts/OFL-Jersey15.txt`](assets/fonts/OFL-Jersey15.txt). It was chosen over Pixelify Sans, which was tried first: in Pixelify, C/O/0, B/8 and S/5 were too alike at title sizes.
- **Everything else** uses the system stack `system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif`, including every number typed or read mid-set. No font download is needed to log.
- **Per-Theme switches** (in `tokens.css`): `--font-display` / `--font-label` (Keep: Jersey 15, Clean: system), `--w-display` / `--w-label` (Keep 400/400, Clean 700/600) and `--display-scale` (Keep 1.15, because Jersey runs narrow; Clean 1). Components multiply display sizes by `--display-scale`. `--font-display` is the single swap point for a personal build.
- Ligatures and contextual alternates are turned off on the display face so labels read letter by letter.

| Style | Size / line (Clean; Keep × 1.15) | Weight (Keep / Clean) | Family | Use |
|---|---|---|---|---|
| `display-xl` | 32 / 36 | 400 / 700 | display | Screen titles |
| `display-l` | 26 / 30 | 400 / 700 | display | Exercise name on the log; Today's Workout title |
| `display-m` | 20 / 24 | 400 / 700 | display | Card titles, sheet titles, Badge names |
| `label` | 14 / 18 | 400 / 600 | display | Section labels, tabs, chips, button labels |
| `body` | 16 / 24 | 400 | system | Guides, Plan editor, Settings |
| `body-s` | 14 / 20 | 400 | system | Exercise header lines, Suggested target, Nudges |
| `caption` | 12 / 16 | 500 | system | Column heads, orb captions (never needed mid-set) |
| `num-input` | 24 / 28 | 800, tabular | system | Set inputs, in every Theme |
| `num-stat` | 20 / 24 | 700, tabular | system | Stats, finish summary |

### 4.5 Spacing

| Token | Value | Use |
|---|---|---|
| `space-1` | `4px` | Icon-to-label gap. |
| `space-2` | `8px` | Gap between Set inputs and between tiles. |
| `space-3` | `12px` | Screen side padding on phones; gap between Set rows. |
| `space-4` | `16px` | Card padding. |
| `space-5` | `20px` | Gap between cards. |
| `space-6` | `24px` | Section gap. |
| `space-8` | `32px` | Large section breaks; desktop gutters. |

### 4.6 Radius

| Token | Value | Use |
|---|---|---|
| `radius-none` | `0px` | Keep: every corner is square stone. Bundle maps all --r-* to this in Keep. |
| `radius-control` | `12px` | Clean: buttons, Set inputs, tiles. |
| `radius-card` | `16px` | Clean: cards and panels. |
| `radius-sheet` | `20px` | Clean: top corners of the bottom action bar. |
| `radius-round` | `9999px` | Both: orbs, avatar-like discs, pills. |

Keep maps every component radius (`--r-control`, `--r-card`, `--r-sheet`) to 0. Clean maps them to 12, 16 and 20.

### 4.7 Touch targets and sizes

| Token | Value | Use |
|---|---|---|
| `tap-min` | `44px` | Smallest touch target anywhere. |
| `tap-log` | `56px` | Set inputs and the Set state button. |
| `tap-primary` | `64px` | Confirm Set, steppers, Start. |
| `bar-tab` | `72px` | Bottom tab bar height incl. safe area padding. |
| `phone-base` | `390px` | Design width. |
| `phone-min` | `360px` | Smallest supported width; the log must fit with no horizontal scroll. |
| `column-log` | `430px` | Widest the log column grows on a tablet or desktop. |

### 4.8 Borders and opacity

| Token | Value | Use |
|---|---|---|
| `border-thin` | `2px` | Inputs, outlines, chips. |
| `border-thick` | `3px` | Current Set outline, focus ring. |
| `outline-offset` | `2px` | Gap between a focus ring or the current-Set outline and its control. |

| Token | Value | Use |
|---|---|---|
| `opacity-upcoming` | `0.55` | Set rows not yet reached. |
| `opacity-locked` | `0.45` | Locked Badge artwork. |

### 4.9 Motion

There is no motion token family. Buttons move 1px when pressed; the Badge unlock toast fades in (200 ms) on the finish summary only. With `prefers-reduced-motion: reduce`, everything is instant.

### 4.10 Colour encoding

- **Session types:** `session-strength` (ochre), `session-cardio` (blue), `session-body` (violet), `session-test` (yellow), always shown with the Session code letter.
- **States:** see C-07. Every state has a glyph and a word as well as a colour.

### 4.11 Iconography

All icons are original, 24 × 24 viewBox, single colour via `currentColor`, 2px square-capped strokes (solid fills only where a shape needs weight). The same files are used in both Themes. Decorative icons get `aria-hidden="true"`; icon-only buttons carry an `aria-label`. No third-party icon set is used.

| Icon | File | Meaning | Used in |
|---|---|---|---|
| add-exercise | [`icon-add-exercise.svg`](assets/icons/icon-add-exercise.svg) | Add an extra Exercise | Exercise menu |
| back | [`icon-back.svg`](assets/icons/icon-back.svg) | Back / close a pushed page | S-02, S-03 and pushed pages |
| backup | [`icon-backup.svg`](assets/icons/icon-backup.svg) | Backup (chest) | S-10, S-11 |
| badge | [`icon-badge.svg`](assets/icons/icon-badge.svg) | Badge (rosette) | Badge references |
| benchmark | [`icon-benchmark.svg`](assets/icons/icon-benchmark.svg) | Benchmark exercise (banner) | C-04 tag, Exercise menu |
| break | [`icon-break.svg`](assets/icons/icon-break.svg) | Break (banked campfire) | S-06 Add a Break |
| calendar | [`icon-calendar.svg`](assets/icons/icon-calendar.svg) | Calendar / date | C-15 |
| check | [`icon-check.svg`](assets/icons/icon-check.svg) | Done / confirmed Set | C-02 done, save |
| close | [`icon-close.svg`](assets/icons/icon-close.svg) | Close / dismiss | C-08 dismiss |
| copy | [`icon-copy.svg`](assets/icons/icon-copy.svg) | Copy to clipboard | S-10 |
| delete | [`icon-delete.svg`](assets/icons/icon-delete.svg) | Delete | Discard, Badge editor |
| download | [`icon-download.svg`](assets/icons/icon-download.svg) | Download file | S-10 |
| edit | [`icon-edit.svg`](assets/icons/icon-edit.svg) | Edit | S-03, S-05, S-13 |
| export | [`icon-export.svg`](assets/icons/icon-export.svg) | Export | S-09, S-10 |
| filter | [`icon-filter.svg`](assets/icons/icon-filter.svg) | Filter by tag | S-04 |
| guide | [`icon-guide.svg`](assets/icons/icon-guide.svg) | Guide (open book) | Exercise menu |
| home | [`icon-home.svg`](assets/icons/icon-home.svg) | Tab: Home (keep) | C-12 |
| import | [`icon-import.svg`](assets/icons/icon-import.svg) | Import | S-10, S-12 |
| info | [`icon-info.svg`](assets/icons/icon-info.svg) | Info / why | banners |
| install | [`icon-install.svg`](assets/icons/icon-install.svg) | Install to home screen | S-11, S-12 |
| lock | [`icon-lock.svg`](assets/icons/icon-lock.svg) | Locked (Badge) | locked Badge |
| logged-afterwards | [`icon-logged-afterwards.svg`](assets/icons/icon-logged-afterwards.svg) | Logged afterwards (back-dated) | S-02 banner |
| max-set | [`icon-max-set.svg`](assets/icons/icon-max-set.svg) | Max set (axe) | Max set flag |
| menu-more | [`icon-menu-more.svg`](assets/icons/icon-menu-more.svg) | Exercise menu / more actions | C-04, S-02 menus |
| minus | [`icon-minus.svg`](assets/icons/icon-minus.svg) | Decrease | steppers (text alternative) |
| missed | [`icon-missed.svg`](assets/icons/icon-missed.svg) | Missed session (dashed ring) | C-07, C-18 |
| more | [`icon-more.svg`](assets/icons/icon-more.svg) | Tab: More | C-12 |
| note | [`icon-note.svg`](assets/icons/icon-note.svg) | My notes | S-03 My notes |
| nudge | [`icon-nudge.svg`](assets/icons/icon-nudge.svg) | Nudge (speech scroll) | C-08 |
| offline | [`icon-offline.svg`](assets/icons/icon-offline.svg) | Offline (needs a connection) | C-21 offline |
| pain | [`icon-pain.svg`](assets/icons/icon-pain.svg) | Pain (cross) | C-07, C-13 |
| partial | [`icon-partial.svg`](assets/icons/icon-partial.svg) | Partial workout (half disc) | C-07, C-18 |
| phase | [`icon-phase.svg`](assets/icons/icon-phase.svg) | Phase (castle tower) | Phase rows |
| plus | [`icon-plus.svg`](assets/icons/icon-plus.svg) | Add | add buttons |
| pr | [`icon-pr.svg`](assets/icons/icon-pr.svg) | PR (shield with peak) | C-07 |
| progress | [`icon-progress.svg`](assets/icons/icon-progress.svg) | Tab: Progress | C-12, library rows |
| race | [`icon-race.svg`](assets/icons/icon-race.svg) | Race countdown (flag) | race countdown |
| reorder | [`icon-reorder.svg`](assets/icons/icon-reorder.svg) | Drag to reorder | C-20 |
| search | [`icon-search.svg`](assets/icons/icon-search.svg) | Search | S-04 |
| settings | [`icon-settings.svg`](assets/icons/icon-settings.svg) | Settings (cog) | More menu |
| share | [`icon-share.svg`](assets/icons/icon-share.svg) | Share sheet | S-10 |
| skip | [`icon-skip.svg`](assets/icons/icon-skip.svg) | Skip an Exercise (Skipped state) | C-07, Exercise menu |
| storage | [`icon-storage.svg`](assets/icons/icon-storage.svg) | Persistent storage | S-11, S-12 |
| streak | [`icon-streak.svg`](assets/icons/icon-streak.svg) | Streak (torch flame) | C-11, summary |
| swap | [`icon-swap.svg`](assets/icons/icon-swap.svg) | Swap an Exercise (Swapped state) | C-14, C-07, S-06 move |
| timer | [`icon-timer.svg`](assets/icons/icon-timer.svg) | Rest timer (hourglass) | rest timer, picker icon |
| trophies | [`icon-trophies.svg`](assets/icons/icon-trophies.svg) | Tab: Trophies | C-12 |
| undo | [`icon-undo.svg`](assets/icons/icon-undo.svg) | Undo (draft item) | C-16, S-06 draft |
| video | [`icon-video.svg`](assets/icons/icon-video.svg) | Video link | S-03 |
| warning | [`icon-warning.svg`](assets/icons/icon-warning.svg) | Format warning | C-02 error |
| week | [`icon-week.svg`](assets/icons/icon-week.svg) | Tab: Week | C-12 |
| weight | [`icon-weight.svg`](assets/icons/icon-weight.svg) | Weigh-in (scales) | S-01 Log weight, S-08 |

## 5. Component inventory

Styles: [`assets/components.css`](assets/components.css) (plain CSS, `fa-` prefix, built on the tokens). `.fa-root` is a preview helper only.


### C-01 Button

Bevelled stone in Keep, flat rounded in Clean; one set of classes for both.

| Variant | Class | Size | Use |
|---|---|---|---|
| Primary | `fa-btn fa-btn--primary` | `tap-primary` (64px) tall | One per screen: Confirm Set, Start, Apply |
| Secondary | `fa-btn fa-btn--secondary` | ≥ `tap-min` | Do a different Session, Back up, Swap |
| Stepper | `fa-btn fa-btn--stepper` | 60 × 64 | −/+ weight increment beside Confirm |
| Icon | `fa-btn fa-btn--icon` | 44 × 44 | Back, rest timer, Exercise menu; always `aria-label` |

States: focus = 3px `focus` outline with 2px offset; disabled = 45% opacity; pressed = 1px nudge down (none with reduced motion).
Primary sits in the bottom 25% of the screen (thumb zone). Labels use the glossary verbs: Start, Continue, Confirm Set, Finish.

### C-02 SetRow

One row per Set: number, two fast inputs, and a state cell. Prefilled from last time; confirm or edit.

- Grid: `24px 1fr 14px 1fr 56px`, gap `space-2`. Fits 360px with `space-3` side padding.
- Inputs: `fa-input`, 56px tall, `num-input` style, `inputmode="decimal"` (weight) / `"numeric"` (reps), select-all on focus.
- States: `--done` (green tick button, tap to edit again), PR (shield in Keep, "PR!" block in Clean, plus `aria-label` on the reps input), `--current` (3px `focus` outline on the field being edited), `fa-input--pending` (prefilled, muted until confirmed), `--upcoming` (55% opacity), `fa-input--error` (vermilion border + plain-language message below, `aria-invalid`).
- Log kinds reuse the same grid: weight × reps, `bw` × reps, kg-per-hand × metres, reps only (one wide input), time `m:ss` (one wide input, lower is better), distance + time (pace shown under the row), tick (one wide toggle).
- Never colour alone: done has a tick, PR has the word PR, error has text.

### C-03 PR marker
Graphic: [`pr-marker.svg`](assets/graphics/pr-marker.svg).
Shown in a Set row's state cell when the Set strictly beats the previous best (time-based: lower is better; a Max set is the source of truth).
- Keep: a vermilion shield (`pr` fill, `outline` stroke, `art-ochre` dashed trim) with "PR" in the pixel face. Clean: a `pr` block with "PR!".
- Always paired with text: the visible word plus `aria-label` "PR" on the marker and ", PR" on the reps input label.
- On the finish summary and Home "recent PRs", each PR shows the previous best: "Goblet squat 22 × 9 (was 22 × 8)".

### C-04 ExerciseHeader

The current Exercise: tappable name (opens its Guide), planned sets × reps and cue, Last and Best, and the Suggested target with its one-line reason.

- Name uses `display-l` in the display face; Keep colours it `text-label` (orange), Clean uses `text`.
- Last/Best on one line, `body-s`. Suggested target sits in a sunken well; the target itself is bold, the reason follows a dash.
- The menu button holds Swap, Skip (with reason), Add Exercise, Mark as benchmark, View Guide. Light, secondary, never a modal mid-set.

### C-05 RpeSelector

Optional per-Exercise effort. Five 44px toggles (6–10) cover the useful range; a long-press or "more" reveals 1–5 if ever needed.

- `aria-pressed` toggles; tapping the selected value clears it.
- Never blocks Confirm. Feeds the Suggested target (hold at 9–10, bigger jump at ≤6).

### C-06 ConfirmBar

Fixed to the bottom of the Workout log. Holds the Up next strip, the optional rest timer readout, and the one primary action with steppers either side.

- Steppers change the current Set's weight by the Plan's increment (2 kg default; shows lb when units are lb).
- After the last Set of the last Exercise the primary label becomes **Finish**; Partial workouts finish from the header menu with a plain summary ("5 of 8 done; the rest are recorded as skipped").
- Respects the safe-area inset at the bottom.

### C-07 StateChips

How every state is shown in lists, the Week view and the calendar. Each has a distinct glyph AND a word AND a colour, so no state relies on colour.

| State | Token | Glyph | Notes |
|---|---|---|---|
| PR | `pr-text` | ▲ | Also the shield marker on Set rows |
| Done | `done-text` | ✓ | |
| Partial workout | `state-partial` | ◐ | Half-filled in the calendar |
| Skipped exercise | `state-skipped` | — | Reason shown in detail |
| Swapped exercise | `state-swapped` | ⇄ | Shows "was: X" in detail |
| Missed session | `state-missed` | ○ | Dashed outline; neutral, never red |
| Pain | `state-pain` | ✚ | From the Pain check |

Glyphs here are text placeholders; the shipped set is 24×24 `currentColor` SVG icons (`icon-pr.svg` etc.).

### C-08 Nudge

A calm in-app message on Home only (Keep: styled like an old chatbox). Never during a Workout, never a push notification.

- At most two visible; each has one action and a dismiss.
- Copy is neutral and specific: "Session A was missed: make it up?", "You flagged knee pain twice this week: consider an easier variation".

### C-09 TodayCard

The hero of Home. States: today's Session (Start), in progress (Continue), rest day ("Rest day: next is Session C on Wednesday"), done (summary with duration, Sets, PRs), Break active (quiet, no button), no Plan (first-run Import).

- Keep adds the zigzag tapestry band on top; Clean hides it.
- The two alternatives sit directly under the card, never inside it.

### C-10 BadgeTile

Badges laid out like an old-school stats panel: three columns of slots, each with artwork and "x / y" progress. Tap opens the Badge detail.

- Unlocked: green inner outline, full-colour art. In progress: full art, yellow value. Locked: art at `opacity-locked`, muted value.
- Order: unlocked first, then closest to unlocking (brief §6.13).
- Name truncates with an ellipsis; the full name is in `aria-label` and the detail view.
- Art: linocut family (green, vermilion, ochre on ink) from the `art-*` tokens, so it reads the same in every Theme.


**Starter Badge art** (96 × 112 shield family: linocut glyph, ribbon text in the system font, works on dark and light; locked = greys + lock):

| Badge | Unlocked | Locked |
|---|---|---|
| First Workout | [`badge-workouts-1.svg`](assets/badges/badge-workouts-1.svg) | [`badge-workouts-1-locked.svg`](assets/badges/badge-workouts-1-locked.svg) |
| 10 Workouts | [`badge-workouts-10.svg`](assets/badges/badge-workouts-10.svg) | [`badge-workouts-10-locked.svg`](assets/badges/badge-workouts-10-locked.svg) |
| 25 Workouts | [`badge-workouts-25.svg`](assets/badges/badge-workouts-25.svg) | [`badge-workouts-25-locked.svg`](assets/badges/badge-workouts-25-locked.svg) |
| 50 Workouts | [`badge-workouts-50.svg`](assets/badges/badge-workouts-50.svg) | [`badge-workouts-50-locked.svg`](assets/badges/badge-workouts-50-locked.svg) |
| 100 Workouts | [`badge-workouts-100.svg`](assets/badges/badge-workouts-100.svg) | [`badge-workouts-100-locked.svg`](assets/badges/badge-workouts-100-locked.svg) |
| 2-week Streak | [`badge-streak-2.svg`](assets/badges/badge-streak-2.svg) | [`badge-streak-2-locked.svg`](assets/badges/badge-streak-2-locked.svg) |
| 4-week Streak | [`badge-streak-4.svg`](assets/badges/badge-streak-4.svg) | [`badge-streak-4-locked.svg`](assets/badges/badge-streak-4-locked.svg) |
| 8-week Streak | [`badge-streak-8.svg`](assets/badges/badge-streak-8.svg) | [`badge-streak-8-locked.svg`](assets/badges/badge-streak-8-locked.svg) |
| 12-week Streak | [`badge-streak-12.svg`](assets/badges/badge-streak-12.svg) | [`badge-streak-12-locked.svg`](assets/badges/badge-streak-12-locked.svg) |
| Best week (5 of 5) | [`badge-best-week.svg`](assets/badges/badge-best-week.svg) | [`badge-best-week-locked.svg`](assets/badges/badge-best-week-locked.svg) |
| 10 PRs | [`badge-prs-10.svg`](assets/badges/badge-prs-10.svg) | [`badge-prs-10-locked.svg`](assets/badges/badge-prs-10-locked.svg) |
| 25 PRs | [`badge-prs-25.svg`](assets/badges/badge-prs-25.svg) | [`badge-prs-25-locked.svg`](assets/badges/badge-prs-25-locked.svg) |
| 50 PRs | [`badge-prs-50.svg`](assets/badges/badge-prs-50.svg) | [`badge-prs-50-locked.svg`](assets/badges/badge-prs-50-locked.svg) |
| First benchmark test | [`badge-first-benchmark.svg`](assets/badges/badge-first-benchmark.svg) | [`badge-first-benchmark-locked.svg`](assets/badges/badge-first-benchmark-locked.svg) |
| Phase complete | [`badge-phase-complete.svg`](assets/badges/badge-phase-complete.svg) | [`badge-phase-complete-locked.svg`](assets/badges/badge-phase-complete-locked.svg) |
| First Max set | [`badge-first-max-set.svg`](assets/badges/badge-first-max-set.svg) | [`badge-first-max-set-locked.svg`](assets/badges/badge-first-max-set-locked.svg) |
| Weight goal | [`badge-weight-goal.svg`](assets/badges/badge-weight-goal.svg) | [`badge-weight-goal-locked.svg`](assets/badges/badge-weight-goal-locked.svg) |
| Carry 1 km | [`badge-carry-1km.svg`](assets/badges/badge-carry-1km.svg) | [`badge-carry-1km-locked.svg`](assets/badges/badge-carry-1km-locked.svg) |
| Run 50 km | [`badge-run-50km.svg`](assets/badges/badge-run-50km.svg) | [`badge-run-50km-locked.svg`](assets/badges/badge-run-50km-locked.svg) |
| Back after a Break | [`badge-back-after-break.svg`](assets/badges/badge-back-after-break.svg) | [`badge-back-after-break-locked.svg`](assets/badges/badge-back-after-break-locked.svg) |
| Custom Badge template | [`badge-frame-template.svg`](assets/badges/badge-frame-template.svg) | swap to the locked greys |

### C-11 StatusOrb

A ringed gauge for one number with progress: Streak (weeks, ring = this week's progress toward the minimum), Phase (week x of y), race countdown (days; ring = share of the Plan elapsed).

- Pure SVG: a ring, a track and one arc (`stroke-dasharray` in the build). The value is SVG text plus an `aria-label` on the SVG.
- During a Break the Streak orb keeps its number and greys its arc (`orb-track`), with the caption "On a Break"; it never looks broken mid-week.

Related graphics: Streak mark [`streak-mark.svg`](assets/graphics/streak-mark.svg) and its Break state [`streak-mark-break.svg`](assets/graphics/streak-mark-break.svg) (torch banked to embers, never out, captioned "On a Break"); race countdown card [`race-countdown.svg`](assets/graphics/race-countdown.svg) (days, weeks, date, Plan elapsed, current Phase; generic flag only, no HYROX branding). All static; no motion.

### C-12 TabBar

Five destinations: Home, Week, Progress, Trophies, More (Plans, Exercise library, Weight, Weekly review, Export/Import, Settings).

- Hidden on the Workout log: the log is entered from Home (Start/Continue) and left via Back or Finish, so a stray tap can't leave a Workout.
- Each tab: 24px icon + 11px label, `aria-current="page"` on the active one (Keep: red-brown inset like an old selected tab).

### C-13 PainCheckSheet
Purpose: the optional "Any pain today?" step after Finish.
- Anatomy: bottom sheet with the title, three equal buttons **No** (primary), **Yes**, **Skip**. Yes expands in place: body area chips (Back, Shoulder, Wrist, Knee, Other; multi-select), severity chips (Mild, Moderate, Sharp; single), "During which Exercise?" (select, optional, lists this Workout's Exercises), "Which side?" (Left/Right/Both, optional), Note (textarea, optional), then **Save and see summary**.
- States: closed; open; Yes expanded; saving. **Skip** records "not answered", which is different from **No**.
- Accessibility: `role="dialog"`, `aria-modal`, focus starts on **No**, Escape = Skip, chips are toggle buttons with `aria-pressed`.
- Mockups: [`S-02-log-pain-check-dark.png`](assets/mockups/S-02-log-pain-check-dark.png), [`S-02-log-pain-check-yes-dark.png`](assets/mockups/S-02-log-pain-check-yes-dark.png).

### C-14 ExercisePicker (Swap / Add)
Purpose: choose an Exercise from the library.
- Anatomy: sheet title ("Swap Pull-up" / "Add an Exercise"); **Similar** group first (shares the most tags with the current Exercise, highest first), then **All Exercises**; each row = icon by log kind, name, tags; Cancel.
- Behaviour: Swap replaces the Exercise in place, keeps the planned sets, records "Swapped · was X". Add inserts after the current Exercise and marks it "+ Added". Exercises already in the Workout are hidden.
- States: default, search (S-04 search box at the top when the library has more than 20 Exercises), empty search ("No match. Add a new Exercise").
- Mockup: [`S-02-log-swap-picker-dark.png`](assets/mockups/S-02-log-swap-picker-dark.png).

### C-15 DateRangePicker
Purpose: pick a range (Export, Break, Phase dates).
- Anatomy: a field showing the range in words ("Last 12 weeks · 24 Aug – 16 Nov"); tap opens a sheet with presets (Last week, Last 4 weeks, Last 12 weeks, Whole Plan, Custom) and, for Custom, two native date inputs (`<input type="date">`).
- Rules: start ≤ end, otherwise an inline message; never a custom calendar widget (native inputs are accessible and cheap).
- Used in: [`S-10-export-dark.png`](assets/mockups/S-10-export-dark.png), [`S-06-week-break-dark.png`](assets/mockups/S-06-week-break-dark.png), [`S-05-plan-editor-phases-dark.png`](assets/mockups/S-05-plan-editor-phases-dark.png).

### C-16 ImportPreview (diff list)
Purpose: show in plain language what an import will do before **Apply**.
- Anatomy: summary panel ("Creates a new Plan called X, adds 3 Exercises, changes the target on 2, touches no Workouts"), then one row per change with an inset left bar: green = add, ochre = change (old → new), blue = needs a decision ("Did you mean Goblet squat?" **Yes / New**; unknown names **Add**); a Backup note; **Cancel / Apply**.
- Guide imports: per Guide **Keep mine / Replace** (or **Add** when new).
- Errors: nothing applied; a banner lists field-level problems ("Session 2, Exercise 4: reps must be a number (got "eight")").
- Mockups: [`S-10-import-preview-dark.png`](assets/mockups/S-10-import-preview-dark.png), [`S-10-import-preview-light.png`](assets/mockups/S-10-import-preview-light.png), [`S-10-import-error-dark.png`](assets/mockups/S-10-import-error-dark.png), [`S-10-import-guides-dark.png`](assets/mockups/S-10-import-guides-dark.png).

### C-17 LineChart
Purpose: one Exercise (or body weight) over time.
- Spec: plain inline SVG, viewBox 358 × 220, plot inset left 40 / right 12 / top 30 / bottom 40. Gridlines `border` 1px; axis labels `caption` in `text-muted`; line `primary` 3px; normal points r=4 `primary`; **PR points** r=6 `pr` with a 2px white ring; **best point** labelled ("Best 33.3 · +7.3 since W1"); gaps are breaks, never interpolated; Phase bands behind the plot at 16–18% opacity of `done` / `art-ochre`.
- Weight variant: every weigh-in as r=2.5 `text-muted` dots, the **7-entry rolling average** as the main line, optional dashed goal line in `done-text`, lowest weigh-in ringed.
- Interaction: tap a point for a tooltip (date, value); a "Table" toggle shows the same data as a list (screen readers).
- Samples: [`chart-line.svg`](assets/charts/chart-line.svg), [`chart-weight.svg`](assets/charts/chart-weight.svg).

### C-18 CalendarHeatmap
Purpose: the whole Plan from start to race day.
- Spec: columns = weeks (Monday at the top), cells 26 × 26 with 4px gaps, `surface-sunken` empty slots. Fill = Session type colour (`session-*`) when done; **Partial** = bottom half filled plus outline; **Missed** = dashed outline in `state-missed`; **Break** = hatched; **PR** = small `pr` triangle in the top-right corner; today = 2px `focus` outline; future days empty. A Phase strip runs above the weeks (Phase colour, name, length). Legend below.
- Interaction: horizontal scroll in a panel; tap a day for its Workout. Screen readers get one label per day ("Wed 18 Nov: Session C, done, 1 PR").
- Sample: [`chart-heatmap.svg`](assets/charts/chart-heatmap.svg).

### C-19 ProgressBar
Purpose: x of y toward a Badge, goal or Phase.
- Spec: label (`label` face, `text-label`) left, value right (`text-heading`), 12px bar with a 2px `outline` border on an `orb-track` track; fill in the state colour. Value text always shown.
- Sample: [`chart-progress-bar.svg`](assets/charts/chart-progress-bar.svg).

### C-20 FormPatterns (Plan editor rows)
- **Editable row:** list row with drag handle (`icon-reorder`, decorative), name and summary, Edit button. Tapping Edit expands an inline panel: Sets / Reps / Step (numeric inputs), Cue, Benchmark toggle, **Move up / Move down / Remove** text buttons (accessible alternative to drag).
- **Toggle:** 52 × 30 switch (square in Keep), `role="switch"`, `aria-checked`.
- **Segmented choice:** `pick` buttons with `aria-pressed` (tabs within a screen, Theme/Mode, units).
- **Banner:** info icon + one line ("Edits apply going forward. Past Workouts are unchanged.").
- Mockups: [`S-05-plan-editor-dark.png`](assets/mockups/S-05-plan-editor-dark.png), [`S-05-plan-editor-phases-dark.png`](assets/mockups/S-05-plan-editor-phases-dark.png).

### C-21 EmptyState
Purpose: encouraging, never blank.
- Anatomy: sunken card, small illustration (≤ 240 × 160), one bold line, one muted line, one primary action.
- Variants: First run ([`empty-first-run.svg`](assets/illustrations/empty-first-run.svg)), No Plan / No Guide ([`empty-no-plan.svg`](assets/illustrations/empty-no-plan.svg)), No history ([`empty-no-history.svg`](assets/illustrations/empty-no-history.svg)), Offline ([`empty-offline.svg`](assets/illustrations/empty-offline.svg)). Clean Theme shows the same art at 60% opacity or none.
- Copy: "Your first Workout fills the first slot." · "No Guide yet" · "No signal. Everything else works offline."

### C-22 BadgeEditor
Purpose: create, edit and delete a Badge on a phone.
- Fields: art (pick from the starter family or the frame template), Name, Metric (segmented: Workouts, Streak, Best week, PR count, Exercise best, Body weight), then metric-specific fields (Exercise + target "24 × 8"; weight target or "drop by" kg), a live line "Now: 22 × 9. Unlocks when…", **Delete / Save**.
- Mockup: [`S-13-badge-editor-dark.png`](assets/mockups/S-13-badge-editor-dark.png).

### C-23 UnlockToast
Purpose: the calm Badge celebration.
- Shown on the finish summary only (never during logging), auto-hides after 4 s, tap opens the Badge. Motion: fade + 8px rise over 200 ms; with reduced motion it appears instantly. Sound and haptics off.
- Graphic: [`unlock-toast.svg`](assets/graphics/unlock-toast.svg). Seen in [`S-02-log-summary-dark.png`](assets/mockups/S-02-log-summary-dark.png).

### C-24 WeekStrip
Purpose: the week at a glance on Home.
- Seven tiles (Mon–Sun), 52px tall: day, Session code coloured by type, state glyph (✓ done, ○ missed, ◐ partial). Today has a 3px `focus` outline. Tap opens Week view at that day.
- Seen in [`S-01-home-dark.png`](assets/mockups/S-01-home-dark.png).

## 6. Screen specifications

All phone mockups are 390 × 844 at 2x (Keep dark unless named), rendered from the real tokens and `components.css`. `-light` = Keep light. `-clean-*` = Clean Theme. The S-02 shots are taken from `prototype-log.html` itself. Sample content follows brief §9 (today is **Wed 18 Nov 2026**, Foundation week 5 of 8, 157 days to race).

### S-01 Home
- **Purpose:** today's Workout first, motivation below.
- **Layout (top to bottom):** date + Phase label · "Good morning" + **Log weight** (C-01 secondary) · TodayCard (C-09) · **Do a different Session** / **Build a Custom workout** · WeekStrip (C-24) · "This week 1 of 5 · target 5 · Missed: A" · three StatusOrbs (C-11: Streak, Phase, days to race) · up to two Nudges (C-08) · Recent PRs panel (each with previous best) · Weight panel (latest weigh-in, weekly average and change) · TabBar (C-12).
- **States:**
  - Today's Workout (Start): [`S-01-home-dark.png`](assets/mockups/S-01-home-dark.png) · [`S-01-home-light.png`](assets/mockups/S-01-home-light.png) · [`S-01-home-clean-dark.png`](assets/mockups/S-01-home-clean-dark.png) · [`S-01-home-clean-light.png`](assets/mockups/S-01-home-clean-light.png)
  - In progress (Continue, "3 of 6 done · 24 min"): [`S-01-home-inprogress-dark.png`](assets/mockups/S-01-home-inprogress-dark.png)
  - Rest day ("Next is Session E on Saturday", no button): [`S-01-home-restday-dark.png`](assets/mockups/S-01-home-restday-dark.png)
  - Done today (summary stats, PR line): [`S-01-home-done-dark.png`](assets/mockups/S-01-home-done-dark.png)
  - Break active (quiet: no alternatives, no Nudges, Streak orb "On a Break"): [`S-01-home-break-dark.png`](assets/mockups/S-01-home-break-dark.png)
  - Plan finished: [`S-01-home-planfinished-dark.png`](assets/mockups/S-01-home-planfinished-dark.png)
  - First run / no Plan: see S-12.
  - Desktop (two columns, left rail): [`S-01-home-desktop.png`](assets/mockups/S-01-home-desktop.png)
- **Interactions:** Start/Continue → S-02. Do a different Session → sheet of this week's undone Sessions (F-03). Nudge action → its flow (F-02 Make up, F-10/Settings for Backup). Orbs → Progress. Log weight → weight sheet (S-08).

### S-02 Workout log
- **Purpose:** log Sets as fast as possible. The deep-dive is in §8, and the clickable version is [`prototype-log.html`](assets/prototype-log.html).
- **Layout:** top bar (Back, Session name, "x of n done · PRs · min", Workout menu) · optional banners (Resumed, Logged afterwards) · Exercise pager (numbered chips with ✓ / — / ⇄) · ExerciseHeader (C-04) with Suggested target · SetRows (C-02) for the log kind · Add Set / Mark as Max set · RPE (C-05) · ConfirmBar (C-06). There is no tab bar.
- **States and mockups:**
  - Default in each Theme and Mode: [`S-02-log-dark.png`](assets/mockups/S-02-log-dark.png) · [`S-02-log-light.png`](assets/mockups/S-02-log-light.png) · [`S-02-log-clean-dark.png`](assets/mockups/S-02-log-clean-dark.png) · [`S-02-log-clean-light.png`](assets/mockups/S-02-log-clean-light.png)
  - 360 px wide: [`S-02-log-360-dark.png`](assets/mockups/S-02-log-360-dark.png) · desktop: [`S-02-log-desktop.png`](assets/mockups/S-02-log-desktop.png)
  - PR achieved: [`S-02-log-pr-dark.png`](assets/mockups/S-02-log-pr-dark.png) · format error: [`S-02-log-error-dark.png`](assets/mockups/S-02-log-error-dark.png) · Exercise done: [`S-02-log-exercise-done-dark.png`](assets/mockups/S-02-log-exercise-done-dark.png)
  - Log kinds: bodyweight [`S-02-log-bodyweight-dark.png`](assets/mockups/S-02-log-bodyweight-dark.png) · carry [`S-02-log-carry-dark.png`](assets/mockups/S-02-log-carry-dark.png) · seconds [`S-02-log-seconds-dark.png`](assets/mockups/S-02-log-seconds-dark.png) · time [`S-02-log-time-dark.png`](assets/mockups/S-02-log-time-dark.png) · distance + time with pace [`S-02-log-distance-time-dark.png`](assets/mockups/S-02-log-distance-time-dark.png)
  - Menus: Exercise menu [`S-02-log-exercise-menu-dark.png`](assets/mockups/S-02-log-exercise-menu-dark.png) · Swap picker [`S-02-log-swap-picker-dark.png`](assets/mockups/S-02-log-swap-picker-dark.png) · Swapped [`S-02-log-swapped-dark.png`](assets/mockups/S-02-log-swapped-dark.png) · Skip sheet [`S-02-log-skip-sheet-dark.png`](assets/mockups/S-02-log-skip-sheet-dark.png) · Skipped [`S-02-log-skipped-dark.png`](assets/mockups/S-02-log-skipped-dark.png) · Workout menu [`S-02-log-workout-menu-dark.png`](assets/mockups/S-02-log-workout-menu-dark.png)
  - Rest timer running: [`S-02-log-rest-timer-dark.png`](assets/mockups/S-02-log-rest-timer-dark.png) · Resumed: [`S-02-log-resumed-dark.png`](assets/mockups/S-02-log-resumed-dark.png) · Logged afterwards: [`S-02-log-afterwards-dark.png`](assets/mockups/S-02-log-afterwards-dark.png)
  - Finish as Partial: [`S-02-log-finish-partial-dark.png`](assets/mockups/S-02-log-finish-partial-dark.png) · Pain check: [`S-02-log-pain-check-dark.png`](assets/mockups/S-02-log-pain-check-dark.png), [`S-02-log-pain-check-yes-dark.png`](assets/mockups/S-02-log-pain-check-yes-dark.png) · Summary: [`S-02-log-summary-dark.png`](assets/mockups/S-02-log-summary-dark.png)

### S-03 Guide
- **Purpose:** the knowledge base for one Exercise, editable on the phone.
- **Read view:** title + "Session A · 4 × 8 · Best 22 × 9" · sections in order: What it works, Why it's in your plan, Setup, How to do it, Cues, Common mistakes, Easier / harder variations, Watch-outs · **Watch a demo** (opens the link; offline → C-21 offline) · My notes (with Edit notes) · Your history (last 3, PRs marked) · where it sits in the Plan. [`S-03-guide-dark.png`](assets/mockups/S-03-guide-dark.png) · [`S-03-guide-light.png`](assets/mockups/S-03-guide-light.png)
- **Edit view:** each section becomes a labelled textarea; **Cancel / Save**; leaving with unsaved changes asks "Discard changes?". [`S-03-guide-edit-dark.png`](assets/mockups/S-03-guide-edit-dark.png)
- **No Guide yet:** C-21 with **Write a Guide** / **Import Guides**. [`S-03-guide-noguide-dark.png`](assets/mockups/S-03-guide-noguide-dark.png)
- **Offline video:** [`S-12-offline-dark.png`](assets/mockups/S-12-offline-dark.png)

### S-04 Exercise library
- **Purpose:** browse, search, filter and add Exercises; it also backs the Swap and Add pickers (C-14).
- **Layout:** search field + Filter button · tag chips (All, legs, pull, core, hyrox, cardio…) · groups by Session, then "Not in a Session" · rows: icon, name, "log kind · tags", Guide status, chevron. A **+** in the top bar adds an Exercise. [`S-04-library-dark.png`](assets/mockups/S-04-library-dark.png) · [`S-04-library-light.png`](assets/mockups/S-04-library-light.png)
- **New Exercise:** name, how it's logged (9 log kinds as `pick` buttons), tags, optional cue, "Write a Guide now", **Cancel / Add Exercise**. [`S-04-library-add-dark.png`](assets/mockups/S-04-library-add-dark.png)
- **Empty search:** "No match. Add 'squ' as a new Exercise".

### S-05 Plans list and Plan editor
- **Plans list:** the active Plan (outlined, "Active · week 5 of 27"), other Plans with **Make active**, and New Plan options (From scratch, Copy this Plan, Import a Plan). [`S-05-plans-dark.png`](assets/mockups/S-05-plans-dark.png) · [`S-05-plans-light.png`](assets/mockups/S-05-plans-light.png)
- **Plan editor:** segmented tabs Settings · Phases · Sessions · Badges. Sessions shows each Session's Exercises as C-20 editable rows (one expanded), **Add Exercise from library**, and the banner "Edits apply going forward. Past Workouts are unchanged." [`S-05-plan-editor-dark.png`](assets/mockups/S-05-plan-editor-dark.png) · [`S-05-plan-editor-light.png`](assets/mockups/S-05-plan-editor-light.png)
- **Phases:** coloured rows with dates and weeks; editing one shows start/end (C-15) and an optional Phase goal. [`S-05-plan-editor-phases-dark.png`](assets/mockups/S-05-plan-editor-phases-dark.png) · desktop: [`S-05-plan-editor-desktop.png`](assets/mockups/S-05-plan-editor-desktop.png)
- **Settings tab** (same patterns): start date, goal and date, weekly target, Streak minimum, weight increment. **Badges tab:** the list from S-13 with edit buttons.

### S-06 Week view
- **Purpose:** this week's Sessions on their days; move, mark a Break, accept a suggested re-arrangement.
- **Rows:** day, Session, state chip (Done / Missed / Today), a Move button (sheet: pick a day; "This week only" or "Make this the default day"). [`S-06-week-dark.png`](assets/mockups/S-06-week-dark.png) · [`S-06-week-light.png`](assets/mockups/S-06-week-light.png)
- **Add a Break:** from/to (C-15), reason, explanation, **Start Break**. [`S-06-week-break-dark.png`](assets/mockups/S-06-week-break-dark.png)
- **Suggested week (draft):** summary banner, changed rows highlighted (ochre inset) with per-row Undo, **Cancel / Apply**. [`S-06-week-draft-dark.png`](assets/mockups/S-06-week-draft-dark.png)

### S-07 Progress, Benchmarks, Calendar
- **Progress:** tabs Progress · Benchmarks · Calendar; group chips Strength · Carries · Engine · Bodyweight · Body; a LineChart (C-17) per Exercise logged twice or more (estimated 1RM for strength; change since first entry; best point labelled); other Exercises as rows ("1 entry · needs 2 for a chart"). [`S-07-progress-dark.png`](assets/mockups/S-07-progress-dark.png) · [`S-07-progress-light.png`](assets/mockups/S-07-progress-light.png) · desktop: [`S-07-progress-desktop.png`](assets/mockups/S-07-progress-desktop.png)
- **Benchmarks:** latest test vs previous, per benchmark Exercise (▲ when better), progress bars, next test date. [`S-07-benchmarks-dark.png`](assets/mockups/S-07-benchmarks-dark.png)
- **Calendar:** CalendarHeatmap (C-18) with a Phase strip, scrolling sideways to race day. [`S-07-calendar-dark.png`](assets/mockups/S-07-calendar-dark.png) · [`S-07-calendar-light.png`](assets/mockups/S-07-calendar-light.png)

### S-08 Weight
- **Layout:** three stats (now, change since start, this week's average) · weight LineChart (C-17 weight variant, with the 7-entry rolling average and goal line) · weekly averages table · **+** Log weight. [`S-08-weight-dark.png`](assets/mockups/S-08-weight-dark.png) · [`S-08-weight-light.png`](assets/mockups/S-08-weight-light.png)
- **Log weight sheet:** one large decimal field prefilled with the last value, "Last: 86.3 on Sun 15 Nov", **Cancel / Save**. Usable any day. [`S-08-weight-log-dark.png`](assets/mockups/S-08-weight-log-dark.png)

### S-09 Weekly review
- **Layout:** verdict card ("On track" / "Behind" / "Target out of reach" / "Target hit") with Workouts vs target and Streak · rows for Missed, PRs, weight average vs last week, Pain checks · PRs this week · **Export for the LLM** (preset: last 7 days, Markdown, health data off). [`S-09-weekly-review-dark.png`](assets/mockups/S-09-weekly-review-dark.png) · [`S-09-weekly-review-light.png`](assets/mockups/S-09-weekly-review-light.png)

### S-10 Export, Import, Backup, Prompt pack
- **Export:** date range (C-15, default last 12 weeks) · Include ticks (Workouts, Plan, Exercises on; Guides off) · **Health data** switch (weigh-ins and Pain checks; off by default for LLM exports) · format Markdown / JSON · delivery Copy / File / Share · **Export everything (Backup)** with last-backed-up date. [`S-10-export-dark.png`](assets/mockups/S-10-export-dark.png) · [`S-10-export-light.png`](assets/mockups/S-10-export-light.png)
- **Prompt pack:** three prompt cards with explanations and **Copy prompt**. [`S-10-prompt-pack-dark.png`](assets/mockups/S-10-prompt-pack-dark.png)
- **Import:** big paste box first, **Check**, file picker second, "automatic Backup" note. [`S-10-import-dark.png`](assets/mockups/S-10-import-dark.png) · errors: [`S-10-import-error-dark.png`](assets/mockups/S-10-import-error-dark.png)
- **Preview (C-16):** [`S-10-import-preview-dark.png`](assets/mockups/S-10-import-preview-dark.png) · [`S-10-import-preview-light.png`](assets/mockups/S-10-import-preview-light.png) · Guide import: [`S-10-import-guides-dark.png`](assets/mockups/S-10-import-guides-dark.png)

### S-11 Settings
- **Sections:** Look (Theme Keep/Clean, Mode System/Dark/Light, live preview of a primary button) · Units and training (kg/lb, km/miles, increment, rest timer default) · Streak rules (minimum per week, what counts) · Data (last backed up + **Back up**, persistent storage status, installed status). [`S-11-settings-dark.png`](assets/mockups/S-11-settings-dark.png) · [`S-11-settings-light.png`](assets/mockups/S-11-settings-light.png)

### S-12 First run and install
- **Welcome:** illustration, "Welcome to the Keep", one line on local-only data, **Import my Plan** (primary, to S-10 Import), **Restore a Backup**, "Start with an empty Plan". [`S-12-first-run-dark.png`](assets/mockups/S-12-first-run-dark.png) · [`S-12-first-run-light.png`](assets/mockups/S-12-first-run-light.png)
- **Two quick things:** install to home screen, persistent storage with a plain explanation, Backup reminder. [`S-12-first-run-install-dark.png`](assets/mockups/S-12-first-run-install-dark.png)
- **Offline state** (only where a connection is needed): [`S-12-offline-dark.png`](assets/mockups/S-12-offline-dark.png)

### S-13 Trophies: Badges, detail, editor, bests
- **Badges:** tabs Badges · Bests · Calendar · BadgeTile grid (C-10), unlocked first then closest to unlocking · "Badges unlocked: 3 / 20" · progress bars for the closest. [`S-13-badges-dark.png`](assets/mockups/S-13-badges-dark.png) · [`S-13-badges-light.png`](assets/mockups/S-13-badges-light.png)
- **Empty (no history yet):** C-21. [`S-13-badges-empty-dark.png`](assets/mockups/S-13-badges-empty-dark.png)
- **Badge detail:** large art, name, what it needs, progress, history of contributing events, Edit. [`S-13-badge-detail-dark.png`](assets/mockups/S-13-badge-detail-dark.png)
- **Badge editor (C-22):** [`S-13-badge-editor-dark.png`](assets/mockups/S-13-badge-editor-dark.png)
- **Bests:** personal bests table with dates, plus "Best of the week". [`S-13-bests-dark.png`](assets/mockups/S-13-bests-dark.png)

## 7. Flow specifications

Each flow is also a diagram in `assets/flows/` (steps on the left, edge cases branching right). Screen (S-) and component (C-) numbers refer to §5 and §6.

### F-01 Log a Workout
Diagram: [`F-01-log-workout.svg`](assets/flows/F-01-log-workout.svg)

1. **S-01** — Home: TodayCard shows Session A · Lower + core. Tap Start (C-09).
2. **S-02** — Log opens on Exercise 1, Sets prefilled from last time; Suggested target shown (C-04).
3. **S-02** — Tap Confirm Set (C-06) or edit a field first (keypad, ±2 kg steppers) (C-02).
4. **S-02** — Set 2 beats Best: PR! marker on the row + toast (C-03).
5. **S-02** — Optional RPE tap (C-05). Bar changes to Next: Pull-up.
6. **S-02** — Exercise menu › Swap › pick a similar Exercise (C-14). Recorded as Swapped.
7. **S-02** — Workout menu › Finish. 5 of 8 done: 'the rest are recorded as skipped' › Finish (Partial).
8. **S-02** — Pain check sheet (C-13): No / Yes / Skip. Yes: area, severity, Exercise, note.
9. **S-02** — Finish summary: duration, Sets, PRs with previous best; unlock toast if a Badge unlocked (C-23).
10. **S-01** — Home: TodayCard shows the summary; recent PRs updated.

Edge cases:
- (at step 3) Invalid input: field-level message under the row; Confirm stays on that Set.
- (at step 2) Leave mid-Workout: Back keeps it in progress; Home shows Continue; reopening shows 'Resumed'.
- (at step 8) Skip ≠ No: Skip records 'not answered'.

### F-02 Make up a Missed session
Diagram: [`F-02-make-up-missed.svg`](assets/flows/F-02-make-up-missed.svg)

1. **S-01** — Nudge (C-08): 'Session A was missed: make it up?' › Make up.
2. **S-02** — Custom workout draft opens with Session A's Exercises (C-20 list), each removable.
3. **S-04** — + Add Exercise › library picker, similar first (C-14).
4. **S-02** — Start › log as normal (F-01 steps 3-9).
5. **S-01** — Home: Session A stays Missed in history; the Custom workout counts toward the Streak.

Edge cases:
- (at step 1) The Nudge only appears after Session A's day has passed undone.
- (at step 1) Dismiss hides it for this week.

### F-03 Do a different Session today
Diagram: [`F-03-different-session.svg`](assets/flows/F-03-different-session.svg)

1. **S-01** — Tap 'Do a different Session' under TodayCard.
2. **S-01** — Sheet lists this week's undone Sessions (code, name, type, default day).
3. **S-01** — Pick Session D · Full body.
4. **S-02** — Log opens for Session D (F-01).
5. **S-06** — Week view: D shows done today; A stays on Monday as outstanding.

Edge cases:
- (at step 2) No undone Sessions left: sheet says so and offers Build a Custom workout.

### F-04 Edit the Plan
Diagram: [`F-04-edit-plan.svg`](assets/flows/F-04-edit-plan.svg)

1. **S-05** — More › Plans › active Plan › Sessions › Session A.
2. **S-05** — Tap an Exercise row: inline edit sets, reps, cue, log kind, benchmark toggle (C-20).
3. **S-04** — + Add Exercise › library picker (C-14) › choose › added at the end.
4. **S-05** — Drag handle to reorder (icon-reorder) or Move up/down buttons (accessible).
5. **S-05** — Phases › Build › change end date (C-15 date range).
6. **S-05** — Save: banner 'Applies from today. Past Workouts are unchanged.'

Edge cases:
- (at step 2) Invalid sets/reps: inline message; Save disabled until fixed.
- (at step 5) Overlapping Phases: plain message naming both Phases.

### F-05 Import a Plan an LLM wrote
Diagram: [`F-05-import-plan.svg`](assets/flows/F-05-import-plan.svg)

1. **S-10** — More › Export & Import › Prompt pack › Copy 'Write me a Plan'.
2. **S-10** — (In a chat app) paste the prompt, copy the reply.
3. **S-10** — Import › big paste box › Paste › Check.
4. **S-10** — Preview (C-16): 'creates a new Plan called X, adds 3 Exercises, touches no Workouts'.
5. **S-10** — Name matches: 'Did you mean Goblet squat?' Confirm / Keep as new, per name.
6. **S-10** — Apply › 'Backup made automatically' › Plan created.
7. **S-05** — Prompt: Make this the active Plan? › Yes.

Edge cases:
- (at step 4) Bad file: field-level errors ('Session 2, Exercise 4: reps must be a number') and nothing applied.
- (at step 6) Cancel at any point changes nothing.

### F-06 Export for an LLM
Diagram: [`F-06-export-llm.svg`](assets/flows/F-06-export-llm.svg)

1. **S-09** — Weekly review › Export for the LLM (one tap, preset to the past week).
2. **S-10** — Picker opens prefilled: last 7 days; Workouts, Plan ticked; health data (weight, pain) ticked off by default.
3. **S-10** — Format: Markdown (readable) / JSON (canonical).
4. **S-10** — Deliver: Copy to clipboard / Download file / Share sheet.
5. **S-10** — Confirmation: 'Copied. Health data left out.'

Edge cases:
- (at step 2) Full picker path: More › Export, default range 12 weeks, Guides off by default.
- (at step 4) Export everything = Backup (all data, JSON).

### F-07 Weekly review (Sunday)
Diagram: [`F-07-weekly-review.svg`](assets/flows/F-07-weekly-review.svg)

1. **S-01** — Sunday: TodayCard shows W · Weigh-in › Log weight.
2. **S-08** — Weight entry: one field (kg), keypad, Save. Average vs last week shown.
3. **S-09** — Weekly review opens: Workouts 4 of 5, Missed: A, PRs this week, weight avg, Pain checks.
4. **S-09** — One-line message: 'On track' / 'Behind' / 'Target out of reach' / 'Target hit'.
5. **S-09** — Optional: Export for the LLM (F-06).

Edge cases:
- (at step 2) Weigh-ins don't count toward the Streak.

### F-08 Break and re-arrange the week
Diagram: [`F-08-break-rearrange.svg`](assets/flows/F-08-break-rearrange.svg)

1. **S-06** — Week view › Add a Break › date range + reason (C-15).
2. **S-01** — Home goes quiet: Break card, Streak shows 'On a Break' (streak-mark-break).
3. **S-06** — Break ends: draft re-arranged week proposed; changes highlighted, one summary line.
4. **S-06** — Per item: move to another day (this week only / make default) or undo.
5. **S-06** — Apply › week updated.

Edge cases:
- (at step 3) The draft avoids back-to-back Sessions for the same muscle group.
- (at step 5) Cancel keeps the old week; outstanding Sessions become Missed only after their day passes.

### F-09 Edit a Guide and add notes
Diagram: [`F-09-edit-guide.svg`](assets/flows/F-09-edit-guide.svg)

1. **S-02** — Tap the Exercise name (or Exercise menu › View Guide).
2. **S-03** — Guide read view: sections, video link, history, My notes.
3. **S-03** — Edit › section-by-section text areas; Save.
4. **S-03** — My notes › add 'Left knee: keep the box high' › Save.
5. **S-02** — Back to the same Set; nothing lost.

Edge cases:
- (at step 2) New Exercise with no Guide: 'No Guide yet' with Write one / Import Guides.
- (at step 3) Leave with unsaved edits: 'Discard changes?' Keep editing / Discard.

### F-10 Restore a Backup after storage was cleared
Diagram: [`F-10-restore-backup.svg`](assets/flows/F-10-restore-backup.svg)

1. **S-12** — App opens empty (storage cleared): first-run screen with 'Restore a Backup' option.
2. **S-10** — Import › paste or pick the Backup file.
3. **S-10** — Preview: 'Restores 2 Plans, 64 Workouts, 41 weigh-ins (to 15 Nov)'. Apply.
4. **S-11** — Persistent storage request with a plain explanation.
5. **S-01** — Home restored; 'Last backed up' shown in Settings.

Edge cases:
- (at step 3) Older Backup version: migrated silently; failures are listed field by field.

## 8. Log screen deep-dive

This section describes the reference behaviour in [`prototype-log.html`](assets/prototype-log.html), which was tested at 390 and 360 px.

### 8.1 Set row per log kind
All rows use the C-02 grid `24px 1fr 14px 1fr 56px`. "Wide" means a single input spanning columns 2–4.

| Log kind | Example | Fields (inputmode) | PR rule (strictly better than Best) | Mockup |
|---|---|---|---|---|
| Weight × reps | `20x10` | kg (`decimal`) × reps (`numeric`) | higher estimated 1RM, using Epley: w × (1 + r/30) | [`S-02-log-dark.png`](assets/mockups/S-02-log-dark.png) |
| Bodyweight × reps | `bwx8` | fixed "bw" tag × reps (`numeric`) | more reps | [`S-02-log-bodyweight-dark.png`](assets/mockups/S-02-log-bodyweight-dark.png) |
| Carry | `24x40` | kg/hand (`decimal`) × metres (`numeric`) | more weight at ≥ the same distance, or more distance at ≥ the same weight | [`S-02-log-carry-dark.png`](assets/mockups/S-02-log-carry-dark.png) |
| Seconds | `45` | wide seconds (`numeric`) | longer | [`S-02-log-seconds-dark.png`](assets/mockups/S-02-log-seconds-dark.png) |
| Reps only | `12` | wide reps (`numeric`) | more | (same layout as seconds) |
| Time | `1:48` | wide `m:ss` (`text`, accepts `108` as seconds) | **lower** | [`S-02-log-time-dark.png`](assets/mockups/S-02-log-time-dark.png) |
| Distance + time | `1 km in 4:35` | km (`decimal`) × `m:ss`; pace shown under the row | lower pace | [`S-02-log-distance-time-dark.png`](assets/mockups/S-02-log-distance-time-dark.png) |
| Minutes/seconds | `12:30` | as Time | as set per Exercise (lower or higher is better) | — |
| Free text | "felt good" | wide text | none | — |
| Tick | done | one tick button | none | — |

A **Max set** flag (link under the rows) marks the current Set. When one exists, it is the source of truth for Best.

### 8.2 Input behaviour
- Rows are **prefilled from last time** and look "pending" (muted) until confirmed or edited. Confirming a pending row = "same as last".
- Focusing a field selects all its text, so typing replaces the value. **Enter** moves from the first field to the second in the same row; on the current row's last field, Enter confirms the Set.
- The **±increment steppers** (C-06) change the current Set and carry the new value down to later unconfirmed Sets. The step is the Plan's increment (2 kg), 5 s for seconds, 1 rep for bodyweight; steppers are hidden for time and distance.
- **Validation runs on Confirm**, not while typing. The message sits under the row ("Weight should be a number, like 22 or 22.5"), the fields get `aria-invalid`, and focus returns to the bad field. Commas are accepted as decimal points.
- Tapping a done tick reopens the Set for editing. Nothing is lost on reload: state is saved after every change (IndexedDB in the app; localStorage in the prototype) and the log shows "Resumed where you left off".
- The screen stays awake while logging (Wake Lock where supported; silent no-op otherwise).

### 8.3 PR feedback
- When a confirmed Set strictly beats Best: the state cell shows the **PR marker** (C-03), the input's label gains ", PR", a short toast says "PR! 22 kg × 9 beats 22 × 8", and the header count updates ("1 PR").
- The summary lists one PR per Exercise (the best Set) with the previous best. Home's "Recent PRs" shows the same.

### 8.4 Menus
- **Exercise menu** (C-01 icon button beside the name): Swap (C-14, similar first) · Skip (reason chips: Short on time, Pain, No equipment, Other; all optional) · Add an Exercise (after this one) · Mark as / Unmark as benchmark · View Guide (S-03 as a sheet; "Back to the Set" returns without losing anything).
- **Workout menu** (top right): rest timer Off / 60 / 90 / 120 s (off by default; starts on Confirm; shows "Rest 1:12" in the ConfirmBar) · Theme · Mode · Logged afterwards · **Finish Workout** · (prototype only) Discard.
- **Exercise pager:** numbered chips for jumping between Exercises; ✓ done, — skipped, ⇄ swapped. When an Exercise is complete, the primary button becomes **Next: <Exercise>**; after the last one it becomes **Finish**.

### 8.5 Finish, Partial and Pain check
- Finish with Exercises left: "Finish as Partial? 5 of 8 done; the rest are recorded as skipped. Partial still counts toward your Streak." **Keep going / Finish**.
- Then the Pain check (C-13), then the summary: duration (— when logged afterwards), Sets, PRs, every Exercise with Done / Skipped / Swapped and RPE, the Pain check result ("Yes: Left knee, mild (during Goblet squat)"), the Streak line, and the unlock toast (C-23) if a Badge unlocked.

### 8.6 Logged afterwards
A Workout logged for a past date shows the banner "Logged afterwards · Sun 15 Nov. No timers." The rest timer and elapsed time are hidden, and the duration is not recorded. History marks it "logged afterwards". There is no date limit.

## 9. Responsive behaviour

| Width | Layout |
|---|---|
| 360–430 px (design target) | Single column, side padding `space-3`. The log fits 360 px with no horizontal scroll (tested). The tab bar sits at the bottom; on the log, the ConfirmBar does. |
| 431–899 px (large phones, small tablets) | Content column capped at 560 px and centred; background fills the rest. |
| ≥ 900 px (tablet landscape, desktop) | The tab bar becomes a **left rail** (220 px). Home and Progress use two columns (main 560 px + side panels). The Plan editor shows list and detail side by side. The **Workout log stays a single centred column** (max 430 px), because two-handed desktop logging is incidental. Mockups: [`S-01-home-desktop.png`](assets/mockups/S-01-home-desktop.png), [`S-02-log-desktop.png`](assets/mockups/S-02-log-desktop.png), [`S-05-plan-editor-desktop.png`](assets/mockups/S-05-plan-editor-desktop.png), [`S-07-progress-desktop.png`](assets/mockups/S-07-progress-desktop.png). |
| Text scaling | All sizes in `rem` in the build; rows grow; Exercise names wrap; tile names ellipsis with the full name in `aria-label`. |
| Safe areas | The ConfirmBar and tab bar pad by `env(safe-area-inset-bottom)`; the top bar by `env(safe-area-inset-top)`. |

## 10. Accessibility checklist

| Requirement (brief §4) | How it's met |
|---|---|
| WCAG AA contrast | Table 4.2, computed for all four Theme × Mode sets |
| Visible focus | 3px `focus` outline with 2px offset on every interactive class |
| Labels on all inputs | Every Set input has an `aria-label` ("Set 2 reps"); errors use `aria-invalid` + `aria-describedby` |
| Respect reduced motion | The only motion (press nudge, toast fade) is disabled |
| 44 × 44 minimum | `tap-min`; log inputs `tap-log` 56; primary `tap-primary` 64 |
| Text scaling | `rem` in the build; rows grow, names wrap or ellipsis with the full name in `aria-label` |
| Not colour alone | Glyph + word + colour for every state (C-07); PR marker has text |
| Bright light | Light inputs on dark panels (Keep dark), Clean light Mode, heavy borders |

## 11. Build handoff notes

- **Recommendation: plain CSS with tokens, no component library.** The components are simple, the Theme switch is pure CSS custom properties, and a library would fight the Keep bevels. Use Blazor CSS isolation (`Component.razor.css`) for layout, and keep the token-driven skin classes from `components.css` global so Themes apply everywhere.
- Component boundaries: one Razor component per C-number. SetRow takes a `LogKind` and renders the right inputs; TodayCard takes a state enum.
- Theme switching: set `data-theme` and `data-mode` on `<html>` from Settings (a few lines of JS interop); resolve "system" with `matchMedia`.
- Orbs, charts and Badge art are inline SVG with token-bound classes; no canvas.
- **Costly items, with cheaper alternatives:** stone texture → a single 2 KB tiled SVG noise, or none; animated unlock → a static toast; per-Badge artwork → one shield frame plus a glyph set.
- The display font is self-hosted as one 16 KB WOFF2 (Jersey 15, OFL) declared in `components.css`; it is never loaded from Google at runtime.
- **PWA install assets:** master [`app-icon.svg`](assets/app-icon/app-icon.svg); maskable master [`app-icon-maskable.svg`](assets/app-icon/app-icon-maskable.svg) (art inside the 80% safe circle); monochrome [`app-icon-monochrome.svg`](assets/app-icon/app-icon-monochrome.svg) → [`icon-monochrome-512.png`](assets/app-icon/icon-monochrome-512.png); exports [`icon-any-192.png`](assets/app-icon/icon-any-192.png), [`icon-any-512.png`](assets/app-icon/icon-any-512.png), [`icon-maskable-192.png`](assets/app-icon/icon-maskable-192.png), [`icon-maskable-512.png`](assets/app-icon/icon-maskable-512.png), [`apple-touch-icon-180.png`](assets/app-icon/apple-touch-icon-180.png); favicon [`favicon.svg`](assets/app-icon/favicon.svg), [`favicon-32.png`](assets/app-icon/favicon-32.png), [`favicon-16.png`](assets/app-icon/favicon-16.png); colours in [`splash.json`](assets/app-icon/splash.json) (`background_color` #2B2620, `theme_color` #4A4237, and the Clean variants). These replace the placeholder icons currently in `src/Web/wwwroot`.
- Assets to copy into `src/Web/wwwroot`: `tokens.css`, `components.css`, `fonts/`, `icons/` (inline them as Razor components or use them as `<img>`/mask), `badges/`, `graphics/`, `illustrations/`, and the `app-icon/` exports for the manifest (see `app-icon/splash.json` for `background_color` and `theme_color`).
- `prototype-log.html` is the reference implementation of S-02's behaviour (state model, PR rules, validation, keyboard order); port its logic to C# in `Core` and its markup to Razor.

## 12. Open issues and assumptions

**Assumptions**
- The Streak orb shows weeks; its ring shows this week's progress toward the Streak minimum.
- Estimated 1RM uses Epley, w × (1 + r/30), for both PR detection and Progress charts of weight × reps Exercises.
- The carry PR needs more weight at ≥ the same distance, or more distance at ≥ the same weight.
- Partial Workouts count toward the Streak, and so do Custom workouts. Weigh-ins and benchmark tests don't (brief §8).
- "Do a different Session" lists only this week's undone Sessions; anything else goes through Build a Custom workout.
- Pain check "Which side?" (Left/Right/Both) was added so the brief's example "Left knee, mild, goblet squat" can be recorded.
- Mockup data is the brief's sample content; numbers such as Badge counts and weights are illustrative.

**Open issues**
- Keep light ("sandstone") passes contrast but has less character; Keep is designed dark-first and Clean light is the recommended choice for very bright gyms.
- Wake Lock isn't available in every browser; there's no fallback beyond the OS screen timeout.
- The rest timer is in-app only (no push notifications, per the brief), so it can't alert while the phone is locked.
- RuneScape fonts: not used or shipped. A personal build may swap `--font-display` locally; don't do this for a public release.
- The moodboard canvas still shows the earlier Pixelify-based direction D; this document and `assets/` are the source of truth.

**Possible issues with the product brief**
- "Minutes/seconds" and "time" overlap as log kinds. Suggest one *duration* kind with a per-Exercise "lower is better" flag.
- The brief lists body areas without a side. Adding Left/Right (as above) makes Pain check trends more useful.
