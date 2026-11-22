using Core.Model;

namespace Core.Rules.Phases;

/// <summary>What is wrong with a Phase's weeks, or with how the Phases' weeks fit together.</summary>
public enum PhaseRangeProblem
{
    /// <summary>The Phase starts before week 1.</summary>
    BadStart,

    /// <summary>The Phase ends before the week it starts.</summary>
    EndBeforeStart,

    /// <summary>The Phase starts in a week an earlier Phase already covers.</summary>
    Overlap,

    /// <summary>Weeks between two Phases are in no Phase.</summary>
    Gap,

    /// <summary>Weeks before the first Phase (from week 1) are in no Phase.</summary>
    LeadingGap,
}

/// <summary>
/// One finding of <see cref="PhaseRanges.Check"/>. <see cref="Index"/> is the position of <see cref="Phase"/> in the list that was checked.
/// For Overlap, <see cref="Earlier"/> is the Phase whose weeks it runs into; for Gap, the Phase before the gap. For Gap and LeadingGap,
/// <see cref="FirstWeek"/> to <see cref="LastWeek"/> are the weeks in no Phase and <see cref="Phase"/> is the Phase after them.
/// </summary>
public sealed record PhaseRangeFinding(PhaseRangeProblem Problem, int Index, Phase Phase, Phase? Earlier, int FirstWeek, int LastWeek);

/// <summary>
/// The one home of the Phase week-range rule: each Phase starts at week 1 or later and ends no earlier than it starts, and the Phases,
/// whatever order they are listed in, start at week 1 and follow each other with no gap and no overlap.
/// </summary>
public static class PhaseRanges
{
    /// <summary>
    /// The findings for <paramref name="phases"/>: the range problems of single Phases (in list order) first, then the coverage problems
    /// (in week order). A Phase with a range problem, or one <paramref name="isUnreadable"/> says could not be read, is left out of the
    /// coverage check. A gap before a Phase is excused when such an excluded Phase starts inside it (or starts before week 1), because that
    /// Phase's real weeks are not known; an overlap between two judged Phases is always real.
    /// </summary>
    public static IReadOnlyList<PhaseRangeFinding> Check(IReadOnlyList<Phase> phases, Func<Phase, bool>? isUnreadable = null)
    {
        var findings = new List<PhaseRangeFinding>();
        var judged = new List<(Phase Phase, int Index)>();
        var excluded = new List<Phase>();
        for (var i = 0; i < phases.Count; i++)
        {
            var phase = phases[i];
            if (isUnreadable?.Invoke(phase) == true)
            {
                excluded.Add(phase);
            }
            else if (phase.StartWeek < 1)
            {
                findings.Add(new PhaseRangeFinding(PhaseRangeProblem.BadStart, i, phase, null, phase.StartWeek, phase.EndWeek));
                excluded.Add(phase);
            }
            else if (phase.EndWeek < phase.StartWeek)
            {
                findings.Add(new PhaseRangeFinding(PhaseRangeProblem.EndBeforeStart, i, phase, null, phase.StartWeek, phase.EndWeek));
                excluded.Add(phase);
            }
            else
            {
                judged.Add((phase, i));
            }
        }

        findings.AddRange(Coverage(judged, excluded));
        return findings;
    }

    private static IEnumerable<PhaseRangeFinding> Coverage(List<(Phase Phase, int Index)> judged, List<Phase> excluded)
    {
        var expectedStart = 1;
        Phase? reaching = null;
        foreach (var (phase, index) in judged.OrderBy(p => p.Phase.StartWeek).ThenBy(p => p.Phase.EndWeek))
        {
            if (phase.StartWeek > expectedStart && !GapIsExcused(excluded, expectedStart, phase.StartWeek - 1))
            {
                yield return new PhaseRangeFinding(
                    reaching is null ? PhaseRangeProblem.LeadingGap : PhaseRangeProblem.Gap, index, phase, reaching, expectedStart, phase.StartWeek - 1);
            }
            else if (phase.StartWeek < expectedStart)
            {
                yield return new PhaseRangeFinding(PhaseRangeProblem.Overlap, index, phase, reaching, phase.StartWeek, phase.EndWeek);
            }

            if (phase.EndWeek + 1 > expectedStart)
            {
                expectedStart = phase.EndWeek + 1;
                reaching = phase;
            }
        }
    }

    private static bool GapIsExcused(List<Phase> excluded, int firstMissing, int lastMissing) =>
        excluded.Any(p => p.StartWeek < 1 || (p.StartWeek >= firstMissing && p.StartWeek <= lastMissing));
}
