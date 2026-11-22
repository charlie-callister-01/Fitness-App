using Core.Model;
using Core.Rules.Performance;
using Core.Rules.Phases;
using Core.Rules.Tracks;

namespace Core.Rules.Guides;

/// <summary>One of the last results shown on a Guide. <see cref="IsPr"/> is true when that Workout set a PR for the Exercise.</summary>
public sealed record GuideResult(LastResult Result, bool IsPr);

/// <summary>Where an Exercise sits in one Session of the Plan: the Session, its row (sets, reps, cue) under the current Phase, and the Session label (naming the Track when the Plan has more than one Track).</summary>
public sealed record GuidePlacement(Session Session, SessionExercise Row, string SessionLabel);

/// <summary>Where an Exercise sits in the active Plan: every Session it is in, and the current Phase (null when none).</summary>
public sealed record GuidePlace(IReadOnlyList<GuidePlacement> Placements, Phase? Phase);

/// <summary>What a Guide shows around its text: the last results with PRs marked, the Best, and where the Exercise sits in the Plan.</summary>
public sealed record GuideHistory(IReadOnlyList<GuideResult> Recent, BestResult? Best, GuidePlace? Place);

/// <summary>Builds the history part of a Guide, reusing the performance and Phase rules.</summary>
public static class GuideHistoryView
{
    /// <summary>How many past results a Guide shows.</summary>
    public const int ResultCount = 3;

    /// <summary>
    /// The last <see cref="ResultCount"/> results of <paramref name="exercise"/> (fewer when there are fewer), newest first, with the
    /// ones that set a PR marked; its Best; and, when there is a <paramref name="plan"/>, the Sessions it is in under the Phase in force
    /// on <paramref name="today"/>. The place is null when there is no Plan or no Session holds the Exercise.
    /// </summary>
    public static GuideHistory Build(Exercise exercise, IReadOnlyList<Workout> history, Plan? plan, DateOnly today)
    {
        var prIds = ExercisePerformance.Prs(exercise, history).Select(p => p.WorkoutId).ToHashSet();
        var recent = ExercisePerformance.Recent(exercise, history, ResultCount)
            .Select(r => new GuideResult(r, prIds.Contains(r.WorkoutId)))
            .ToList();
        return new GuideHistory(recent, ExercisePerformance.Best(exercise, history), PlaceOf(exercise, plan, today));
    }

    private static GuidePlace? PlaceOf(Exercise exercise, Plan? plan, DateOnly today)
    {
        if (plan is null)
        {
            return null;
        }

        var phase = PhaseRules.CurrentPhase(plan, TrackRules.Main(plan), today);
        var multiTrack = plan.Tracks.Count > 1;
        var placements = plan.Sessions
            .Select(s => (Session: s, Row: PhaseRules.ExerciseRows(plan, s.Id, today).FirstOrDefault(r => r.ExerciseId == exercise.Id)))
            .Where(x => x.Row is not null)
            .Select(x => new GuidePlacement(x.Session, x.Row!, FormatSessionLabel(plan, x.Session, multiTrack)))
            .ToList();
        return placements.Count == 0 ? null : new GuidePlace(placements, phase);
    }

    private static string FormatSessionLabel(Plan plan, Session session, bool multiTrack)
    {
        if (!multiTrack || TrackRules.TrackOf(plan, session) is not { } track)
        {
            return session.Name;
        }

        return track.Rhythm is TrackRhythm.Daily
            ? $"{session.Name} ({track.Name}, daily)"
            : $"{session.Name} ({track.Name})";
    }
}
