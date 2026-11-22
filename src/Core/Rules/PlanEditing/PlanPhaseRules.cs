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
    /// Why a Track's Phases cannot be saved, or null when they can: a Phase must end no earlier than it starts and start at week 1 or
    /// later; each Phase goal needs an Exercise, a weight of 0 or more and at least 1 rep; Phase ids must be unique; two Phases may not
    /// share a week (the message names both); and Phases must start at week 1 and leave no gap.
    /// </summary>
    public static string? ValidatePhases(IReadOnlyList<Phase> phases)
    {
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var phase in phases)
        {
            if (!seenIds.Add(phase.Id))
            {
                return $"Two Phases share id \"{phase.Id}\".";
            }
        }

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

    /// <summary>
    /// Why <paramref name="phases"/> cannot be saved on <paramref name="trackId"/>, or null when they can: range and goal checks,
    /// document-wide Phase id uniqueness across Tracks (and <paramref name="otherPlans"/> when given), and Phase overrides naming only
    /// Sessions belonging to that Track.
    /// </summary>
    public static string? ValidatePhases(
        Plan plan, string trackId, IReadOnlyList<Phase> phases, IReadOnlyList<Plan>? otherPlans = null)
    {
        if (plan.Tracks.All(t => t.Id != trackId))
        {
            return "That Track is no longer in the Plan.";
        }

        if (ValidatePhases(phases) is { } listReason)
        {
            return listReason;
        }

        var usedIds = plan.Tracks
            .Where(t => t.Id != trackId)
            .SelectMany(t => t.Phases)
            .Concat((otherPlans ?? []).Where(p => p.Id != plan.Id).SelectMany(p => p.Tracks).SelectMany(t => t.Phases))
            .Select(p => p.Id)
            .ToHashSet(StringComparer.Ordinal);

        var trackSessionIds = plan.Sessions
            .Where(s => s.TrackId == trackId)
            .Select(s => s.Id)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var phase in phases)
        {
            if (usedIds.Contains(phase.Id))
            {
                return $"Another Phase already uses id \"{phase.Id}\".";
            }

            var overridden = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in phase.SessionOverrides)
            {
                if (!trackSessionIds.Contains(item.SessionId))
                {
                    return $"A Phase override in {Name(phase)} names a Session that is not on this Track.";
                }

                if (!overridden.Add(item.SessionId))
                {
                    return $"A Phase override in {Name(phase)} names the same Session more than once.";
                }
            }
        }

        return null;
    }

    /// <summary>
    /// The Plan with <paramref name="phases"/> as <paramref name="trackId"/>'s Phases, or the plain reason they are refused (see
    /// <see cref="ValidatePhases(Plan, string, IReadOnlyList{Phase}, IReadOnlyList{Plan}?)"/>).
    /// </summary>
    public static PlanEditResult<Plan> WithPhases(
        Plan plan, string trackId, IReadOnlyList<Phase> phases, IReadOnlyList<Plan>? otherPlans = null)
    {
        if (ValidatePhases(plan, trackId, phases, otherPlans) is { } reason)
        {
            return new PlanEditResult<Plan>.Invalid(reason);
        }

        var ordered = phases.OrderBy(p => p.StartWeek).ToList();
        return new PlanEditResult<Plan>.Valid(plan with
        {
            Tracks = [.. plan.Tracks.Select(t => t.Id == trackId ? t with { Phases = ordered } : t)],
        });
    }

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
