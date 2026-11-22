Status: resolved
Stage: A5
Blocked by: 06

# Ticket 07: A2 assumption pass against view.js and docs update

## Scope
- Check A2's nine assumptions against `.scratch/a5-starter-data/legacy/phone-2026-11-19/_scripts/fitness/view.js` and record any differences as open items (never silent changes).
- Update `docs/build-plan.md` and `docs/README.md` for A5 completion, and prepare `.scratch/a5-starter-data/pr-body.md` (if needed before `/ship`).

## A2 assumption check
Checked all 9 assumptions in `docs/specs/a2-core-rules.md` against `view.js`:
1. **First log is baseline, not a PR:** Matches `view.js` (lines 192-196, 424, 534, 560).
2. **RPE <= 6 bigger jump:** Not in `view.js` (RPE is a new-app feature).
3. **Carry at 40 m resets to 20 m:** **Difference** — `view.js` (line 578) adds `INC` kg and keeps `p.dist || 30` (does not reset to 20 m). Recorded as open item in `docs/build-plan.md`.
4. **Time target and per-kind step table:** **Two differences** — `view.js` (line 73) uses `step: 50` for `m` (vs `+10` in A2), and (line 579) suggests `"under <best>"` for `time` with no fixed step subtracted (vs `best - fixed step` in A2). `reps` (+1), `secs` (+5), `mins` (+1), `km` (+0.5) match. Recorded as open item in `docs/build-plan.md`.
5. **Break week excused from Streak:** Not in `view.js` (Breaks are a new-app feature).
6. **Consistency message:** Not in `view.js` (new-app feature).
7. **"One short of target" Nudge from Thursday:** Not in `view.js` (Nudges are a new-app feature).
8. **Phase goal readiness:** Not in `view.js` (Phase goals are a new-app feature).
9. **Rearrangement muscle-group clash:** Not in `view.js` (Week schedule/rearrangement are new-app features).

## Acceptance
- `dotnet build` and `dotnet test` pass with 0 warnings.
- All 9 A2 assumptions are verified and documented; docs updated.
