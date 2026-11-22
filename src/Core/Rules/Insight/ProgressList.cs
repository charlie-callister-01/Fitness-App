using Core.Exchange;
using Core.Model;
using Core.Rules.LogKinds;
using Core.Rules.Performance;
using Core.Rules.Units;

namespace Core.Rules.Insight;

/// <summary>An Exercise logged on one day only: listed as "1 entry · needs 2 for a chart".</summary>
public sealed record NeedsTwoRow(string ExerciseId, string Name);

public sealed record ProgressGroupView(ExerciseGroup Group, IReadOnlyList<ExerciseSeries> Charts, IReadOnlyList<NeedsTwoRow> NeedsTwo);

/// <summary>The Progress tab: groups in chip order, each with its charts and its one-entry rows. Groups with nothing to show are absent.</summary>
public sealed record ProgressListView(IReadOnlyList<ProgressGroupView> Groups)
{
    public bool IsEmpty => Groups.Count == 0;
}

public static class ProgressList
{
    public static ProgressListView Build(IEnumerable<Exercise> exercises, Plan? plan, IReadOnlyList<Workout> history, UnitConverter units, DateRange? range = null)
    {
        var groups = exercises
            .Where(e => ExerciseGroups.Of(e.LogKind) is not null)
            .OrderBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(e => (Exercise: e, Group: ExerciseGroups.Of(e.LogKind)!.Value, Series: ProgressSeries.Build(e, plan, history, units, range)))
            .Select(x => (x.Exercise, x.Group, x.Series, Single: x.Series is null && DaysLogged(x.Exercise, history, range) == 1))
            .Where(x => x.Series is not null || x.Single)
            .GroupBy(x => x.Group)
            .OrderBy(g => g.Key)
            .Select(g => new ProgressGroupView(
                g.Key,
                [.. g.Where(x => x.Series is not null).Select(x => x.Series!)],
                [.. g.Where(x => x.Single).Select(x => new NeedsTwoRow(x.Exercise.Id, x.Exercise.Name))]))
            .ToList();
        return new ProgressListView(groups);
    }

    /// <summary>The days the Exercise was done with something that can be charted.</summary>
    private static int DaysLogged(Exercise exercise, IReadOnlyList<Workout> history, DateRange? range)
    {
        var rules = LogKindRules.For(exercise.LogKind);
        return ExercisePerformance.Recent(exercise, history, int.MaxValue)
            .Where(r => r.Sets.Any(s => rules.ChartValue(s) is not null))
            .Select(r => r.Date)
            .Where(d => range is null || range.Contains(d))
            .Distinct()
            .Count();
    }
}
