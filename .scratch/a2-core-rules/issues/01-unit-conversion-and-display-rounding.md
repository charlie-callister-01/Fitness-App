# 01: Unit conversion and display rounding

**What to build:** The owner can see weights and distances in their chosen display units (kg or lb, km or miles), sometimes both side by side, and anything typed in display units is converted to canonical units (kg, km, m) before it is stored. Stored values are never rounded by display rules, and converting to display and back does not drift. Culture never changes the result. Spec: `docs/specs/a2-core-rules.md` (Units, Input parsing stories 12, 14 to 18).

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Convert canonical to display and display to canonical for weight (kg/lb) and distance (km/mi), driven by the Settings display units
- [ ] Display rounding gives sensible values (weights to a sensible increment, km/mi to two decimals) and never alters stored values
- [ ] A "show both units" result is available for a weight and a distance
- [ ] Round-trip (canonical to display and back) stays within display rounding with no cumulative drift over repeated edits
- [ ] The weight increment used for targets stays in kg whatever the display unit
- [ ] Tests run under a comma-decimal culture (for example de-DE) and prove identical results
