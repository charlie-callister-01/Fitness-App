using Core.Model;
using Core.Rules.Phases;
using Core.Rules.Weeks;

namespace Core.Rules.PlanEditing;

/// <summary>The rules for editing a Plan's Phases. The week-range part is the shared <see cref="PhaseRanges"/> rule.</summary>
public static class PlanPhaseRules
{
    /// <summary>The first and last day of a Phase, from the Plan's start week (weeks are numbered from 1 and run Monday to Sunday).</summary>
    public static (DateOnly Start, DateOnly End) PhaseDates(Plan plan, Phase phase)
    {
        var firstMonday = WeekCalendar.MondayOf(plan.StartDate);
        return (WeekCalendar.AddWeeks(firstMonday, phase.StartWeek - 1), WeekCalendar.AddWeeks(firstMonday, phase.EndWeek - 1).AddDays(6));
    }

    /// <summary>
    /// Why the Plan's Phases cannot be saved, or null when they can: a Phase must end no earlier than it starts and start at week 1 or
    /// later; each Phase goal needs an Exercise, a weight of 0 or more and at least 1 rep; two Phases may not share a week (the message names both); and Phases must start at week 1 and leave no gap.
    /// </summary>
    public static string? ValidatePhases(IReadOnlyList<Phase> phases)
    {
        var findings = PhaseRanges.Check(phases);
        for (var i = 0; i < phases.Count; i++)
        {
            var own = findings.FirstOrDefault(f => f.Index == i && f.Problem is PhaseRangeProblem.BadStart or PhaseRangeProblem.EndBeforeStart);
            if (own is not null)
            {
                return Sentence(own);
            }

            if (ValidateGoals(phases[i]) is { } goalReason)
            {
                return goalReason;
            }
        }

        return findings.Count > 0 ? Sentence(findings[0]) : null;
    }

    /// <summary>The Plan with <paramref name="phases"/> as its Phases, or the plain reason they are refused (see <see cref="ValidatePhases"/>).</summary>
    public static PlanEditResult<Plan> WithPhases(Plan plan, IReadOnlyList<Phase> phases) =>
        ValidatePhases(phases) is { } reason
            ? new PlanEditResult<Plan>.Invalid(reason)
            : new PlanEditResult<Plan>.Valid(plan with { Phases = [.. phases.OrderBy(p => p.StartWeek)] });

    private static string Sentence(PhaseRangeFinding finding)
    {
        var phase = finding.Phase;
        var earlier = finding.Earlier;
        return finding.Problem switch
        {
            PhaseRangeProblem.BadStart => $"{Name(phase)} must start at week 1 or later.",
            PhaseRangeProblem.EndBeforeStart => $"{Name(phase)} ends (week {phase.EndWeek}) before it starts (week {phase.StartWeek}).",
            PhaseRangeProblem.Overlap when earlier is not null =>
                $"{Name(phase)} (weeks {phase.StartWeek} to {phase.EndWeek}) overlaps {Name(earlier)} (weeks {earlier.StartWeek} to {earlier.EndWeek}).",
            PhaseRangeProblem.Gap when earlier is not null =>
                $"Weeks {finding.FirstWeek} to {finding.LastWeek} are between {Name(earlier)} and {Name(phase)} and in no Phase.",
            PhaseRangeProblem.LeadingGap =>
                $"Weeks {finding.FirstWeek} to {finding.LastWeek} are before {Name(phase)} and in no Phase. Phases must start at week 1.",
            _ => $"{Name(phase)} has weeks that do not fit with the other Phases.",
        };
    }

    private static string? ValidateGoals(Phase phase)
    {
        foreach (var goal in phase.Goals)
        {
            if (string.IsNullOrWhiteSpace(goal.ExerciseId))
            {
                return $"A Phase goal in {Name(phase)} needs an Exercise.";
            }

            if (goal.WeightKg < 0 || goal.Reps < 1)
            {
                return $"A Phase goal in {Name(phase)} needs a weight of 0 or more and at least 1 rep.";
            }
        }

        return null;
    }

    private static string Name(Phase phase) => string.IsNullOrWhiteSpace(phase.Focus) ? "A Phase" : $"\"{phase.Focus}\"";
}
