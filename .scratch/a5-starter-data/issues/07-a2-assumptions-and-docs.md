Status: ready-for-agent
Stage: A5
Blocked by: 06

# Ticket 07: A2 assumption pass against view.js and docs update

## Scope
- Check A2's nine assumptions against `.scratch/a5-starter-data/legacy/phone-2026-11-19/_scripts/fitness/view.js` and record any differences as open items (never silent changes).
- Update `docs/build-plan.md` and `docs/README.md` for A5 completion, and prepare `.scratch/a5-starter-data/pr-body.md` (if needed before `/ship`).

## Acceptance
- `dotnet build` and `dotnet test` pass with 0 warnings.
- All 9 A2 assumptions are verified and documented; docs updated.
