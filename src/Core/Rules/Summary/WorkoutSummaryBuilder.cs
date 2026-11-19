using Core.Model;
using Core.Rules.Badges;
using Core.Rules.Outcomes;
using Core.Rules.Performance;
using Core.Rules.Weeks;

namespace Core.Rules.Summary;

/// <summary>Turns a finished Workout and the history into the finish summary. Pure: no I/O, no clock.</summary>
public static class WorkoutSummaryBuilder
{
    private static readonly string[] SidePhrases = ["Left side", "Right side", "Both sides"];

    /// <param name="workout">The finished Workout.</param>
    /// <param name="history">Everything Badge progress and the Streak are worked out from. Its Workouts may or may not include <paramref name="workout"/>.</param>
    /// <param name="badges">The Badges to check for a new unlock.</param>
    /// <param name="today">The day the summary is read; Streak and Badge rules take it as given.</param>
    public static WorkoutSummary Build(Workout workout, BadgeHistory history, IReadOnlyList<Badge> badges, DateOnly today)
    {
        var others = history.Workouts.Where(w => w.Id != workout.Id).ToList();
        var withThis = history with { Workouts = [.. others, workout] };
        var without = history with { Workouts = others };

        var outcome = WorkoutOutcomes.Of(workout);
        var rows = Rows(workout, outcome);
        var doneCount = outcome.Rows.Count(r => r.Kind is RowOutcomeKind.Done or RowOutcomeKind.Swapped);
        var sets = workout.Entries.Where(CountsAsDone).Sum(e => e.Sets.Count);

        return new WorkoutSummary(
            sets,
            doneCount,
            workout.Planned.Count,
            outcome.IsPartial,
            Prs(workout, others.Where(w => w.Date <= workout.Date).ToList(), history.Exercises),
            rows,
            PainCheckText(workout.PainCheck, history.Exercises),
            StreakLine(withThis, today),
            UnlockedBadges(badges, withThis, without, today),
            Duration(workout));
    }

    private static bool CountsAsDone(WorkoutEntry e) => e.Status is EntryStatus.Done or EntryStatus.Swapped;

    // One PR per Exercise: the Workout's PR from the per-Workout PR rule, with the best that stood before this Workout. "others" is the
    // history on or before the Workout's own date, so a Workout logged afterwards is judged as of the day it happened.
    private static IReadOnlyList<SummaryPr> Prs(Workout workout, IReadOnlyList<Workout> others, IReadOnlyList<Exercise> exercises)
    {
        var prs = new List<SummaryPr>();
        var ids = workout.Entries.Where(CountsAsDone).Select(e => e.ExerciseId).Distinct();
        foreach (var id in ids)
        {
            if (exercises.FirstOrDefault(e => e.Id == id) is not { } exercise) continue;
            var record = ExercisePerformance.Prs(exercise, [.. others, workout]).FirstOrDefault(p => p.WorkoutId == workout.Id);
            if (record is null) continue;
            prs.Add(new SummaryPr(id, record.Set, ExercisePerformance.Best(exercise, others)));
        }
        return prs;
    }

    // Planned order, then extras. Entries are matched to planned rows the way WorkoutOutcomes matches them, to carry the RPE.
    private static IReadOnlyList<SummaryRow> Rows(Workout workout, WorkoutOutcome outcome)
    {
        var remaining = workout.Entries.ToList();
        var rows = new List<SummaryRow>();
        foreach (var row in outcome.Rows)
        {
            var index = remaining.FindIndex(e => Matches(e, row.Row));
            var entry = index < 0 ? null : remaining[index];
            if (index >= 0) remaining.RemoveAt(index);

            var shown = row.Kind == RowOutcomeKind.Swapped ? row.ReplacementExerciseId! : row.Row.ExerciseId;
            var original = row.Kind == RowOutcomeKind.Swapped ? row.Row.ExerciseId : null;
            rows.Add(new SummaryRow(shown, row.Kind, row.SkipReason, original, entry?.Rpe, false));
        }

        rows.AddRange(outcome.Extras.Select(e => new SummaryRow(e.ExerciseId, RowOutcomeKind.Done, null, null, e.Rpe, true)));
        return rows;
    }

    private static bool Matches(WorkoutEntry entry, SessionExercise row) =>
        entry.Status == EntryStatus.Swapped
            ? entry.SwappedFromExerciseId == row.ExerciseId
            : entry.ExerciseId == row.ExerciseId;

    private static string PainCheckText(PainCheck? pain, IReadOnlyList<Exercise> exercises)
    {
        if (pain is null) return "Not answered";
        if (pain.Status == PainStatus.None) return "No";

        var side = SidePhrases.FirstOrDefault(p => pain.Note?.StartsWith(p, StringComparison.OrdinalIgnoreCase) == true)?.Split(' ')[0];
        var areas = string.Join(", ", pain.Areas.Select(AreaName));
        var where = side is null ? areas : $"{side} {areas}";
        var parts = new List<string> { Capitalise(where) };
        if (pain.Severity is { } severity) parts.Add(severity.ToString().ToLowerInvariant());

        var text = string.Join(", ", parts.Where(p => p.Length > 0));
        if (exercises.FirstOrDefault(e => e.Id == pain.ExerciseId) is { } during) text += $" (during {during.Name})";
        return text.Length == 0 ? "Yes" : $"Yes: {text}";
    }

    private static string AreaName(BodyArea area) => area switch
    {
        BodyArea.UpperBack => "upper back",
        BodyArea.LowerBack => "lower back",
        _ => area.ToString().ToLowerInvariant(),
    };

    private static string Capitalise(string s) => s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..];

    private static string? StreakLine(BadgeHistory history, DateOnly today)
    {
        var current = WeekRules.Streak(history.Plan, history.Workouts, history.Breaks, today).Current;
        return current > 0 ? $"Week {current} of your Streak" : null;
    }

    private static IReadOnlyList<Badge> UnlockedBadges(IReadOnlyList<Badge> badges, BadgeHistory with, BadgeHistory without, DateOnly today) =>
        badges
            .Where(b => BadgeRules.Progress(b, with, today).Reached && !BadgeRules.Progress(b, without, today).Reached)
            .ToList();

    private static TimeSpan? Duration(Workout workout) =>
        workout is { LoggedAfterwards: false, StartedAt: { } start, EndedAt: { } end } && end >= start ? end - start : null;
}
