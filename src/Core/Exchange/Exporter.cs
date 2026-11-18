using Core.Serialisation;
using Core.Storage;

namespace Core.Exchange;

/// <summary>Turns a snapshot and a request into an Export. Pure: no clock, no storage, no file I/O.</summary>
public static class Exporter
{
    /// <param name="snapshot">The data to export from.</param>
    /// <param name="request">What to include.</param>
    /// <param name="exportedFrom">A build string the host passes in; written to the JSON as <c>exportedFrom</c>.</param>
    /// <param name="now">The injected time; written as <c>exportedAt</c> and used for the file name of a Backup.</param>
    public static ExportResult Export(DataSnapshot snapshot, ExportRequest request, string exportedFrom, DateTimeOffset now)
    {
        var document = ExportSlicer.Slice(snapshot, request) with { ExportedAt = now, ExportedFrom = exportedFrom };
        var summary = ExportSummary.Lines(document, request);
        var text = request.Format switch
        {
            ExportFormat.Json => ExchangeFormat.Write(document, indented: true),
            _ => MarkdownRenderer.Render(document, request, summary, SessionNames(snapshot)),
        };

        return new ExportResult(text, ExportSummary.FileName(request, now), summary, request.IsBackup);
    }

    private static Dictionary<string, string> SessionNames(DataSnapshot snapshot) =>
        snapshot.Plans.SelectMany(p => p.Sessions).GroupBy(s => s.Id).ToDictionary(g => g.Key, g => g.First().Name);
}
