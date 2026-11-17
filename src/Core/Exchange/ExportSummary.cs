using System.Globalization;
using Core.Model;
using Core.Storage;

namespace Core.Exchange;

/// <summary>The plain-language lines and the suggested file name for an Export. Wording lives here so it can be tested and changed in one place.</summary>
internal static class ExportSummary
{
    public static IReadOnlyList<string> Lines(ExchangeDocument document, ExportRequest request)
    {
        var lines = new List<string>();
        if (request.IsBackup)
        {
            lines.Add("Backup: every kind of data, all time");
        }

        lines.Add(string.Join(", ", Counts(document)));
        lines.Add(request.Range is { } range ? RangeText(range) : "All time");
        if (!request.Exercises)
        {
            lines.Add("Exercises: only the Exercises used");
        }

        return lines;
    }

    public static string FileName(ExportRequest request, DateTimeOffset now)
    {
        var extension = request.Format == ExportFormat.Json ? "json" : "md";
        return request switch
        {
            { IsBackup: true } => BackupFileName(now),
            { Range: { } range } => $"fitness-export-{Iso(range.From)}-to-{Iso(range.To)}.{extension}",
            _ => $"fitness-export-all-time-{Today(now)}.{extension}",
        };
    }

    /// <summary>The one place Backup files are named: <c>fitness-backup-2026-11-17.json</c>, or <c>fitness-backup-before-import-…</c> for the automatic one.</summary>
    public static string BackupFileName(DateTimeOffset now, bool beforeImport = false) =>
        $"fitness-backup-{(beforeImport ? "before-import-" : "")}{Today(now)}.json";

    private static string Today(DateTimeOffset now) => Iso(DateOnly.FromDateTime(now.DateTime));

    private static IEnumerable<string> Counts(ExchangeDocument document)
    {
        var painChecks = document.Workouts?.Count(w => w.PainCheck is not null);
        var counts = new (int? Count, (string Singular, string Plural) Name)[]
        {
            (document.Workouts?.Count, Wording.Names[RecordKind.Workout]),
            (document.Plans?.Count, Wording.Names[RecordKind.Plan]),
            (document.WeighIns?.Count, Wording.Names[RecordKind.WeighIn]),
            (painChecks, Wording.PainCheck),
            (document.Guides?.Count, Wording.Names[RecordKind.Guide]),
            (document.Exercises?.Count, Wording.Names[RecordKind.Exercise]),
        };
        return counts.Where(c => c.Count.HasValue).Select(c => Wording.Counted(c.Count!.Value, c.Name));
    }

    /// <summary>"26 Aug to 17 Nov 2026"; the first year is spelled out only when it differs.</summary>
    private static string RangeText(DateRange range)
    {
        var from = range.From.ToString(range.From.Year == range.To.Year ? "d MMM" : "d MMM yyyy", CultureInfo.InvariantCulture);
        return $"{from} to {range.To.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}";
    }

    private static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
