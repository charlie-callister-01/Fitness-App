# 02: Input parsing for every log kind

**What to build:** The owner types a Set in whatever form their phone keyboard makes easiest and gets a Set in canonical units, or a plain-language reason saying what was expected for that Exercise. Parsing is forgiving and deterministic for every log kind. Spec: `docs/specs/a2-core-rules.md` (Input parsing stories 1 to 14, "Input parsing and units" decisions).

**Blocked by:** 01 (Unit conversion and display rounding)

**Status:** ready-for-agent

- [ ] Load: `20x10`, `20 x 10`, `20×10`, `20*10` and comma decimals (`22,5x8`) parse to weight and reps; `bwx8` is bodyweight reps
- [ ] Bodyweight-with-load takes added weight and reps; carry (`32x40`) is weight per hand and metres
- [ ] Time accepts `m:ss`, `h:mm:ss` and plain seconds; seconds, minutes, km and m kinds take a bare number in that kind's unit and accept an unambiguous suffix (`45s`, `3min`, `5km`, `400m`)
- [ ] Reps, body measurements (kg, cm), tick-only (no value) and text (non-empty) kinds parse
- [ ] Spaces are ignored anywhere; input in display units is converted to canonical before returning
- [ ] Unparseable input returns a failure with a message naming what was expected (for example "Expected weight x reps, like 20x10"); parsing never throws
- [ ] Table-driven tests cover every kind including the awkward and rejected inputs, and a de-DE culture run proves culture independence
