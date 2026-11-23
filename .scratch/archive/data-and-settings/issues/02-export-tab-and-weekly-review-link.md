# 02: Export tab, Backup download, and Weekly review link (S-10 Export, F-06)

**What to build:** Replace the Export and Import placeholder (`/backup`) with a three-tab layout (**Export**, **Import**, **Prompts**) supporting query-string deep links, and build the **Export** tab using `Core.Exchange`. Export defaults to the last 12 weeks with Workouts, Plan, and Exercises ticked on, Guides off, and Health data (Weigh-ins and Pain checks) off. The owner can pick custom start/end dates (with inline validation if start is after end), toggle Markdown or JSON, see a live one-line summary of counts and date range, and deliver via **Copy**, **Download file**, or **Share** (shown only when the browser supports Web Share). A dedicated **Export everything (Backup)** section shows the last-backed-up date, downloads a complete JSON Backup, and updates the Backup timestamp (resetting the Backup Nudge). Query parameters support the 7-day LLM preset (`?preset=llm`), and the **Weekly review** page's "Export for the LLM" placeholder is replaced with a working link to that preset.

**Blocked by:** 01: Browser capabilities seam and Settings page (S-11)

**Status:** ready-for-agent

- [ ] `/backup` renders the three-tab shell (**Export**, **Import**, **Prompts**) and honours query parameters for active tab and Export presets.
- [ ] Export defaults to the last 12 weeks, Workouts/Plan/Exercises on, Guides off, Health data off, and shows a live one-line summary matching `Exporter.Export`.
- [ ] Custom start/end date inputs validate that start is not after end with an inline message.
- [ ] Switching between Markdown and JSON and toggling Health data updates the Export output and summary accurately.
- [ ] **Copy** copies the Export text with confirmation; **Download file** triggers download with the suggested filename; **Share** appears only when supported by the browser.
- [ ] **Export everything (Backup)** displays the last-backed-up date, downloads a full JSON Backup, and records the Backup timestamp via `BackupRecorder`.
- [ ] **Weekly review** links "Export for the LLM" to `/backup` prefilled to the past 7 days, Markdown format, Workouts and Plan on, and Health data off.
- [ ] UI tests cover Export defaults, date validation, LLM preset deep link from Weekly review, Copy/Download/Share visibility and payloads, and Backup recording with 0 build warnings.
