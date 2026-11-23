# 05: Left rail and Home two columns

Spec: `docs/specs/hardening-b11.md` (Implementation Decisions: Wide layout).

**What to build:** From 900 px, the TabBar (C-12) becomes a fixed 220 px left rail: same items and order as the bottom bar including More, stacked, icon beside word. The content area is offset by the rail, and safe-area padding follows. The Workout log keeps the rail and stays a single centred column capped at 430 px, with its ConfirmBar spanning the log column, not the rail. Home uses the shared wide-first two-column rule from 900 px: TodayCard, WeekStrip, orb row (Streak, Phase, days to race) and Nudges in the main column; Recent PRs and Weight in the side column, grouped as in `S-01-home-desktop.png`. First run stays single column. 431–899 px keeps the 560 px centred column. Plan editor list-and-detail is not built.

**Blocked by:** 01

**Status:** resolved

- [x] At ≥ 900 px the TabBar sits as a left rail with the same items as at 360 px; below 900 px it is the bottom bar.
- [x] At ≥ 900 px the Workout log column is at most 430 px wide and centred in the space beside the rail; the ConfirmBar does not cover the rail.
- [x] At ≥ 900 px Home shows the main and side columns side by side with the grouping from the mockup; at 360 px it is one column in the current order.
- [x] Browser tests assert these by element positions at 1280 px, 700 px and 360 px.
- [x] bUnit tests updated for Home markup; `dotnet build` 0 warnings; all tests green.
