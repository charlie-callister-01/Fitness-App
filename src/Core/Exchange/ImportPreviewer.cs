using Core.Model;
using Core.Serialisation;
using Core.Storage;

namespace Core.Exchange;

/// <summary>
/// Step three of an Import: match Exercises, let the chosen kind work out its changes, and assemble the preview.
/// Pure: the same snapshot, file, kind and decisions always give the same preview.
/// </summary>
public static class ImportPreviewer
{
    private static readonly Dictionary<ImportKind, IImportKind> Kinds = new()
    {
        [ImportKind.Plan] = new PlanImport(),
        [ImportKind.Guides] = new GuidesImport(),
        [ImportKind.Merge] = new MergeImport(),
        [ImportKind.Restore] = new NotYetSupportedImport(ImportKind.Restore),
    };

    public static ImportPreview Preview(DataSnapshot snapshot, Inspected inspected, ImportKind kind, ImportDecisions decisions)
    {
        var rules = Kinds[kind];
        var matching = ExerciseMatcher.Match(snapshot, inspected, decisions);
        var built = rules.Build(new ImportContext(snapshot, inspected, matching, decisions));
        var items = built.Items
            .Concat(matching.Findings.Where(f => rules.Reads.Contains(PathHead(f.Path))).Select(ToItem))
            .Concat(IgnoredItems(matching.Document, rules))
            .ToList();
        if (!items.Any(i => i.Action == ImportItemAction.Blocker) && !matching.OpenDecisions.Any())
        {
            items.AddRange(ValidationBlockers(snapshot, built.Changes));
        }

        return new ImportPreview(kind, built.Summary, items, matching.Matches, built.Changes, snapshot);
    }

    /// <summary>
    /// Applies the changes to a copy of the snapshot and runs the same validator the commit will run, so the preview cannot promise
    /// what the commit would reject. Skipped while something else already blocks, because the result would only repeat it.
    /// </summary>
    private static IEnumerable<ImportItem> ValidationBlockers(DataSnapshot snapshot, IReadOnlyList<Change> changes)
    {
        var result = changes.Aggregate(snapshot, (s, change) => change.ApplyTo(s));
        return ExchangeFormat.Validate(result.ToDocument())
            .Select(issue => new ImportItem(ImportItemAction.Blocker, KindAt(issue.Path), issue.Path, $"{issue.Path}: {issue.Message}"));
    }

    private static ImportItem ToItem(MatchFinding finding)
    {
        var action = finding.Kind switch
        {
            MatchFindingKind.Blocker => ImportItemAction.Blocker,
            MatchFindingKind.Warning => ImportItemAction.Warning,
            _ => ImportItemAction.Note,
        };
        return new ImportItem(action, KindAt(finding.Path), finding.Path, finding.Message);
    }

    /// <summary>The record kind a document path starts in (<c>workouts[1].entries</c> is Workout), or null for the file as a whole.</summary>
    private static RecordKind? KindAt(string path) =>
        Enum.GetValues<RecordKind>().Cast<RecordKind?>().FirstOrDefault(k => k!.Value.WireName() == PathHead(path));

    private static string PathHead(string path) => path.Split('[', '.')[0];

    private static IEnumerable<ImportItem> IgnoredItems(ExchangeDocument document, IImportKind rules)
    {
        foreach (var (kind, count) in PresentCollections(document))
        {
            if (!rules.Reads.Contains(kind.WireName()))
            {
                yield return new ImportItem(ImportItemAction.Ignore, kind, kind.WireName(), IgnoredText(kind, count, rules));
            }
        }
    }

    private static string IgnoredText(RecordKind kind, int? count, IImportKind rules) => count is { } n
        ? $"Ignored: {Wording.Counted(n, Wording.Names[kind])} in the file. {rules.Name} does not use them."
        : $"Ignored: the file's {Wording.Names[kind].Plural}. {rules.Name} does not use them.";

    /// <summary>The kinds of record the document carries, with how many of each (null for Settings, which is one record).</summary>
    private static IEnumerable<(RecordKind Kind, int? Count)> PresentCollections(ExchangeDocument d)
    {
        if (d.Exercises is { } exercises) { yield return (RecordKind.Exercise, exercises.Count); }
        if (d.Settings is not null) { yield return (RecordKind.Settings, null); }
        if (d.Plans is { } plans) { yield return (RecordKind.Plan, plans.Count); }
        if (d.WeighIns is { } weighIns) { yield return (RecordKind.WeighIn, weighIns.Count); }
        if (d.Breaks is { } breaks) { yield return (RecordKind.Break, breaks.Count); }
        if (d.Badges is { } badges) { yield return (RecordKind.Badge, badges.Count); }
        if (d.Guides is { } guides) { yield return (RecordKind.Guide, guides.Count); }
        if (d.Workouts is { } workouts) { yield return (RecordKind.Workout, workouts.Count); }
        if (d.WeekSchedules is { } weeks) { yield return (RecordKind.WeekSchedule, weeks.Count); }
    }
}
