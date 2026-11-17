using System.Globalization;
using Core.Model;

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
        var today = Iso(DateOnly.FromDateTime(now.DateTime));
        return request switch
        {
            { IsBackup: true } => $"fitness-backup-{today}.{extension}",
            { Range: { } range } => $"fitness-export-{Iso(range.From)}-to-{Iso(range.To)}.{extension}",
            _ => $"fitness-export-all-time-{today}.{extension}",
        };
    }

    private static IEnumerable<string> Counts(ExchangeDocument document)
    {
        var painChecks = document.Workouts?.Count(w => w.PainCheck is not null);
        var counts = new (int? Count, string Singular, string Plural)[]
        {
            (document.Workouts?.Count, "Workout", "Workouts"),
            (document.Plans?.Count, "Plan", "Plans"),
            (document.WeighIns?.Count, "Weigh-in", "Weigh-ins"),
            (painChecks, "Pain check", "Pain checks"),
            (document.Guides?.Count, "Guide", "Guides"),
            (document.Exercises?.Count, "Exercise", "Exercises"),
        };
        return counts.Where(c => c.Count.HasValue).Select(c => $"{c.Count} {(c.Count == 1 ? c.Singular : c.Plural)}");
    }

    /// <summary>"26 Aug to 17 Nov 2026"; the first year is spelled out only when it differs.</summary>
    private static string RangeText(DateRange range)
    {
        var from = range.From.ToString(range.From.Year == range.To.Year ? "d MMM" : "d MMM yyyy", CultureInfo.InvariantCulture);
        return $"{from} to {range.To.ToString("d MMM yyyy", CultureInfo.InvariantCulture)}";
    }

    private static string Iso(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
