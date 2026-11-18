# 03: Core: row input join and pre-fill

**What to build:** Two small pure Core rules next to the parser. (a) The two-field join: turn the row's one or two typed fields into the parser's own string for the kind (`weight x reps`, `bw x reps`, `kg x metres`; the single field unchanged for single-field kinds) so the existing `InputParser` is used as-is, a blank field yields the parser's own plain-language failure, and nothing in the parser changes. (b) Pre-fill: for an Exercise and a Set number, the starting values: the same-numbered Set of the Exercise's Last Workout, else the Suggested target, else empty with the planned reps as a hint. Display-unit conversion stays with the existing unit converter. Spec: `docs/specs/b1-workout-log-core-input.md` ("building rows and checking input").

**Blocked by:** None (can start immediately).

**Status:** done

- [x] Join works for every kind, accepts commas, `m:ss`, suffixes, `bw`; failure messages are exactly the parser's
- [x] Pre-fill follows the order above, including no history, a Last Workout with fewer Sets than planned, and a null Plan
- [x] Pre-filled values are not stored anywhere; the rule is pure
- [x] Core unit tests in the style of `InputParserTests` and `SuggestedTargetTests`
