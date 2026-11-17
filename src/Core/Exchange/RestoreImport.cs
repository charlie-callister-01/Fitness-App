using System.Globalization;
using Core.Model;
using Core.Storage;

namespace Core.Exchange;

/// <summary>
/// Restore a Backup: the file replaces everything, so there are no changes, only the document to replace with and the Backup date to stamp
/// (the file's <c>exportedAt</c>, else the time the caller gave). Takes the file as it is: no Exercise matching. A file that lacks any
/// collection or Settings (Week schedules are optional) is not a Backup and is refused with <c>not_a_backup</c>. The preview warns about
/// what the restore loses: current records the file lacks, those Workouts that are newer than the file, and records startup set aside.
/// </summary>
internal sealed class RestoreImport : IImportKind
{
    private const string NotABackup = "not_a_backup";

    /// <summary>The order lost records are named in: the owner's history first.</summary>
    private static readonly RecordKind[] LostOrder =
    [
        RecordKind.Workout, RecordKind.WeighIn, RecordKind.Plan, RecordKind.Exercise,
        RecordKind.Guide, RecordKind.Break, RecordKind.Badge, RecordKind.WeekSchedule,
    ];

    public string Name => "A restore";

    public IReadOnlySet<RecordKind> Reads { get; } = Enum.GetValues<RecordKind>().ToHashSet();

    public bool UsesExerciseMatching => false;

    public KindResult Build(ImportContext context)
    {
        var document = context.Inspected.Document;
        var missing = DocumentCollections.MissingFromBackup(document);
        if (missing.Count > 0)
        {
            var text = $"This file is not a complete Backup, so it cannot replace your data. It is missing: {string.Join(", ", missing)}.";
            return new KindResult([], [new ImportItem(ImportItemAction.Blocker, null, NotABackup, text)], [text]);
        }

        var summary = new List<string> { "Replaces all your data with this Backup." };
        var items = new List<ImportItem>();
        foreach (var (kind, text) in LossLines(context, document))
        {
            summary.Add(text);
            items.Add(new ImportItem(ImportItemAction.Warning, kind, "lost", text));
        }

        return new KindResult([], items, summary, document, document.ExportedAt ?? context.Now);
    }

    private static IEnumerable<(RecordKind? Kind, string Text)> LossLines(ImportContext context, ExchangeDocument document)
    {
        var inFile = StoredRecords.FromDocument(document).Select(r => (r.Kind, r.Key)).ToHashSet();
        var lost = StoredRecords.FromDocument(context.Snapshot.ToDocument()).Where(r => !inFile.Contains((r.Kind, r.Key))).ToList();
        var counts = LostOrder
            .Select(kind => (Kind: kind, Count: lost.Count(r => r.Kind == kind)))
            .Where(c => c.Count > 0)
            .Select(c => Wording.Counted(c.Count, Wording.Names[c.Kind]))
            .ToList();
        if (counts.Count > 0)
        {
            yield return (null, $"Loses {JoinWithAnd(counts)} that {(lost.Count == 1 ? "is" : "are")} not in the file.");
        }

        if (document.ExportedAt is { } exportedAt)
        {
            var lostWorkouts = lost.Where(r => r.Kind == RecordKind.Workout).Select(r => r.Key).ToHashSet();
            var made = DateOnly.FromDateTime(exportedAt.Date);
            var newer = context.Snapshot.Workouts.Count(w => lostWorkouts.Contains(w.Id) && w.Date > made);
            if (newer > 0)
            {
                var when = exportedAt.Date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
                yield return (RecordKind.Workout, $"{newer} of your Workouts {(newer == 1 ? "is" : "are")} dated after this Backup was made ({when}).");
            }
        }

        if (context.SetAsideCount > 0)
        {
            var n = context.SetAsideCount;
            yield return (null, $"Removes {n} {(n == 1 ? "record" : "records")} that could not be read and {(n == 1 ? "was" : "were")} set aside.");
        }
    }

    private static string JoinWithAnd(List<string> parts) =>
        parts.Count == 1 ? parts[0] : $"{string.Join(", ", parts.SkipLast(1))} and {parts[^1]}";
}
